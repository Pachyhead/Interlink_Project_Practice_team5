using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Data;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;


public class ImageSwitcher_MultiEnd : MonoBehaviour
{
    void Start()
    {
        float rate = CalcData();
        UpdateZone(rate);
    }

    [Header("Zone Target")]
    public Image zoneImage;
    public TextMeshProUGUI endingText;

    [Header("Shared Sprites (All Zones Use These)")]
    public Sprite goodSprite;    // 80% 이상
    public Sprite normalSprite;  // 50% 이상
    public Sprite badSprite;     // 50% 미만

    [Header("Ending Messages")]
    [TextArea] public string goodMessage = "Good End\n정답률 : ";
    [TextArea] public string normalMessage = "Normal End\n정답률 : ";
    [TextArea] public string badMessage = "Bad End\n정답률 : ";

    private void SetImageBasedOnRate(Image target, float rate)
    {
        string scoreText = rate.ToString("F0") + "%";
        if (rate >= 80f)
        {
            target.sprite = goodSprite;
            if (endingText != null) endingText.text = goodMessage + scoreText;
            if (endingText != null) endingText.color = Color.white;
        }
        else if (rate >= 50f)
        {
            target.sprite = normalSprite;
            if (endingText != null) endingText.text = normalMessage + scoreText;
            if (endingText != null) endingText.color = Color.white;
        }
        else
        {
            target.sprite = badSprite;
            if (endingText != null) endingText.text = badMessage + scoreText;
            if (endingText != null) endingText.color = Color.white;
        }
    }

    public void UpdateZone(float rate)
    {
        SetImageBasedOnRate(zoneImage, rate);
        Debug.Log($"Zone 1 정답률 ({rate}%) 적용 완료.");
    }

    public float LoadData()
    {
        float latestRate = 0f;

        string connectionString = "URI=file:" + Application.streamingAssetsPath + "/test.db";
        IDbConnection dbConnection = new SqliteConnection(connectionString);
        dbConnection.Open();

        string tablename = "Record";

        IDbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = "SELECT rate_total FROM " + tablename + " ORDER BY session_id DESC LIMIT 1";

        IDataReader dataReader = dbCommand.ExecuteReader();

        if (dataReader.Read())
        {
            if (!dataReader.IsDBNull(0))
            {
                latestRate = dataReader.GetFloat(0);
            }
        }

        dataReader.Close();
        dbConnection.Close();

        return latestRate;
    }

    public float CalcData()
    {
        int sessionId = GlobalData.SessionId; // 세션id 불러오기

        List<int> sceneTotList = new List<int>(); // 이번 세션의 시나리오별 전체문제수(Ai)
        List<int> sceneCorrectList = new List<int>(); // 이번 세션의 시나리오별 맞은문제수(Bi)

        // db 연결
        string connectionString = "URI=file:" + Application.streamingAssetsPath + "/test.db";
        IDbConnection dbConnection = new SqliteConnection(connectionString);
        dbConnection.Open();

        IDbCommand dbCommand = dbConnection.CreateCommand();
        

        // 이번 세션의 시나리오별 전체문제수 계산
        dbCommand.CommandText = "select COUNT(*) as cnt from Log, Question WHERE Log.question_id = Question.question_id and Log.session_id = " + sessionId + " group by Question.scenario_id";
        sceneTotList = executeQuery(dbCommand); // 이번 세션의 시나리오별 전체문제수
        Debug.Log("sql: " + dbCommand.CommandText);

        // 이번 세션의 시나리오별 맞은문제수 계산
        List<int> sceneIdList = new List<int>(); // 시나리오 번호들

        dbCommand.CommandText = "select scenario_id from Scenario";
        sceneIdList = executeQuery(dbCommand);
        Debug.Log("sql: " + dbCommand.CommandText);

        List<int> sceneQuestionList = new List<int>(); // 특정 시나리오(id=sceneId)에 속하는 문제 번호들
        foreach (int sceneId in sceneIdList) // 각 시나리오 번호에 대해서
        {
            string subQuery = "select Log.question_id as question_id, Question.scenario_id as scenario_id from Log, Question where Log.question_id = Question.question_id and Log.session_id = " + sessionId; // 문제 번호와 시나리오 번호 얻어옴
            dbCommand.CommandText = "select question_id from (" + subQuery + ") where scenario_id = " + sceneId; // 시나리오별 문제번호 얻음
            sceneQuestionList = executeQuery(dbCommand); // 특정 시나리오(id=sceneId)에 속하는 문제 번호들
            Debug.Log(sceneId + "번 시나리오에 속하는 문제 번호들");
            Debug.Log("sql: " + dbCommand.CommandText);
            

            List<int> sessionQuestionList = new List<int>(); // 이번 세션의 전체 문제 중 맞은 문제 번호
            dbCommand.CommandText = "select question_id from Log where session_id = " + sessionId + " and is_correct = 'Y'";
            sessionQuestionList = executeQuery(dbCommand);
            Debug.Log("sql: " + dbCommand.CommandText);
            Debug.Log("이번 세션의 전체 문제 중 맞은 문제 번호");

            List<int> linqMethodResult = sessionQuestionList.Intersect(sceneQuestionList).ToList(); // 특정 시나리오의 문제 번호와, 이번 세션의 전체 문제 중 맞은 문제 번호의 교집합 -> 특정 시나리오에서 맞은 문제 번호
            sceneCorrectList.Add(linqMethodResult.Count); // 특정 시나리오의 맞은 문제 개수
            if (linqMethodResult.Any())
            {
                Debug.Log(sceneId + "번 시나리오의 맞은 문제 개수: " + linqMethodResult.Count);
            }
        }

        float tot_all = 0f;
        float tot_corr = 0f;
        float rate1 = -1f;
        float rate3 = -1f;
        float rate5 = -1f;
        float total = -1f;

        if(sceneTotList.Any()) // 비어있지 않다면
        {
            foreach (int i in sceneTotList)
            {
                tot_all += i;
            }
        }
        if(sceneCorrectList.Any()) // 비어있지 않다면
        {
            foreach (int i in sceneCorrectList)
            {
                tot_corr += i;
            }
        }
        Debug.Log("tot_all: " + tot_all + ", tot_corr: " + tot_corr);

        if (tot_all == 0f || tot_corr == 0f) // 푼 문제가 없거나, 풀었지만 맞은 문제가 없는 경우
        { 
            rate1 = 0f;
            rate3 = 0f;
            rate5 = 0f;
            total = 0f;
        }
        else
        {
            Debug.Log("sceneCorrectList len: " + sceneCorrectList.Count + ", sceneTotList len: " + sceneTotList.Count);
            for (int i = 0; i < sceneCorrectList.Count; i++)
            {
                Debug.Log((i + 1) + ". sceneCorrectList: " + sceneCorrectList[i] + ", sceneTotList: " + sceneTotList[i]);
                if (i == 0)
                {
                    rate1 = ((float)sceneCorrectList[0] / (float)sceneTotList[0]) * 100f;
                }
                else if (i == 1)
                {
                    rate3 = ((float)sceneCorrectList[1] / (float)sceneTotList[1]) * 100f;
                }
                else if (i == 2)
                {
                    rate5 = ((float)sceneCorrectList[2] / (float)sceneTotList[2]) * 100f;
                }

            }
            total = (tot_corr / tot_all) * 100f;
        }

        string ending = "";
        if(total >= 80f)
        {
            ending = "Happy";
        }
        else if(total >= 50f)
        {
            ending = "Normal";
        }
        else
        {
            ending = "Bad";
        }

        dbCommand.CommandText = "UPDATE Record SET play_date = '" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + "', rate_stage_1 = " + rate1 + ", rate_stage_3 = " + rate3 + ", rate_stage_5 = " + rate5 + ", rate_total = " + total + ", ending_result = '" + ending + "' WHERE session_id = " + GlobalData.SessionId;
        dbCommand.ExecuteNonQuery();
        Debug.Log("sql: " + dbCommand.CommandText);
        dbCommand.Dispose();

        dbConnection.Close();

        return total;
    }

    public List<int> executeQuery(IDbCommand dbCommand) // db 관련 공통 부분 분리
    {
        List<int> temp = new List<int>();

        IDataReader dataReader = dbCommand.ExecuteReader();
        while (dataReader.Read())
        {
            if (!dataReader.IsDBNull(0))
            {
                temp.Add(dataReader.GetInt32(0));
            }            
        }
        dataReader.Close();

        return temp;
    }
}

using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Data;
using Mono.Data.Sqlite;

public class QuizManager : MonoBehaviour
{
    public TMP_Text questionText;

    private List<KeyValuePair<int, string>> questions;
    private int currentIndex = 0;  // 현재 문제 번호

    private char submitAnswer;

    void Start()
    {
        QuizDB db = new QuizDB();
        questions = db.LoadQuestions();

        if (questions.Count > 0)
        {
            ShowQuestion(currentIndex);
        }
        else
        {
            questionText.text = "문제가 없습니다.";
        }
    }

    public void NextQuestion()
    {
        if (currentIndex < questions.Count - 1)
        {
            currentIndex++;
            ShowQuestion(currentIndex);
        }
        else
        {
            Debug.Log("마지막 문제입니다.");
        }
    }

    public void PrevQuestion()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowQuestion(currentIndex);
        }
        else
        {
            Debug.Log("첫 번째 문제입니다.");
        }
    }

    public void ShowQuestion(int index)
    {
        questionText.text = questions[index].Value;
        Debug.Log("현재 문제: " + questionText.text);

        // 응답 결과가 나왔다고 가정
        submitAnswer = 'O';
        KeyValuePair<int, string> temp = questions[currentIndex];
        bool rst = CheckAnswer(temp.Key, submitAnswer); // 정답과 비교
        InsertLog(rst, temp.Key); // 기록 저장
    }

    // 응답 결과와 정답 비교하는 함수
    public bool CheckAnswer(int curIdx, char submitAnswer)
    {
        bool rst = false;

        using (IDbConnection conn = new SqliteConnection("URI=file:" + Application.streamingAssetsPath + "/test.db"))
        {
            conn.Open();
            using (IDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT answer FROM Question where question_id = " + curIdx;
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            string answer = reader.GetString(0);
                            if (answer[0] == submitAnswer)
                            {
                                rst = true;
                            }
                        }
                    }
                }
            }
        }

        return rst;
    }

    // 기록을 db에 저장하는 함수
    public void InsertLog(bool rst, int q_id)
    {
        using (IDbConnection conn = new SqliteConnection("URI=file:" + Application.streamingAssetsPath + "/test.db"))
        {
            conn.Open();
            using (IDbCommand cmd = conn.CreateCommand())
            {
                string is_correct = "";
                if(rst)
                {
                    is_correct = "Y";
                }
                else
                {
                    is_correct = "N";
                }
                //cmd.CommandText = "INSERT INTO Log VALUES(" + PlayerPrefs.GetInt("SessionId") + ", " + q_id + ", " + is_correct + ")";
                cmd.CommandText = "INSERT INTO Log (session_id, question_id, is_correct) VALUES(" + GlobalData.SessionId + ", " + q_id + ", '" + is_correct + "')";
                cmd.ExecuteNonQuery();
                Debug.Log("sql: " + cmd.CommandText);
                cmd.Dispose();
            }
            conn.Close();
        }
    }
}

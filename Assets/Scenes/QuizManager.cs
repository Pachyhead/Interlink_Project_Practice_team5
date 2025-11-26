using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Data;
using Mono.Data.Sqlite;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class QuizManager : MonoBehaviour
{
    public int currentStageNumber = 1;
    [Header("UI References")]
    public GameObject resultButtonObj;
    public Image resultButtonImage;

    [Header("Feedback Sprites")]
    public Sprite CorrectSprite;
    public Sprite IncorrectSprite;

    public bool isCorrect = false; // 정답여부(피드백 이미지 처리용)

    public Image questionImageView;
    public TMP_Text questionText;
    public int targetScenarioId = 1;
    private List<KeyValuePair<int, string>> questions;
    private int currentIndex = 0;  // 현재 문제 번호

    private char submitAnswer;

    void Start()
    {
        QuizDB db = new QuizDB();
        questions = db.LoadQuestions(targetScenarioId);

        if (resultButtonObj != null)
        {
            resultButtonObj.SetActive(false);
        }

        if (questions.Count > 0)
        {
            ShowQuestion(currentIndex);
        }
        else
        {
            questionText.text = "해당 시나리오에 문제가 없습니다.";
        }
    }

    public void NextQuestion()
    {
        if (currentIndex < questions.Count - 1)
        {
            currentIndex++;
            ShowQuestion(currentIndex);
        }
        else // 마지막 문제
        {
            Debug.Log("마지막 문제입니다.");
            int nextStage = currentStageNumber + 1;
            if (nextStage > PlayerPrefs.GetInt("ClearedLevel"))
            {
                PlayerPrefs.SetInt("ClearedLevel", nextStage);
                PlayerPrefs.Save();
            }
            SceneManager.LoadScene("Game"); // 이동
        }
    }
    
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Return)) // 엔터 누르면
        {
            if (resultButtonObj.activeSelf)
            {
                resultButtonObj.SetActive(false);
                NextQuestion(); // 다음 문제
            }
        }
    }

    public void ShowQuestion(int index)
    {
        questionText.text = questions[index].Value;

        int currentQID = questions[index].Key;

        if (questionImageView != null)
        {
            string imagePath = "QuestionImages/Question_" + currentQID;
            Sprite loadedSprite = Resources.Load<Sprite>(imagePath);

            if (loadedSprite != null)
            {
                // 이미지 파일이 있으면 보여줌
                questionImageView.sprite = loadedSprite;
                questionImageView.gameObject.SetActive(true);
            }
            else
            {
                // 이미지 파일이 없으면 숨김
                questionImageView.sprite = null;
                questionImageView.gameObject.SetActive(false);
            }
        }
    }

    // [추가] PythonConnector가 호출할 답변 제출 및 채점 함수
    public void SubmitAnswerFromVision(char answer)
    {
        submitAnswer = answer;
        int currentQID = questions[currentIndex].Key;

        Debug.Log($"비전 인식으로 답변 '{submitAnswer}' 제출됨. 채점을 시작합니다.");
        bool is_correct = CheckAnswer(currentQID, submitAnswer);
        Debug.Log("채점 결과: " + (is_correct ? "정답" : "오답"));
        
        // 채점 결과를 DB에 기록
        InsertLog(isCorrect, currentQID);

        isCorrect = is_correct;

        FeedBack();
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

    // 피드백 함수
    public void FeedBack()
    {
        // 피드백 이미지를 바꿔주는 부분
        if (isCorrect) // 정답이라면
        {
            if (CorrectSprite != null) resultButtonImage.sprite = CorrectSprite; // 긍정적 이미지
            Debug.Log("Good Job");
        }
        else // 오답이라면
        {
            if (IncorrectSprite != null) resultButtonImage.sprite = IncorrectSprite;  // 부정적 이미지
            Debug.Log("Try again");
        }
        if (resultButtonObj != null)
        {
            resultButtonObj.SetActive(true); // 버튼 보이게
        }
    }
}

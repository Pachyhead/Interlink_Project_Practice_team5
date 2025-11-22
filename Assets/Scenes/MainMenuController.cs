using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // 씬 이동을 위해 필수
using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;

public class MainMenuController : MonoBehaviour
{
    void Start()
    {
        PlayerPrefs.SetInt("ClearedLevel", 1);
        PlayerPrefs.Save();
    }
    // 게임 시작 버튼 연결 함수
    public void OnClickStart()
    {
        CreateSession();
        SceneManager.LoadScene("Game");
    }

    // 기록 버튼 연결 함수
    public void OnClickRecord()
    {
        SceneManager.LoadScene("Record");
    }

    // Stage1 진입
    public void OnClickStage1()
    {
        SceneManager.LoadScene("Stage1");
    }

    // Stage2 진입
    public void OnClickStage2()
    {
        SceneManager.LoadScene("Stage2");
    }

    // Stage3 진입
    public void OnClickStage3()
    {
        SceneManager.LoadScene("Stage3");
    }

    // Stage4 진입
    public void OnClickStage4()
    {
        SceneManager.LoadScene("Stage4");
    }

    // Stage5 진입
    public void OnClickStage5()
    {
        SceneManager.LoadScene("Stage5");
    }


    public GameObject quitPanel;

    // 게임 종료 버튼 연결 함수
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (quitPanel.activeSelf)
            {
                CloseQuitPanel();
            }
            else
            {
                ShowQuitPanel();
            }
        }
    }

    public void ShowQuitPanel()
    {
        quitPanel.SetActive(true);
    }

    public void CloseQuitPanel()
    {
        quitPanel.SetActive(false);
    }

    public void GameQuit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // 에디터에서 끄기
        #else
            Application.Quit(); // 실제 게임에서 끄기
        #endif
    }

    // 세션키 생성하기
    public void CreateSession()
    {
        using (IDbConnection conn = new SqliteConnection("URI=file:" + Application.streamingAssetsPath + "/test.db"))
        {
            conn.Open();
            using (IDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT session_id FROM Record order by session_id DESC LIMIT 1";
                int newSessionId = 0;
                using (IDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            newSessionId = reader.GetInt32(0) + 1;
                            Debug.Log("session_id: " + newSessionId);
                            GlobalData.SessionId = newSessionId;
                        }
                    }
                }
                cmd.CommandText = "INSERT INTO Record VALUES(" + GlobalData.SessionId + ", null, null, null, null, null, null)";
                cmd.ExecuteNonQuery();
                Debug.Log("sql: " + cmd.CommandText);
                cmd.Dispose();
            }
            conn.Close();
        }
    }
}
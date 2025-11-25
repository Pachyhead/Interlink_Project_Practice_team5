using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement; // 씬 이동을 위해 필수

public class End : MonoBehaviour
{
    public GameObject videoPanel;
    public VideoPlayer videoPlayer;

    public string nextSceneName = "Game";
    public int currentStageNumber = 1;

    void Start()
    {
        videoPanel.SetActive(true);
        videoPlayer.Play();
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        SaveClearData();
        SceneManager.LoadScene(nextSceneName);
    }

    void SaveClearData()
    {
        int nextStage = currentStageNumber + 1;
        if (nextStage > PlayerPrefs.GetInt("ClearedLevel", 1))
        {
            PlayerPrefs.SetInt("ClearedLevel", nextStage);
            PlayerPrefs.Save();
            Debug.Log("스테이지 클리어 저장 완료: " + nextStage);
        }
    }
}
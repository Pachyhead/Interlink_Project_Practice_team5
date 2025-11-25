using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class ReNext : MonoBehaviour
{
    [Header("Panels")]
    public GameObject videoPanel;     // 영상 패널
    public GameObject finishPanel;    // R/N 안내 패널

    [Header("Video")]
    public VideoPlayer videoPlayer;

    private bool isWaitingForInput = false;

    void Start()
    {
        videoPanel.SetActive(true);
        finishPanel.SetActive(false);

        videoPlayer.Stop();
        videoPlayer.loopPointReached += OnVideoEnd;

        videoPlayer.Play();
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        finishPanel.SetActive(true);
        isWaitingForInput = true;
    }

    void Update()
    {
        if (!isWaitingForInput) return;

        // 다시 보기
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartVideo();
        }

        // 다음 영상
        if (Input.GetKeyDown(KeyCode.N))
        {
            GoNextVideo();
        }
    }

    void RestartVideo()
    {
        finishPanel.SetActive(false);
        isWaitingForInput = false;

        videoPlayer.Stop();
        videoPlayer.Play();
    }

    void GoNextVideo()
    {
        SceneManager.LoadScene("Stage2_next");
    }
}

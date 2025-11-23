using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class Stage3_Intro : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject endImage;

    private bool videoFinished = false;

    void Start()
    {
        endImage.SetActive(false);

        videoPlayer.loopPointReached += OnVideoEnd;
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        endImage.SetActive(true);
        videoPlayer.gameObject.SetActive(false);

        videoFinished = true;   // 이제 S 키 입력 가능!
    }

    void Update()
    {
        if (videoFinished && Input.GetKeyDown(KeyCode.S))
        {
            SceneManager.LoadScene("Stage1");   // 이동할 씬 이름 입력
        }
    }
}

using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject stageIntroPanel;   // 스테이지 설명 UI
    public GameObject videoPanel;        // 영상 표시 UI

    [Header("Video")]
    public VideoPlayer videoPlayer;

    [Header("Config")]
    public float introDuration = 2.5f;   // 설명 보여주는 시간(sec)

    void Start()
    {
        // 시작 시: 설명만 보이도록 설정
        stageIntroPanel.SetActive(true);
        videoPanel.SetActive(false);

        // 영상은 먼저 재생하지 않기
        videoPlayer.Stop();

        // 단계 시작
        StartCoroutine(StageFlow());
    }

    IEnumerator StageFlow()
    {
        // 1) 소개 패널 보여주기
        yield return new WaitForSeconds(introDuration);

        // 2) 소개 패널 숨기고 영상 보여주기
        stageIntroPanel.SetActive(false);
        videoPanel.SetActive(true);

        // 3) 영상 재생
        videoPlayer.Play();

        // 4) 영상이 끝날 때까지 대기
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        // Stage2 영상이 끝나면 Scene2로 이동
        SceneManager.LoadScene("Scene2");
    }
}

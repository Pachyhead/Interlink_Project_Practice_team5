using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoEndController : MonoBehaviour
{
    public int currentStageNumber = 1;

    [Header("연결할 요소")]
    public VideoPlayer videoPlayer;    // 비디오 플레이어
    public GameObject replayButton;    // 다시보기 버튼 (UI)
    public GameObject returnButton;    // 돌아가기 버튼 (UI) [추가됨]

    [Header("설정")]
    public string returnSceneName = "Game"; // 돌아갈 씬 이름

    void Start()
    {
        // 1. 시작할 때 버튼들은 숨김
        replayButton.SetActive(false);
        returnButton.SetActive(false);

        // 2. 영상이 끝났을 때 호출될 이벤트 연결
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    // 영상이 끝났을 때 실행
    void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("영상 종료. 버튼 표시");

        // 버튼 두 개 모두 표시
        replayButton.SetActive(true);
        returnButton.SetActive(true);
    }

    // [다시보기 버튼 연결용]
    public void OnClickReplay()
    {
        // 버튼 다시 숨기기
        replayButton.SetActive(false);
        returnButton.SetActive(false);

        // 영상 처음부터 다시 재생
        videoPlayer.frame = 0;
        videoPlayer.Play();
    }

    // [돌아가기 버튼 연결용]
    public void OnClickReturn()
    {
        int nextStage = currentStageNumber + 1;

        // 기존 기록보다 높을 때만 저장 (이미 깬 거 다시 깼을 때 초기화 방지)
        if (nextStage > PlayerPrefs.GetInt("ClearedLevel", 1))
        {
            PlayerPrefs.SetInt("ClearedLevel", nextStage);
            PlayerPrefs.Save(); // 저장 확정
        }
        SceneManager.LoadScene(returnSceneName);
    }
}
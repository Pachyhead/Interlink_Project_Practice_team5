using UnityEngine;
using UnityEngine.Video;

public class VideoEndHandler : MonoBehaviour
{
    [Header("관리할 오브젝트")]
    [Tooltip("동영상 재생이 끝나면 활성화할 AI 관리자 오브젝트")]
    public GameObject aiManager;

    [Tooltip("동영상 재생이 끝나면 활성화할 스케줄러 관리자 오브젝트")]
    public GameObject schedulerManager;

    [Tooltip("동영상 재생이 끝나면 비활성화할 비디오 패널 오브젝트")]
    public GameObject videoPanel;

    private VideoPlayer videoPlayer;
    private bool isVideoFinished = false; // 동영상 종료 처리를 한 번만 실행하기 위한 플래그

    void Awake()
    {
        // 이 스크립트가 붙어있는 게임 오브젝트의 VideoPlayer 컴포넌트를 가져옵니다.
        videoPlayer = GetComponent<VideoPlayer>();

        // 필수 오브젝트들이 연결되었는지 확인합니다.
        if (aiManager == null || schedulerManager == null || videoPanel == null)
        {
            Debug.LogError("VideoEndHandler에 필요한 게임 오브젝트들이 Inspector에서 모두 할당되지 않았습니다.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // 시작 시에는 AI 관련 기능들을 비활성화합니다.
        aiManager.SetActive(false);
        schedulerManager.SetActive(false);
    }

    void Update()
    {
        // 아직 종료 처리를 하지 않았고, 비디오 플레이어가 재생 준비를 마쳤는지 확인합니다.
        if (!isVideoFinished && videoPlayer.isPrepared)
        {
            // 현재 프레임이 전체 프레임 수와 같거나 크면 동영상이 끝난 것으로 간주합니다.
            // frameCount가 ulong이므로 비교를 위해 long으로 형변환합니다.
            if (videoPlayer.frame >= (long)videoPlayer.frameCount - 1)
            {
                // 종료 처리 플래그를 true로 설정하여 이 로직이 반복 실행되지 않도록 합니다.
                isVideoFinished = true;
                OnVideoFinished();
            }
        }
    }

    void OnVideoFinished()
    {
        Debug.Log("동영상 재생이 완료되었습니다. AI 분석을 시작합니다.");

        // AI 관리자와 스케줄러를 활성화합니다.
        aiManager.SetActive(true);
        schedulerManager.SetActive(true);

        // 비디오 패널을 비활성화하여 화면에서 숨깁니다.
        if (videoPanel != null)
        {
            videoPanel.SetActive(false);
        }
    }
}
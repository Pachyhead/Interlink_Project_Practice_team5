using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class Stage2B : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject nextMessagePanel;

    private CanvasGroup messageCanvasGroup;
    private bool isVideoFinished = false;
    private bool isBlinking = false;

    void Start()
    {
        nextMessagePanel.SetActive(false);

        // CanvasGroup 가져오기
        messageCanvasGroup = nextMessagePanel.GetComponent<CanvasGroup>();

        videoPlayer.loopPointReached += OnVideoEnd;
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        nextMessagePanel.SetActive(true);
        isVideoFinished = true;

        // 깜빡임 시작
        StartCoroutine(BlinkMessage());
    }

    void Update()
    {
        if (isVideoFinished && Input.GetKeyDown(KeyCode.S))
        {
            // 깜빡임 멈추기
            StopAllCoroutines();
            messageCanvasGroup.alpha = 1f;

            SceneManager.LoadScene("Stage2_2");
        }
    }

    // 🔥 깜빡이는 코루틴 (속도 절반으로 느리게)
    System.Collections.IEnumerator BlinkMessage()
    {
        isBlinking = true;

        while (isBlinking)
        {
            // Alpha 1 → 0 (느리게)
            for (float t = 1f; t >= 0; t -= Time.deltaTime * 1f)
            {
                messageCanvasGroup.alpha = t;
                yield return null;
            }

            // Alpha 0 → 1 (느리게)
            for (float t = 0; t <= 1f; t += Time.deltaTime * 1f)
            {
                messageCanvasGroup.alpha = t;
                yield return null;
            }
        }
    }
}

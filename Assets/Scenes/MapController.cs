using UnityEngine;
using UnityEngine.SceneManagement;

public class MapController : MonoBehaviour
{
    [Header("연결할 요소")]
    public Transform playerIcon;
    public Transform[] stageNodes;    // [중요] 인스펙터에서 5개(스테이지1~4 + 화산)를 연결하세요.

    [Header("설정")]
    public float moveSpeed = 10f;

    // 내부 변수
    private int currentIndex = 0;
    private int unlockedStage = 1;

    void Start()
    {
        // [수정] 테스트용 기본값 변경 (총 5개 노드이므로 최대값 5로 변경)
        unlockedStage = PlayerPrefs.GetInt("ClearedLevel", 5);

        // [기존 로직 유지]
        // 2. 캐릭터를 현재 해금된 가장 마지막 위치나 0번 위치에 둡니다.
        currentIndex = Mathf.Clamp(unlockedStage - 1, 0, stageNodes.Length - 1);
        playerIcon.position = stageNodes[currentIndex].position;
    }

    void Update()
    {
        HandleMovementInput();
        HandleEnterInput();
        MoveCharacterSmoothly();
    }

    void HandleMovementInput()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            // [동적 처리] stageNodes.Length를 사용하여 배열 크기에 맞춰 자동으로 작동
            if (currentIndex < stageNodes.Length - 1 && (currentIndex + 1) < unlockedStage)
            {
                currentIndex++;
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentIndex > 0)
            {
                currentIndex--;
            }
        }
    }

    void MoveCharacterSmoothly()
    {
        playerIcon.position = Vector3.Lerp(playerIcon.position, stageNodes[currentIndex].position, moveSpeed * Time.deltaTime);
    }

    void HandleEnterInput()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            EnterStage();
        }
    }

    void EnterStage()
    {
        // [핵심 수정] 하드코딩(숫자 5)을 없애고, 배열의 마지막 인덱스인지 확인
        // 이렇게 하면 노드가 4개든 10개든, '마지막'은 항상 화산(엔딩)으로 인식합니다.
        if (currentIndex == stageNodes.Length - 1)
        {
            Debug.Log("화산 진입! 멀티 엔딩 씬으로 이동");
            SceneManager.LoadScene("MultiEnding");
        }
        else
        {
            // 일반 스테이지 진입 (Stage1, Stage2, Stage3, Stage4)
            string sceneName = "Stage" + (currentIndex + 1);
            Debug.Log(sceneName + " 진입");
            SceneManager.LoadScene(sceneName);
        }
    }
}
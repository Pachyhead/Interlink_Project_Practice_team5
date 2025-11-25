using UnityEngine;
using TMPro;

public class AnalysisScheduler : MonoBehaviour
{
    [Header("UI 설정")]
    [Tooltip("남은 시간을 표시할 TextMeshPro UI 요소를 연결합니다.")]
    public TMP_Text timerText;

    [Header("타이머 설정")]
    [Tooltip("분석을 실행할 주기(초)입니다.")]
    public float analysisInterval = 5.0f;

    private PythonConnector pythonConnector;
    private float countdown;

    void Awake()
    {
        // 씬에 존재하는 PythonConnector 컴포넌트를 자동으로 찾아서 할당합니다.
        // FindObjectOfType은 구버전(obsolete) API이므로 FindFirstObjectByType으로 대체합니다.
        pythonConnector = FindFirstObjectByType<PythonConnector>();
    }

    void Start()
    {
        // 타이머 초기화
        countdown = analysisInterval;

        // 필수 컴포넌트가 연결되었는지 확인
        if (pythonConnector == null)
        {
            Debug.LogError("씬에서 PythonConnector를 찾을 수 없습니다. AI_Manager 객체가 활성화되어 있는지 확인해주세요.");
            enabled = false; // 스크립트 비활성화
            return;
        }

        if (timerText == null)
        {
            Debug.LogWarning("timerText가 연결되지 않았습니다. 남은 시간이 표시되지 않습니다.");
        }
    }

    void Update()
    {
        // 카운트다운 감소
        countdown -= Time.deltaTime;

        // UI 텍스트 업데이트
        if (timerText != null)
        {
            // 소수점을 올림하여 정수로 표시 (예: 4.3초 -> 5)
            timerText.text = Mathf.CeilToInt(countdown).ToString();
        }

        // 카운트다운이 0 이하로 내려가면 분석 실행
        if (countdown <= 0f)
        {
            Debug.Log($"{analysisInterval}초가 경과하여 웹캠 분석을 실행합니다.");
            
            // Python 분석 실행
            pythonConnector.CaptureAndAnalyze();

            // 타이머 리셋
            countdown = analysisInterval;
        }
    }
}
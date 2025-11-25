using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events; // 이벤트 기능을 위해 필수
using System.Collections;
using System.IO;
using System;
using System.Text;

public class STT_Manager : MonoBehaviour
{
    [Header("1. Google AI Studio 설정")]
    [Tooltip("AI Studio에서 발급받은 API 키를 입력하세요")]
    private string apiKey = "";

    [Header("2. 감지할 단어 설정")]
    public string[] targetKeywords = { "불이야", "fire", "불 이야"};

    [Header("3. 성공 시 실행할 기능들")]
    [Tooltip("여기에 퀴즈 매니저나 탐지 매니저를 켜는 기능을 연결하세요.")]
    public UnityEvent onSceneStart; // ★ 만능 스위치

    // 내부 변수들 (외부 노출 불필요)
    private string _microphoneID = null;
    private AudioClip _recordingClip;
    private bool _isRecording = false;
    private bool _isLooping = false;

    // ================================================================
    // 1. 시작 및 종료 로직
    // ================================================================

    void Start()
    {
        // 1. 보안 키 파일 읽어오기
        LoadApiKey();

        // 2. 키가 없으면 작동 중지
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("❌ API 키가 없습니다! Assets/StreamingAssets/gemini_api_key.txt 파일을 확인하세요.");
            return; // 여기서 멈춤
        }

        Debug.Log("🎤 STT_Manager 시작: '불이야'를 기다립니다...");

        // 씬 시작 시 자동으로 루프 시작
        StartListeningLoop();
    }

    public void StartListeningLoop()
    {
        StopListeningLoop(); // 안전장치: 기존 루프 끄기
        _isLooping = true;
        StartRecording();
    }

    public void StopListeningLoop()
    {
        _isLooping = false;
        _isRecording = false;

        // 좀비 방지: 예약된 작업 및 통신 종료
        CancelInvoke();
        StopAllCoroutines();

        Microphone.End(_microphoneID);
    }

    // ================================================================
    // 2. 내부 녹음 및 통신 로직 (GeminiSTT 기능 흡수)
    // ================================================================

    private void StartRecording()
    {
        if (!_isLooping) return;

        _isRecording = true;
        // 4초 단위 녹음
        _recordingClip = Microphone.Start(_microphoneID, false, 4, 44100);

        // 4초 뒤 전송 예약
        Invoke("StopRecordingAndSend", 4f);
    }

    private void StopRecordingAndSend()
    {
        if (!_isRecording) return;

        _isRecording = false;
        Microphone.End(_microphoneID);

        if (_isLooping)
        {
            StartCoroutine(SendToGeminiFlash(_recordingClip));
        }
    }

    IEnumerator SendToGeminiFlash(AudioClip clip)
    {
        byte[] audioBytes = ToWav(clip);
        string base64Audio = Convert.ToBase64String(audioBytes);
        string keywordString = string.Join("' or '", targetKeywords);

        // 프롬프트: 숫자 1만 요구
        string promptText = $"Listen carefully. If you clearly hear any of these words: ['{keywordString}'], output ONLY the number '1'. Otherwise output '0'. Do not add any other text.";

        string jsonBody = $@"
        {{
            ""contents"": [{{
                ""parts"": [
                    {{ ""text"": ""{promptText}"" }},
                    {{
                        ""inline_data"": {{
                            ""mime_type"": ""audio/wav"",
                            ""data"": ""{base64Audio}""
                        }}
                    }}
                ]
            }}]
        }}";

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (!_isLooping) yield break;

            if (request.result == UnityWebRequest.Result.Success)
            {
                CheckResult(request.downloadHandler.text);
            }
            else
            {
                // 에러 처리
                long code = request.responseCode;
                if (code == 429 || code >= 500 || code == 0)
                {
                    Debug.LogWarning($"⚠️ 재시도 중... ({request.error})");
                    Invoke("StartRecording", 1f);
                }
                else
                {
                    Debug.LogError($"⛔ 치명적 오류 ({request.error}, {code}). 중단.");
                    StopListeningLoop();
                }
            }
        }
    }

    void CheckResult(string jsonResponse)
    {
        // 판독 로직
        if (jsonResponse.Contains("\"text\": \"1\"") || jsonResponse.Contains("'1'") || jsonResponse.Trim() == "1")
        {
            Debug.Log("🚀 [성공] 키워드 감지! 씬 기능을 시작합니다.");

            StopListeningLoop(); // 루프 종료

            // ★ 통합된 부분: 바로 이벤트를 실행!
            onSceneStart.Invoke();
        }
        else
        {
            // 실패 시 재시도
            if (_isLooping) Invoke("StartRecording", 0.1f);
        }
    }

    // ================================================================
    // 3. WAV 변환 유틸리티
    // ================================================================
    byte[] ToWav(AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            int hz = clip.frequency;
            int channels = clip.channels;
            int samples = clip.samples;

            stream.Write(Encoding.UTF8.GetBytes("RIFF"), 0, 4);
            stream.Write(BitConverter.GetBytes(36 + samples * 2), 0, 4);
            stream.Write(Encoding.UTF8.GetBytes("WAVE"), 0, 4);
            stream.Write(Encoding.UTF8.GetBytes("fmt "), 0, 4);
            stream.Write(BitConverter.GetBytes(16), 0, 4);
            stream.Write(BitConverter.GetBytes((ushort)1), 0, 2);
            stream.Write(BitConverter.GetBytes((ushort)channels), 0, 2);
            stream.Write(BitConverter.GetBytes(hz), 0, 4);
            stream.Write(BitConverter.GetBytes(hz * channels * 2), 0, 4);
            stream.Write(BitConverter.GetBytes((ushort)(channels * 2)), 0, 2);
            stream.Write(BitConverter.GetBytes((ushort)16), 0, 2);
            stream.Write(Encoding.UTF8.GetBytes("data"), 0, 4);
            stream.Write(BitConverter.GetBytes(samples * 2), 0, 4);

            float[] data = new float[samples * channels];
            clip.GetData(data, 0);

            foreach (var sample in data)
            {
                short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                stream.Write(BitConverter.GetBytes(intSample), 0, 2);
            }
            return stream.ToArray();
        }
    }


    // ★ 파일을 읽어서 apiKey 변수에 채워넣는 함수
    void LoadApiKey()
    {
        // StreamingAssets 폴더 경로
        string path = Path.Combine(Application.streamingAssetsPath, "gemini_api_key.txt");

        if (File.Exists(path))
        {
            // 파일 내용을 읽고 앞뒤 공백 제거해서 저장
            apiKey = File.ReadAllText(path).Trim();
            // Debug.Log("🔑 API 키 로드 성공!"); // 보안상 로그는 안 남기는 게 좋음
        }
        else
        {
            Debug.LogError($"❌ 키 파일을 찾을 수 없습니다. 경로: {path}");
        }
    }
}

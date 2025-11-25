using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using System.IO;

public class PythonConnector : MonoBehaviour
{
    // 외부에 노출하지 않고 내부에서 계산하므로 private으로 변경
    private string pythonExePath;
    private string scriptPath;
    private string imagePath; // 이미지 경로 변수 추가

    // 유니티 UI에 있는 RawImage 컴포넌트를 여기에 연결해야 내 얼굴이 보입니다.
    public RawImage displayImage;

    // [추가] 실제 웹캠 하드웨어를 제어하는 변수
    // 이 변수가 카메라를 켜고(Play), 끄고(Stop), 이미지를 받아옵니다.
    private WebCamTexture webCamTexture;

    // [추가] 퀴즈 관리자와 통신하기 위한 변수
    private QuizManager quizManager;

    void Awake()
    {
        // 1. 파이썬 실행 파일 경로 설정
        // Application.dataPath는 ".../Assets"를 가리킴
        // Directory.GetParent(...)를 쓰면 한 단계 위인 프로젝트 루트 폴더로 이동
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;

        // Path.Combine을 쓰면 윈도우(\)나 맥(/)에 맞춰서 알아서 경로를 합쳐줌 (매우 중요!)
        pythonExePath = Path.Combine(projectRoot, ".venv", "Scripts", "python.exe");

        // 2. 파이썬 스크립트 경로 설정
        // Assets/PythonScript/detect.py
        scriptPath = Path.Combine(Application.dataPath, "PythonScript", "detect.py");

        // 3. 이미지 저장 경로 설정 (추천 방식)
        // 윈도우 기준: C:\Users\사용자\AppData\LocalLow\회사명\프로젝트명\capture.jpg
        imagePath = Path.Combine(Application.persistentDataPath, "capture.jpg");

        // [디버깅용] 콘솔에 찍어서 경로가 맞는지 확인해보세요!
        UnityEngine.Debug.Log("Python Path: " + pythonExePath);
        UnityEngine.Debug.Log("Script Path: " + scriptPath);
        UnityEngine.Debug.Log("Image Path: " + imagePath);

        // [추가] 씬에서 QuizManager를 자동으로 찾습니다.
        quizManager = FindFirstObjectByType<QuizManager>();
    }

    // [추가] 게임 시작 시 웹캠을 찾아 켜는 함수
    void Start()
    {
        // [추가] QuizManager를 찾지 못했으면 경고를 표시합니다.
        if (quizManager == null)
        {
            UnityEngine.Debug.LogError("씬에서 QuizManager를 찾을 수 없습니다.");
        }

        // 1. 컴퓨터에 연결된 카메라 장치가 하나라도 있는지 확인
        if (WebCamTexture.devices.Length > 0)
        {
            // 2. 첫 번째 카메라(devices[0])를 선택하고, 해상도를 640x640으로 설정
            // YOLO 모델이 640 크기를 좋아하므로 미리 맞춰주면 성능이 좋습니다.
            webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 1280, 720);

            // 3. 카메라 전원 켜기 (실제 웹캠에 불이 들어옴)
            webCamTexture.Play();

            // 4. 내 눈에 보이게 연결하기
            // RawImage에 현재 웹캠이 찍고 있는 화면을 실시간으로 뿌려줍니다.
            if (displayImage != null)
            {
                displayImage.texture = webCamTexture;
                displayImage.material.mainTexture = webCamTexture;
            }
        }
        else
        {
            UnityEngine.Debug.LogError("오류: 연결된 웹캠을 찾을 수 없습니다!");
        }
    }

    // [변경] 버튼 클릭 시 실행되는 캡처 로직
    public void CaptureAndAnalyze()
    {
        // 1. 안전장치: 카메라가 꺼져있는데 캡처하려 하면 멈춤
        if (webCamTexture == null || !webCamTexture.isPlaying)
        {
            UnityEngine.Debug.LogError("웹캠이 동작 중이 아닙니다.");
            return;
        }

        // 2. 스냅샷 찍기 (비디오 스트림 -> 정지 이미지 변환)
        // 현재 웹캠 크기(width, height)에 맞는 빈 도화지(Texture2D)를 만듭니다.
        Texture2D snap = new Texture2D(webCamTexture.width, webCamTexture.height);

        // 3. 픽셀 복사하기 (가장 중요!)
        // 현재 흐르고 있는 웹캠 영상의 한 프레임 픽셀들을 쫙 긁어옵니다.
        snap.SetPixels(webCamTexture.GetPixels());
        snap.Apply(); // 변경 사항 확정

        // 4. 파일로 저장 (이후 과정은 기존과 동일)
        byte[] bytes = snap.EncodeToJPG();
        File.WriteAllBytes(imagePath, bytes); // capture.jpg로 덮어쓰기

        UnityEngine.Debug.Log("웹캠 사진 저장 완료: " + imagePath);

        // 5. 메모리 청소 (메모리 누수 방지)
        Destroy(snap);

        // 6. 파이썬 호출
        RunYoloDetection();
    }

    public void RunYoloDetection()
    {
        // 프로세스 설정
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = pythonExePath;

        // 인자로 스크립트 경로와 이미지 경로를 전달
        // 포맷: python detect.py image.jpg
        start.Arguments = string.Format("\"{0}\" \"{1}\"", scriptPath, imagePath);

        start.UseShellExecute = false;
        start.RedirectStandardOutput = true; // 파이썬의 print 결과를 받기 위해 필수
        start.CreateNoWindow = true; // 검은색 콘솔창 안 뜨게 설정

        // 프로세스 실행
        using (Process process = Process.Start(start))
        {
            // 파이썬 출력 읽기
            using (StreamReader reader = process.StandardOutput)
            {
                string result = reader.ReadToEnd();

                // 결과 처리 (문자열을 숫자로 변환)
                if (int.TryParse(result.Trim(), out int totalCode))
                {
                    // 파이썬이 보낸 암호: "302" (3명, 2명)

                    // 100으로 나눈 몫 = O영역 (왼쪽)
                    int countO = totalCode / 100;

                    // 100으로 나눈 나머지 = X영역 (오른쪽)
                    int countX = totalCode % 100;

                    UnityEngine.Debug.Log($"<color=yellow>분석 결과 도착!</color>");
                    UnityEngine.Debug.Log($"O영역(좌측): {countO}명");
                    UnityEngine.Debug.Log($"X영역(우측): {countX}명");

                    // [변경] 분석 결과를 바탕으로 답변 결정 후 QuizManager로 전달
                    if (quizManager != null)
                    {
                        if (countO >= countX)
                        {
                            quizManager.SubmitAnswerFromVision('O');
                        }
                        else if (countX > countO)
                        {
                            quizManager.SubmitAnswerFromVision('X');
                        }
                        // 동점인 경우 아무것도 하지 않음 (혹은 특정 로직 추가 가능)
                    }
                }
            }
        }
    }
}
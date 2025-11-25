using UnityEngine;
using UnityEngine.SceneManagement;

public class Stage2_IntroController : MonoBehaviour
{
    void Update()
    {
        // S 키 입력 감지
        if (Input.GetKeyDown(KeyCode.S))
        {
            LoadStage2();
        }
    }

    void LoadStage2()
    {
        // Stage2 씬 로드
        SceneManager.LoadScene("Stage2");
    }
}

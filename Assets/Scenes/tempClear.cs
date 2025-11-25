using UnityEngine;
using UnityEngine.SceneManagement;

public class tempClear : MonoBehaviour
{
    public int currentStageNumber = 1;
    public void OnClickBackButton()
    {
        int nextStage = currentStageNumber + 1;

        // 기존 기록보다 높을 때만 저장 (이미 깬 거 다시 깼을 때 초기화 방지)
        if (nextStage > PlayerPrefs.GetInt("ClearedLevel", 1))
        {
            PlayerPrefs.SetInt("ClearedLevel", nextStage);
            PlayerPrefs.Save(); // 저장 확정
        }
        SceneManager.LoadScene("Game");
    }
}

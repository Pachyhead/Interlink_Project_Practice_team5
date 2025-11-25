using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMenu : MonoBehaviour
{
    public void OnClickBackButton()
    {
        SceneManager.LoadScene("MainMenu");
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI; // [필수] 이미지 제어를 위해 추가
using UnityEngine.SceneManagement;

public class MultiIntroManager : MonoBehaviour
{
    [Header("연결 요소")]
    public Image displayImage;      // 화면에 보여질 UI Image 오브젝트
    public Sprite[] introSprites;   // 보여줄 4개의 이미지 리스트

    [Header("설정")]
    public float perImageTime = 2f; // 각 이미지당 보여줄 시간 (초)
    public string nextSceneName = "Game"; // 다음 씬 이름

    void Start()
    {
        StartCoroutine(PlaySequence());
    }

    void Update()
    {
        // 클릭하면 스킵하고 바로 다음 씬으로
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            StopAllCoroutines();
            SceneManager.LoadScene(nextSceneName);
        }
    }

    IEnumerator PlaySequence()
    {
        // 등록된 이미지 개수만큼 반복
        for (int i = 0; i < introSprites.Length; i++)
        {
            // 1. 이미지 교체
            displayImage.sprite = introSprites[i];

            // 2. 지정된 시간만큼 대기
            yield return new WaitForSeconds(perImageTime);
        }

        // 3. 모든 이미지를 다 보여줬으면 다음 씬으로 이동
        SceneManager.LoadScene(nextSceneName);
    }
}
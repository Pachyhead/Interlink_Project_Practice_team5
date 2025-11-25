using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MainMenuGestureController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject quitPanel; // 종료 확인 패널 오브젝트

    [Header("Button Lists")]
    public List<Button> mainButtons; // 기존 메뉴 버튼 (Start, Record, Quit)
    public List<Button> quitPanelButtons; // 종료 패널 버튼 (Yes, No)

    private List<Button> currentButtons; // 현재 제어 중인 버튼 리스트
    private int currentButtonIndex = 0;
    private bool isPanelActive = false;

    void Start()
    {
        // 시작 시 메인 버튼으로 초기화
        currentButtons = mainButtons;
        isPanelActive = false;
        SelectButton(currentButtonIndex);
    }

    void Update()
    {
        // QuitPanel의 활성화 상태가 변경되었는지 확인
        if (quitPanel.activeSelf != isPanelActive)
        {
            isPanelActive = quitPanel.activeSelf;
            SwitchButtonContext();
        }
    }

    // 제어할 버튼 리스트를 전환하는 함수
    private void SwitchButtonContext()
    {
        currentButtonIndex = 0; // 인덱스 초기화

        if (isPanelActive)
        {
            Debug.Log("제스처 제어: 종료 패널로 전환");
            currentButtons = quitPanelButtons;
        }
        else
        {
            Debug.Log("제스처 제어: 메인 메뉴로 전환");
            currentButtons = mainButtons;
        }

        // 전환 후 첫 번째 버튼 선택
        SelectButton(currentButtonIndex);
    }

    // 스와이프 제스처: 다음 버튼 선택
    public void SelectNextButton()
    {
        if (currentButtons == null || currentButtons.Count == 0) return;
        
        currentButtonIndex = (currentButtonIndex + 1) % currentButtons.Count;
        SelectButton(currentButtonIndex);
    }

    // 주먹 제스처: 현재 버튼 클릭
    public void ClickCurrentButton()
    {
        if (currentButtons == null || currentButtons.Count == 0) return;

        if (currentButtonIndex >= 0 && currentButtonIndex < currentButtons.Count)
        {
            Debug.Log($"버튼 클릭: {currentButtons[currentButtonIndex].name}");
            currentButtons[currentButtonIndex].onClick.Invoke();
        }
    }

    // 특정 인덱스의 버튼을 선택하는 내부 함수
    private void SelectButton(int index)
    {
        if (currentButtons == null || currentButtons.Count == 0) return;

        if (index >= 0 && index < currentButtons.Count)
        {
            currentButtons[index].Select();
            Debug.Log($"버튼 선택: {currentButtons[index].name}");
        }
    }
}
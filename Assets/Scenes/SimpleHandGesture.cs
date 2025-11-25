using UnityEngine;
using Mediapipe;
using Mediapipe.Unity;
using Google.Protobuf.Collections;

public class SimpleHandGesture : MonoBehaviour
{
    private float prevWristX = 0;
    public float swipeSensitivity = 0.01f; // 감도 매우 낮게 설정

    public void OnHandUpdate(NormalizedLandmarkList landmarkList)
    {
        // 1. 데이터가 오는지 확인 (이 로그가 안 뜨면 연결 문제 100%)
        // Debug.Log("데이터 수신 확인!"); 

        if (landmarkList == null || landmarkList.Landmark.Count == 0) return;

        float currentWristX = landmarkList.Landmark[0].X;

        // 2. 현재 X좌표를 무조건 출력해보세요.
        // 이 숫자가 콘솔에 미친듯이 올라와야 정상입니다.
        Debug.Log($"현재 X좌표: {currentWristX}");

        float movement = currentWristX - prevWristX;

        if (movement > swipeSensitivity)
        {
            Debug.Log("!!! 1 (제스처 성공) !!!");
        }

        prevWristX = currentWristX;
    }
}
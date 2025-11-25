// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System; // Action을 사용하기 위해 추가
using System.Collections;
using System.Collections.Concurrent; // ConcurrentQueue를 사용하기 위해 추가
using System.Collections.Generic;
using Mediapipe;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
    public class HandLandmarkerRunner : VisionTaskApiRunner<HandLandmarker>
    {
        [SerializeField] private HandLandmarkerResultAnnotationController _handLandmarkerResultAnnotationController;

        public UnityEvent OnSwipeGesture;
        public UnityEvent OnFistGesture;

        // 메인 스레드에서 실행할 작업을 저장하는 큐
        private readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

        private Experimental.TextureFramePool _textureFramePool;

        public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig();

        // =================================================================
        // [변수] 감도 및 상태 저장
        // =================================================================
        private float _prevWristX = 0f;
        public float _swipeHandSizeRatio = 0.08f;

        // 2. 흔들기(Swipe) 관련 (System.DateTime 사용)
        private long _lastSwipeTimeTicks = 0;    // 마지막 흔들기 시간 (Ticks 단위)
        private long _swipeCooldownTicks = 5000000; // 0.5초 = 5,000,000 Ticks

        // 3. 주먹(Fist) 관련 (System.DateTime 사용)
        private long _fistEnterTimeTicks = 0;    // 주먹 쥐기 시작한 시간
        private long _fistHoldThresholdTicks = 2000000; // 0.1초 = 1,000,000 Ticks
        private bool _isFistState = false;
        // =================================================================

        private void Update()
        {
            // 큐에 작업이 있으면 하나씩 꺼내서 메인 스레드에서 실행
            while (_mainThreadActions.TryDequeue(out var action))
            {
                action.Invoke();
            }
        }

        public override void Stop()
        {
            base.Stop();
            _textureFramePool?.Dispose();
            _textureFramePool = null;
        }

        protected override IEnumerator Run()
        {
            // 초기화 로그
            Debug.Log($"Delegate = {config.Delegate}");
            Debug.Log($"Image Read Mode = {config.ImageReadMode}");
            Debug.Log($"Running Mode = {config.RunningMode}");
            Debug.Log($"NumHands = {config.NumHands}");
            Debug.Log($"MinHandDetectionConfidence = {config.MinHandDetectionConfidence}");
            Debug.Log($"MinHandPresenceConfidence = {config.MinHandPresenceConfidence}");
            Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");

            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            var options = config.GetHandLandmarkerOptions(config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnHandLandmarkDetectionOutput : null);
            taskApi = HandLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
            var imageSource = ImageSourceProvider.ImageSource;

            yield return imageSource.Play();

            if (!imageSource.isPrepared)
            {
                Debug.LogError("Failed to start ImageSource, exiting...");
                yield break;
            }

            _textureFramePool = new Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);
            screen.Initialize(imageSource);
            SetupAnnotationController(_handLandmarkerResultAnnotationController, imageSource);

            var transformationOptions = imageSource.GetTransformationOptions();
            var flipHorizontally = transformationOptions.flipHorizontally;
            var flipVertically = transformationOptions.flipVertically;
            var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: (int)transformationOptions.rotationAngle);

            AsyncGPUReadbackRequest req = default;
            var waitUntilReqDone = new WaitUntil(() => req.done);
            var waitForEndOfFrame = new WaitForEndOfFrame();
            var result = HandLandmarkerResult.Alloc(options.numHands);

            var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
            using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

            while (true)
            {
                if (isPaused) yield return new WaitWhile(() => isPaused);

                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                Mediapipe.Image image;
                switch (config.ImageReadMode)
                {
                    case ImageReadMode.GPU:
                        if (!canUseGpuImage) throw new System.Exception("ImageReadMode.GPU is not supported");
                        textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        image = textureFrame.BuildGPUImage(glContext);
                        yield return waitForEndOfFrame;
                        break;
                    case ImageReadMode.CPU:
                        yield return waitForEndOfFrame;
                        textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        image = textureFrame.BuildCPUImage();
                        textureFrame.Release();
                        break;
                    case ImageReadMode.CPUAsync:
                    default:
                        req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        yield return waitUntilReqDone;
                        if (req.hasError) { Debug.LogWarning($"Failed to read texture"); continue; }
                        image = textureFrame.BuildCPUImage();
                        textureFrame.Release();
                        break;
                }

                switch (taskApi.runningMode)
                {
                    case Tasks.Vision.Core.RunningMode.IMAGE:
                        if (taskApi.TryDetect(image, imageProcessingOptions, ref result)) _handLandmarkerResultAnnotationController.DrawNow(result);
                        else _handLandmarkerResultAnnotationController.DrawNow(default);
                        break;
                    case Tasks.Vision.Core.RunningMode.VIDEO:
                        if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result)) _handLandmarkerResultAnnotationController.DrawNow(result);
                        else _handLandmarkerResultAnnotationController.DrawNow(default);
                        break;
                    case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
                        taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
                        break;
                }
            }
        }

        private void OnHandLandmarkDetectionOutput(HandLandmarkerResult result, Mediapipe.Image image, long timestamp)
        {
            _handLandmarkerResultAnnotationController.DrawLater(result);

            if (result.handLandmarks != null && result.handLandmarks.Count > 0)
            {
                var firstHandWrapper = result.handLandmarks[0];

                if (firstHandWrapper.landmarks != null && firstHandWrapper.landmarks.Count > 0)
                {
                    var landmarks = firstHandWrapper.landmarks;
                    var wrist = landmarks[0];

                    long currentTicks = System.DateTime.Now.Ticks;

                    // ----------------------------------------------------------------
                    // 1. 주먹 감지 (기능: 떨림 방지 Debounce 적용)
                    // ----------------------------------------------------------------

                    // (거리 계산 로직은 동일)
                    float distTip8 = (landmarks[8].x - wrist.x) * (landmarks[8].x - wrist.x) + (landmarks[8].y - wrist.y) * (landmarks[8].y - wrist.y);
                    float distPip6 = (landmarks[6].x - wrist.x) * (landmarks[6].x - wrist.x) + (landmarks[6].y - wrist.y) * (landmarks[6].y - wrist.y);
                    bool isIndexFolded = distTip8 < distPip6;

                    float distTip12 = (landmarks[12].x - wrist.x) * (landmarks[12].x - wrist.x) + (landmarks[12].y - wrist.y) * (landmarks[12].y - wrist.y);
                    float distPip10 = (landmarks[10].x - wrist.x) * (landmarks[10].x - wrist.x) + (landmarks[10].y - wrist.y) * (landmarks[10].y - wrist.y);
                    bool isMiddleFolded = distTip12 < distPip10;

                    float distTip16 = (landmarks[16].x - wrist.x) * (landmarks[16].x - wrist.x) + (landmarks[16].y - wrist.y) * (landmarks[16].y - wrist.y);
                    float distPip14 = (landmarks[14].x - wrist.x) * (landmarks[14].x - wrist.x) + (landmarks[14].y - wrist.y) * (landmarks[14].y - wrist.y);
                    bool isRingFolded = distTip16 < distPip14;

                    float distTip20 = (landmarks[20].x - wrist.x) * (landmarks[20].x - wrist.x) + (landmarks[20].y - wrist.y) * (landmarks[20].y - wrist.y);
                    float distPip18 = (landmarks[18].x - wrist.x) * (landmarks[18].x - wrist.x) + (landmarks[18].y - wrist.y) * (landmarks[18].y - wrist.y);
                    bool isPinkyFolded = distTip20 < distPip18;

                    // 현재 프레임 기준 주먹 여부
                    bool isCurrentFist = isIndexFolded && isMiddleFolded && isRingFolded && isPinkyFolded;

                    // [떨림 방지 로직 - 시간 비교 방식 변경]
                    if (isCurrentFist)
                    {
                        // 주먹을 처음 쥐었다면 시작 시간 기록
                        if (_fistEnterTimeTicks == 0)
                        {
                            _fistEnterTimeTicks = currentTicks;
                        }

                        // 현재 시간 - 시작 시간 > 0.1초(1,000,000 Ticks)
                        if (currentTicks - _fistEnterTimeTicks > _fistHoldThresholdTicks)
                        {
                            if (!_isFistState)
                            {
                                Debug.Log("0 (주먹 확정)");
                                // 직접 호출하는 대신, 메인 스레드 큐에 작업을 추가
                                _mainThreadActions.Enqueue(() => OnFistGesture?.Invoke());
                                _isFistState = true;
                            }
                        }
                    }
                    else
                    {
                        // 주먹을 펴면 시간 초기화
                        _fistEnterTimeTicks = 0;

                        if (_isFistState)
                        {
                            _isFistState = false;
                            // Debug.Log("주먹 해제");
                        }
                    }

                    // ----------------------------------------------------------------
                    // [중요] 차단 로직: 주먹 상태라면 스와이프 계산 안 함
                    // ----------------------------------------------------------------
                    var currentWristX = wrist.x;

                    if (_isFistState)
                    {
                        // 주먹 쥔 채로 이동해도 위치는 계속 갱신해줘야
                        // 주먹을 풀었을 때 스와이프로 오인식되지 않음.
                        _prevWristX = currentWristX;
                        return; // 여기서 함수 강제 종료!
                    }


                    // ----------------------------------------------------------------
                    // 2. 흔들기 감지 (동적 감도 적용 Dynamic Threshold)
                    // ----------------------------------------------------------------

                    // A. 손 크기 측정 (손목(0) <-> 중지 뿌리(9))
                    // Sqrt를 써서 실제 거리를 구합니다.
                    float handSize = Mathf.Sqrt(
                        (landmarks[9].x - wrist.x) * (landmarks[9].x - wrist.x) +
                        (landmarks[9].y - wrist.y) * (landmarks[9].y - wrist.y)
                    );

                    // [수정 후] 비율대로 계산하되, 최소 0.08 (혹은 0.008) 밑으로는 내려가지 않게 방어
                    // 만약 의도가 0.008이었다면 0.08f 자리에 0.008f를 넣으세요.
                    float dynamicThreshold = Mathf.Max(handSize * _swipeHandSizeRatio, 0.014f);

                    // (디버깅용: 감도가 어떻게 변하는지 궁금하면 주석 풀고 확인)
                    // Debug.Log($"손크기: {handSize:F4} / 감도: {dynamicThreshold:F4}");

                    float movement = currentWristX - _prevWristX;

                    // C. 동적 감도와 비교
                    if (movement > dynamicThreshold && (currentTicks - _lastSwipeTimeTicks > _swipeCooldownTicks))
                    {
                        Debug.Log($"1 (스와이프 성공 - 감도: {dynamicThreshold:F4})");
                        // 직접 호출하는 대신, 메인 스레드 큐에 작업을 추가
                        _mainThreadActions.Enqueue(() => OnSwipeGesture?.Invoke());
                        _lastSwipeTimeTicks = currentTicks;
                    }

                    _prevWristX = currentWristX;
                }
            }
        }
    }
}
import sys
import cv2
from ultralytics import YOLO

def detect_split_regions(image_path):
    # 1. YOLO11n 모델 로드 (처음 실행 시 자동으로 다운로드됨)
    model = YOLO("yolo11n.pt") 
    
    # 2. 이미지 읽기 (OpenCV 사용)
    img = cv2.imread(image_path)
    
    if img is None:
        print("0") # 이미지가 없으면 0 리턴
        return

    # 3. 이미지 반으로 자르기 (Slicing)
    # img.shape는 (높이, 너비, 채널) 순서입니다.
    height, width, _ = img.shape
    mid_point = width // 2 # 1280 / 2 = 640

    # :mid_point -> 0부터 640까지 (왼쪽 O영역)
    img_o = img[:, :mid_point] 
    # mid_point: -> 640부터 1280까지 (오른쪽 X영역)
    img_x = img[:, mid_point:]

    # 4. 각각 YOLO 돌리기
    # (verbose=False로 로그 끄기 필수)
    results_o = model.predict(img_o, verbose=False)
    results_x = model.predict(img_x, verbose=False)
    
    count_o = 0
    count_x = 0
    
    # 5. O영역 인원수 세기
    for result in results_o:
        for cls_id in result.boxes.cls.cpu().numpy():
            if int(cls_id) == 0: count_o += 1
            
    # 6. X영역 인원수 세기
    for result in results_x:
        for cls_id in result.boxes.cls.cpu().numpy():
            if int(cls_id) == 0: count_x += 1

    # 7. 결과 인코딩 (x100 기법)
    # 예: 왼쪽 3명, 오른쪽 2명 -> 302
    final_code = (count_o * 100) + count_x
                
    # 4. 유니티로 결과 전송 (print문 사용)
    print(final_code)

if __name__ == "__main__":
    # 유니티에서 보낸 이미지 경로 받기
    if len(sys.argv) > 1:
        target_image = sys.argv[1]
        detect_split_regions(target_image)
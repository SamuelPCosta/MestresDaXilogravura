import cv2
import numpy as np
import socket
import time
import json
import base64

threshold_brilho = 250
udp_ip = "127.0.0.1"
udp_port = 8764

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

aruco_dict = cv2.aruco.getPredefinedDictionary(cv2.aruco.DICT_4X4_50)
detector = cv2.aruco.ArucoDetector(aruco_dict, cv2.aruco.DetectorParameters())

# Parametros da cam
camera_matrix = np.array([[800, 0, 320], [0, 800, 240], [0, 0, 1]], dtype=np.float32)
dist_coeffs = np.zeros((4, 1))

cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)
# cap.set(cv2.CAP_PROP_FRAME_WIDTH, 960)
# cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 540)
# cap.set(cv2.CAP_PROP_FPS, 24)

tvec_history = []
trail = []

SMOOTHING_FACTOR = 5

def makePreviewFrame(frame, target_height=240):
    
    target_width = int(target_height * (16/9))
    resized = cv2.resize(frame, (target_width, target_height))
    
    _, buffer = cv2.imencode('.jpg', resized, [int(cv2.IMWRITE_JPEG_QUALITY), 70])
    return base64.b64encode(buffer).decode('utf-8')

try:
    print("############## OpenCV Started ##############")
    TARGET_FPS = 10  # Define a taxa desejada
    FRAME_TIME = 1.0 / TARGET_FPS  # Tempo entre frames
    while True:
        start_time = time.time()
        ret, frame = cap.read()
        if not ret:
            print("Error on capture frame")
            continue

        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)

        # Deteccao do LED
        _, thresh = cv2.threshold(gray, threshold_brilho, 255, cv2.THRESH_BINARY)
        led_aceso = np.sum(thresh == 255) > 600
        
        # Processa apenas o PRIMEIRO marcador (se existir)
        corners, ids, _ = detector.detectMarkers(gray)
        
        data = {}
        if ids is not None and len(ids) > 0:
            first_corner = corners[0].reshape(-1, 2)
            first_id = int(ids[0][0])
            object_points = np.array([[0, 0, 0], [1, 0, 0], [1, 1, 0], [0, 1, 0]], dtype=np.float32)
            success, rvec, tvec = cv2.solvePnP(
                object_points,
                first_corner,
                camera_matrix,
                dist_coeffs
            )

            center_x = int(np.mean(first_corner[:, 0]))
            center_y = int(np.mean(first_corner[:, 1]))
            
            if success:
                tvec = tvec.flatten()
                tvec_history.append(tvec)

                if len(tvec_history) > SMOOTHING_FACTOR:
                    tvec_history.pop(0)

                smoothed_tvec = np.mean(tvec_history, axis=0)

                center_x = int(np.mean(first_corner[:, 0]))
                center_y = int(np.mean(first_corner[:, 1]))

                trail.append((center_x, center_y))
                if len(trail) > 10:
                    trail.pop(0)

                frameTrail = frame.copy()
                frameTrail = (frameTrail * 0.4).astype("uint8")
                for i in range(1, len(trail)):
                    cv2.line(frameTrail, trail[i - 1], trail[i], (0, 255, 0), 10)
                    
                cv2.aruco.drawDetectedMarkers(frame, [corners[0]], np.array([[first_id]]))
                cv2.drawFrameAxes(frame, camera_matrix, dist_coeffs, rvec, tvec, 0.03)
                framePrev = makePreviewFrame(frameTrail)
                data = {
                    "led": "true" if led_aceso else "false",
                    "id": first_id,
                    "position": smoothed_tvec.tolist(),
                    "rotation": [
                        float(rvec[0][0]),
                        float(rvec[1][0]),
                        float(rvec[2][0])
                    ],
                    "frame": framePrev
                }
        else:
            trail.clear()
            framePrev = makePreviewFrame(frame)
            data = {
                "led": "true" if led_aceso else "false",
                "id": -1,
                "frame": framePrev
            }
                
        json_data = json.dumps(data)
        sock.sendto(json_data.encode(), (udp_ip, udp_port))

        elapsed_time = time.time() - start_time
        sleep_time = max(0, FRAME_TIME - elapsed_time)
        time.sleep(sleep_time)

except Exception as e:
    print(f"Error: {e}")
finally:
    cap.release()
    cv2.destroyAllWindows()
    sock.close()
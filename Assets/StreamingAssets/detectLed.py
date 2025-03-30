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
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 960)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 540)
cap.set(cv2.CAP_PROP_FPS, 24)

def resize_frame(frame, target_height=240):
    
    target_width = int(target_height * (16/9))
    resized = cv2.resize(frame, (target_width, target_height))
    
    _, buffer = cv2.imencode('.jpg', resized, [int(cv2.IMWRITE_JPEG_QUALITY), 70])
    return base64.b64encode(buffer).decode('utf-8')

try:
    print("############## OpenCV Started ##############")
    while True:
        ret, frame = cap.read()
        if not ret:
            print("Error on capture frame")
            continue

        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)

        # Deteccao do LED
        # oldStatus = locals().get("led_aceso", False)
        _, thresh = cv2.threshold(gray, threshold_brilho, 255, cv2.THRESH_BINARY)
        led_aceso = np.sum(thresh == 255) > 500
        
        # if oldStatus != led_aceso:
        #     print(led_aceso)
            
        # if led_aceso == False:
        #     data = {
        #         "led": "true" if led_aceso else "false"
        #     }
        #     json_data = json.dumps(data)
        #     sock.sendto(json_data.encode(), (udp_ip, udp_port))
        #     # print(f"Enviado: {json_data}")
        #     continue
        
        
        # Processa apenas o PRIMEIRO marcador (se existir)
        corners, ids, _ = detector.detectMarkers(gray)
        
        
        framePrev = resize_frame(frame)
        data = {
            "led": "true" if led_aceso else "false",
            "id": -1,
            "frame": framePrev
        }
        
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

            if success:
                cv2.aruco.drawDetectedMarkers(frame, [corners[0]], np.array([[first_id]]))
                cv2.drawFrameAxes(frame, camera_matrix, dist_coeffs, rvec, tvec, 0.03)
                data = {
                    "led": "true" if led_aceso else "false",
                    "id": first_id,
                    "position": [
                        float(tvec[0][0]), # X
                        float(tvec[1][0]), # Y
                        float(tvec[2][0])  # Z
                    ],
                    "rotation": [
                        float(rvec[0][0]), #X (em graus)
                        float(rvec[1][0]), #Y (em graus)
                        float(rvec[2][0])  #Z (em graus)
                    ],
                    "frame": framePrev
                }
                
        json_data = json.dumps(data)
        sock.sendto(json_data.encode(), (udp_ip, udp_port))
        #print(f"Enviado: {json_data}")
        # Mostra a imagem
        # cv2.imshow("ArUco Detection", frame)
        # if cv2.waitKey(1) & 0xFF == ord('q'):
        #     break

        time.sleep(0.1)

except Exception as e:
    print(f"Error: {e}")
finally:
    cap.release()
    cv2.destroyAllWindows()
    sock.close()
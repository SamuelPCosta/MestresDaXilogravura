import cv2
import numpy as np
import socket
import time

threshold_brilho = 250
udp_ip = "127.0.0.1"
udp_port = 8764

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

cap = cv2.VideoCapture(0)

try:
    print("############## Detection Started ##############")
    while True:
        ret, frame = cap.read()
        if not ret:
            print("Erro ao capturar frame")
            continue

        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        _, thresh = cv2.threshold(gray, 250, 255, cv2.THRESH_BINARY)
        brilho_detectado = np.sum(thresh == 255)

        oldStatus = locals().get("led_aceso", False)
        led_aceso = brilho_detectado > 500

        if oldStatus != led_aceso:
            print(led_aceso)

        sock.sendto(str(led_aceso).encode(), (udp_ip, udp_port))
        time.sleep(0.1)

except Exception as e:
    print(f"Erro: {e}")
finally:
    cap.release()
    cv2.destroyAllWindows()
    sock.close()

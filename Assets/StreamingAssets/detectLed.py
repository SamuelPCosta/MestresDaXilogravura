import cv2
import numpy as np
import asyncio
import websockets
import base64

threshold_brilho = 250

async def process_frame(websocket):
    print("############## Detection Started ##############")
    cap = cv2.VideoCapture(0)
    try:
        while True:
            ret, frame = cap.read()
            if not ret:
                print("Erro ao capturar frame")
                continue

            # Processa o frame para detectar o LED
            gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
            _, thresh = cv2.threshold(gray, 250, 255, cv2.THRESH_BINARY)
            brilho_detectado = np.sum(thresh == 255)
            oldStatus = led_aceso if 'led_aceso' in locals() else False
            led_aceso = brilho_detectado > 500

            if oldStatus is not led_aceso:
                print(led_aceso)
                
            await websocket.send(str(led_aceso).lower().strip())
            await asyncio.sleep(0.1)

    except Exception as e:
        print(f"Erro: {e}")
    finally:
        cap.release()
        cv2.destroyAllWindows()

async def main():
    print("############## System started #############")
    while True:
        try:
            server = await websockets.serve(process_frame, "localhost", 8764)
            print("Servidor WebSocket iniciado")
            await server.wait_closed()
        except OSError as e:
            print(f"Erro: {e}. Tentando novamente em 5 segundos...")
            await asyncio.sleep(5)

asyncio.run(main())

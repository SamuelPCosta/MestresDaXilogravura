import cv2
import numpy as np
import asyncio
import websockets
import base64

threshold_brilho = 250

async def process_frame(websocket):
    print("############## Vuforia started ##############")
    try:
        while True:
            frame_data = await websocket.recv()

            if not frame_data:
                continue

            # Decode the base64 data to get raw image bytes
            try:
                frame_data = base64.b64decode(frame_data)
            except Exception as e:
                print(f"Erro ao decodificar o frame: {e}")
                continue

            # Convert the raw bytes to a numpy array and decode to an image
            np_arr = np.frombuffer(frame_data, dtype=np.uint8)
            frame = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)

            if frame is None:  # Check if the frame is valid
                print("Frame inválido")
                continue

            # Process the frame as before
            gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
            _, thresh = cv2.threshold(gray, threshold_brilho, 255, cv2.THRESH_BINARY)
            brilho_detectado = np.sum(thresh == 255)
            
            oldStatus = led_aceso if 'led_aceso' in locals() else False
            led_aceso = brilho_detectado > 500

            status = "LED ACESO" if led_aceso else "LED APAGADO"
            #cv2.putText(frame, status, (10, 50), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)

            if oldStatus is not led_aceso:
                print(led_aceso)
            # cv2.imshow("Camera", frame)
            # cv2.imshow("Threshold", thresh)
            cv2.waitKey(1)

            response = str(led_aceso)
            await websocket.send(response)

    except Exception as e:
        print(f"Erro: {e}")

async def main():
    print("############## System started #############")
    async with websockets.serve(process_frame, "localhost", 8765):
        await asyncio.Future()

asyncio.run(main())

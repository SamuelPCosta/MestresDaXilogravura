using System;
using UnityEngine;
using Vuforia;
using WebSocketSharp;

public class VuforiaWebSocketSender : MonoBehaviour
{
    private bool ledStatus = false;
    private WebSocket ws;
    private PixelFormat pixelFormat = PixelFormat.RGB888; // Or GRAYSCALE8, etc.

    void Start()
    {
        VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;

        ws = new WebSocket("ws://localhost:8765");
        ws.OnOpen += (sender, e) => Debug.Log("WebSocket Connected");
        ws.OnError += (sender, e) => Debug.LogError("WebSocket Error: " + e.Message);
        ws.OnClose += (sender, e) => Debug.Log("WebSocket Closed");
        ws.OnMessage += OnMessageReceived;

        ws.ConnectAsync();
    }

    private void OnVuforiaStarted()
    {
        VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(pixelFormat, true);
    }

    void Update()
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            Vuforia.Image image = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(pixelFormat);
            if (image != null)
            {
                SendImageToPython(image);
            }
        }

    }

    void SendImageToPython(Vuforia.Image image)
    {
        if (image == null || image.Pixels == null || image.Pixels.Length == 0) return;

        try
        {
            Texture2D texture = new Texture2D(image.Width, image.Height, TextureFormat.RGB24, false);
            Color32[] pixels = ConvertToColor32(image);
            texture.SetPixels32(pixels);
            texture.Apply();

            Texture2D resizedTexture = ResizeTexture(texture, 320, 240);
            byte[] resizedImageData = resizedTexture.EncodeToJPG();
            string base64Image = Convert.ToBase64String(resizedImageData);
            ws.Send(base64Image);
            Debug.Log("Image data sent.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error sending image data: " + ex.Message);
        }
    }

    Color32[] ConvertToColor32(Vuforia.Image image)
    {
        byte[] pixels = image.Pixels;
        Color32[] colors = new Color32[image.Width * image.Height];

        for (int i = 0; i < colors.Length; i++)
        {
            byte r = pixels[i * 3];
            byte g = pixels[i * 3 + 1];
            byte b = pixels[i * 3 + 2];
            colors[i] = new Color32(r, g, b, 255);
        }
        return colors;
    }

    Texture2D ResizeTexture(Texture2D original, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 24);
        RenderTexture.active = rt;
        Graphics.Blit(original, rt);
        Texture2D resized = new Texture2D(width, height, TextureFormat.RGB24, false);
        resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resized.Apply();
        return resized;
    }

    void OnMessageReceived(object sender, MessageEventArgs e)
    {
        ledStatus = e.Data.Trim().ToLower() == "true";
        Debug.Log("LED Status: " + ledStatus);
    }

    void OnDestroy()
    {
        if (ws != null)
        {
            ws.Close();
        }
    }
}
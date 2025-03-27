using UnityEngine;
using Vuforia;

public class LedDetector : MonoBehaviour
{
    private PixelFormat pixelFormat = PixelFormat.GRAYSCALE;
    [SerializeField][Range(0, 255)] private int ledDetectionThreshold = 180;
    [SerializeField][Range(0, 150)] private int minPixels = 50;

    private int width;
    private int height;
    private int centerX;
    private int centerY;
    private int areaSize;

    void Start()
    {
        VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;
    }

    private void OnVuforiaStarted()
    {
        VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(pixelFormat, true);
        Vuforia.Image image = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(pixelFormat);
        if (image != null)
        {
            width = image.Width;
            height = image.Height;
            centerX = width / 2;
            centerY = height / 2;
            areaSize = width * height;
        }
    }

    void Update()
    {
        Vuforia.Image image = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(pixelFormat);
        if (image != null)
        {
            DetectLed(image);
        }
    }

    void DetectLed(Vuforia.Image image)
    {
        if (image != null)
        {
            width = image.Width;
            height = image.Height;
            centerX = width / 2;
            centerY = height / 2;
            areaSize = width * height;
        }
        byte[] pixels = image.Pixels;
        int totalIntensity = 0;

        int count = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixelFormat == PixelFormat.GRAYSCALE)
                {
                    int intensity = pixels[y * width + x];
                    if (intensity > ledDetectionThreshold)
                    {
                        count++;   
                    }
                    //int index = (y * width + x) * 3;
                    //int mediaRGB = (pixels[index] + pixels[index + 1] + pixels[index + 2]) / 3;
                    //totalIntensity += mediaRGB; // Média dos canais RGB
                    //if(mediaRGB > ledDetectionThreshold)
                    //    Debug.Log("LED aceso detectado!");
                }
            }
        }

        int averageIntensity = totalIntensity / areaSize;
        if (count > minPixels) { 
            Debug.Log("LED aceso detectado!");
            return;
        }
    }
}

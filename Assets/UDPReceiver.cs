using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;
using System.Collections.Generic;

public enum Tools
{
    PENCIL,
    GOUGE
};

public class UDPReceiver : MonoBehaviour
{
    UdpClient udpClient;
    Thread receiveThread;
    public RenderTexture displayTarget;
    private Texture2D _receivedTexture;
    public bool ledStatus;
    public Transform testObj;
    private Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    private float _timer = 0f;
    private const float TargetFPS = 20f; //20 verificacoes dos dados por segundo
    private const float FrameTime = 1f / TargetFPS;

    void Start()
    {
        udpClient = new UdpClient(8764);
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
        _receivedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
    }

    void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= FrameTime)
        {
            while (mainThreadActions.Count > 0)
            {
                mainThreadActions.Dequeue().Invoke();
            }
            _timer -= FrameTime;
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 8764);
        while (true)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string receivedData = Encoding.UTF8.GetString(data).Trim();
                try
                {
                    var jsonData = JsonUtility.FromJson<UDPData>(receivedData);
                    ledStatus = jsonData.led == "true";

                    if (ledStatus)
                    {
                        Debug.Log(jsonData.id);
                    }

                    if (jsonData.id >= 0)
                    {
                        if (jsonData.position != null && jsonData.position.Count >= 3 &&
                            jsonData.rotation != null && jsonData.rotation.Count >= 3)
                        {
                            mainThreadActions.Enqueue(() => {
                                UpdateTransform(jsonData);
                            });
                        }
                    }

                    if (!string.IsNullOrEmpty(jsonData.frame))
                    {
                        byte[] imageBytes = Convert.FromBase64String(jsonData.frame);
                        mainThreadActions.Enqueue(() => {
                            if (_receivedTexture.width != 2 || _receivedTexture.height != 2)
                            {
                                _receivedTexture.Reinitialize(2, 2);
                            }
                            _receivedTexture.LoadImage(imageBytes);
                            Graphics.Blit(_receivedTexture, displayTarget);
                        });
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Erro ao processar JSON: " + e.Message);
                }
            }
            catch (SocketException) { }
        }
    }

    private void UpdateTransform(UDPData data)
    {
        float influency = -0.25f;
        float influencyRoatation = 58f;
        float lerpFactor = 0.85f;

        Vector3 targetPosition = new Vector3(
            data.position[0],
            data.position[1],
            data.position[2]
        ) * influency;

        testObj.localPosition = Vector3.Lerp(
            testObj.localPosition,
            targetPosition,
            lerpFactor
        );

        Quaternion targetRotation = Quaternion.Euler(
            data.rotation[0] * influencyRoatation,
            data.rotation[1] * influencyRoatation,
            data.rotation[2] * influencyRoatation
        );

        testObj.localRotation = Quaternion.Lerp(
            testObj.localRotation,
            targetRotation,
            lerpFactor
        );
    }

    void OnDestroy()
    {
        receiveThread?.Abort();
        udpClient?.Close();
    }
}

using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;
using System.Collections.Generic;

public class UDPReceiver : MonoBehaviour
{
    UdpClient udpClient;
    Thread receiveThread;
    public RenderTexture displayTarget;
    private Texture2D _receivedTexture;
    public bool ledStatus;
    public bool isCursor = false;
    public GameObject ledIndicator;
    [SerializeField] private Tool tool;

    //######################
    private Transform currentTool;
    private Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    private float _timer = 0f;
    private const float TargetFPS = 16f; //16 verificacoes dos dados por segundo (antes 20)
    private const float FrameTime = 1f / TargetFPS;

    void Start()
    {
        udpClient = new UdpClient(8763);
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
        _receivedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        ledIndicator.SetActive(false);
    }

    private UDPData pendingData;
    void FixedUpdate()
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
        if (pendingData != null)
        {
            tool.checkTool(pendingData, isCursor);
            showFrame(pendingData);
            pendingData = null;
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 8764);
        while (true){
            try{
                byte[] data = udpClient.Receive(ref remoteEP);
                string receivedData = Encoding.UTF8.GetString(data).Trim();
                try
                {
                    var jsonData = JsonUtility.FromJson<UDPData>(receivedData);
                    ledStatus = jsonData.led == "true";
                    //if (ledStatus) Debug.Log(jsonData.id);

                    pendingData = jsonData;
                }
                catch (Exception e){
                    Debug.LogError("Erro ao processar JSON: " + e.Message);
                }
            }
            catch (SocketException) { }
        }
    }

    private void showFrame(UDPData jsonData)
    {
        if (!string.IsNullOrEmpty(jsonData.frame)){
            byte[] imageBytes = Convert.FromBase64String(jsonData.frame);
            mainThreadActions.Enqueue(() => {
                if (_receivedTexture.width != 2 || _receivedTexture.height != 2)
                    _receivedTexture.Reinitialize(2, 2);
                _receivedTexture.LoadImage(imageBytes);
                Graphics.Blit(_receivedTexture, displayTarget);
                ledIndicator.SetActive(ledStatus);
            });
        }
    }

    void OnDestroy()
    {
        receiveThread?.Abort();
        udpClient?.Close();
    }
}

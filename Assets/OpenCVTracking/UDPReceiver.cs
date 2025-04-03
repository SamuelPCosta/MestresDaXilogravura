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
    public ProjectionMode mode;
    public RenderTexture displayTarget;
    private Texture2D _receivedTexture;
    public bool ledStatus;
    public bool isCursor = false;
    public Transform pointer;
    public Transform[] tools;
    public Transform[] boardTools;
    public GameObject ledIndicator;
    private Transform currentTool;
    private Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    private float _timer = 0f;
    private const float TargetFPS = 16f; //16 verificacoes dos dados por segundo (antes 20)
    private const float FrameTime = 1f / TargetFPS;

    private const int POINTER_ID = 10;
    private bool stabilize = false;

    void Start()
    {
        udpClient = new UdpClient(8764);
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
        _receivedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        ledIndicator.SetActive(false);
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
        while (true){
            try{
                byte[] data = udpClient.Receive(ref remoteEP);
                string receivedData = Encoding.UTF8.GetString(data).Trim();
                try
                {
                    var jsonData = JsonUtility.FromJson<UDPData>(receivedData);
                    ledStatus = jsonData.led == "true";
                    if (ledStatus) Debug.Log(jsonData.id);

                    checkTool(jsonData);
                    showFrame(jsonData);
                }
                catch (Exception e){
                    Debug.LogError("Erro ao processar JSON: " + e.Message);
                }
            }
            catch (SocketException) { }
        }
    }

    private bool resetCursor = false;
    private void checkTool(UDPData jsonData)
    {
        if (jsonData.id == POINTER_ID)
        {
            mainThreadActions.Enqueue(() => {
                pointer.gameObject.SetActive(true);
                foreach (Transform t in tools)
                    t.gameObject.SetActive(false);
                foreach (Transform t in boardTools)
                    t.gameObject.SetActive(true);

                currentTool = pointer;
                isCursor = true;
                if (!resetCursor)
                {
                    resetCursor = true;
                    pointer.GetComponent<TrackingBtnController>().resetClick();
                }
                UpdateTransform(jsonData, true);
            });
        }
        else
        if (jsonData.id >= 0 && jsonData.id < 10)
        {
            if (jsonData.position != null && jsonData.position.Count >= 3 &&
                jsonData.rotation != null && jsonData.rotation.Count >= 3)
            {
                mainThreadActions.Enqueue(() => {
                    bool ret = UpdateTool(jsonData);
                    if (ret) { 
                        UpdateTransform(jsonData, false);
                        mode.setTool(tools[jsonData.id]);
                    }
                });
            }
        }
        else{
            mainThreadActions.Enqueue(() => {
                isCursor = false;
                pointer.gameObject.SetActive(false);

                foreach (Transform t in tools)
                    t.gameObject.SetActive(false);
                foreach (Transform t in boardTools)
                    t.gameObject.SetActive(true);
                currentTool = null;
                mode.resetTool();
                stabilize = false;
                resetCursor = false;
            });
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

    private bool UpdateTool(UDPData data)
    {
        for (int i = 0; i < tools.Length; i++) { 
            tools[i].gameObject.SetActive(data.id == i);
            if(data.id == i) { 
                currentTool = tools[i];
                boardTools[i].gameObject.SetActive(false);
            }
        }

        if (currentTool == null)
            return false;
        return true;
    }

    private void UpdateTransform(UDPData data, bool Movement2D)
    {
        float influency = -0.45f;
        float influencyRoatation = 58f;
        float lerpFactor = 0.85f;

        Vector3 targetPosition = new Vector3(
            data.position[0] * influency,
            data.position[1] * influency,
            !Movement2D ? data.position[2] * (influency/2) : 5f
        );
        
        Vector3 futurePositon = Vector3.Lerp(
            currentTool.localPosition,
            targetPosition,
            lerpFactor
        );

        currentTool.localPosition = futurePositon;

        if (Movement2D)
            return;

        Quaternion targetRotation = Quaternion.Euler(
            data.rotation[0] * influencyRoatation,
            data.rotation[1] * influencyRoatation,
            data.rotation[2] * influencyRoatation
        );

        currentTool.localRotation = Quaternion.Lerp(
            currentTool.localRotation,
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

using UnityEngine;
using WebSocketSharp;

public class WebSocketConnection : MonoBehaviour
{
    WebSocket ws;
    public bool ledStatus;

    void Start()
    {
        Invoke("ConnectWS", 1f);
    }

    public void ConnectWS()
    {
        ws = new WebSocket("ws://localhost:8764");
        ws.OnMessage += OnMessageReceived;
        ws.OnOpen += (sender, e) => {
            Debug.Log("WebSocket conectado com sucesso!");
        };
        ws.OnError += (sender, e) => {
            Debug.LogError("Erro na conex�o WebSocket: " + e.Message);
        };
        ws.Connect();
    }

    void OnMessageReceived(object sender, MessageEventArgs e)
    {
        string receivedData = e.Data.Trim().ToLower();
        ledStatus = receivedData == "true";
        Debug.LogError("LED Status: " + ledStatus);
    }

    void OnDestroy()
    {
        if (ws?.IsAlive == true) ws.Close();
    }
}
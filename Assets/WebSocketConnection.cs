using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UDPReceiver : MonoBehaviour
{
    UdpClient udpClient;
    Thread receiveThread;
    public bool ledStatus;

    void Start()
    {
        udpClient = new UdpClient(8764);
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 8764);
        while (true)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string receivedData = Encoding.UTF8.GetString(data).Trim().ToLower();
                ledStatus = receivedData == "true";
                Debug.Log("LED Status: " + ledStatus);
            }
            catch (SocketException) { }
        }
    }

    void OnDestroy()
    {
        receiveThread?.Abort();
        udpClient?.Close();
    }
}

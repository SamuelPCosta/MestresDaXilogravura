using UnityEngine;
using System.IO.Ports;
using System.Collections;
using System;

public class AutoConnectJoystick : MonoBehaviour
{
    private SerialPort serialPort;
    private bool isConnected = false;
    private float xAxisValue = 0f;

    void Start() => StartCoroutine(FindESP32());

    IEnumerator FindESP32(){
        while (!isConnected){
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports){
                bool success = false;
                try{
                    serialPort = new SerialPort(port, 115200);
                    serialPort.ReadTimeout = 500;
                    serialPort.Open();
                    success = true;
                }
                catch{
                    if (serialPort != null) serialPort.Dispose();
                }

                if (success){
                    yield return new WaitForSeconds(0.5f);
                    try{
                        if (serialPort.BytesToRead > 0){
                            string test = serialPort.ReadLine();
                            if (!string.IsNullOrEmpty(test)){
                                isConnected = true;
                                break;
                            }
                        }
                    }
                    catch { }

                    if (!isConnected){
                        serialPort.Close();
                        serialPort.Dispose();
                    }
                }
            }
            if (!isConnected) yield return new WaitForSeconds(1);
        }
    }

    void Update(){
        if (isConnected && serialPort != null && serialPort.IsOpen){
            try{
                string data = serialPort.ReadLine();
                xAxisValue = float.Parse(data, System.Globalization.CultureInfo.InvariantCulture);
                Debug.Log(xAxisValue);
            }
            catch (TimeoutException) { }
        }
    }

    void OnApplicationQuit(){
        if (serialPort != null && serialPort.IsOpen) serialPort.Close();
    }

    public float getXAxis() => xAxisValue;
}
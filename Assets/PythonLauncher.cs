using System.Diagnostics;
using UnityEngine;

public class PythonLauncher : MonoBehaviour
{
    private Process pythonProcess;

    void Start()
    {
        StartPythonScript();
    }

    void StartPythonScript()
    {
        pythonProcess = new Process();
        pythonProcess.StartInfo.FileName = "python";  //"python3" dependendo do sistema
        pythonProcess.StartInfo.Arguments = "detectLed.py";
        pythonProcess.StartInfo.UseShellExecute = false;
        pythonProcess.StartInfo.RedirectStandardOutput = true;
        pythonProcess.StartInfo.RedirectStandardError = true;
        pythonProcess.StartInfo.CreateNoWindow = true;

        pythonProcess.OutputDataReceived += (sender, e) => UnityEngine.Debug.Log("Python: " + e.Data);
        pythonProcess.ErrorDataReceived += (sender, e) => UnityEngine.Debug.LogError("Python Error: " + e.Data);

        pythonProcess.Start();
        pythonProcess.BeginOutputReadLine();
        pythonProcess.BeginErrorReadLine();
    }

    void OnApplicationQuit()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            pythonProcess.Dispose();
        }
    }
}

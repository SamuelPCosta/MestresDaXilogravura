using System.Diagnostics;
using UnityEngine;
using System.IO;

public class PythonLauncher : MonoBehaviour
{
    private Process process;

    void Start()
    {
        string pythonScriptPath = Path.Combine(Application.streamingAssetsPath, "detectLed.py");
        pythonScriptPath = "\"" + pythonScriptPath + "\"";

        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = "python";
        psi.Arguments = pythonScriptPath;
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.CreateNoWindow = true;

        process = new Process();
        process.StartInfo = psi;

        process.OutputDataReceived += (sender, args) => UnityEngine.Debug.Log("Python Output: " + args.Data);
        process.ErrorDataReceived += (sender, args) => UnityEngine.Debug.LogError("Python Error: " + args.Data);

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            UnityEngine.Debug.Log("Python script started successfully.");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("Error starting Python process: " + ex.Message);
        }
    }

    void OnApplicationQuit()
    {
        if (process != null && !process.HasExited)
        {
            UnityEngine.Debug.Log("Closing Python process...");
            process.Kill();
        }
    }
}

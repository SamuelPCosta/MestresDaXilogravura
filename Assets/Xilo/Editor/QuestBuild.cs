using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Xilo.EditorTools
{
    // One-click Quest APK into Builds/ (outside git) with the enabled build scenes. Writes a short report next to
    // the APK so the result can be read after the build finishes.
    static class QuestBuild
    {
        const string Folder = "Builds";

        [MenuItem("Xilo/Build/Quest APK (Development)")]
        static void BuildDevelopment() => Build(BuildOptions.Development, "dev");

        [MenuItem("Xilo/Build/Quest APK (Release)")]
        static void BuildRelease() => Build(BuildOptions.None, "release");

        static void Build(BuildOptions options, string suffix)
        {
            Directory.CreateDirectory(Folder);
            string reportPath = Path.Combine(Folder, "build-report.txt");
            string apk = Path.Combine(Folder, $"MestresDaXilogravura-{PlayerSettings.bundleVersion}-{suffix}.apk");
            string summary;
            try
            {
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                    locationPathName = apk,
                    target = BuildTarget.Android,
                    options = options,
                });
                summary = Describe(report);
            }
            catch (Exception exception)
            {
                summary = $"result Exception\n{exception}";
            }

            File.WriteAllText(reportPath, summary);
            Debug.Log($"[XiloVR][Build] {summary}");
        }

        static string Describe(BuildReport report)
        {
            BuildSummary s = report.summary;
            string text = $"result {s.result}\nerrors {s.totalErrors} warnings {s.totalWarnings}\n" +
                          $"size {s.totalSize / (1024f * 1024f):0.0} MB\ntime {s.totalTime}\npath {s.outputPath}\n";
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage message in step.messages.Where(m => m.type is LogType.Error or LogType.Exception))
                    text += $"ERROR [{step.name}] {message.content}\n";
            }
            return text;
        }
    }
}

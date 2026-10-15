using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// After an iPhone build, turns off Xcode's "User Script Sandboxing" in the generated Xcode
    /// project. Newer Xcode versions switch it on, and it stops Unity's IL2CPP build step from
    /// reading and writing its files ("Sandbox: ... deny file-write-create"), so the build fails.
    /// Runs by itself every time Unity builds for iOS; nothing to click.
    /// </summary>
    public class XcodeSandboxFix : IPostprocessBuildWithReport
    {
        public int callbackOrder => 1000;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
            string path = Path.Combine(report.summary.outputPath, "Unity-iPhone.xcodeproj", "project.pbxproj");
            if (!File.Exists(path))
            {
                Debug.LogWarning("Bubble Tea: couldn't find " + path + " to turn off Xcode's script sandboxing.");
                return;
            }

            string text = File.ReadAllText(path);
            text = text.Replace("ENABLE_USER_SCRIPT_SANDBOXING = YES;", "ENABLE_USER_SCRIPT_SANDBOXING = NO;");
            // Every build configuration gets the setting, so no target falls back to Xcode's default (on).
            var result = new System.Text.StringBuilder();
            int from = 0;
            const string marker = "buildSettings = {";
            while (true)
            {
                int at = text.IndexOf(marker, from, System.StringComparison.Ordinal);
                if (at < 0) break;
                int end = at + marker.Length;
                result.Append(text, from, end - from);
                int close = text.IndexOf("};", end, System.StringComparison.Ordinal);
                bool hasSetting = close > end && text.IndexOf("ENABLE_USER_SCRIPT_SANDBOXING", end, close - end, System.StringComparison.Ordinal) >= 0;
                if (!hasSetting) result.Append("\n\t\t\t\tENABLE_USER_SCRIPT_SANDBOXING = NO;");
                from = end;
            }
            result.Append(text, from, text.Length - from);
            File.WriteAllText(path, result.ToString());
            Debug.Log("Bubble Tea: turned off Xcode's User Script Sandboxing so the IL2CPP step can run.");
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// After an iPhone build, turns off two checks that newer Xcode versions switch on and that
    /// make Unity's Xcode project fail to build:
    ///  - User Script Sandboxing, which stops Unity's IL2CPP build step from reading and writing
    ///    its files ("Sandbox: ... deny file-write-create");
    ///  - the Module Verifier, which rejects UnityFramework's headers ("umbrella header for module
    ///    'UnityFramework' does not include header", "double-quoted include in framework header",
    ///    "could not build module 'Test'").
    /// Runs by itself every time Unity builds for iOS; nothing to click.
    /// </summary>
    public class XcodeSandboxFix : IPostprocessBuildWithReport
    {
        private static readonly string[] Settings = { "ENABLE_USER_SCRIPT_SANDBOXING", "ENABLE_MODULE_VERIFIER" };

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
            foreach (string setting in Settings) text = text.Replace(setting + " = YES;", setting + " = NO;");
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
                foreach (string setting in Settings)
                {
                    bool hasSetting = close > end && text.IndexOf(setting, end, close - end, System.StringComparison.Ordinal) >= 0;
                    if (!hasSetting) result.Append("\n\t\t\t\t" + setting + " = NO;");
                }
                from = end;
            }
            result.Append(text, from, text.Length - from);
            File.WriteAllText(path, result.ToString());
            Debug.Log("Bubble Tea: turned off Xcode's User Script Sandboxing and Module Verifier so the project builds.");
        }
    }
}

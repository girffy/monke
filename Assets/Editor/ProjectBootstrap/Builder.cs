using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ProjectBootstrap
{
    // CI/CLI build entry point. WebGL (and other non-desktop targets) have no
    // built-in `unity build` path, so this reads -buildOutput (and optionally
    // -buildTarget) from the command line and drives BuildPipeline directly.
    public static class Builder
    {
        public static void PerformBuild()
        {
            string outputPath = GetArg("-buildOutput");
            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("[Builder] -buildOutput not supplied.");
                EditorApplication.Exit(1);
                return;
            }

            var target = BuildTarget.WebGL;
            string targetArg = GetArg("-buildTargetName");
            if (!string.IsNullOrEmpty(targetArg) && Enum.TryParse(targetArg, out BuildTarget parsed))
            {
                target = parsed;
            }

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[Builder] No enabled scenes in Build Settings.");
                EditorApplication.Exit(1);
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Builder] Build succeeded: {report.summary.outputPath} ({report.summary.totalSize} bytes)");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[Builder] Build failed: {report.summary.result}, {report.summary.totalErrors} errors.");
                EditorApplication.Exit(1);
            }
        }

        static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace ProjectBootstrap
{
    // Enters Play mode, lets the game bootstrap run for a few seconds, then exits
    // and reports whether any runtime errors were logged — a quick headless sanity
    // check that doesn't require a human watching the Editor.
    public static class PlayModeSmokeTest
    {
        static double _stopTime;
        static bool _errorSeen;
        static string _firstError;

        public static void Run()
        {
            Application.logMessageReceived += OnLog;
            _stopTime = EditorApplication.timeSinceStartup + 6.0;
            EditorApplication.update += Poll;
            EditorApplication.EnterPlaymode();
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                _errorSeen = true;
                _firstError ??= condition + "\n" + stackTrace;
            }
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _stopTime) return;

            EditorApplication.update -= Poll;
            Application.logMessageReceived -= OnLog;

            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }

            if (_errorSeen)
            {
                Debug.LogError("[PlayModeSmokeTest] FAILED — runtime error detected:\n" + _firstError);
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("[PlayModeSmokeTest] PASSED — no runtime errors during play.");
                EditorApplication.Exit(0);
            }
        }
    }
}

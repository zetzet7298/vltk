using System;
using System.Threading.Tasks;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace VLTKMobile.Editor
{
    [InitializeOnLoad]
    public static class UnityMcpSessionWatchdog
    {
        private const string EnabledPrefKey = "VLTKMobile.UnityMCP.AlwaysKeepSessionAlive";
        private const string MenuPath = "Tools/MCP For Unity/Always Keep Session Alive";
        private const string ReconnectNowMenuPath = "Tools/MCP For Unity/Reconnect Session Now";
        private const double TickIntervalSeconds = 5d;
        private const double RestartCooldownSeconds = 8d;
        private const double LocalServerStartCooldownSeconds = 15d;
        private const double PlayModeTransitionGraceSeconds = 6d;
        private const int LocalServerWaitAttempts = 20;
        private const int LocalServerWaitDelayMs = 500;
        private const string LogPrefix = "[Unity MCP Watchdog]";

        private static bool tickInFlight;
        private static bool isShuttingDown;
        private static double nextTickAt;
        private static double nextReconnectAt;
        private static double nextLocalServerStartAt;
        private static string lastProblem;

        static UnityMcpSessionWatchdog()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.quitting += OnEditorQuitting;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (!EditorPrefs.HasKey(EnabledPrefKey))
            {
                EditorPrefs.SetBool(EnabledPrefKey, true);
            }
        }

        [MenuItem(MenuPath)]
        private static void ToggleAlwaysKeepAlive()
        {
            bool enabled = !IsEnabled();
            EditorPrefs.SetBool(EnabledPrefKey, enabled);
            Menu.SetChecked(MenuPath, enabled);

            if (enabled)
            {
                lastProblem = null;
                nextTickAt = 0d;
                UnityEngine.Debug.Log($"{LogPrefix} Enabled.");
            }
            else
            {
                UnityEngine.Debug.Log($"{LogPrefix} Disabled.");
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleAlwaysKeepAliveValidate()
        {
            Menu.SetChecked(MenuPath, IsEnabled());
            return true;
        }

        [MenuItem(ReconnectNowMenuPath)]
        private static void ReconnectNow()
        {
            if (tickInFlight)
            {
                return;
            }

            nextTickAt = 0d;
            _ = TickAsync(forceReconnect: true);
        }

        [MenuItem(ReconnectNowMenuPath, true)]
        private static bool ReconnectNowValidate()
        {
            return !EditorApplication.isCompiling && !EditorApplication.isUpdating;
        }

        private static bool IsEnabled()
        {
            return EditorPrefs.GetBool(EnabledPrefKey, true);
        }

        private static void OnEditorQuitting()
        {
            isShuttingDown = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            // Avoid bridge restart churn while entering/exiting play mode.
            nextTickAt = EditorApplication.timeSinceStartup + PlayModeTransitionGraceSeconds;
        }

        private static bool IsPlayModeTransitionBusy()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return true;
            }

            if (EditorApplication.isPlaying)
            {
                return true;
            }

            return false;
        }

        private static void OnEditorUpdate()
        {
            if (isShuttingDown || !IsEnabled() || tickInFlight)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || IsPlayModeTransitionBusy())
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now < nextTickAt)
            {
                return;
            }

            nextTickAt = now + TickIntervalSeconds;
            _ = TickAsync(forceReconnect: false);
        }

        private static async Task TickAsync(bool forceReconnect)
        {
            if (tickInFlight)
            {
                return;
            }

            tickInFlight = true;

            try
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    return;
                }

                if (IsPlayModeTransitionBusy())
                {
                    return;
                }

                var bridge = MCPServiceLocator.Bridge;
                bool useHttpTransport = EditorConfigurationCache.Instance.UseHttpTransport;

                if (useHttpTransport)
                {
                    bool serverReady = await EnsureHttpServerReadyAsync();
                    if (!serverReady)
                    {
                        return;
                    }
                }

                if (!forceReconnect && bridge.IsRunning)
                {
                    var verification = await bridge.VerifyAsync();
                    if (verification.Success && verification.PingSucceeded)
                    {
                        ClearProblem();
                        return;
                    }

                    await RestartBridgeAsync($"verification failed: {verification.Message}");
                    return;
                }

                await RestartBridgeAsync(forceReconnect ? "manual reconnect" : "bridge not running");
            }
            catch (Exception ex)
            {
                ReportProblem($"watchdog tick failed: {ex.Message}");
            }
            finally
            {
                tickInFlight = false;
            }
        }

        private static async Task<bool> EnsureHttpServerReadyAsync()
        {
            if (HttpEndpointUtility.IsRemoteScope())
            {
                return true;
            }

            var server = MCPServiceLocator.Server;
            if (server.IsLocalHttpServerReachable())
            {
                return true;
            }

            if (!server.CanStartLocalServer())
            {
                ReportProblem("local HTTP server is not reachable and current MCP settings do not allow auto-start.");
                return false;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now < nextLocalServerStartAt)
            {
                return false;
            }

            nextLocalServerStartAt = now + LocalServerStartCooldownSeconds;

            if (!server.StartLocalHttpServer(quiet: true))
            {
                ReportProblem("failed to start local HTTP server.");
                return false;
            }

            for (int i = 0; i < LocalServerWaitAttempts; i++)
            {
                await Task.Delay(LocalServerWaitDelayMs);

                if (server.IsLocalHttpServerReachable())
                {
                    ClearProblem();
                    return true;
                }
            }

            ReportProblem("local HTTP server did not become reachable after auto-start.");
            return false;
        }

        private static async Task RestartBridgeAsync(string reason)
        {
            double now = EditorApplication.timeSinceStartup;
            if (now < nextReconnectAt)
            {
                return;
            }

            nextReconnectAt = now + RestartCooldownSeconds;

            bool started = await MCPServiceLocator.Bridge.StartAsync();
            if (started)
            {
                ClearProblem();
                UnityEngine.Debug.Log($"{LogPrefix} Session restarted ({reason}).");
                return;
            }

            ReportProblem($"failed to restart session ({reason}).");
        }

        private static void ReportProblem(string message)
        {
            if (string.Equals(lastProblem, message, StringComparison.Ordinal))
            {
                return;
            }

            lastProblem = message;
            UnityEngine.Debug.LogWarning($"{LogPrefix} {message}");
        }

        private static void ClearProblem()
        {
            if (string.IsNullOrEmpty(lastProblem))
            {
                return;
            }

            UnityEngine.Debug.Log($"{LogPrefix} Session healthy again.");
            lastProblem = null;
        }
    }
}

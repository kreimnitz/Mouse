using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Mouse;

public class MouseHooker
{
    //############################################################################
    private Func<int, int, int, int, (int AdjX, int AdjY)?> _onMouseMove;

    public MouseHooker(Func<int, int, int, int, (int AdjX, int AdjY)?> onMouseMove)
    {
        _onMouseMove = onMouseMove;

        _hookCallback = new LowLevelMouseProc(HookCallback);

        _hookHandle = GCHandle.Alloc(_hookCallback);

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        if (curModule != null)
        {
            _hookId = SetWindowsHookEx(WH_MOUSE_LL, _hookCallback,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    public bool UnHook()
    {
        if (!Hooked) return true;
        if (UnhookWindowsHookEx(_hookId))
        {
            _hookHandle.Free();
            _hookId = IntPtr.Zero;
        }

        return !Hooked;
    }

    public bool Hooked => _hookId != IntPtr.Zero;

    //############################################################################

    #region Private variables

    private static IntPtr _hookId = IntPtr.Zero;
    private LowLevelMouseProc _hookCallback;
    private GCHandle _hookHandle;

    private static int _oldX, _oldY;

    private static readonly object _lock = new();
    private static readonly IntPtr NoPtr = new(-1);

    #endregion Private variables

    //############################################################################

    #region Main Code

    private (int X, int Y) Unscale(int x, int y)
    {
        if (x < 0 || x > 3839)
        {
            return (Convert.ToInt32(x / 1.5), Convert.ToInt32(y / 1.5));
        }
        return (x, y);
    }

    private IntPtr HookCallback(int nCode, MouseMessages wParam, IntPtr lParam)
    {
        Console.WriteLine($"Callback {nCode}");
        lock (_lock)
        {
            if (nCode < 0) goto CallNext;
            if (lParam == IntPtr.Zero) goto CallNext;
            if ((wParam & MouseMessages.WM_MOUSEMOVE) == 0) goto CallNext;

            int x;
            int y;
            unsafe
            {
                x = ((MSLLHOOKSTRUCT*)lParam)->x;
                y = ((MSLLHOOKSTRUCT*)lParam)->y;
            }

            if (_oldX == x && _oldY == y) goto CallNext;

            var tuple = _onMouseMove(_oldX, _oldY, x, y);
            if (tuple is not null)
            {
                (_oldX, _oldY) = Unscale(tuple.Value.AdjX, tuple.Value.AdjY);
                SetCursorPos(tuple.Value.AdjX, tuple.Value.AdjY);
                return NoPtr;
            }

            _oldX = x;
            _oldY = y;

            CallNext:
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
    }

    #endregion Main code

    //############################################################################

    #region Native Functions

    private const int WH_MOUSE_LL = 14;
    private const int WH_MOUSE = 7;

    [Flags]
    private enum MouseMessages
    {
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_MOUSEMOVE = 0x0200,
        WM_MOUSEWHEEL = 0x020A,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        //            public POINT pt;
        public int x;
        public int y;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);


    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);


    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, MouseMessages wParam, IntPtr lParam);


    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("User32.Dll")]
    public static extern long SetCursorPos(int x, int y);


    private delegate IntPtr LowLevelMouseProc(int nCode, MouseMessages wParam, IntPtr lParam);
    private delegate IntPtr MouseProc(int nCode, MouseMessages wParam, IntPtr lParam);


    #endregion
}
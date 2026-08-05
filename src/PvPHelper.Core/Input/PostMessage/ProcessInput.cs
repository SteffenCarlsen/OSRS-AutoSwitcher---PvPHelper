// Imported from the user's PostMessageInput library (Author: TaF) and vendored here so the
// app is self-contained. Posts input-shaped window messages to a target HWND, enabling
// background input (no window focus required). Original namespace preserved.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PostMessageInput;

public enum MouseButton
{
    Left,
    Right,
    Middle
}

/// <summary>Posts input-shaped window messages to a Windows process or HWND.</summary>
[SupportedOSPlatform("windows")]
public sealed class ProcessInput
{
    private const uint WmKeyDown = 0x0100;
    private const uint WmKeyUp = 0x0101;
    private const uint WmChar = 0x0102;
    private const uint WmMouseMove = 0x0200;
    private const uint GaRoot = 2;

    private readonly int _processId;
    private readonly nint _window;

    /// <summary>Targets the process's current main window.</summary>
    public ProcessInput(int processId)
    {
        EnsureWindows();
        using Process process = Process.GetProcessById(processId);
        _processId = process.Id;
    }

    private ProcessInput(int processId, nint window)
    {
        _processId = processId;
        _window = window;
    }

    /// <summary>
    /// Targets a known window. For a Credential Provider, pass the owner returned by
    /// ICredentialProviderCredentialEvents::OnCreatingWindow.
    /// </summary>
    public static ProcessInput FromWindow(nint window)
    {
        EnsureWindows();

        if (window == 0)
            throw new ArgumentException("The window handle cannot be zero.", nameof(window));

        uint threadId = NativeMethods.GetWindowThreadProcessId(window, out uint processId);
        if (threadId == 0)
            throw LastWin32("The window handle is not valid.");

        return new ProcessInput(checked((int)processId), window);
    }

    public int ProcessId => _processId;

    /// <summary>
    /// Gets the focused HWND on the target window's UI thread and verifies that it
    /// belongs to both the target process and the same top-level window.
    /// </summary>
    public nint ResolveKeyboardWindow()
    {
        nint target = ResolveTargetWindow();
        uint threadId = NativeMethods.GetWindowThreadProcessId(target, out uint processId);

        if (threadId == 0 || processId != (uint)_processId)
            throw new InvalidOperationException("The target window is no longer valid.");

        GuiThreadInfo info = new() { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
        if (!NativeMethods.GetGUIThreadInfo(threadId, ref info))
            throw LastWin32("Could not inspect the target window's GUI thread.");

        nint focused = info.FocusedWindow;
        if (focused == 0)
            throw new InvalidOperationException("The target UI thread has no focused window.");

        if (NativeMethods.GetWindowThreadProcessId(focused, out uint focusedProcessId) == 0 ||
            focusedProcessId != (uint)_processId ||
            NativeMethods.GetAncestor(focused, GaRoot) != NativeMethods.GetAncestor(target, GaRoot))
        {
            throw new InvalidOperationException(
                "The focused window does not belong to the selected top-level window.");
        }

        return focused;
    }

    public void KeyDown(ConsoleKey key) => KeyDown((ushort)key);

    public void KeyDown(ushort virtualKeyCode)
    {
        nint window = ResolveKeyboardWindow();
        Post(window, WmKeyDown, virtualKeyCode, MessagePacking.Keyboard(window, virtualKeyCode, keyUp: false));
    }

    public void KeyUp(ConsoleKey key) => KeyUp((ushort)key);

    public void KeyUp(ushort virtualKeyCode)
    {
        nint window = ResolveKeyboardWindow();
        Post(window, WmKeyUp, virtualKeyCode, MessagePacking.Keyboard(window, virtualKeyCode, keyUp: true));
    }

    public void PressKey(ConsoleKey key) => PressKey((ushort)key);

    public void PressKey(ushort virtualKeyCode)
    {
        nint window = ResolveKeyboardWindow();
        Post(window, WmKeyDown, virtualKeyCode, MessagePacking.Keyboard(window, virtualKeyCode, keyUp: false));
        Post(window, WmKeyUp, virtualKeyCode, MessagePacking.Keyboard(window, virtualKeyCode, keyUp: true));
    }

    /// <summary>Posts one WM_CHAR message per UTF-16 code unit.</summary>
    public void SendText(string text, int delayMilliseconds = 5)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(delayMilliseconds);

        nint window = ResolveKeyboardWindow();
        for (int i = 0; i < text.Length; i++)
        {
            Post(window, WmChar, text[i], 1);

            if (delayMilliseconds != 0 && i + 1 < text.Length)
                Thread.Sleep(delayMilliseconds);
        }
    }

    /// <param name="x">Client-area x-coordinate of the selected HWND.</param>
    /// <param name="y">Client-area y-coordinate of the selected HWND.</param>
    public void MoveMouse(int x, int y) =>
        Post(ResolveTargetWindow(), WmMouseMove, 0, MessagePacking.Coordinates(x, y));

    public void MouseDown(MouseButton button, int x, int y)
    {
        MouseMessage message = MouseMessage.For(button);
        Post(ResolveTargetWindow(), message.Down, message.ButtonFlag, MessagePacking.Coordinates(x, y));
    }

    public void MouseUp(MouseButton button, int x, int y)
    {
        MouseMessage message = MouseMessage.For(button);
        Post(ResolveTargetWindow(), message.Up, 0, MessagePacking.Coordinates(x, y));
    }

    public void Click(MouseButton button, int x, int y)
    {
        MouseMessage message = MouseMessage.For(button);
        nint window = ResolveTargetWindow();
        nint coordinates = MessagePacking.Coordinates(x, y);

        Post(window, message.Down, message.ButtonFlag, coordinates);
        Post(window, message.Up, 0, coordinates);
    }

    private nint ResolveTargetWindow()
    {
        if (_window != 0)
        {
            if (NativeMethods.GetWindowThreadProcessId(_window, out uint processId) == 0 ||
                processId != (uint)_processId)
            {
                throw new InvalidOperationException("The target window is no longer valid.");
            }

            return _window;
        }

        using Process process = Process.GetProcessById(_processId);
        if (process.HasExited)
            throw new InvalidOperationException($"Process {_processId} has exited.");

        process.Refresh();
        return process.MainWindowHandle != 0
            ? process.MainWindowHandle
            : throw new InvalidOperationException(
                $"Process {_processId} does not currently have a main window; use FromWindow when you already have an HWND.");
    }

    private static void Post(nint window, uint message, nuint wParam, nint lParam)
    {
        if (!NativeMethods.PostMessage(window, message, wParam, lParam))
            throw LastWin32($"PostMessageW(0x{message:x}) failed for HWND 0x{window:x}.");
    }

    private static Win32Exception LastWin32(string message) =>
        new(Marshal.GetLastPInvokeError(), message);

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("PostMessageInput only works on Windows.");
    }

    private readonly record struct MouseMessage(uint Down, uint Up, nuint ButtonFlag)
    {
        public static MouseMessage For(MouseButton button) => button switch
        {
            MouseButton.Left => new(0x0201, 0x0202, 0x0001),
            MouseButton.Right => new(0x0204, 0x0205, 0x0002),
            MouseButton.Middle => new(0x0207, 0x0208, 0x0010),
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, null)
        };
    }
}

internal static class MessagePacking
{
    private const uint MapVkToVscEx = 4;

    internal static nint Keyboard(nint window, ushort virtualKeyCode, bool keyUp)
    {
        uint threadId = NativeMethods.GetWindowThreadProcessId(window, out _);
        uint scanCode = NativeMethods.MapVirtualKeyEx(
            virtualKeyCode,
            MapVkToVscEx,
            NativeMethods.GetKeyboardLayout(threadId));

        return KeyboardFromScanCode(scanCode, keyUp);
    }

    internal static nint KeyboardFromScanCode(uint scanCode, bool keyUp)
    {
        uint value = 1 | ((scanCode & 0xff) << 16);

        if ((scanCode & 0xff00) == 0xe000)
            value |= 1 << 24;

        if (keyUp)
            value |= 0xc0000000;

        return unchecked((nint)(nuint)value);
    }

    internal static nint Coordinates(int x, int y)
    {
        if (x is < short.MinValue or > short.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(x), x, "Coordinates must fit in a signed 16-bit value.");

        if (y is < short.MinValue or > short.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(y), y, "Coordinates must fit in a signed 16-bit value.");

        uint value = (ushort)(short)x | ((uint)(ushort)(short)y << 16);
        return unchecked((nint)(nuint)value);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct GuiThreadInfo
{
    internal uint Size;
    internal uint Flags;
    internal nint ActiveWindow;
    internal nint FocusedWindow;
    internal nint CaptureWindow;
    internal nint MenuOwnerWindow;
    internal nint MoveSizeWindow;
    internal nint CaretWindow;
    internal NativeRect CaretRectangle;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeRect
{
    internal int Left;
    internal int Top;
    internal int Right;
    internal int Bottom;
}

internal static partial class NativeMethods
{
    [LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PostMessage(nint window, uint message, nuint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true)]
    internal static partial uint GetWindowThreadProcessId(nint window, out uint processId);

    [LibraryImport("user32.dll", EntryPoint = "GetGUIThreadInfo", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);

    [LibraryImport("user32.dll", EntryPoint = "GetAncestor")]
    internal static partial nint GetAncestor(nint window, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "GetKeyboardLayout")]
    internal static partial nint GetKeyboardLayout(uint threadId);

    [LibraryImport("user32.dll", EntryPoint = "MapVirtualKeyExW")]
    internal static partial uint MapVirtualKeyEx(uint code, uint mapType, nint keyboardLayout);
}

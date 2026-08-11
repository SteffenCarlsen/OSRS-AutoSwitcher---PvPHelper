using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using PvPHelper.App.Services;
using PvPHelper.Core.Models;

namespace PvPHelper.App.Controls;

/// <summary>Click to capture the next key or mouse button (incl. Mouse 3/4/5) as a <see cref="HotkeyToken"/>.</summary>
public partial class HotkeyCaptureBox : UserControl
{
    /// <summary>While one box is capturing, kick any other box out of capture mode.</summary>
    private static event Action? AnyCaptureStarted;

    public static readonly StyledProperty<HotkeyToken> HotkeyProperty =
        AvaloniaProperty.Register<HotkeyCaptureBox, HotkeyToken>(
            nameof(Hotkey), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsCapturingProperty =
        AvaloniaProperty.Register<HotkeyCaptureBox, bool>(nameof(IsCapturing));

    public HotkeyToken Hotkey
    {
        get => GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    public bool IsCapturing
    {
        get => GetValue(IsCapturingProperty);
        set => SetValue(IsCapturingProperty, value);
    }

    private TopLevel? _topLevel;
    private bool _windowHooked;

    public HotkeyCaptureBox()
    {
        InitializeComponent();
        Focusable = true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // e.Root is the root *visual*, not the TopLevel (obsoleted since Avalonia 12).
        _topLevel = TopLevel.GetTopLevel(this);
        if (IsCapturing) AttachWindowHook();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        AnyCaptureStarted -= OnOtherCaptureStarted;
        DetachWindowHook();
        _topLevel = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCapturingProperty)
            OnIsCapturingChanged(change.GetNewValue<bool>());
    }

    private void OnIsCapturingChanged(bool value)
    {
        if (value)
        {
            // Only one box may be in capture mode at a time.
            AnyCaptureStarted?.Invoke();
            AnyCaptureStarted += OnOtherCaptureStarted;
            AttachWindowHook();
        }
        else
        {
            AnyCaptureStarted -= OnOtherCaptureStarted;
            DetachWindowHook();
        }
    }

    private void OnOtherCaptureStarted() => IsCapturing = false;

    private void AttachWindowHook()
    {
        // Mouse buttons only reach this control's pointer handlers when the cursor is over
        // it. While capturing, listen at the window so a mouse press anywhere in the app
        // (not just on the box) can be bound.
        if (_windowHooked || _topLevel is null) return;
        _topLevel.AddHandler(PointerPressedEvent, OnWindowPointerPressed, RoutingStrategies.Bubble);
        _windowHooked = true;
    }

    private void DetachWindowHook()
    {
        if (!_windowHooked || _topLevel is null) return;
        _topLevel.RemoveHandler(PointerPressedEvent, OnWindowPointerPressed);
        _windowHooked = false;
    }

    private void OnClick(object? sender, RoutedEventArgs e)
    {
        IsCapturing = true;
        Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsCapturing) { base.OnKeyDown(e); return; }

        if (e.Key == Key.Escape)
        {
            IsCapturing = false;
            e.Handled = true;
            return;
        }

        var vk = KeyMap.ToVirtualKey(e.Key);
        if (vk != 0)
        {
            Hotkey = new HotkeyToken(HotkeySource.Keyboard, vk);
            IsCapturing = false;
            e.Handled = true;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!IsCapturing) { base.OnPointerPressed(e); return; }

        if (TryCaptureMouse(e))
            e.Handled = true;
        else
            base.OnPointerPressed(e);
    }

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsCapturing && TryCaptureMouse(e))
            e.Handled = true;
    }

    /// <summary>Bind the pressed mouse button. Returns true when a bindable button was pressed.</summary>
    private bool TryCaptureMouse(PointerPressedEventArgs e)
    {
        var kind = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
        var code = kind switch
        {
            PointerUpdateKind.XButton1Pressed => 5,
            PointerUpdateKind.XButton2Pressed => 6,
            PointerUpdateKind.MiddleButtonPressed => 4,
            _ => 0
        };

        if (code == 0) return false;

        Hotkey = new HotkeyToken(HotkeySource.Mouse, code);
        IsCapturing = false;
        return true;
    }
}

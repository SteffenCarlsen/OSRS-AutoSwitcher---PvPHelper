using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using PvPHelper.App.Services;
using PvPHelper.Core.Models;

namespace PvPHelper.App.Controls;

/// <summary>Click to capture the next key or mouse button (incl. Mouse 4/5) as a <see cref="HotkeyToken"/>.</summary>
public partial class HotkeyCaptureBox : UserControl
{
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

    public HotkeyCaptureBox()
    {
        InitializeComponent();
        Focusable = true;
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

        var kind = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
        var code = kind switch
        {
            PointerUpdateKind.XButton1Pressed => 5,
            PointerUpdateKind.XButton2Pressed => 6,
            PointerUpdateKind.MiddleButtonPressed => 4,
            _ => 0
        };

        if (code != 0)
        {
            Hotkey = new HotkeyToken(HotkeySource.Mouse, code);
            IsCapturing = false;
            e.Handled = true;
        }
        else
        {
            base.OnPointerPressed(e);
        }
    }
}

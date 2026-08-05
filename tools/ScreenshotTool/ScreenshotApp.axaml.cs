using Avalonia;
using Avalonia.Markup.Xaml;

namespace ScreenshotTool;

public class ScreenshotApp : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
}

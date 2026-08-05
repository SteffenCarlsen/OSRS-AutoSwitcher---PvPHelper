using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using PvPHelper.App.ViewModels;
using PvPHelper.App.Views;

namespace PvPHelper.App;

/// <summary>Resolves a View for a ViewModel by naming convention, falling back to a placeholder.</summary>
public class ViewLocator : IDataTemplate
{
    public Control Build(object? data)
    {
        if (data is null)
            return new TextBlock { Text = "null" };

        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type is not null)
            return (Control)Activator.CreateInstance(type)!;

        return new PlaceholderView();
    }

    public bool Match(object? data) => data is PageViewModelBase;
}

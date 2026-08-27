using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace QARegressionManager.Services;

public sealed class NativeWindowAccentBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<NativeWindowAccentBehavior, Window, bool>(
            "IsEnabled");

    static NativeWindowAccentBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<Window>(
            OnIsEnabledChanged);
    }

    public static bool GetIsEnabled(
        Window window) =>
        window.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(
        Window window,
        bool value) =>
        window.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(
        Window window,
        AvaloniaPropertyChangedEventArgs args)
    {
        window.Opened -= Window_OnOpened;
        window.Activated -= Window_OnActivated;

        if (args.NewValue is not true)
        {
            return;
        }

        window.Opened += Window_OnOpened;
        window.Activated += Window_OnActivated;
    }

    private static void Window_OnOpened(
        object? sender,
        EventArgs e)
    {
        if (sender is not Window window)
        {
            return;
        }

        Refresh(window);

        Dispatcher.UIThread.Post(
            () => Refresh(window),
            DispatcherPriority.Loaded);
    }

    private static void Window_OnActivated(
        object? sender,
        EventArgs e)
    {
        if (sender is Window window)
        {
            Refresh(window);
        }
    }

    private static void Refresh(
        Window window)
    {
        if (window.TryGetPlatformHandle() is not null)
        {
            ApplicationAppearanceService.RefreshNativeWindowAccent(
                window);
        }
    }
}

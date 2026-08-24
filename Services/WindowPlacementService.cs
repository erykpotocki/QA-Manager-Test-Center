using System;
using Avalonia;
using Avalonia.Controls;

namespace QARegressionManager.Services;

public static class WindowPlacementService
{
    public static void PlaceNearPreviousWindow(
        Window previousWindow,
        Window nextWindow)
    {
        var screen =
            previousWindow.Screens.ScreenFromWindow(
                previousWindow);

        if (screen is null)
        {
            return;
        }

        var workingArea =
            screen.WorkingArea;

        var scaling =
            screen.Scaling;

        var nextWidth =
            Math.Min(
                workingArea.Width,
                Math.Max(
                    1,
                    (int)Math.Round(
                        nextWindow.Width *
                        scaling)));

        var nextHeight =
            Math.Min(
                workingArea.Height,
                Math.Max(
                    1,
                    (int)Math.Round(
                        nextWindow.Height *
                        scaling)));

        var previousCenterX =
            previousWindow.WindowState ==
            WindowState.Normal
                ? previousWindow.Position.X +
                  previousWindow.Bounds.Width *
                  scaling /
                  2
                : workingArea.X +
                  workingArea.Width /
                  2d;

        var previousCenterY =
            previousWindow.WindowState ==
            WindowState.Normal
                ? previousWindow.Position.Y +
                  previousWindow.Bounds.Height *
                  scaling /
                  2
                : workingArea.Y +
                  workingArea.Height /
                  2d;

        var desiredX =
            (int)Math.Round(
                previousCenterX -
                nextWidth /
                2d);

        var desiredY =
            (int)Math.Round(
                previousCenterY -
                nextHeight /
                2d);

        var maximumX =
            workingArea.Right -
            nextWidth;

        var maximumY =
            workingArea.Bottom -
            nextHeight;

        nextWindow.WindowStartupLocation =
            WindowStartupLocation.Manual;

        nextWindow.Position =
            new PixelPoint(
                Math.Clamp(
                    desiredX,
                    workingArea.X,
                    maximumX),
                Math.Clamp(
                    desiredY,
                    workingArea.Y,
                    maximumY));
    }
}

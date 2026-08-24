using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using QARegressionManager.Services;
using System.Linq;
using System.Text.RegularExpressions;

namespace QARegressionManager.Views;

public partial class NotificationCenterWindow : Window
{
    private readonly string _login;
    private readonly AssignmentService _assignmentService =
        new();

    public event Action? AssignedTestsHighlightRequested;

    public NotificationCenterWindow()
        : this(
            "unknown")
    {
    }

    public NotificationCenterWindow(
        string login)
    {
        InitializeComponent();

        _login =
            login;

        Opened +=
            async (_, _) =>
            {
                var notifications =
                    await _assignmentService.GetNotificationsForUserAsync(
                        _login);

                var activeAssignmentIds =
                    (await _assignmentService.GetActiveAssignmentsForUserAsync(
                        _login))
                    .Select(
                        assignment =>
                            assignment.Id)
                    .ToHashSet();

                var newestActiveAssignmentNotificationId =
                    notifications
                        .Where(
                            notification =>
                                notification.AssignmentId.HasValue &&
                                activeAssignmentIds.Contains(
                                    notification.AssignmentId.Value))
                        .OrderByDescending(
                            notification =>
                                notification.CreatedAt)
                        .Select(
                            notification =>
                                (Guid?)notification.Id)
                        .FirstOrDefault();

                NotificationsPanel.Children.Clear();
                ClearNotificationsButton.IsEnabled =
                    notifications.Length > 0;

                if (notifications.Length == 0)
                {
                    NotificationsPanel.Children.Add(
                        new TextBlock
                        {
                            Text = LocalizationService.T("Notifications.Empty"),
                            Margin = new Thickness(0, 18),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = Brushes.Gray
                        });
                }

                foreach (var notification in notifications)
                {
                    var notificationBorder =
                        new Border
                        {
                            Padding = new Thickness(14),
                            Background = notification.IsRead
                                ? new SolidColorBrush(Color.Parse("#0A68726B"))
                                : new SolidColorBrush(Color.Parse("#1828C76F")),
                            BorderBrush = new SolidColorBrush(Color.Parse("#4068726B")),
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(12),
                            Child = new StackPanel
                            {
                                Spacing = 4,
                                Children =
                                {
                                    new TextBlock
                                    {
                                        Text = GetDisplayTitle(notification.Title),
                                        FontSize = 14,
                                        FontWeight = FontWeight.Bold
                                    },
                                    new TextBlock
                                    {
                                        Text = GetDisplayMessage(notification.Message),
                                        TextWrapping = TextWrapping.Wrap,
                                        FontSize = 13
                                    },
                                    new TextBlock
                                    {
                                        Text = notification.CreatedAt.LocalDateTime.ToString("dd.MM.yyyy HH:mm"),
                                        FontSize = 11,
                                        Foreground = Brushes.Gray
                                    }
                                }
                            }
                        };

                    if (notification.StructureChangeRequestId is Guid requestId)
                    {
                        var request = await _assignmentService
                            .GetStructureChangeRequestAsync(requestId);

                        if (request?.Status == "Pending" &&
                            notificationBorder.Child is StackPanel contentPanel)
                        {
                            var approveButton = new Button
                            {
                                Content = LocalizationService.T("Notifications.ApproveDeletion"),
                                Classes = { "PrimaryAction" }
                            };
                            var rejectButton = new Button
                            {
                                Content = LocalizationService.T("Notifications.Reject"),
                                Classes = { "SecondaryAction" }
                            };
                            var actions = new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                Spacing = 8,
                                Margin = new Thickness(0, 8, 0, 0),
                                Children = { rejectButton, approveButton }
                            };

                            async void Resolve(bool approve)
                            {
                                var resolved = await _assignmentService
                                    .ResolveStructureDeletionAsync(requestId, _login, approve);
                                if (!resolved)
                                {
                                    return;
                                }

                                approveButton.IsEnabled = false;
                                rejectButton.IsEnabled = false;
                                approveButton.Content = approve
                                    ? LocalizationService.T("Notifications.Approved")
                                    : LocalizationService.T("Notifications.Rejected");
                            }

                            approveButton.Click += (_, _) => Resolve(true);
                            rejectButton.Click += (_, _) => Resolve(false);
                            contentPanel.Children.Add(actions);
                        }
                    }

                    if (newestActiveAssignmentNotificationId ==
                        notification.Id)
                    {
                        notificationBorder.Cursor =
                            new Cursor(
                                StandardCursorType.Hand);

                        ToolTip.SetTip(
                            notificationBorder,
                            LocalizationService.T("Notifications.ShowExecuteTip"));

                        notificationBorder.PointerPressed +=
                            (_, eventArgs) =>
                            {
                                if (!eventArgs
                                        .GetCurrentPoint(
                                            notificationBorder)
                                        .Properties
                                        .IsLeftButtonPressed)
                                {
                                    return;
                                }

                                AssignedTestsHighlightRequested?.Invoke();
                            };
                    }

                    NotificationsPanel.Children.Add(
                        notificationBorder);
                }

                await _assignmentService.MarkAllNotificationsReadAsync(
                    _login);
            };
    }

    private void CloseButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }

    private async void ClearNotificationsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var confirmation =
            new ConfirmDeleteWindow(
                LocalizationService.T("Notifications.ClearQuestion"),
                LocalizationService.T("Notifications.ClearDescription"),
                LocalizationService.T("Notifications.ClearAction"));

        if (!await confirmation.ShowDialog<bool>(
                this))
        {
            return;
        }

        await _assignmentService.ClearNotificationsForUserAsync(
            _login);

        NotificationsPanel.Children.Clear();
        NotificationsPanel.Children.Add(
            new TextBlock
            {
                Text =
                    LocalizationService.T("Notifications.Empty"),
                Margin =
                    new Thickness(0, 18),
                HorizontalAlignment =
                    HorizontalAlignment.Center,
                Foreground =
                    Brushes.Gray
            });

        ClearNotificationsButton.IsEnabled =
            false;
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key is Key.Enter or Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private static string GetDisplayTitle(string title)
    {
        if (LocalizationService.IsPolish)
        {
            return title;
        }

        return title switch
        {
            "Nowe testy do wykonania" => LocalizationService.T("Notifications.NewTestsTitle"),
            "Zmieniono przypisane testy" => LocalizationService.T("Notifications.AssignmentChangedTitle"),
            "Wycofano przypisane testy" => LocalizationService.T("Notifications.AssignmentWithdrawnTitle"),
            "Przypisanie ukończone" => LocalizationService.T("Notifications.CompletedTitle"),
            "Przypisanie zostało przeniesione" => LocalizationService.T("Notifications.AssignmentMovedTitle"),
            "Prośba o usunięcie dużej gałęzi" => LocalizationService.T("Notifications.DeletionRequestTitle"),
            "Usunięcie zatwierdzone" => LocalizationService.T("Notifications.DeletionApprovedTitle"),
            "Usunięcie odrzucone" => LocalizationService.T("Notifications.DeletionRejectedTitle"),
            _ => title
        };
    }

    private static string GetDisplayMessage(string message)
    {
        if (LocalizationService.IsPolish)
        {
            return message;
        }

        var assignment = Regex.Match(
            message,
            @"^(?<by>.+) przypisał sesję projektu (?<project>.+), wersja (?<version>.+) \((?<count>\d+) przypadków\)\.$");

        return assignment.Success
            ? LocalizationService.Format(
                "Notifications.NewTestsMessage",
                assignment.Groups["by"].Value,
                assignment.Groups["project"].Value,
                assignment.Groups["version"].Value,
                assignment.Groups["count"].Value)
            : message;
    }
}

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using QARegressionManager.Services;
using System.Linq;

namespace QARegressionManager.Views;

public partial class NotificationCenterWindow : Window
{
    private readonly string _login;
    private readonly AssignmentService _assignmentService =
        new();

    public event Action? AssignedTestsHighlightRequested;

    public NotificationCenterWindow()
        : this(
            "nieznany")
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
                                    notification.AssignmentId.Value) &&
                                notification.Title.StartsWith(
                                    "Nowe testy do wykonania",
                                    StringComparison.OrdinalIgnoreCase))
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
                            Text = "Nie masz jeszcze żadnych powiadomień.",
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
                                        Text = notification.Title,
                                        FontSize = 14,
                                        FontWeight = FontWeight.Bold
                                    },
                                    new TextBlock
                                    {
                                        Text = notification.Message,
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
                                Content = "ZATWIERDŹ USUNIĘCIE",
                                Classes = { "PrimaryAction" }
                            };
                            var rejectButton = new Button
                            {
                                Content = "ODRZUĆ",
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
                                approveButton.Content = approve ? "ZATWIERDZONO" : "ODRZUCONO";
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
                            "Pokaż przycisk Wykonaj testy");

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
                "Wyczyścić powiadomienia?",
                "Wszystkie powiadomienia przypisane do Twojego profilu zostaną trwale usunięte.",
                "WYCZYŚĆ");

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
                    "Nie masz jeszcze żadnych powiadomień.",
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
}

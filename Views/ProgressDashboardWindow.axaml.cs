using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using QARegressionManager.Models;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class ProgressDashboardWindow : Window
{
    private static readonly string[] ChartColors =
    {
        "#2E86D1",
        "#28C76F",
        "#9A6BE8",
        "#F0A34A",
        "#E85D75",
        "#36A6A6",
        "#7A8B99",
        "#D080C3"
    };

    private readonly string _login;
    private readonly IReadOnlyList<string> _systemRoles;
    private readonly AssignmentService _assignmentService =
        new();
    private readonly JsonStorageService _storageService =
        new();
    private readonly TestReportExportService _reportExportService =
        new();

    private TestAssignmentModel[] _visibleAssignments =
        Array.Empty<TestAssignmentModel>();
    private AssignmentProgressRow[] _completedRows =
        Array.Empty<AssignmentProgressRow>();
    private readonly Dictionary<Guid, CheckBox> _activeSessionSelection =
        new();
    private DashboardSection _dashboardSection =
        DashboardSection.Active;
    private readonly DispatcherTimer _refreshTimer =
        new()
        {
            Interval = TimeSpan.FromSeconds(2)
        };
    private string? _renderedDataSignature;
    private bool _backgroundRefreshInProgress;

    public ProgressDashboardWindow()
        : this(
            "nieznany",
            new[]
            {
                SystemRoleService.TesterRole
            })
    {
    }

    public ProgressDashboardWindow(
        string login,
        IReadOnlyList<string> systemRoles)
    {
        InitializeComponent();

        _login =
            login;

        _systemRoles =
            systemRoles;

        Opened +=
            async (_, _) =>
            {
                await LoadDashboardAsync();
                _refreshTimer.Start();
            };

        Closed +=
            (_, _) =>
                _refreshTimer.Stop();

        _refreshTimer.Tick +=
            async (_, _) =>
                await RefreshDashboardInBackgroundAsync();
    }

    private bool CanSeeTeamProgress =>
        _systemRoles.Any(
            role =>
                string.Equals(
                    role,
                    SystemRoleService.AdministratorRole,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    role,
                    SystemRoleService.LeaderRole,
                    StringComparison.OrdinalIgnoreCase));

    private async Task LoadDashboardAsync()
    {
        var assignments =
            _dashboardSection == DashboardSection.Archive
                ? await _assignmentService.GetArchivedAssignmentsAsync()
                : await _assignmentService.GetAssignmentsForDashboardAsync();

        if (!CanSeeTeamProgress)
        {
            assignments =
                assignments
                    .Where(
                        assignment =>
                            string.Equals(
                                assignment.RecipientLogin,
                                _login,
                                StringComparison.OrdinalIgnoreCase))
                    .ToArray();
        }

        var dataSignature =
            CreateDataSignature(
                assignments,
                _dashboardSection);

        if (string.Equals(
                dataSignature,
                _renderedDataSignature,
                StringComparison.Ordinal))
        {
            return;
        }

        _renderedDataSignature =
            dataSignature;

        _visibleAssignments =
            assignments;

        DashboardDescriptionTextBlock.Text =
            _dashboardSection == DashboardSection.Archive
                ? "Zarchiwizowane sesje są automatycznie usuwane po 60 dniach. Można je wcześniej przywrócić do historii."
                : CanSeeTeamProgress
                ? "Bieżący postęp całego zespołu oraz ostatnio ukończone przypisania."
                : "Widzisz wyłącznie postęp testów przypisanych do Twojego profilu.";

        ManagerNavigationPanel.IsVisible =
            CanSeeTeamProgress;

        var progressRows =
            assignments
                .Select(CreateProgressRow)
                .OrderByDescending(row => row.IsCompleted)
                .ThenBy(row => row.IsCompleted ? row.CompletedAt : null)
                .ThenByDescending(row => row.UpdatedAt)
                .ToArray();

        _completedRows =
            GetReportableRows(progressRows, _dashboardSection)
                .OrderBy(row => row.CompletedAt)
                .ToArray();

        var currentRows =
            GetCurrentRowsForTeamOverview(
                progressRows,
                _dashboardSection);

        SessionsCountTextBlock.Text =
            currentRows
                .Select(row => GetBatchId(row.Assignment))
                .Distinct()
                .Count()
                .ToString();

        CompletedCountTextBlock.Text =
            currentRows.Sum(row => row.Completed).ToString();

        RemainingCountTextBlock.Text =
            currentRows.Sum(row => row.Remaining).ToString();

        GenerateTeamReportButton.IsVisible =
            _dashboardSection != DashboardSection.Archive &&
            _completedRows.Length > 0;

        GenerateTeamReportButton.Content =
            CanSeeTeamProgress
                ? "GENERUJ RAPORT"
                : "RAPORT ZBIORCZY";

        BuildTeamOverview(currentRows);

        AssignmentsPanel.Children.Clear();
        _activeSessionSelection.Clear();

        var displayedRows =
            CanSeeTeamProgress
                ? _dashboardSection == DashboardSection.Archive
                    ? progressRows
                    : _dashboardSection == DashboardSection.Active
                    ? progressRows.Where(row => row.Assignment.IsActive).ToArray()
                    : progressRows.Where(row => !row.Assignment.IsActive && !row.Assignment.IsArchived).ToArray()
                : progressRows;

        UpdateDashboardNavigation();

        if (displayedRows.Length == 0)
        {
            AssignmentsPanel.Children.Add(
                new TextBlock
                {
                    Text = _dashboardSection == DashboardSection.Archive
                        ? "Archiwum jest puste."
                        : _dashboardSection == DashboardSection.Active
                        ? "Brak aktywnych przypisań do pokazania."
                        : "Historia ukończonych przypisań jest pusta.",
                    Margin = new Thickness(0, 28),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Brushes.Gray
                });

            return;
        }

        foreach (var batch in displayedRows
                     .GroupBy(row => GetBatchId(row.Assignment))
                     .OrderByDescending(group => group.Max(row => row.UpdatedAt)))
        {
            AssignmentsPanel.Children.Add(
                CreateBatchCard(batch.ToArray()));
        }
    }

    private static Guid GetBatchId(TestAssignmentModel assignment) =>
        assignment.BatchId == Guid.Empty
            ? assignment.Id
            : assignment.BatchId;

    private Control CreateBatchCard(AssignmentProgressRow[] rows)
    {
        var first = rows[0];
        var completed = rows.Sum(row => row.Completed);
        var total = rows.Sum(row => row.Total);
        var batchCompleted = rows.All(row => row.IsCompleted);

        var content = new StackPanel
        {
            Spacing = 7
        };

        foreach (var row in rows
                     .OrderByDescending(row => row.IsCompleted)
                     .ThenBy(row => row.RecipientLogin, StringComparer.OrdinalIgnoreCase))
        {
            content.Children.Add(CreateAssignmentCard(row));
        }

        return new Expander
        {
            IsExpanded = rows.Length == 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Header = new Border
            {
                Padding = new Thickness(12, 9),
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.Parse(batchCompleted ? "#1828C76F" : "#142E86D1")),
                BorderBrush = new SolidColorBrush(Color.Parse(batchCompleted ? "#6628C76F" : "#552E86D1")),
                BorderThickness = new Thickness(1),
                Child = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    Children =
                    {
                        new StackPanel
                        {
                            Spacing = 2,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = $"{first.ProjectName}  •  v{first.Version}",
                                    FontWeight = FontWeight.SemiBold,
                                    FontSize = 14
                                },
                                new TextBlock
                                {
                                    Text = $"Wysłane {first.Assignment.CreatedAt.LocalDateTime:g}  •  {rows.Length} {(rows.Length == 1 ? "osoba" : "osoby")}  •  {completed}/{total}",
                                    FontSize = 11,
                                    Foreground = Brushes.Gray
                                }
                            }
                        },
                        new TextBlock
                        {
                            Text = batchCompleted ? "UKOŃCZONO" : "W TRAKCIE",
                            FontSize = 10,
                            FontWeight = FontWeight.Bold,
                            Foreground = new SolidColorBrush(Color.Parse(batchCompleted ? "#159454" : "#2E86D1")),
                            VerticalAlignment = VerticalAlignment.Center,
                            [Grid.ColumnProperty] = 1
                        }
                    }
                }
            },
            Content = content
        };
    }

    private static AssignmentProgressRow CreateProgressRow(
        TestAssignmentModel assignment)
    {
        var statusByCase =
            assignment.CaseProgress
                .GroupBy(progress => progress.TestCaseId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Last().Status);

        var completed =
            assignment.TestCaseIds.Count(
                testCaseId =>
                    statusByCase.TryGetValue(testCaseId, out var status) &&
                    IsFinalStatus(status));

        var total =
            assignment.TestCaseIds.Count;

        var isCompleted =
            assignment.CompletedAt.HasValue &&
            total > 0 &&
            completed == total;

        DateTime? completedAt =
            null;

        if (isCompleted)
        {
            completedAt =
                assignment.CompletedAt?.LocalDateTime ??
                assignment.CaseProgress
                    .Where(progress => IsFinalStatus(progress.Status))
                    .Select(progress => (DateTime?)progress.UpdatedAt.LocalDateTime)
                    .Max() ??
                assignment.UpdatedAt.LocalDateTime;
        }

        return new AssignmentProgressRow(
            assignment,
            assignment.RecipientLogin,
            assignment.ProjectName,
            assignment.ApplicationVersion,
            total,
            completed,
            Math.Max(0, total - completed),
            total == 0
                ? 0
                : Math.Round(completed * 100.0 / total, 1),
            isCompleted,
            completedAt,
            assignment.UpdatedAt.LocalDateTime);
    }

    private Border CreateAssignmentCard(
        AssignmentProgressRow row)
    {
        var header =
            new Grid
            {
                ColumnDefinitions =
                    new ColumnDefinitions("Auto,*,Auto,Auto"),
                ColumnSpacing = 8
            };

        var title =
            new TextBlock
            {
                Text = $"{row.RecipientLogin}  •  {row.ProjectName}  •  v{row.Version}",
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

        Grid.SetColumn(title, 1);
        header.Children.Add(title);

        if (CanSeeTeamProgress)
        {
            var selection =
                new CheckBox
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0)
                };

            selection.Click += (_, _) => UpdateStopSelectedButton();
            _activeSessionSelection[row.Assignment.Id] = selection;
            Grid.SetColumn(selection, 0);
            header.Children.Add(selection);
        }

        var state =
            new Border
            {
                MinWidth = _dashboardSection == DashboardSection.Archive ? 62 : 72,
                Height = _dashboardSection == DashboardSection.Archive ? 20 : 24,
                Padding = new Thickness(
                    _dashboardSection == DashboardSection.Archive ? 6 : 8,
                    0),
                CornerRadius = new CornerRadius(
                    _dashboardSection == DashboardSection.Archive ? 8 : 10),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(
                    Color.Parse(
                        _dashboardSection == DashboardSection.Archive
                            ? "#243F7182"
                            : row.IsCompleted
                            ? "#1828C76F"
                            : "#182E86D1")),
                Child = new TextBlock
                {
                    Text = _dashboardSection == DashboardSection.Archive
                        ? "ARCHIWUM"
                        : row.IsCompleted
                        ? "UKOŃCZONO"
                        : row.Assignment.IsPaused
                        ? "WSTRZYMANO"
                        : "IN PROGRESS",
                    FontSize = _dashboardSection == DashboardSection.Archive ? 8 : 9,
                    LineHeight = _dashboardSection == DashboardSection.Archive ? 10 : double.NaN,
                    FontWeight = FontWeight.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(
                        Color.Parse(
                            _dashboardSection == DashboardSection.Archive
                                ? "#7A8B99"
                                : row.IsCompleted
                                ? "#159454"
                                : row.Assignment.IsPaused
                                ? "#C26A10"
                                : "#2E86D1"))
                }
            };

        Grid.SetColumn(state, 2);
        header.Children.Add(state);

        if (_dashboardSection == DashboardSection.History &&
            CanSeeTeamProgress &&
            !row.Assignment.IsArchived)
        {
            var archiveButton =
                new Button
                {
                    Content = "×",
                    Width = 31,
                    Height = 31,
                    Padding = new Thickness(0),
                    FontSize = 19,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#DC4C56")),
                    Background = new SolidColorBrush(Color.Parse("#18DC4C56")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#55DC4C56")),
                    CornerRadius = new CornerRadius(9)
                };

            ToolTip.SetTip(
                archiveButton,
                "Przenieś ukończoną sesję do archiwum");

            archiveButton.Click +=
                async (_, _) =>
                    await ArchiveRowAsync(row);

            Grid.SetColumn(archiveButton, 3);
            header.Children.Add(archiveButton);
        }
        else if (_dashboardSection == DashboardSection.Active &&
                 CanSeeTeamProgress && !row.IsCompleted)
        {
            var stopButton =
                new Button
                {
                    Content = "×",
                    Width = 31,
                    Height = 31,
                    Padding = new Thickness(0),
                    FontSize = 19,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#DC4C56")),
                    Background = new SolidColorBrush(Color.Parse("#18DC4C56")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#55DC4C56")),
                    CornerRadius = new CornerRadius(9)
                };

            ToolTip.SetTip(stopButton, "Zatrzymaj tę przypisaną sesję");
            stopButton.Click += async (_, _) =>
                await StopAssignmentsAsync(new[] { row.Assignment.Id });
            Grid.SetColumn(stopButton, 3);
            header.Children.Add(stopButton);
        }

        var progressBar =
            new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = row.Percent,
                Height = 7,
                Foreground = new SolidColorBrush(
                    Color.Parse(row.IsCompleted ? "#28C76F" : "#2E86D1"))
            };

        var card =
            new Border
            {
                Padding = new Thickness(10, 8),
                Background = new SolidColorBrush(
                    Color.Parse(row.IsCompleted ? "#1028C76F" : "#0E2E86D1")),
                BorderBrush = new SolidColorBrush(
                    Color.Parse(row.IsCompleted ? "#7028C76F" : "#702E86D1")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Child = new StackPanel
                {
                    Spacing = 6,
                    Children =
                    {
                        header,
                        progressBar,
                        new TextBlock
                        {
                            Text = $"Wykonane {row.Completed} z {row.Total}  •  pozostało {row.Remaining}  •  {row.Percent:0.#}%",
                            FontSize = 11,
                            Foreground = Brushes.Gray
                        }
                    }
                }
            };

        if (_dashboardSection == DashboardSection.History &&
            CanSeeTeamProgress &&
            !row.Assignment.IsArchived)
        {
            var archiveMenuItem =
                new MenuItem
                {
                    Header = "Przenieś do archiwum"
                };

            archiveMenuItem.Click +=
                async (_, _) =>
                    await ArchiveRowAsync(row);

            card.ContextMenu =
                new ContextMenu
                {
                    Items =
                    {
                        archiveMenuItem
                    }
                };
        }

        return card;
    }

    private async Task ArchiveRowAsync(
        AssignmentProgressRow row)
    {
        var assignmentIds =
            GetAssignmentIdsForBatches(
                new[] { row.Assignment.Id });

        var confirmation =
            new ConfirmDeleteWindow(
                "Przenieść sesję do archiwum?",
                $"Sesja {row.RecipientLogin}, {row.ProjectName}, wersja {row.Version}, zostanie przeniesiona z historii do archiwum. Po 60 dniach zostanie trwale usunięta, chyba że wcześniej ją przywrócisz.",
                "ARCHIWIZUJ");

        if (!await confirmation.ShowDialog<bool>(this))
        {
            return;
        }

        await _assignmentService.ArchiveAssignmentsAsync(
            assignmentIds);

        await LoadDashboardAsync();
    }

    private void ActiveSessionsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _dashboardSection = DashboardSection.Active;
        _ = LoadDashboardAsync();
    }

    private void HistorySessionsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _dashboardSection = DashboardSection.History;
        _ = LoadDashboardAsync();
    }

    private void ArchiveSessionsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _dashboardSection = DashboardSection.Archive;
        _ = LoadDashboardAsync();
    }

    private async void StopSelectedSessionsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var selectedIds =
            _activeSessionSelection
                .Where(pair => pair.Value.IsChecked == true)
                .Select(pair => pair.Key)
                .ToArray();

        if (_dashboardSection == DashboardSection.Archive)
        {
            await DeleteArchivedAssignmentsAsync(
                GetAssignmentIdsForBatches(selectedIds));
            return;
        }

        if (_dashboardSection == DashboardSection.History)
        {
            await ArchiveAssignmentsAsync(
                GetAssignmentIdsForBatches(selectedIds));
            return;
        }

        await StopAssignmentsAsync(selectedIds);
    }

    private async void RestoreArchivedSessionsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var selectedIds =
            GetAssignmentIdsForBatches(
                _activeSessionSelection
                    .Where(pair => pair.Value.IsChecked == true)
                    .Select(pair => pair.Key));

        if (selectedIds.Length == 0)
        {
            return;
        }

        var confirmation =
            new ConfirmDeleteWindow(
                "Przywrócić zaznaczone sesje?",
                "Sesje wrócą z archiwum do historii i zachowają pierwotną kolejność oraz daty.",
                "PRZYWRÓĆ");

        if (!await confirmation.ShowDialog<bool>(this))
        {
            return;
        }

        await _assignmentService.RestoreArchivedAssignmentsAsync(selectedIds);
        await LoadDashboardAsync();
    }

    private async Task ArchiveAssignmentsAsync(
        IReadOnlyCollection<Guid> assignmentIds)
    {
        if (assignmentIds.Count == 0)
        {
            return;
        }

        var confirmation =
            new ConfirmDeleteWindow(
                "Przenieść zaznaczone sesje do archiwum?",
                "Sesje znikną z historii i pozostaną w archiwum przez 60 dni. W tym czasie można je przywrócić. Trwałe usunięcie jest dostępne wyłącznie w archiwum.",
                "PRZENIEŚ");

        if (!await confirmation.ShowDialog<bool>(this))
        {
            return;
        }

        await _assignmentService.ArchiveAssignmentsAsync(assignmentIds);
        await LoadDashboardAsync();
    }

    private Guid[] GetAssignmentIdsForBatches(
        IEnumerable<Guid> assignmentIds)
    {
        var selectedIds = assignmentIds.ToHashSet();
        var selectedBatchIds =
            _visibleAssignments
                .Where(assignment => selectedIds.Contains(assignment.Id))
                .Select(GetBatchId)
                .ToHashSet();

        return _visibleAssignments
            .Where(assignment => selectedBatchIds.Contains(GetBatchId(assignment)))
            .Select(assignment => assignment.Id)
            .Distinct()
            .ToArray();
    }

    private async void DeleteAllArchivedSessionsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var confirmation =
            new ConfirmDeleteWindow(
                "Usunąć całe archiwum?",
                "Ta operacja trwale usunie wszystkie zarchiwizowane sesje oraz powiązane z nimi powiadomienia.",
                "USUŃ WSZYSTKO");

        if (!await confirmation.ShowDialog<bool>(this))
        {
            return;
        }

        await _assignmentService.DeleteAllArchivedAssignmentsAsync();
        await LoadDashboardAsync();
    }

    private async Task DeleteArchivedAssignmentsAsync(
        IReadOnlyCollection<Guid> assignmentIds)
    {
        if (assignmentIds.Count == 0)
        {
            return;
        }

        var confirmation =
            new ConfirmDeleteWindow(
                "Usunąć zaznaczone sesje z archiwum?",
                $"Ta operacja trwale usunie {assignmentIds.Count} sesji oraz powiązane z nimi powiadomienia.",
                "USUŃ TRWALE");

        if (!await confirmation.ShowDialog<bool>(this))
        {
            return;
        }

        await _assignmentService.DeleteArchivedAssignmentsAsync(assignmentIds);
        await LoadDashboardAsync();
    }

    private async Task StopAssignmentsAsync(IReadOnlyCollection<Guid> assignmentIds)
    {
        if (assignmentIds.Count == 0)
        {
            return;
        }

        var selectedAssignments =
            _visibleAssignments
                .Where(assignment => assignmentIds.Contains(assignment.Id))
                .ToArray();

        var startedCount =
            selectedAssignments.Count(
                assignment =>
                    assignment.StartedAt.HasValue ||
                    assignment.CaseProgress.Any(progress => IsFinalStatus(progress.Status)));

        var details =
            startedCount > 0
                ? $"Wybrano {assignmentIds.Count} sesji, w tym {startedCount} już rozpoczętych. Testerzy zostaną przeniesieni do trybu ad-hoc i otrzymają powiadomienie."
                : $"Wybrano {assignmentIds.Count} nierozpoczętych sesji. Zostaną usunięte, a ich przypadki ponownie będą dostępne.";

        var confirmation =
            new ConfirmDeleteWindow(
                "Zatrzymać wybrane sesje?",
                details,
                "ZATRZYMAJ SESJE");

        if (!await confirmation.ShowDialog<bool>(this))
        {
            return;
        }

        foreach (var assignmentId in assignmentIds)
        {
            await _assignmentService.WithdrawAssignmentAsync(assignmentId, _login);
        }

        await LoadDashboardAsync();
    }

    private void UpdateStopSelectedButton()
    {
        var anySelected =
            _activeSessionSelection.Values.Any(checkBox => checkBox.IsChecked == true);

        StopSelectedSessionsButton.IsEnabled = anySelected;
        RestoreArchivedSessionsButton.IsEnabled = anySelected;
    }

    private void UpdateDashboardNavigation()
    {
        if (!CanSeeTeamProgress)
        {
            return;
        }

        var active = _dashboardSection == DashboardSection.Active;
        var history = _dashboardSection == DashboardSection.History;
        var archive = _dashboardSection == DashboardSection.Archive;
        ActiveSessionsButton.Background = new SolidColorBrush(Color.Parse(active ? "#2E86D1" : "#00000000"));
        ActiveSessionsButton.Foreground = active ? Brushes.White : Brushes.Gray;
        HistorySessionsButton.Background = new SolidColorBrush(Color.Parse(history ? "#2E86D1" : "#00000000"));
        HistorySessionsButton.Foreground = history ? Brushes.White : Brushes.Gray;
        ArchiveSessionsButton.Background = new SolidColorBrush(Color.Parse(archive ? "#2E86D1" : "#00000000"));
        ArchiveSessionsButton.Foreground = archive ? Brushes.White : Brushes.Gray;
        StopSelectedSessionsButton.IsVisible = true;
        StopSelectedSessionsButton.Content = archive
            ? "USUŃ ZAZNACZONE"
            : history
            ? "PRZENIEŚ DO ARCHIWUM"
            : "ZATRZYMAJ ZAZNACZONE";
        RestoreArchivedSessionsButton.IsVisible = archive;
        RestoreArchivedSessionsButton.IsEnabled = false;
        DeleteAllArchivedSessionsButton.IsVisible = archive;
        DeleteAllArchivedSessionsButton.IsEnabled =
            archive && _visibleAssignments.Length > 0;
        StopSelectedSessionsButton.IsEnabled = false;
    }

    private void BuildTeamOverview(
        IReadOnlyCollection<AssignmentProgressRow> rows)
    {
        TeamPieChartCanvas.Children.Clear();
        TeamLegendPanel.Children.Clear();

        var people =
            rows
                .GroupBy(row => row.RecipientLogin, StringComparer.OrdinalIgnoreCase)
                .Select(
                    group =>
                        new PersonProgress(
                            group.Key,
                            group.Sum(row => row.Total),
                            group.Sum(row => row.Completed)))
                .OrderByDescending(person => person.Total)
                .ThenBy(person => person.Login, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        TeamOverviewPanel.IsVisible =
            CanSeeTeamProgress &&
            people.Length > 0;

        var teamTotal =
            rows.Sum(row => row.Total);

        var teamCompleted =
            rows.Sum(row => row.Completed);

        var teamPercent =
            teamTotal == 0
                ? 0
                : Math.Round(
                    teamCompleted * 100.0 / teamTotal,
                    1);

        TeamCompletionProgressBar.Value =
            teamPercent;

        TeamCompletionPercentTextBlock.Text =
            $"{teamPercent:0.#}%";

        if (!TeamOverviewPanel.IsVisible)
        {
            return;
        }

        const double center = 75;
        const double radius = 64;
        var grandTotal =
            Math.Max(1, people.Sum(person => person.Total));
        var currentAngle =
            -90.0;

        for (var index = 0; index < people.Length; index++)
        {
            var person =
                people[index];

            var colorText =
                ChartColors[index % ChartColors.Length];

            var brush =
                new SolidColorBrush(Color.Parse(colorText));

            var sweep =
                person.Total * 360.0 / grandTotal;

            if (people.Length == 1 || sweep >= 359.999)
            {
                var circle =
                    new Ellipse
                    {
                        Width = radius * 2,
                        Height = radius * 2,
                        Fill = brush
                    };

                Canvas.SetLeft(circle, center - radius);
                Canvas.SetTop(circle, center - radius);
                TeamPieChartCanvas.Children.Add(circle);
            }
            else
            {
                var startRadians =
                    currentAngle * Math.PI / 180.0;
                var endRadians =
                    (currentAngle + sweep) * Math.PI / 180.0;

                var startX = center + radius * Math.Cos(startRadians);
                var startY = center + radius * Math.Sin(startRadians);
                var endX = center + radius * Math.Cos(endRadians);
                var endY = center + radius * Math.Sin(endRadians);
                var largeArc = sweep > 180 ? 1 : 0;

                var geometryText =
                    FormattableString.Invariant(
                        $"M {center} {center} L {startX} {startY} A {radius} {radius} 0 {largeArc} 1 {endX} {endY} Z");

                TeamPieChartCanvas.Children.Add(
                    new Path
                    {
                        Data = Geometry.Parse(geometryText),
                        Fill = brush,
                        Stroke = new SolidColorBrush(Color.Parse("#40FFFFFF")),
                        StrokeThickness = 1
                    });
            }

            currentAngle +=
                sweep;

            var percent =
                person.Total == 0
                    ? 0
                    : Math.Round(person.Completed * 100.0 / person.Total, 1);

            var legendItem =
                new Grid
                {
                    Width = 245,
                    Margin = new Thickness(0, 0, 12, 8),
                    ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                    ColumnSpacing = 9
                };

            legendItem.Children.Add(
                new Border
                {
                    Width = 13,
                    Height = 13,
                    CornerRadius = new CornerRadius(4),
                    Background = brush,
                    VerticalAlignment = VerticalAlignment.Center
                });

            var legendText =
                new TextBlock
                {
                    Text = $"{person.Login} — {person.Completed}/{person.Total} ({percent:0.#}%)",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold
                };

            Grid.SetColumn(legendText, 1);
            legendItem.Children.Add(legendText);
            TeamLegendPanel.Children.Add(legendItem);
        }

        var hole =
            new Ellipse
            {
                Width = 68,
                Height = 68,
                Fill = new SolidColorBrush(
                    Color.Parse(
                        Application.Current?.ActualThemeVariant ==
                        Avalonia.Styling.ThemeVariant.Dark
                            ? "#18231E"
                            : "#FFFFFF"))
            };

        Canvas.SetLeft(hole, center - 34);
        Canvas.SetTop(hole, center - 34);
        TeamPieChartCanvas.Children.Add(hole);

        var centerText =
            new TextBlock
            {
                Text = $"{rows.Sum(row => row.Completed)}\nwykonano",
                Width = 68,
                TextAlignment = TextAlignment.Center,
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(
                    Color.Parse(
                        Application.Current?.ActualThemeVariant ==
                        Avalonia.Styling.ThemeVariant.Dark
                            ? "#F4F7F5"
                            : "#111111"))
            };

        Canvas.SetLeft(centerText, center - 34);
        Canvas.SetTop(centerText, center - 16);
        TeamPieChartCanvas.Children.Add(centerText);
    }

    private static AssignmentProgressRow[] GetReportableRows(
        IEnumerable<AssignmentProgressRow> rows,
        DashboardSection section)
    {
        var allRows = rows.ToArray();

        if (section == DashboardSection.Archive)
        {
            return Array.Empty<AssignmentProgressRow>();
        }

        if (section == DashboardSection.History)
        {
            return allRows
                .Where(row =>
                    !row.Assignment.IsActive &&
                    !row.Assignment.IsArchived &&
                    row.IsCompleted)
                .ToArray();
        }

        return allRows
            .GroupBy(row => GetBatchId(row.Assignment))
            .Where(group =>
                group.Any() &&
                group.All(row => row.Assignment.IsActive && row.IsCompleted))
            .SelectMany(group => group)
            .ToArray();
    }

    private static AssignmentProgressRow[] GetCurrentRowsForTeamOverview(
        IEnumerable<AssignmentProgressRow> rows,
        DashboardSection section)
    {
        return section switch
        {
            DashboardSection.Active =>
                rows
                    .Where(row => row.Assignment.IsActive)
                    .ToArray(),

            DashboardSection.History =>
                rows
                    .Where(row => !row.Assignment.IsActive && !row.Assignment.IsArchived)
                    .ToArray(),

            _ =>
                rows.ToArray()
        };
    }

    private async Task RefreshDashboardInBackgroundAsync()
    {
        if (_backgroundRefreshInProgress)
        {
            return;
        }

        _backgroundRefreshInProgress =
            true;

        try
        {
            await LoadDashboardAsync();
        }
        finally
        {
            _backgroundRefreshInProgress =
                false;
        }
    }

    private static string CreateDataSignature(
        IEnumerable<TestAssignmentModel> assignments,
        DashboardSection section)
    {
        return string.Join(
            "|",
            assignments
                .OrderBy(assignment => assignment.Id)
                .Select(
                    assignment =>
                        $"{assignment.Id:N}:{assignment.IsActive}:{assignment.IsArchived}:{assignment.IsPaused}:{assignment.CompletedAt?.UtcTicks}:{assignment.WithdrawnAt?.UtcTicks}:{assignment.ReportGeneratedAt?.UtcTicks}:{assignment.ArchivedAt?.UtcTicks}:{assignment.UpdatedAt.UtcTicks}:{assignment.TestCaseIds.Count}:{string.Join(',', assignment.CaseProgress.OrderBy(progress => progress.TestCaseId).Select(progress => $"{progress.TestCaseId:N}={progress.Status}"))}")) +
            $"|section={section}";
    }

    private async void GenerateTeamReportButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!CanSeeTeamProgress ||
            _completedRows.Length == 0)
        {
            return;
        }

        var versions =
            _completedRows
                .Select(row => row.Version)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var reportVersion =
            versions.Length == 1
                ? versions[0]
                : "Wiele wersji";

        var dialog =
            new ReportVersionWindow(
                reportVersion,
                $"RAPORT_ZBIORCZY_{DateTime.Now:yyyyMMdd_HHmm}");

        var request =
            await dialog.ShowDialog<ReportExportRequest?>(this);

        if (request is null)
        {
            return;
        }

        GenerateTeamReportButton.IsEnabled =
            false;

        try
        {
            var report =
                await CreateTeamReportAsync(request.ApplicationVersion);

            var path =
                await _reportExportService.ExportAsync(
                    report,
                    request.DirectoryPath,
                    request.FileNameBase,
                    request.Format);

            if (string.IsNullOrWhiteSpace(path))
            {
                await new OperationResultWindow(
                        false,
                        "Nie udało się utworzyć raportu",
                        "Sprawdź miejsce zapisu i spróbuj ponownie.")
                    .ShowDialog(this);
                return;
            }

            await _assignmentService.MarkReportsGeneratedAsync(
                _completedRows.Select(
                    row => row.Assignment.Id));

            await new OperationResultWindow(
                    true,
                    "Raport został zapisany",
                    $"{path}\n\nCały ukończony pakiet przypisań został przeniesiony do historii.")
                .ShowDialog(this);

            await LoadDashboardAsync();
        }
        finally
        {
            GenerateTeamReportButton.IsEnabled =
                true;
        }
    }

    private async Task<TestReport> CreateTeamReportAsync(
        string reportVersion)
    {
        var data =
            await _storageService.LoadAsync();

        var testCasesById =
            data.TestCases.ToDictionary(testCase => testCase.Id);

        var collectionsByKey =
            data.Collections
                .GroupBy(collection => collection.CollectionKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.OrdinalIgnoreCase);

        var reportCases =
            new List<TestReportCase>();

        foreach (var row in _completedRows.OrderBy(item => item.CompletedAt))
        {
            var progressById =
                row.Assignment.CaseProgress
                    .GroupBy(progress => progress.TestCaseId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Last().Status);

            foreach (var testCaseId in row.Assignment.TestCaseIds)
            {
                testCasesById.TryGetValue(testCaseId, out var testCase);

                var collectionName =
                    testCase is not null &&
                    collectionsByKey.TryGetValue(testCase.SectionKey, out var collection)
                        ? collection.Name
                        : "Przypisany zakres";

                reportCases.Add(
                    new TestReportCase
                    {
                        TestType = row.RecipientLogin,
                        Collection = collectionName,
                        Path = $"{row.ProjectName} / wersja {row.Version}",
                        Name = testCase?.Name ?? testCaseId.ToString(),
                        Status = progressById.TryGetValue(testCaseId, out var status)
                            ? status
                            : "InProgress"
                    });
            }
        }

        var success = reportCases.Count(item => item.Status == "Success");
        var failed = reportCases.Count(item => item.Status == "Failed");
        var blocked = reportCases.Count(item => item.Status == "Blocked");
        var notApplicable = reportCases.Count(item => item.Status == "NA");
        var inProgress = reportCases.Count(item => item.Status == "InProgress");
        var notStarted = reportCases.Count(item => item.Status == "None");
        var completed = success + failed + blocked + notApplicable;

        var projects =
            _completedRows
                .Select(row => row.ProjectName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        return new TestReport
        {
            Metadata = new TestReportMetadata
            {
                SessionId = Guid.Empty,
                SessionMode = "Assigned",
                ProjectName = projects.Length == 1 ? projects[0] : "Wiele projektów",
                ApplicationVersion = reportVersion,
                TesterLogin = "Zespół QA",
                GeneratedAt = DateTimeOffset.Now
            },
            Summary = new TestReportSummary
            {
                Total = reportCases.Count,
                Success = success,
                Failed = failed,
                Blocked = blocked,
                NotApplicable = notApplicable,
                InProgress = inProgress,
                NotStarted = notStarted,
                CompletionPercent = reportCases.Count == 0
                    ? 0
                    : Math.Round(completed * 100.0 / reportCases.Count, 2)
            },
            TestCases = reportCases
        };
    }

    private static bool IsFinalStatus(
        string status)
    {
        return status is
            "Success" or
            "Failed" or
            "NA" or
            "Blocked";
    }

    private void CloseButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private enum DashboardSection
    {
        Active,
        History,
        Archive
    }

    private sealed record AssignmentProgressRow(
        TestAssignmentModel Assignment,
        string RecipientLogin,
        string ProjectName,
        string Version,
        int Total,
        int Completed,
        int Remaining,
        double Percent,
        bool IsCompleted,
        DateTime? CompletedAt,
        DateTime UpdatedAt);

    private sealed record PersonProgress(
        string Login,
        int Total,
        int Completed);
}

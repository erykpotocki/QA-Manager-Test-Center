using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using QARegressionManager.Models;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class ExplorerView : UserControl
{
    private const string StatusNone = "None";
    private const string StatusInProgress = "InProgress";
    private const string StatusSuccess = "Success";
    private const string StatusFailed = "Failed";
    private const string StatusNa = "NA";
    private const string StatusBlocked = "Blocked";

    private const string ProjectRootKey = "project-root";
    private const string ProjectTestTypeKey = "PROJECT";
    private const string RegressionTestTypeKey = "REGRESSION";
    private const string FunctionalTestTypeKey = "FUNCTIONAL";
    private const string OtherTestTypeKey = "OTHER";

    private readonly string _projectName;
    private readonly string _projectKey;
    private string _applicationVersion;
    private readonly string _testerName;
    private readonly string _loggedInLogin;
    private readonly string _highestSystemRole;
    private readonly IReadOnlyList<string> _systemRoles;
    private IReadOnlyList<string> _projectRoles;
    private readonly Dictionary<string, string> _projectRoleColors =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _projectRoleBackgroundColors =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _projectRoleTextColors =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _currentProjectRoleNames =
        new(StringComparer.OrdinalIgnoreCase);
    private bool _projectRoleScopeLoaded;

    private readonly JsonStorageService _jsonStorageService;
    private readonly UserTestCaseService _userTestCaseService;
    private readonly UserFolderService _userFolderService;
    private readonly UserCollectionService _userCollectionService;
    private readonly TestReportExportService _testReportExportService;
    private readonly AssignmentService _assignmentService =
        new();
    private readonly UserProfileService _userProfileService =
        new();

    private readonly SessionManager? _sessionManager;
    private readonly SessionStateModel? _sessionState;
    private readonly Action? _logoutAction;
    private readonly Action? _returnToStartAction;
    private readonly string? _savedTestTypeKey;
    private readonly string? _savedCollectionKey;

    private readonly List<FolderData> _folders;
    private readonly List<TestCollectionData> _collections;

    private bool _isDarkMode;
    private bool _userDataLoaded;
    private int _currentCollectionIndex = -1;
    private int _testCaseScrollAnimationVersion;
    private int _treeScrollAnimationVersion;
    private string? _lastAutoScrollCollectionKey;
    private int _lastAutoScrollCaseIndex = -1;

    private TextBlock? _projectInfoTextBlock;
    private TextBlock? _loggedInUserTextBlock;
    private ScrollViewer? _roleBadgesScrollViewer;
    private StackPanel? _roleBadgesPanel;
    private Button? _roleOverflowButton;
    private StackPanel? _hiddenRoleBadgesPanel;
    private int _visibleRoleBadgeCount = -1;
    private ScrollViewer? _testCasesScrollViewer;
    private TextBlock? _themeIconTextBlock;
    private Button? _themeToggleButton;
    private Button? _projectToolsButton;
    private Button? _notificationCenterButton;
    private Border? _notificationBadgeBorder;
    private TextBlock? _notificationBadgeTextBlock;
    private Button? _executeAssignedTestsButton;
    private Button? _restartAssignedTestsButton;
    private Button? _finishEarlyButton;
    private TextBlock? _executeAssignedTestsLabel;
    private Border? _executeAssignmentPendingDot;
    private Button? _progressDashboardButton;
    private Border? _dashboardPendingReportDot;
    private string _dashboardPendingReportSignature = string.Empty;
    private string _acknowledgedDashboardReportSignature = string.Empty;
    private StackPanel? _adminTestMenuPanel;
    private TreeView? _testTreeView;
    private TextBox? _testTreeSearchTextBox;
    private string _testTreeSearchText = string.Empty;
    private TextBlock? _testTreeTitleTextBlock;
    private ScrollViewer? _testTreeScrollViewer;
    private Grid? _explorerBodyGrid;
    private Border? _testTreePanelBorder;
    private GridSplitter? _testTreeGridSplitter;
    private Button? _toggleCompactTestTreePanelButton;
    private Button? _collapseTestTreePanelButton;
    private double _lastTestTreePanelWidth =
        285;
    private TreePanelState _treePanelState =
        TreePanelState.Full;
    private bool _isCompactTreeTypography;
    private Grid? _contentAreaGrid;
    private ContentControl? _inlineDashboardHost;
    private ProgressDashboardWindow? _inlineDashboardController;
    private AssignmentManagementWindow? _inlineAssignmentController;
    private StackPanel? _welcomePanel;
    private TextBlock? _welcomeTitleTextBlock;
    private TextBlock? _welcomeDescriptionTextBlock;
    private Button? _emptyFolderBackButton;
    private Grid? _testExecutionPanel;
    private TextBlock? _currentSectionTitleTextBlock;
    private TextBlock? _currentSectionPathTextBlock;
    private TextBlock? _currentSectionProgressTextBlock;
    private Button? _addCollectionDescriptionButton;
    private Grid? _collectionDescriptionPanel;
    private TextBlock? _currentCollectionDescriptionTextBlock;
    private StackPanel? _testCasesStackPanel;
    private TextBlock? _successCountTextBlock;
    private TextBlock? _inProgressCountTextBlock;
    private TextBlock? _failedCountTextBlock;
    private TextBlock? _naCountTextBlock;
    private TextBlock? _blockedCountTextBlock;
    private TextBlock? _remainingCountTextBlock;
    private TextBlock? _remainingLabelTextBlock;
    private Button? _previousSectionButton;
    private Button? _nextSectionButton;

    private Grid? _summaryPanel;
    private TextBlock? _summaryCompletedTitleTextBlock;
    private TextBlock? _summarySuccessCountTextBlock;
    private TextBlock? _summaryInProgressCountTextBlock;
    private TextBlock? _summaryFailedCountTextBlock;
    private TextBlock? _summaryNaCountTextBlock;
    private TextBlock? _summaryBlockedCountTextBlock;
    private TextBlock? _summaryRemainingCountTextBlock;
    private StackPanel? _summaryNextTypePanel;
    private TextBlock? _summaryNextTypeNameTextBlock;
    private TextBlock? _summaryNextTypeCaseCountTextBlock;
    private TextBlock? _summaryAllDoneTextBlock;
    private Button? _downloadReportButton;
    private Button? _summaryBackButton;
    private Button? _summaryContinueButton;
    private Border? _completionCelebrationOverlay;

    private string? _lastCompletedTestTypeKey;
    private string? _emptyFolderReturnTestTypeKey;
    private string? _pendingTreeSelectionKey;
    private StructureClipboardItem? _structureClipboard;
    private readonly List<UndoHistoryEntry> _undoHistory =
        new();
    private FolderData? _selectedFolder;
    private TestCollectionData? _selectedCollection;
    private TestCaseData? _selectedTestCase;
    private TestAssignmentModel[] _activeAssignments =
        Array.Empty<TestAssignmentModel>();
    private HashSet<Guid>? _activeAssignmentCaseIds;
    private Dictionary<Guid, Guid> _activeAssignmentIdByCaseId =
        new();
    private Guid? _activeAssignmentId;
    private readonly HashSet<Task> _pendingAssignmentStatusWrites =
        new();
    private Dictionary<Guid, string>? _adHocStatusSnapshot;
    private string? _adHocCollectionKeyBeforeAssignedMode;
    private bool _adHocWasWelcomeBeforeAssignedMode;
    private readonly DispatcherTimer _assignmentGlowTimer =
        new()
        {
            Interval =
                TimeSpan.FromMilliseconds(1600)
        };
    private readonly DispatcherTimer _assignmentValidityTimer =
        new()
        {
            Interval =
                TimeSpan.FromSeconds(3)
        };
    private bool _checkingAssignmentValidity;
    private bool _isFinishingAssignedTests;
    private bool _assignmentGlowBright;
    private int _assignmentDotAnimationVersion;
    private bool _isRefreshing;
    private double _refreshIndicatorAngle;
    private readonly DispatcherTimer _refreshIndicatorTimer =
        new()
        {
            Interval = TimeSpan.FromMilliseconds(55)
        };
    private Border? _refreshIndicatorBorder;
    private RotateTransform? _refreshIndicatorRotateTransform;
    private string? _lastClickedFolderKey;
    private long _lastFolderClickTimestamp;

    public ExplorerView()
        : this(
            DemoCatalog.PrimaryProjectName,
            "Nie podano",
            "Nie podano",
            null,
            null,
            null)
    {
    }

    public ExplorerView(
        string projectName)
        : this(
            projectName,
            "Nie podano",
            "Nie podano",
            null,
            null,
            null)
    {
    }

    public ExplorerView(
        string projectName,
        string applicationVersion,
        string testerName)
        : this(
            projectName,
            applicationVersion,
            testerName,
            null,
            null,
            null)
    {
    }

    public ExplorerView(
        string projectName,
        string applicationVersion,
        string testerName,
        SessionManager? sessionManager,
        SessionStateModel? sessionState,
        string? loggedInLogin = null,
        Action? logoutAction = null,
        string? highestSystemRole = null,
        IEnumerable<string>? systemRoles = null,
        IEnumerable<string>? projectRoles = null,
        Action? returnToStartAction = null)
    {
        _projectName =
            projectName;

        _applicationVersion =
            applicationVersion
                ?.Trim()
            ?? string.Empty;

        _testerName =
            string.IsNullOrWhiteSpace(testerName)
                ? "Nie podano"
                : testerName.Trim();

        _loggedInLogin =
            string.IsNullOrWhiteSpace(loggedInLogin)
                ? _testerName
                : loggedInLogin.Trim();

        _highestSystemRole =
            string.IsNullOrWhiteSpace(
                highestSystemRole)
                ? "Tester"
                : highestSystemRole.Trim();

        _systemRoles =
            (systemRoles ?? new[]
            {
                _highestSystemRole
            })
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _projectRoles =
            (projectRoles ?? Enumerable.Empty<string>())
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _sessionManager =
            sessionManager;

        _sessionState =
            sessionState;

        _logoutAction =
            logoutAction;

        _returnToStartAction =
            returnToStartAction;

        _savedTestTypeKey =
            sessionState?.LastOpenedTestType;

        _savedCollectionKey =
            sessionState?.LastOpenedCollectionKey;

        _projectKey =
            CreateProjectKey(
                projectName);

        _jsonStorageService =
            new JsonStorageService();

        _userTestCaseService =
            new UserTestCaseService(
                _jsonStorageService);

        _userFolderService =
            new UserFolderService(
                _jsonStorageService);

        _userCollectionService =
            new UserCollectionService(
                _jsonStorageService);

        _testReportExportService =
            new TestReportExportService();

        InitializeComponent();
        FindControls();

        SizeChanged +=
            (_, _) =>
                RefreshRoleBadges();

        _refreshIndicatorTimer.Tick +=
            (_, _) =>
            {
                _refreshIndicatorAngle =
                    (_refreshIndicatorAngle + 24) % 360;
                if (_refreshIndicatorRotateTransform is not null)
                {
                    _refreshIndicatorRotateTransform.Angle =
                        _refreshIndicatorAngle;
                }
            };

        _assignmentGlowTimer.Tick +=
            (_, _) =>
            {
                if (_executeAssignedTestsButton is null ||
                    !_executeAssignedTestsButton.IsVisible)
                {
                    _assignmentGlowTimer.Stop();
                    return;
                }

                _assignmentGlowBright =
                    !_assignmentGlowBright;

                _executeAssignedTestsButton.Opacity =
                    _assignmentGlowBright
                        ? 1
                        : 0.9;
            };

        _assignmentValidityTimer.Tick +=
            async (_, _) =>
            {
                await CheckActiveAssignmentValidityAsync();
            };

        AddHandler(
            KeyDownEvent,
            ExplorerView_OnPreviewKeyDown,
            RoutingStrategies.Tunnel);

        AttachedToVisualTree +=
            async (_, _) =>
            {
                await LoadProjectRoleColorsAsync();
                await RefreshAssignmentAndNotificationStateAsync();
            };

        LocalizationService.LanguageChanged +=
            LocalizationService_OnLanguageChanged;

        DetachedFromVisualTree +=
            (_, _) =>
            {
                LocalizationService.LanguageChanged -=
                    LocalizationService_OnLanguageChanged;

                HideInlineDashboard();
            };

        _folders =
            CreateSystemFolders();

        _collections =
            CreateSystemCollections();

        InitializeDefaultSortOrders();
        RefreshCollectionPaths();

        if (_projectInfoTextBlock is not null)
        {
            var isAssignedSession =
                string.Equals(
                    _sessionState?.SessionMode,
                    "Assigned",
                    StringComparison.OrdinalIgnoreCase);

            _projectInfoTextBlock.Text =
                isAssignedSession &&
                !string.IsNullOrWhiteSpace(
                    _applicationVersion)
                    ? $"{_projectName} • {LocalizationService.T("Common.RegressionExecution")} • v{_applicationVersion}"
                    : $"{_projectName} • {(isAssignedSession ? LocalizationService.T("Common.RegressionExecution") : "ad-hoc")}";
        }

        if (_loggedInUserTextBlock is not null)
        {
            _loggedInUserTextBlock.Text =
                string.Format(
                    LocalizationService.T("Common.LoggedIn"),
                    _loggedInLogin);
        }

        BuildRoleBadges();

        if (_adminTestMenuPanel is not null)
        {
            _adminTestMenuPanel.IsVisible =
                CanAssignTests;
        }

        _isDarkMode =
            Application.Current?.RequestedThemeVariant ==
            ThemeVariant.Dark;

        BuildTestTree();
        UpdateThemeButton();

        RefreshRoleBadges();
        UpdateSessionSummary();

        AttachedToVisualTree +=
            async (_, _) =>
            {
                await LoadUserDataAsync();
                RestoreSavedSessionLocation();
            };
    }

    private void RestoreSavedSessionLocation()
    {
        TestCollectionData? collection =
            null;

        if (!string.IsNullOrWhiteSpace(
                _savedCollectionKey))
        {
            collection =
                _collections.FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Key,
                            _savedCollectionKey,
                            StringComparison.OrdinalIgnoreCase));
        }

        if (collection is null &&
            !string.IsNullOrWhiteSpace(
                _savedTestTypeKey))
        {
            collection =
                _collections
                    .Where(
                        item =>
                            string.Equals(
                                item.TestTypeKey,
                                _savedTestTypeKey,
                                StringComparison.OrdinalIgnoreCase))
                    .OrderBy(
                        item =>
                            item.SortOrder)
                    .FirstOrDefault();
        }

        collection ??=
            GetCollectionsForTestType(
                    RegressionTestTypeKey)
                .FirstOrDefault(
                    item => item.Cases.Any(
                        IsCaseVisibleForActiveAssignment));

        collection ??=
            GetCollectionsForTestType(
                    FunctionalTestTypeKey)
                .FirstOrDefault(
                    item => item.Cases.Any(
                        IsCaseVisibleForActiveAssignment));

        if (collection is not null)
        {
            SelectCollection(
                collection,
                revealInTree: false,
                expandPath: false);
        }
    }

    private async Task TrackCurrentLocationAsync(
        TestCollectionData collection)
    {
        if (_sessionManager is null ||
            _sessionState is null)
        {
            return;
        }

        await _sessionManager.UpdateLastOpenedLocationAsync(
            _sessionState,
            collection.TestTypeKey,
            collection.Key);
    }

    private async Task TrackResultChangeAsync(
        string? testName = null)
    {
        if (_sessionManager is null ||
            _sessionState is null)
        {
            return;
        }

        await _sessionManager.MarkResultChangedAsync(
            _sessionState,
            testName);
    }

    private void InitializeDefaultSortOrders()
    {
        for (var collectionIndex = 0;
             collectionIndex < _collections.Count;
             collectionIndex++)
        {
            var collection =
                _collections[collectionIndex];

            collection.SortOrder =
                (collectionIndex + 1) * 1000;

            for (var caseIndex = 0;
                 caseIndex < collection.Cases.Count;
                 caseIndex++)
            {
                collection.Cases[caseIndex].SortOrder =
                    (caseIndex + 1) * 1000;
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(
            this);
    }

    private void FindControls()
    {
        _projectInfoTextBlock =
            this.FindControl<TextBlock>(
                "ProjectInfoTextBlock");

        _loggedInUserTextBlock =
            this.FindControl<TextBlock>(
                "LoggedInUserTextBlock");

        _refreshIndicatorBorder =
            this.FindControl<Border>(
                "RefreshIndicatorBorder");

        var refreshIndicatorIcon =
            this.FindControl<TextBlock>(
                "RefreshIndicatorIcon");

        if (refreshIndicatorIcon is not null)
        {
            _refreshIndicatorRotateTransform = new RotateTransform();
            refreshIndicatorIcon.RenderTransform =
                _refreshIndicatorRotateTransform;
        }

        _roleBadgesScrollViewer =
            this.FindControl<ScrollViewer>(
                "RoleBadgesScrollViewer");

        _roleBadgesPanel =
            this.FindControl<StackPanel>(
                "RoleBadgesPanel");

        _roleOverflowButton =
            this.FindControl<Button>(
                "RoleOverflowButton");

        _hiddenRoleBadgesPanel =
            this.FindControl<StackPanel>(
                "HiddenRoleBadgesPanel");

        _testCasesScrollViewer =
            this.FindControl<ScrollViewer>(
                "TestCasesScrollViewer");

        _themeIconTextBlock =
            this.FindControl<TextBlock>(
                "ThemeIconTextBlock");

        _themeToggleButton =
            this.FindControl<Button>(
                "ThemeToggleButton");

        _projectToolsButton =
            this.FindControl<Button>(
                "ProjectToolsButton");

        _notificationCenterButton =
            this.FindControl<Button>(
                "NotificationCenterButton");

        _notificationBadgeBorder =
            this.FindControl<Border>(
                "NotificationBadgeBorder");

        _notificationBadgeTextBlock =
            this.FindControl<TextBlock>(
                "NotificationBadgeTextBlock");

        _executeAssignedTestsButton =
            this.FindControl<Button>(
                "ExecuteAssignedTestsButton");

        _restartAssignedTestsButton =
            this.FindControl<Button>(
                "RestartAssignedTestsButton");

        _finishEarlyButton =
            this.FindControl<Button>(
                "FinishEarlyButton");

        _executeAssignedTestsLabel =
            this.FindControl<TextBlock>(
                "ExecuteAssignedTestsLabel");

        _executeAssignmentPendingDot =
            this.FindControl<Border>(
                "ExecuteAssignmentPendingDot");

        _progressDashboardButton =
            this.FindControl<Button>(
                "ProgressDashboardButton");

        _dashboardPendingReportDot =
            this.FindControl<Border>(
                "DashboardPendingReportDot");

        _adminTestMenuPanel =
            this.FindControl<StackPanel>(
                "AdminTestMenuPanel");

        _testTreeView =
            this.FindControl<TreeView>(
                "TestTreeView");

        _testTreeSearchTextBox =
            this.FindControl<TextBox>(
                "TestTreeSearchTextBox");

        _testTreeTitleTextBlock =
            this.FindControl<TextBlock>(
                "TestTreeTitleTextBlock");

        _explorerBodyGrid =
            this.FindControl<Grid>(
                "ExplorerBodyGrid");

        _testTreePanelBorder =
            this.FindControl<Border>(
                "TestTreePanelBorder");

        _testTreeGridSplitter =
            this.FindControl<GridSplitter>(
                "TestTreeGridSplitter");

        _toggleCompactTestTreePanelButton =
            this.FindControl<Button>(
                "ToggleCompactTestTreePanelButton");

        _collapseTestTreePanelButton =
            this.FindControl<Button>(
                "CollapseTestTreePanelButton");

        if (_explorerBodyGrid is not null)
        {
            _explorerBodyGrid.LayoutUpdated +=
                (_, _) =>
                    UpdateTreePanelTypography();
        }

        if (_testTreeGridSplitter is not null)
        {
            _testTreeGridSplitter.PointerReleased +=
                (_, _) =>
                {
                    if (_treePanelState ==
                        TreePanelState.Collapsed)
                    {
                        return;
                    }

                    var width =
                        GetCurrentTreePanelWidth();

                    _lastTestTreePanelWidth =
                        Math.Clamp(
                            width,
                            260,
                            460);

                    _treePanelState =
                        TreePanelState.Full;

                    var leftColumn =
                        _explorerBodyGrid?
                            .ColumnDefinitions[0];

                    if (leftColumn is not null &&
                        width < 260)
                    {
                        leftColumn.Width =
                            new GridLength(260);
                    }

                    UpdateTreePanelButtons();
                };
        }

        _testTreeScrollViewer =
            this.FindControl<ScrollViewer>(
                "TestTreeScrollViewer");

        _contentAreaGrid =
            this.FindControl<Grid>(
                "ContentAreaGrid");

        _inlineDashboardHost =
            this.FindControl<ContentControl>(
                "InlineDashboardHost");

        _welcomePanel =
            this.FindControl<StackPanel>(
                "WelcomePanel");

        _welcomeTitleTextBlock =
            this.FindControl<TextBlock>(
                "WelcomeTitleTextBlock");

        _welcomeDescriptionTextBlock =
            this.FindControl<TextBlock>(
                "WelcomeDescriptionTextBlock");

        _emptyFolderBackButton =
            this.FindControl<Button>(
                "EmptyFolderBackButton");

        _testExecutionPanel =
            this.FindControl<Grid>(
                "TestExecutionPanel");

        _currentSectionTitleTextBlock =
            this.FindControl<TextBlock>(
                "CurrentSectionTitleTextBlock");

        _currentSectionPathTextBlock =
            this.FindControl<TextBlock>(
                "CurrentSectionPathTextBlock");

        _currentSectionProgressTextBlock =
            this.FindControl<TextBlock>(
                "CurrentSectionProgressTextBlock");

        _addCollectionDescriptionButton =
            this.FindControl<Button>(
                "AddCollectionDescriptionButton");

        _collectionDescriptionPanel =
            this.FindControl<Grid>(
                "CollectionDescriptionPanel");

        _currentCollectionDescriptionTextBlock =
            this.FindControl<TextBlock>(
                "CurrentCollectionDescriptionTextBlock");

        _testCasesStackPanel =
            this.FindControl<StackPanel>(
                "TestCasesStackPanel");

        _successCountTextBlock =
            this.FindControl<TextBlock>(
                "SuccessCountTextBlock");

        _inProgressCountTextBlock =
            this.FindControl<TextBlock>(
                "InProgressCountTextBlock");

        _failedCountTextBlock =
            this.FindControl<TextBlock>(
                "FailedCountTextBlock");

        _naCountTextBlock =
            this.FindControl<TextBlock>(
                "NaCountTextBlock");

        _blockedCountTextBlock =
            this.FindControl<TextBlock>(
                "BlockedCountTextBlock");

        _remainingCountTextBlock =
            this.FindControl<TextBlock>(
                "RemainingCountTextBlock");

        _remainingLabelTextBlock =
            this.FindControl<TextBlock>(
                "RemainingLabelTextBlock");

        _previousSectionButton =
            this.FindControl<Button>(
                "PreviousSectionButton");

        _nextSectionButton =
            this.FindControl<Button>(
                "NextSectionButton");

        _summaryPanel =
            this.FindControl<Grid>(
                "SummaryPanel");

        _summaryCompletedTitleTextBlock =
            this.FindControl<TextBlock>(
                "SummaryCompletedTitleTextBlock");

        _summarySuccessCountTextBlock =
            this.FindControl<TextBlock>(
                "SummarySuccessCountTextBlock");

        _summaryInProgressCountTextBlock =
            this.FindControl<TextBlock>(
                "SummaryInProgressCountTextBlock");

        _summaryFailedCountTextBlock =
            this.FindControl<TextBlock>(
                "SummaryFailedCountTextBlock");

        _summaryNaCountTextBlock =
            this.FindControl<TextBlock>(
                "SummaryNaCountTextBlock");

        _summaryBlockedCountTextBlock =
            this.FindControl<TextBlock>(
                "SummaryBlockedCountTextBlock");

        _summaryRemainingCountTextBlock =
            this.FindControl<TextBlock>(
                "SummaryRemainingCountTextBlock");

        _summaryNextTypePanel =
            this.FindControl<StackPanel>(
                "SummaryNextTypePanel");

        _summaryNextTypeNameTextBlock =
            this.FindControl<TextBlock>(
                "SummaryNextTypeNameTextBlock");

        _summaryNextTypeCaseCountTextBlock =
            this.FindControl<TextBlock>(
                "SummaryNextTypeCaseCountTextBlock");

        _summaryAllDoneTextBlock =
            this.FindControl<TextBlock>(
                "SummaryAllDoneTextBlock");

        _downloadReportButton =
            this.FindControl<Button>(
                "DownloadReportButton");

        _summaryBackButton =
            this.FindControl<Button>(
                "SummaryBackButton");

        _summaryContinueButton =
            this.FindControl<Button>(
                "SummaryContinueButton");

        _completionCelebrationOverlay =
            this.FindControl<Border>(
                "CompletionCelebrationOverlay");
    }

    private static string CreateProjectKey(
        string projectName)
    {
        return projectName
            .Trim()
            .Replace(
                " ",
                "_")
            .ToUpperInvariant();
    }

    private static string NormalizeRegressionTerminology(
        string value)
    {
        return string.Equals(
                value,
                "Testy regresyjne",
                StringComparison.OrdinalIgnoreCase)
            ? "Testy regresji"
            : value;
    }

    private List<FolderData> CreateSystemFolders()
    {
        return new List<FolderData>
        {
            CreateSystemFolder(
                ProjectRootKey,
                string.Empty,
                _projectName,
                ProjectTestTypeKey,
                0),

            CreateSystemFolder(
                "regression-root",
                ProjectRootKey,
                "Testy regresji",
                RegressionTestTypeKey,
                1000),

            CreateSystemFolder(
                "functional-root",
                ProjectRootKey,
                "Testy funkcjonalne",
                FunctionalTestTypeKey,
                2000)
        };
    }

    private static FolderData CreateSystemFolder(
        string key,
        string parentKey,
        string name,
        string testTypeKey,
        int sortOrder)
    {
        var isProtected =
            key == ProjectRootKey ||
            key == "regression-root" ||
            key == "functional-root";

        return new FolderData
        {
            Id =
                Guid.Empty,

            Key =
                key,

            ParentKey =
                parentKey,

            Name =
                name,

            TestTypeKey =
                testTypeKey,

            IsSystem =
                true,

            IsProtected =
                isProtected,

            SortOrder =
                sortOrder
        };
    }

    private List<TestCollectionData> CreateSystemCollections() =>
        new();

    private List<TestCollectionData> CreateLegacySystemCollections()
    {
        return new List<TestCollectionData>
        {
            CreateSystemCollection(
                "sales",
                "gui",
                "Sprzedaż",
                "Regresja / GUI",
                "Sprzedaż kartą stykową",
                "Sprzedaż kartą zbliżeniową",
                "Sprzedaż telefonem",
                "Sprzedaż z kodem PIN",
                "Zapis sprzedaży w historii transakcji"),

            CreateSystemCollection(
                "cashback",
                "gui",
                "Cashback",
                "Regresja / GUI",
                "Sprzedaż z usługą cashback",
                "Minimalna kwota cashback",
                "Maksymalna kwota cashback",
                "Odmowa cashback przez hosta",
                "Zapis cashback w historii transakcji"),

            CreateSystemCollection(
                "refund",
                "gui",
                "Zwrot",
                "Regresja / GUI",
                "Zwrot pełnej kwoty",
                "Zwrot częściowy",
                "Zwrot kartą stykową",
                "Zwrot kartą zbliżeniową",
                "Zapis zwrotu w historii transakcji"),

            CreateSystemCollection(
                "menu",
                "gui",
                "Menu",
                "Regresja / GUI",
                "Otwarcie menu głównego",
                "Przejście do historii transakcji",
                "Przejście do konfiguracji",
                "Powrót do ekranu głównego",
                "Obsługa przycisku Wstecz"),

            CreateSystemCollection(
                "theme",
                "gui",
                "Motyw",
                "Regresja / GUI",
                "Jasny motyw",
                "Ciemny motyw",
                "Zmiana motywu podczas pracy",
                "Czytelność tekstu w ciemnym motywie",
                "Zachowanie motywu po zmianie ekranu"),

            CreateSystemCollection(
                "messages",
                "gui",
                "Wiadomości",
                "Regresja / GUI",
                "Komunikat transakcji zaakceptowanej",
                "Komunikat transakcji odrzuconej",
                "Komunikat błędu połączenia",
                "Komunikat anulowania operacji",
                "Poprawność polskich znaków"),

            CreateSystemCollection(
                "app-start",
                "basic",
                "Uruchomienie aplikacji",
                "Regresja / Podstawowe funkcjonalności",
                "Prawidłowe uruchomienie aplikacji",
                "Wyświetlenie ekranu głównego",
                "Uruchomienie bez połączenia",
                "Ponowne uruchomienie aplikacji",
                "Powrót po wymuszonym zamknięciu"),

            CreateSystemCollection(
                "basic-transactions",
                "basic",
                "Podstawowe transakcje",
                "Regresja / Podstawowe funkcjonalności",
                "Podstawowa sprzedaż",
                "Anulowanie sprzedaży",
                "Odmowa transakcji",
                "Ponowienie transakcji",
                "Zapis transakcji w historii"),

            CreateSystemCollection(
                "history",
                "basic",
                "Historia transakcji",
                "Regresja / Podstawowe funkcjonalności",
                "Otwarcie historii",
                "Wyświetlenie ostatniej transakcji",
                "Filtrowanie historii",
                "Szczegóły transakcji",
                "Powrót z historii"),

            CreateSystemCollection(
                "configuration",
                "basic",
                "Konfiguracja",
                "Regresja / Podstawowe funkcjonalności",
                "Otwarcie konfiguracji",
                "Zmiana ustawienia",
                "Zapis ustawienia",
                "Anulowanie zmiany",
                "Przywrócenie ustawień"),

            CreateSystemCollection(
                "communication",
                "basic",
                "Komunikacja",
                "Regresja / Podstawowe funkcjonalności",
                "Połączenie z hostem",
                "Brak połączenia z hostem",
                "Ponowienie połączenia",
                "Zmiana interfejsu komunikacji",
                "Powrót po błędzie komunikacji"),

            CreateSystemCollection(
                "errors",
                "other-functions",
                "Obsługa błędów",
                "Regresja / Inne funkcjonalności",
                "Błąd połączenia",
                "Błąd konfiguracji",
                "Błąd aplikacji",
                "Powrót po błędzie",
                "Czytelność komunikatu błędu"),

            CreateSystemCollection(
                "updates",
                "other-functions",
                "Aktualizacja",
                "Regresja / Inne funkcjonalności",
                "Sprawdzenie dostępności aktualizacji",
                "Rozpoczęcie aktualizacji",
                "Anulowanie aktualizacji",
                "Restart po aktualizacji",
                "Weryfikacja wersji po aktualizacji"),

            CreateSystemCollection(
                "restart",
                "other-functions",
                "Restart aplikacji",
                "Regresja / Inne funkcjonalności",
                "Restart z menu",
                "Restart po błędzie",
                "Restart po zmianie konfiguracji",
                "Zachowanie danych po restarcie",
                "Powrót do ekranu głównego"),

            CreateSystemCollection(
                "logging",
                "other-functions",
                "Logowanie",
                "Regresja / Inne funkcjonalności",
                "Utworzenie logu",
                "Poprawność daty w logu",
                "Poprawność godziny w logu",
                "Zapis błędu w logu",
                "Dostępność pliku logu"),

            CreateSystemCollection(
                "about",
                "other-functions",
                "Informacje o aplikacji",
                "Regresja / Inne funkcjonalności",
                "Otwarcie informacji o aplikacji",
                "Wyświetlenie wersji",
                "Wyświetlenie nazwy aplikacji",
                "Wyświetlenie danych producenta",
                "Powrót do poprzedniego ekranu"),

            CreateSystemCollection(
                "digital-wallets",
                "additional-modules",
                "Portfele cyfrowe",
                "Regresja / Moduły dodatkowe",
                "Uruchomienie płatności mobilnej",
                "Podstawowa operacja portfela cyfrowego",
                "Anulowanie operacji portfela cyfrowego",
                "Obsługa błędu portfela cyfrowego",
                "Powrót z portfela cyfrowego"),

            CreateSystemCollection(
                "self-service",
                "additional-modules",
                "Transakcje samoobsługowe",
                "Regresja / Moduły dodatkowe",
                "Uruchomienie transakcji samoobsługowej",
                "Podstawowa transakcja samoobsługowa",
                "Anulowanie transakcji samoobsługowej",
                "Obsługa błędu transakcji samoobsługowej",
                "Powrót z trybu samoobsługowego"),

            CreateSystemCollection(
                "additional-services",
                "additional-modules",
                "Usługi dodatkowe",
                "Regresja / Moduły dodatkowe",
                "Uruchomienie usługi dodatkowej",
                "Podstawowa operacja usługi",
                "Anulowanie operacji usługi",
                "Obsługa błędu usługi",
                "Powrót z usługi")
        };
    }

    private static TestCollectionData CreateSystemCollection(
        string key,
        string parentFolderKey,
        string name,
        string path,
        params string[] testCases)
    {
        return new TestCollectionData
        {
            Id =
                Guid.Empty,

            Key =
                key,

            ParentFolderKey =
                parentFolderKey,

            Name =
                name,

            Path =
                path,

            TestTypeKey =
                RegressionTestTypeKey,

            IsSystem =
                true,

            IsProtected =
                false,

            Cases =
                testCases
                    .Select(
                        (
                            testCaseName,
                            index) =>
                            new TestCaseData
                            {
                                Id =
                                    CreateStableTestCaseId(
                                        key,
                                        index),

                                Number =
                                    index + 1,

                                Name =
                                    testCaseName,

                                IsSystem =
                                    true
                            })
                    .ToList()
        };
    }


    private static Guid CreateStableTestCaseId(
        string collectionKey,
        int index)
    {
        var source =
            $"QAManager:{collectionKey}:{index}";

        var bytes =
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(
                    source));

        return new Guid(
            bytes.AsSpan(0, 16));
    }

    private static Guid CreateStableEntityId(
        string source)
    {
        var bytes =
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(
                    source));

        return new Guid(
            bytes.AsSpan(0, 16));
    }

    private async Task ShowInformationAsync(string title, string message)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is not null)
        {
            await new OperationResultWindow(true, title, message).ShowDialog(owner);
        }
    }

    private bool IsOwnedByCurrentUser(string createdByLogin) =>
        !string.IsNullOrWhiteSpace(createdByLogin) &&
        string.Equals(
            createdByLogin,
            _loggedInLogin,
            StringComparison.OrdinalIgnoreCase);

    private static bool IsCoreProtectedFolder(FolderData folder) =>
        string.Equals(folder.Key, ProjectRootKey, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(folder.Key, "regression-root", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(folder.Key, "functional-root", StringComparison.OrdinalIgnoreCase);

    private bool CanModifyFolder(FolderData folder) =>
        !IsCoreProtectedFolder(folder) &&
        (CanAssignTests || IsOwnedByCurrentUser(folder.CreatedByLogin));

    private bool CanMoveFolder(FolderData folder) =>
        CanReorderStructure &&
        !string.Equals(folder.Key, ProjectRootKey, StringComparison.OrdinalIgnoreCase) &&
        (CanAssignTests || IsOwnedByCurrentUser(folder.CreatedByLogin));

    private bool CanModifyCollection(TestCollectionData collection) =>
        CanAssignTests || IsOwnedByCurrentUser(collection.CreatedByLogin);

    private bool CanMoveCollection(TestCollectionData collection) =>
        CanReorderStructure && CanModifyCollection(collection);

    private bool CanModifyTestCase(TestCaseData testCase) =>
        CanAssignTests || IsOwnedByCurrentUser(testCase.CreatedByLogin);

    private bool CanMoveTestCase(TestCaseData testCase) =>
        CanReorderStructure && CanModifyTestCase(testCase);

    private static TestStepModel CloneStep(TestStepModel step) =>
        new()
        {
            Number = step.Number,
            Actions = step.Actions,
            ExpectedResults = step.ExpectedResults,
            ExecutionType = step.ExecutionType
        };

    private static void CopyTestCaseDetails(
        TestCaseModel source,
        TestCaseData target)
    {
        target.Summary = source.Summary;
        target.Preconditions = source.Preconditions;
        target.ExternalId = source.ExternalId;
        target.SourceVersion = source.SourceVersion;
        target.Importance = source.Importance;
        target.ExecutionType = source.ExecutionType;
        target.EstimatedDuration = source.EstimatedDuration;
        target.Platforms = source.Platforms.ToList();
        target.Steps = source.Steps.Select(CloneStep).ToList();
    }

    private async Task LoadUserDataAsync()
    {
        if (_userDataLoaded)
        {
            return;
        }

        _userDataLoaded =
            true;

        var data =
            await _jsonStorageService.LoadAsync();

        _folders.Clear();
        _folders.AddRange(CreateSystemFolders());
        _collections.Clear();
        _collections.AddRange(CreateSystemCollections());

        if (DemoDataSeedService.EnsureSeeded(
                data,
                _projectKey,
                _projectName))
        {
            await _jsonStorageService.SaveAsync(data);
        }

        foreach (var folder in data.Folders)
        {
            if (!string.Equals(
                    folder.ProjectKey,
                    _projectKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(
                    folder.TestTypeKey,
                    OtherTestTypeKey,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    folder.SectionKey,
                    "other-tests-root",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var existingFolder =
                _folders.FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Key,
                            folder.SectionKey,
                            StringComparison.OrdinalIgnoreCase));

            if (existingFolder is not null)
            {
                // Rekord systemowego folderu może pełnić rolę trwałego
                // nadpisania kolejności zapisanej przez użytkownika.
                existingFolder.SortOrder =
                    folder.SortOrder;

                if (!existingFolder.IsProtected)
                {
                    existingFolder.Name =
                        NormalizeRegressionTerminology(
                            folder.Name);
                }

                continue;
            }

            _folders.Add(
                new FolderData
                {
                    Id =
                        folder.Id,

                    Key =
                        folder.SectionKey,

                    ParentKey =
                        folder.ParentSectionKey,

                    Name =
                        NormalizeRegressionTerminology(
                            folder.Name),

                    CreatedByLogin =
                        folder.CreatedByLogin,

                    TestTypeKey =
                        folder.TestTypeKey,

                    IsSystem =
                        false,

                    IsProtected =
                        false,

                    RequiresManagerRole =
                        folder.RequiresManagerRole,

                    SortOrder =
                        folder.SortOrder
                });
        }

        foreach (var collection in data.Collections)
        {
            if (!string.Equals(
                    collection.ProjectKey,
                    _projectKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(
                    collection.TestTypeKey,
                    OtherTestTypeKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var existingCollection =
                _collections.FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Key,
                            collection.CollectionKey,
                            StringComparison.OrdinalIgnoreCase));

            if (existingCollection is not null)
            {
                // Rekord systemowego zbioru może przechowywać trwałe
                // nadpisanie kolejności bez tworzenia jego duplikatu.
                existingCollection.SortOrder =
                    collection.SortOrder;

                existingCollection.Name =
                    NormalizeRegressionTerminology(
                        collection.Name);

                existingCollection.Description =
                    collection.Description ?? string.Empty;

                continue;
            }

            _collections.Add(
                new TestCollectionData
                {
                    Id =
                        collection.Id,

                    Key =
                        collection.CollectionKey,

                    ParentFolderKey =
                        collection.ParentFolderKey,

                    Name =
                        NormalizeRegressionTerminology(
                            collection.Name),

                    Description =
                        collection.Description ?? string.Empty,

                    CreatedByLogin =
                        collection.CreatedByLogin,

                    Path =
                        BuildFolderPath(
                            collection.ParentFolderKey),

                    TestTypeKey =
                        collection.TestTypeKey,

                    IsSystem =
                        false,

                    IsProtected =
                        false,

                    RequiresManagerRole =
                        collection.RequiresManagerRole,

                    SortOrder =
                        collection.SortOrder
                });
        }

        foreach (var testCase in data.TestCases)
        {
            if (!string.Equals(
                    testCase.ProjectKey,
                    _projectKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(
                    testCase.TestTypeKey,
                    OtherTestTypeKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var collection =
                _collections.FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Key,
                            testCase.SectionKey,
                            StringComparison.OrdinalIgnoreCase));

            if (collection is null)
            {
                continue;
            }

            var existingCase =
                collection.Cases.FirstOrDefault(
                    item =>
                        item.Id ==
                        testCase.Id);

            if (existingCase is not null)
            {
                existingCase.Name =
                    testCase.Name;

                existingCase.Status =
                    string.IsNullOrWhiteSpace(
                        testCase.Status)
                        ? StatusNone
                        : testCase.Status;

                existingCase.Comment =
                    testCase.Comment ?? string.Empty;

                existingCase.CreatedByLogin =
                    testCase.CreatedByLogin;

                CopyTestCaseDetails(testCase, existingCase);

                continue;
            }

            collection.Cases.Add(
                new TestCaseData
                {
                    Id =
                        testCase.Id,

                    Number =
                        collection.Cases.Count + 1,

                    Name =
                        testCase.Name,

                    CreatedByLogin =
                        testCase.CreatedByLogin,

                    IsSystem =
                        false,

                    IsProtected =
                        false,

                    SortOrder =
                        testCase.SortOrder,

                    Comment =
                        testCase.Comment ?? string.Empty,

                    Summary = testCase.Summary,
                    Preconditions = testCase.Preconditions,
                    ExternalId = testCase.ExternalId,
                    SourceVersion = testCase.SourceVersion,
                    Importance = testCase.Importance,
                    ExecutionType = testCase.ExecutionType,
                    EstimatedDuration = testCase.EstimatedDuration,
                    Platforms = testCase.Platforms.ToList(),
                    Steps = testCase.Steps.Select(CloneStep).ToList(),

                    Status =
                        string.IsNullOrWhiteSpace(
                            testCase.Status)
                            ? StatusNone
                            : testCase.Status
                });
        }

        foreach (var collection in _collections)
        {
            RenumberCollectionCases(
                collection);
        }

        BuildTestTree();
        UpdateSessionSummary();

        if (_currentCollectionIndex >= 0)
        {
            RenderCurrentCollectionCases();
            UpdateCurrentCollectionProgress();
            UpdateActiveCollectionHighlight();
        }
    }

    private void BuildTestTree()
    {
        if (_testTreeView is null)
        {
            return;
        }

        var expandedFolderKeys =
            _folders
                .Where(
                    folder =>
                        folder.TreeItem?.IsExpanded ==
                        true)
                .Select(
                    folder =>
                        folder.Key)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        var activeCollectionKey =
            _currentCollectionIndex >= 0 &&
            _currentCollectionIndex < _collections.Count
                ? _collections[_currentCollectionIndex].Key
                : null;

        _testTreeView.Items.Clear();

        var rootFolder =
            _folders.First(
                folder =>
                    folder.Key ==
                    ProjectRootKey);

        var rootItem =
            CreateFolderTreeItem(
                rootFolder,
                true);

        AddFolderChildren(
            rootItem,
            rootFolder.Key);

        _testTreeView.Items.Add(
            rootItem);

        foreach (var folder in _folders)
        {
            if (folder.TreeItem is null)
            {
                continue;
            }

            folder.TreeItem.IsExpanded =
                folder.Key == ProjectRootKey ||
                !string.IsNullOrWhiteSpace(
                    _testTreeSearchText) ||
                expandedFolderKeys.Contains(
                    folder.Key);
        }

        if (!string.IsNullOrWhiteSpace(
                activeCollectionKey))
        {
            ExpandPathToCollection(
                activeCollectionKey);
        }

        if (!string.IsNullOrWhiteSpace(
                _pendingTreeSelectionKey))
        {
            SelectPendingTreeElement(
                _pendingTreeSelectionKey);

            _pendingTreeSelectionKey =
                null;
        }

        UpdateActiveCollectionHighlight();
    }

    private void AddFolderChildren(
        TreeViewItem parentItem,
        string parentFolderKey)
    {
        // Zbiory przypadków pokazujemy najpierw, a foldery pod nimi.
        // Dzięki temu nowy folder nie wygląda jak rodzic istniejących zbiorów.
        var childCollections =
            _collections
                .Where(
                    collection =>
                        string.Equals(
                            collection.ParentFolderKey,
                            parentFolderKey,
                            StringComparison.OrdinalIgnoreCase) &&
                        (_activeAssignmentCaseIds is null ||
                         collection.Cases.Any(
                             testCase =>
                                 _activeAssignmentCaseIds.Contains(
                                     testCase.Id))) &&
                        CollectionMatchesTreeSearch(
                            collection))
                .OrderBy(
                    collection =>
                        collection.SortOrder)
                .ThenBy(
                    collection =>
                        collection.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        foreach (var collection in childCollections)
        {
            parentItem.Items.Add(
                CreateCollectionTreeItem(
                    collection));
        }

        var childFolders =
            _folders
                .Where(
                    folder =>
                        string.Equals(
                            folder.ParentKey,
                            parentFolderKey,
                            StringComparison.OrdinalIgnoreCase) &&
                        (_activeAssignmentCaseIds is null ||
                         FolderContainsActiveAssignmentCases(
                             folder.Key)) &&
                        FolderContainsTreeSearchMatches(
                            folder.Key))
                .OrderBy(
                    folder =>
                        folder.SortOrder)
                .ThenBy(
                    folder =>
                        folder.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        foreach (var childFolder in childFolders)
        {
            var folderItem =
                CreateFolderTreeItem(
                    childFolder,
                    false);

            AddFolderChildren(
                folderItem,
                childFolder.Key);

            parentItem.Items.Add(
                folderItem);
        }
    }

    private TreeViewItem CreateFolderTreeItem(
        FolderData folder,
        bool isExpanded)
    {
        var isTopLevelFolder =
            string.Equals(
                folder.ParentKey,
                ProjectRootKey,
                StringComparison.OrdinalIgnoreCase);

        var hasChildFolders =
            _folders.Any(
                child =>
                    string.Equals(
                        child.ParentKey,
                        folder.Key,
                        StringComparison.OrdinalIgnoreCase));

        var folderIcon =
            folder.Key == ProjectRootKey
                ? "▣"
                : isTopLevelFolder
                    ? "▰"
                    : hasChildFolders
                        ? "▸"
                        : "▱";

        var folderLabel =
            new TextBlock
            {
                Text =
                    $"{folderIcon} {GetFolderDisplayName(folder)}",

                FontWeight =
                    folder.Key == ProjectRootKey ||
                    isTopLevelFolder ||
                    hasChildFolders
                        ? FontWeight.SemiBold
                        : FontWeight.Normal,

                VerticalAlignment =
                    VerticalAlignment.Center
            };

        Control folderHeaderContent =
            folderLabel;

        if (folder.Key == ProjectRootKey)
        {
            var isAssignedSession =
                string.Equals(
                    _sessionState?.SessionMode,
                    "Assigned",
                    StringComparison.OrdinalIgnoreCase);

            var modeIcon =
                new TextBlock
                {
                    Text =
                        isAssignedSession
                            ? "⇥"
                            : "⚡",
                    FontSize = 11,
                    FontWeight = FontWeight.Bold,
                    Foreground =
                        new SolidColorBrush(
                            Color.Parse(
                                isAssignedSession
                                    ? "#2878D0"
                                    : "#C98C00")),
                    VerticalAlignment = VerticalAlignment.Center
                };

            ToolTip.SetTip(
                modeIcon,
                isAssignedSession
                    ? (LocalizationService.IsPolish
                        ? "Powrót do testów przypisanych"
                        : "Return to assigned tests")
                    : "ad-hoc");

            folderHeaderContent =
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        folderLabel,
                        modeIcon
                    }
                };
        }

        var headerBorder =
            new Border
            {
                Background =
                    Brushes.Transparent,

                HorizontalAlignment =
                    HorizontalAlignment.Stretch,

                Padding =
                    new Thickness(
                        4,
                        5),

                Child =
                    folderHeaderContent
            };

        if (folder.Key == ProjectRootKey)
        {
            headerBorder.Classes.Add(
                "ProjectTreeHeader");

            folderLabel.Classes.Add(
                "ProjectTreeLabel");
        }
        else if (string.Equals(
                     folder.ParentKey,
                     ProjectRootKey,
                     StringComparison.OrdinalIgnoreCase))
        {
            headerBorder.Classes.Add(
                "TestTypeTreeHeader");

            folderLabel.Classes.Add(
                "TestTypeTreeLabel");
        }

        var addFolderItem =
            new MenuItem
            {
                Header =
                    LocalizationService.T("Structure.AddFolder")
            };

        addFolderItem.Click +=
            async (_, _) =>
            {
                await AddFolderAsync(
                    folder);
            };

        var addCollectionItem =
            new MenuItem
            {
                Header =
                    LocalizationService.T("Structure.AddCollection")
            };

        addCollectionItem.Click +=
            async (_, _) =>
            {
                await AddCollectionAsync(
                    folder);
            };

        var menuItems =
            new List<MenuItem>
            {
                addFolderItem,
                addCollectionItem
            };

        if (folder.Key != ProjectRootKey)
        {
            var copyFolderItem =
                new MenuItem
                {
                    Header =
                        LocalizationService.T("Structure.CopyFolder")
                };

            copyFolderItem.Click +=
                (_, _) =>
                {
                    CopyFolderToClipboard(
                        folder);
                };

            menuItems.Add(
                copyFolderItem);
        }

        var pasteIntoFolderItem =
            new MenuItem
            {
                Header =
                    LocalizationService.T("Structure.PasteHere"),

                IsEnabled =
                    CanPasteIntoFolder(
                        folder)
            };

        pasteIntoFolderItem.Click +=
            async (_, _) =>
            {
                await PasteIntoFolderAsync(
                    folder);
            };

        menuItems.Add(
            pasteIntoFolderItem);

        if (folder.Key != ProjectRootKey)
        {
            var siblingFolders =
                GetSiblingFolders(
                    folder);

            var siblingIndex =
                siblingFolders.IndexOf(
                    folder);

            var moveUpItem =
                new MenuItem
                {
                    Header =
                        LocalizationService.T("Structure.MoveUp"),

                    IsEnabled =
                        CanMoveFolder(folder) &&
                        siblingIndex > 0
                };

            moveUpItem.Click +=
                async (_, _) =>
                {
                    await MoveFolderAsync(
                        folder,
                        -1);
                };

            var moveDownItem =
                new MenuItem
                {
                    Header =
                        LocalizationService.T("Structure.MoveDown"),

                    IsEnabled =
                        CanMoveFolder(folder) &&
                        siblingIndex >= 0 &&
                        siblingIndex <
                        siblingFolders.Count - 1
                };

            moveDownItem.Click +=
                async (_, _) =>
                {
                    await MoveFolderAsync(
                        folder,
                        1);
                };

            menuItems.Add(
                moveUpItem);

            menuItems.Add(
                moveDownItem);
        }

        if (CanModifyFolder(folder))
        {
            var renameItem =
                new MenuItem
                {
                    Header =
                        LocalizationService.T("Structure.Rename")
                };

            renameItem.Click +=
                async (_, _) =>
                {
                    await RenameFolderAsync(
                        folder);
                };

            var deleteItem =
                new MenuItem
                {
                    Header =
                        LocalizationService.T("Structure.DeleteFolder")
                };

            deleteItem.Click +=
                async (_, _) =>
                {
                    await DeleteFolderAsync(
                        folder);
                };

            menuItems.Add(
                renameItem);

            menuItems.Add(
                deleteItem);
        }

        var folderItem =
            new TreeViewItem
            {
                Header =
                    headerBorder,

                IsExpanded =
                    isExpanded,

                ContextMenu =
                    new ContextMenu
                    {
                        ItemsSource =
                            menuItems
                    },

                HorizontalAlignment =
                    HorizontalAlignment.Stretch,

                Cursor =
                    new Cursor(
                        StandardCursorType.Hand)
            };

        folderItem.AddHandler(
            PointerPressedEvent,
            (
                _,
                eventArgs) =>
            {
                var point =
                    eventArgs.GetCurrentPoint(
                        folderItem);

                if (!point.Properties.IsLeftButtonPressed)
                {
                    return;
                }

                var clickedItem =
                    FindClickedTreeViewItem(
                        eventArgs.Source);

                if (!ReferenceEquals(
                        clickedItem,
                        folderItem))
                {
                    return;
                }

                var pointerPosition =
                    eventArgs.GetPosition(
                        folderItem);

                if (IsPointerInsideExpander(
                        eventArgs.Source))
                {
                    _lastClickedFolderKey =
                        null;

                    _lastFolderClickTimestamp =
                        0;

                    SelectFolderForCommands(
                        folder);

                    return;
                }

                if (pointerPosition.X <= 46)
                {
                    _lastClickedFolderKey =
                        null;

                    _lastFolderClickTimestamp =
                        0;

                    folderItem.IsExpanded =
                        !folderItem.IsExpanded;

                    SelectFolderForCommands(
                        folder);

                    eventArgs.Handled =
                        true;

                    return;
                }

                SelectFolderForCommands(
                    folder);

                if (_testTreeView is not null)
                {
                    _testTreeView.SelectedItem =
                        folderItem;
                }

                folderItem.IsSelected =
                    true;

                var clickTimestamp =
                    Environment.TickCount64;

                var isSecondClick =
                    string.Equals(
                        _lastClickedFolderKey,
                        folder.Key,
                        StringComparison.OrdinalIgnoreCase) &&
                    clickTimestamp -
                    _lastFolderClickTimestamp <=
                    550;

                if (isSecondClick)
                {
                    _lastClickedFolderKey =
                        null;

                    _lastFolderClickTimestamp =
                        0;

                    folderItem.IsExpanded =
                        !folderItem.IsExpanded;

                    ShowFolderScreen(
                        folder);

                    folderItem.IsSelected =
                        true;
                }
                else
                {
                    _lastClickedFolderKey =
                        folder.Key;

                    _lastFolderClickTimestamp =
                        clickTimestamp;
                }

                eventArgs.Handled =
                    true;
            },
            RoutingStrategies.Bubble,
            handledEventsToo: true);

        EnableTreeItemDrag(
            folderItem,
            $"qa-folder:{folder.Key}",
            CanMoveFolder(folder));

        ConfigureFolderDropTarget(
            folderItem,
            headerBorder,
            folder);

        folder.TreeItem =
            folderItem;

        return folderItem;
    }

    private TreeViewItem CreateCollectionTreeItem(
        TestCollectionData collection)
    {
        var activeIndicator =
            new Border
            {
                Width =
                    3,

                Background =
                    Brushes.Transparent,

                CornerRadius =
                    new CornerRadius(2),

                Margin =
                    new Thickness(
                        0,
                        2,
                        4,
                        2)
            };

        var stateIcon =
            new TextBlock
            {
                Text =
                    "○",

                Width =
                    14,

                VerticalAlignment =
                    VerticalAlignment.Center
            };

        var nameText =
            new TextBlock
            {
                Text =
                    collection.Name,

                TextTrimming =
                    TextTrimming.CharacterEllipsis,

                FontSize =
                    12,

                VerticalAlignment =
                    VerticalAlignment.Center
            };

        var progressText =
            new TextBlock
            {
                Text =
                    $"0/{collection.Cases.Count}",

                Margin =
                    new Thickness(
                        4,
                        0,
                        2,
                        0),

                MinWidth =
                    30,

                FontSize =
                    11,

                FontWeight =
                    FontWeight.SemiBold,

                TextAlignment =
                    TextAlignment.Right,

                VerticalAlignment =
                    VerticalAlignment.Center
            };

        var headerGrid =
            new Grid
            {
                ColumnDefinitions =
                    new ColumnDefinitions(
                        "Auto,Auto,*,Auto"),

                HorizontalAlignment =
                    HorizontalAlignment.Stretch
            };

        Grid.SetColumn(
            activeIndicator,
            0);

        Grid.SetColumn(
            stateIcon,
            1);

        Grid.SetColumn(
            nameText,
            2);

        Grid.SetColumn(
            progressText,
            3);

        headerGrid.Children.Add(
            activeIndicator);

        headerGrid.Children.Add(
            stateIcon);

        headerGrid.Children.Add(
            nameText);

        headerGrid.Children.Add(
            progressText);

        var headerBorder =
            new Border
            {
                Child =
                    headerGrid,

                MinWidth =
                    0,

                Padding =
                    new Thickness(
                        4,
                        4),

                CornerRadius =
                    new CornerRadius(7),

                Background =
                    Brushes.Transparent,

                HorizontalAlignment =
                    HorizontalAlignment.Stretch
            };

        headerBorder.Classes.Add(
            "TreeProgressRow");

        var addCaseItem =
            new MenuItem
            {
                Header =
                    LocalizationService.T("Structure.AddCase")
            };

        addCaseItem.Click +=
            async (_, _) =>
            {
                await AddUserTestCaseAsync(
                    collection);
            };

        var duplicateCollectionItem =
            new MenuItem
            {
                Header =
                    LocalizationService.T("Structure.DuplicateCollection")
            };

        duplicateCollectionItem.Click +=
            async (_, _) =>
            {
                await DuplicateCollectionAsync(
                    collection);
            };

        var siblingCollections =
            GetSiblingCollections(
                collection);

        var collectionIndex =
            siblingCollections.IndexOf(
                collection);

        var moveUpItem =
            new MenuItem
            {
                Header =
                    LocalizationService.T("Structure.MoveUp"),

                IsEnabled =
                    CanMoveCollection(collection) &&
                    collectionIndex > 0
            };

        moveUpItem.Click +=
            async (_, _) =>
            {
                await MoveCollectionAsync(
                    collection,
                    -1);
            };

        var moveDownItem =
            new MenuItem
            {
                Header =
                    LocalizationService.T("Structure.MoveDown"),

                IsEnabled =
                    CanMoveCollection(collection) &&
                    collectionIndex >= 0 &&
                    collectionIndex <
                    siblingCollections.Count - 1
            };

        moveDownItem.Click +=
            async (_, _) =>
            {
                await MoveCollectionAsync(
                    collection,
                    1);
            };

        var menuItems =
            new List<MenuItem>
            {
                addCaseItem,
                duplicateCollectionItem,
                moveUpItem,
                moveDownItem
            };

        var copyCollectionItem =
            new MenuItem
            {
                Header =
                    LocalizationService.T("Structure.CopyCollection")
            };

        copyCollectionItem.Click +=
            (_, _) =>
            {
                CopyCollectionToClipboard(
                    collection);
            };

        var pasteCaseItem =
            new MenuItem
            {
                Header =
                    LocalizationService.T("Structure.PasteCaseHere"),

                IsEnabled =
                    _structureClipboard is
                        TestCaseClipboardItem
            };

        pasteCaseItem.Click +=
            async (_, _) =>
            {
                await PasteTestCaseIntoCollectionAsync(
                    collection);
            };

        menuItems.Add(
            copyCollectionItem);

        menuItems.Add(
            pasteCaseItem);

        if (CanModifyCollection(collection))
        {
            var renameItem =
                new MenuItem
                {
                    Header =
                        LocalizationService.T("Structure.Rename")
                };

            renameItem.Click +=
                async (_, _) =>
                {
                    await RenameCollectionAsync(
                        collection);
                };

            var deleteItem =
                new MenuItem
                {
                    Header =
                        LocalizationService.T("Structure.DeleteCollection")
                };

            deleteItem.Click +=
                async (_, _) =>
                {
                    await DeleteCollectionAsync(
                        collection);
                };

            menuItems.Add(
                renameItem);

            menuItems.Add(
                deleteItem);
        }

        var item =
            new TreeViewItem
            {
                Header =
                    headerBorder,

                ContextMenu =
                    new ContextMenu
                    {
                        ItemsSource =
                            menuItems
                    },

                HorizontalAlignment =
                    HorizontalAlignment.Stretch,

                Cursor =
                    new Cursor(
                        StandardCursorType.Hand)
            };

        item.AddHandler(
            PointerPressedEvent,
            (
                _,
                eventArgs) =>
            {
                var point =
                    eventArgs.GetCurrentPoint(
                        item);

                if (!point.Properties.IsLeftButtonPressed)
                {
                    return;
                }

                var clickedItem =
                    FindClickedTreeViewItem(
                        eventArgs.Source);

                if (!ReferenceEquals(
                        clickedItem,
                        item))
                {
                    return;
                }

                SelectCollection(
                    collection);

                SelectCollectionForCommands(
                    collection);

                eventArgs.Handled =
                    true;
            },
            RoutingStrategies.Bubble,
            handledEventsToo: true);

        EnableTreeItemDrag(
            item,
            $"qa-collection:{collection.Key}",
            CanMoveCollection(collection));

        ConfigureCollectionDropTarget(
            item,
            headerBorder,
            collection);

        collection.TreeItem =
            item;

        collection.HeaderBorder =
            headerBorder;

        collection.ActiveIndicator =
            activeIndicator;

        collection.StateIcon =
            stateIcon;

        collection.ProgressText =
            progressText;

        UpdateCollectionState(
            collection);

        return item;
    }

    private static TreeViewItem? FindClickedTreeViewItem(
        object? source)
    {
        if (source is TreeViewItem directTreeViewItem)
        {
            return directTreeViewItem;
        }

        if (source is not Visual sourceVisual)
        {
            return null;
        }

        return sourceVisual
            .GetVisualAncestors()
            .OfType<TreeViewItem>()
            .FirstOrDefault();
    }

    private static bool IsPointerInsideExpander(
        object? source)
    {
        if (source is ToggleButton)
        {
            return true;
        }

        if (source is not Visual sourceVisual)
        {
            return false;
        }

        return sourceVisual
            .GetVisualAncestors()
            .OfType<ToggleButton>()
            .Any();
    }

    private static void EnableTreeItemDrag(
        TreeViewItem item,
        string payload,
        bool canDrag)
    {
        if (!canDrag)
        {
            return;
        }

        Point? dragStartPoint =
            null;

        PointerPressedEventArgs? triggerEvent =
            null;

        item.AddHandler(
            PointerPressedEvent,
            (
                _,
                eventArgs) =>
            {
                var point =
                    eventArgs.GetCurrentPoint(
                        item);

                if (!point.Properties.IsLeftButtonPressed ||
                    IsPointerInsideExpander(
                        eventArgs.Source) ||
                    !ReferenceEquals(
                        FindClickedTreeViewItem(
                            eventArgs.Source),
                        item))
                {
                    return;
                }

                dragStartPoint =
                    eventArgs.GetPosition(
                        item);

                triggerEvent =
                    eventArgs;
            },
            RoutingStrategies.Bubble,
            handledEventsToo: true);

        item.AddHandler(
            PointerMovedEvent,
            async (
                _,
                eventArgs) =>
            {
                if (dragStartPoint is null ||
                    triggerEvent is null)
                {
                    return;
                }

                var point =
                    eventArgs.GetCurrentPoint(
                        item);

                if (!point.Properties.IsLeftButtonPressed)
                {
                    dragStartPoint =
                        null;

                    triggerEvent =
                        null;

                    return;
                }

                var delta =
                    eventArgs.GetPosition(
                        item) -
                    dragStartPoint.Value;

                if (Math.Abs(delta.X) < 6 &&
                    Math.Abs(delta.Y) < 6)
                {
                    return;
                }

                var pressedEvent =
                    triggerEvent;

                dragStartPoint =
                    null;

                triggerEvent =
                    null;

                var dragData =
                    new DataTransfer();

                dragData.Add(
                    DataTransferItem.CreateText(
                        payload));

                await DragDrop.DoDragDropAsync(
                    pressedEvent,
                    dragData,
                    DragDropEffects.Move);
            },
            RoutingStrategies.Bubble,
            handledEventsToo: true);

        item.AddHandler(
            PointerReleasedEvent,
            (
                _,
                _) =>
            {
                dragStartPoint =
                    null;

                triggerEvent =
                    null;
            },
            RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    private void ConfigureFolderDropTarget(
        TreeViewItem item,
        Border headerBorder,
        FolderData targetFolder)
    {
        DragDrop.SetAllowDrop(
            item,
            CanReorderStructure);

        DragDrop.AddDragOverHandler(
            item,
            (
                sender,
                eventArgs) =>
            {
                var acceptsDrop =
                    CanDropOnFolder(
                        eventArgs,
                        targetFolder);

                var payload =
                    eventArgs.DataTransfer.TryGetText()
                    ?? string.Empty;

                var dropZone =
                    acceptsDrop &&
                    TryGetDraggedFolder(
                        payload,
                        out _)
                        ? GetFolderDropZone(
                            eventArgs.GetPosition(
                                item),
                            item.Bounds.Height)
                        : acceptsDrop
                            ? TreeDropZone.Inside
                            : TreeDropZone.None;

                eventArgs.DragEffects =
                    acceptsDrop
                        ? DragDropEffects.Move
                        : DragDropEffects.None;

                SetTreeDropTargetStyle(
                    headerBorder,
                    dropZone);

                eventArgs.Handled =
                    true;
            });

        DragDrop.AddDragLeaveHandler(
            item,
            (
                _,
                _) =>
            {
                SetTreeDropTargetStyle(
                    headerBorder,
                    TreeDropZone.None);
            });

        DragDrop.AddDropHandler(
            item,
            async (
                _,
                eventArgs) =>
            {
                eventArgs.Handled =
                    true;

                SetTreeDropTargetStyle(
                    headerBorder,
                    TreeDropZone.None);

                if (!CanDropOnFolder(
                        eventArgs,
                        targetFolder))
                {
                    eventArgs.DragEffects =
                        DragDropEffects.None;

                    return;
                }

                var payload =
                    eventArgs.DataTransfer.TryGetText()
                    ?? string.Empty;

                if (TryGetDraggedFolder(
                        payload,
                        out var sourceFolder))
                {
                    var position =
                        eventArgs.GetPosition(
                            item);

                    var dropZone =
                        GetFolderDropZone(
                            position,
                            item.Bounds.Height);

                    var placeInside =
                        dropZone ==
                        TreeDropZone.Inside;

                    if (!await ExecuteTreeDropSafelyAsync(
                            () => MoveFolderByDropAsync(
                                sourceFolder,
                                placeInside
                                    ? targetFolder.Key
                                    : targetFolder.ParentKey,
                                placeInside
                                    ? null
                                    : targetFolder,
                                dropZone ==
                                TreeDropZone.After)))
                    {
                        eventArgs.DragEffects =
                            DragDropEffects.None;

                        return;
                    }
                }
                else
                {
                    if (TryGetDraggedCollection(
                            payload,
                            out var sourceCollection))
                    {
                        if (!await ExecuteTreeDropSafelyAsync(
                                () => MoveCollectionByDropAsync(
                                    sourceCollection,
                                    targetFolder,
                                    null,
                                    true)))
                        {
                            eventArgs.DragEffects =
                                DragDropEffects.None;

                            return;
                        }
                    }
                    else
                    {
                        eventArgs.DragEffects =
                            DragDropEffects.None;

                        return;
                    }
                }

                eventArgs.DragEffects =
                    DragDropEffects.Move;

            });
    }

    private void ConfigureCollectionDropTarget(
        TreeViewItem item,
        Border headerBorder,
        TestCollectionData targetCollection)
    {
        DragDrop.SetAllowDrop(
            item,
            CanReorderStructure);

        DragDrop.AddDragOverHandler(
            item,
            (
                dragSender,
                eventArgs) =>
            {
                if (!CanReorderStructure)
                {
                    eventArgs.DragEffects =
                        DragDropEffects.None;

                    eventArgs.Handled =
                        true;

                    return;
                }

                var payload =
                    eventArgs.DataTransfer.TryGetText()
                    ?? string.Empty;

                var isCollectionReorder =
                    TryGetDraggedCollection(
                        payload,
                        out var sourceCollection) &&
                    CanMoveCollection(
                        sourceCollection) &&
                    !ReferenceEquals(
                        sourceCollection,
                        targetCollection);

                var acceptsDrop =
                    isCollectionReorder ||
                    TryGetDraggedTestCase(
                        payload,
                        out _,
                        out var sourceTestCase) &&
                    CanMoveTestCase(
                        sourceTestCase) &&
                    !targetCollection.Cases.Contains(
                        sourceTestCase);

                var dropZone =
                    isCollectionReorder
                        ? eventArgs.GetPosition(
                                item)
                            .Y <
                            item.Bounds.Height / 2
                            ? TreeDropZone.Before
                            : TreeDropZone.After
                        : acceptsDrop
                            ? TreeDropZone.Inside
                            : TreeDropZone.None;

                eventArgs.DragEffects =
                    acceptsDrop
                        ? DragDropEffects.Move
                        : DragDropEffects.None;

                SetTreeDropTargetStyle(
                    headerBorder,
                    dropZone);

                eventArgs.Handled =
                    true;
            });

        DragDrop.AddDragLeaveHandler(
            item,
            (
                _,
                _) =>
            {
                SetTreeDropTargetStyle(
                    headerBorder,
                    TreeDropZone.None);
            });

        DragDrop.AddDropHandler(
            item,
            async (
                _,
                eventArgs) =>
            {
                // Oznaczamy zdarzenie przed pierwszym await. W przeciwnym
                // razie to samo upuszczenie może dojść również do folderu
                // nadrzędnego i uruchomić dwie operacje przenoszenia naraz.
                eventArgs.Handled =
                    true;

                SetTreeDropTargetStyle(
                    headerBorder,
                    TreeDropZone.None);

                if (!CanReorderStructure)
                {
                    eventArgs.DragEffects =
                        DragDropEffects.None;

                    return;
                }

                var payload =
                    eventArgs.DataTransfer.TryGetText()
                    ?? string.Empty;

                if (TryGetDraggedTestCase(
                        payload,
                        out var sourceCollection,
                        out var sourceTestCase))
                {
                    if (!await ExecuteTreeDropSafelyAsync(
                            () => MoveTestCaseToCollectionEndAsync(
                                sourceCollection,
                                sourceTestCase,
                                targetCollection)))
                    {
                        eventArgs.DragEffects =
                            DragDropEffects.None;

                        return;
                    }
                }
                else if (TryGetDraggedCollection(
                             payload,
                             out var draggedCollection) &&
                         !ReferenceEquals(
                             draggedCollection,
                             targetCollection))
                {
                    var targetFolder =
                        _folders.FirstOrDefault(
                            folder =>
                                folder.Key ==
                                targetCollection.ParentFolderKey);

                    if (targetFolder is null)
                    {
                        eventArgs.DragEffects =
                            DragDropEffects.None;

                        return;
                    }

                    if (!await ExecuteTreeDropSafelyAsync(
                            () => MoveCollectionByDropAsync(
                                draggedCollection,
                                targetFolder,
                                targetCollection,
                                eventArgs.GetPosition(
                                        item)
                                    .Y >
                                item.Bounds.Height / 2)))
                    {
                        eventArgs.DragEffects =
                            DragDropEffects.None;

                        return;
                    }
                }
                else
                {
                    eventArgs.DragEffects =
                        DragDropEffects.None;

                    return;
                }

                eventArgs.DragEffects =
                    DragDropEffects.Move;

            });
    }

    private bool CanDropOnFolder(
        DragEventArgs eventArgs,
        FolderData targetFolder)
    {
        if (!CanReorderStructure ||
            targetFolder.Key ==
            ProjectRootKey)
        {
            return false;
        }

        var payload =
            eventArgs.DataTransfer.TryGetText()
            ?? string.Empty;

        if (TryGetDraggedFolder(
                payload,
                out var sourceFolder))
        {
            return CanMoveFolder(sourceFolder) &&
                   !ReferenceEquals(
                       sourceFolder,
                       targetFolder) &&
                   !IsFolderInsideScope(
                       targetFolder.Key,
                       sourceFolder.Key) &&
                   !string.Equals(
                       sourceFolder.ParentKey,
                       targetFolder.Key,
                       StringComparison.OrdinalIgnoreCase);
        }

        return TryGetDraggedCollection(
            payload,
            out var sourceCollection) &&
               CanMoveCollection(sourceCollection) &&
               !string.Equals(
                   sourceCollection.ParentFolderKey,
                   targetFolder.Key,
                   StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> ExecuteTreeDropSafelyAsync(
        Func<Task> operation)
    {
        try
        {
            await operation();

            return true;
        }
        catch
        {
            await ShowInformationAsync(
                "Nie udało się przenieść elementu",
                "Struktura testów nie została zmieniona. Odśwież widok i spróbuj ponownie.");

            return false;
        }
    }

    private bool TryGetDraggedFolder(
        string payload,
        out FolderData folder)
    {
        const string prefix =
            "qa-folder:";

        folder =
            null!;

        if (!payload.StartsWith(
                prefix,
                StringComparison.Ordinal))
        {
            return false;
        }

        folder =
            _folders.FirstOrDefault(
                item =>
                    item.Key ==
                    payload[prefix.Length..])!;

        return folder is not null;
    }

    private bool TryGetDraggedCollection(
        string payload,
        out TestCollectionData collection)
    {
        const string prefix =
            "qa-collection:";

        collection =
            null!;

        if (!payload.StartsWith(
                prefix,
                StringComparison.Ordinal))
        {
            return false;
        }

        collection =
            _collections.FirstOrDefault(
                item =>
                    item.Key ==
                    payload[prefix.Length..])!;

        return collection is not null;
    }

    private bool TryGetDraggedTestCase(
        string payload,
        out TestCollectionData collection,
        out TestCaseData testCase)
    {
        const string prefix =
            "qa-test-case:";

        collection =
            null!;

        testCase =
            null!;

        if (!payload.StartsWith(
                prefix,
                StringComparison.Ordinal) ||
            !Guid.TryParse(
                payload[prefix.Length..],
                out var testCaseId))
        {
            return false;
        }

        foreach (var candidateCollection in _collections)
        {
            var candidateCase =
                candidateCollection.Cases.FirstOrDefault(
                    item =>
                        item.Id ==
                        testCaseId);

            if (candidateCase is null)
            {
                continue;
            }

            collection =
                candidateCollection;

            testCase =
                candidateCase;

            return true;
        }

        return false;
    }

    private static TreeDropZone GetFolderDropZone(
        Point position,
        double height)
    {
        if (height <= 0)
        {
            return TreeDropZone.None;
        }

        if (position.Y <
            height * 0.25)
        {
            return TreeDropZone.Before;
        }

        if (position.Y >
            height * 0.75)
        {
            return TreeDropZone.After;
        }

        return TreeDropZone.Inside;
    }

    private static void SetTreeDropTargetStyle(
        Border border,
        TreeDropZone dropZone)
    {
        if (dropZone ==
            TreeDropZone.None)
        {
            border.ClearValue(
                Border.BorderBrushProperty);

            border.ClearValue(
                Border.BorderThicknessProperty);

            return;
        }

        border.BorderBrush =
            new SolidColorBrush(
                Color.Parse(
                    "#28C76F"));

        border.BorderThickness =
            dropZone switch
            {
                TreeDropZone.Before =>
                    new Thickness(0, 3, 0, 0),

                TreeDropZone.After =>
                    new Thickness(0, 0, 0, 3),

                _ =>
                    new Thickness(2)
            };
    }

    private async Task MoveFolderByDropAsync(
        FolderData sourceFolder,
        string targetParentKey,
        FolderData? relativeFolder,
        bool insertAfter)
    {
        if (!CanMoveFolder(sourceFolder) ||
            sourceFolder.Key ==
                targetParentKey ||
            relativeFolder is null &&
            string.Equals(
                sourceFolder.ParentKey,
                targetParentKey,
                StringComparison.OrdinalIgnoreCase) ||
            IsFolderInsideScope(
                targetParentKey,
                sourceFolder.Key))
        {
            return;
        }

        var targetParent =
            _folders.FirstOrDefault(
                folder =>
                    folder.Key ==
                    targetParentKey);

        if (targetParent is null)
        {
            return;
        }

        var isProjectRootReorder =
            targetParent.Key ==
                ProjectRootKey &&
            relativeFolder is not null;

        if (!isProjectRootReorder &&
            !await ConfirmTestTypeChangeAsync(
                sourceFolder.Name,
                sourceFolder.TestTypeKey,
                targetParent.TestTypeKey))
        {
            return;
        }

        var oldParentKey =
            sourceFolder.ParentKey;

        sourceFolder.ParentKey =
            targetParentKey;

        if (!isProjectRootReorder)
        {
            UpdateFolderBranchTestType(
                sourceFolder,
                targetParent.TestTypeKey);
        }

        NormalizeFolderOrderInMemory(
            oldParentKey,
            null,
            null,
            false);

        NormalizeFolderOrderInMemory(
            targetParentKey,
            sourceFolder,
            relativeFolder,
            insertAfter);

        RefreshCollectionPaths();

        await PersistCurrentStructureAsync();

        _pendingTreeSelectionKey =
            sourceFolder.Key;

        BuildTestTree();
        UpdateSessionSummary();
    }

    private async Task MoveCollectionByDropAsync(
        TestCollectionData sourceCollection,
        FolderData targetFolder,
        TestCollectionData? relativeCollection,
        bool insertAfter)
    {
        if (!CanMoveCollection(sourceCollection) ||
            !_folders.Contains(targetFolder) ||
            relativeCollection is null &&
            string.Equals(
                sourceCollection.ParentFolderKey,
                targetFolder.Key,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!await ConfirmTestTypeChangeAsync(
                sourceCollection.Name,
                sourceCollection.TestTypeKey,
                targetFolder.TestTypeKey))
        {
            return;
        }

        var oldParentKey =
            sourceCollection.ParentFolderKey;
        var oldTestTypeKey =
            sourceCollection.TestTypeKey;
        var sortOrderSnapshot =
            _collections.ToDictionary(
                collection => collection,
                collection => collection.SortOrder);

        try
        {
            sourceCollection.ParentFolderKey =
                targetFolder.Key;

            sourceCollection.TestTypeKey =
                targetFolder.TestTypeKey;

            NormalizeCollectionOrderInMemory(
                oldParentKey,
                null,
                null,
                false);

            NormalizeCollectionOrderInMemory(
                targetFolder.Key,
                sourceCollection,
                relativeCollection,
                insertAfter);

            RefreshCollectionPaths();

            await PersistCurrentStructureAsync();
        }
        catch
        {
            sourceCollection.ParentFolderKey =
                oldParentKey;
            sourceCollection.TestTypeKey =
                oldTestTypeKey;

            foreach (var (collection, sortOrder) in sortOrderSnapshot)
            {
                collection.SortOrder =
                    sortOrder;
            }

            RefreshCollectionPaths();
            throw;
        }

        _pendingTreeSelectionKey =
            sourceCollection.Key;

        BuildTestTree();
        UpdateNavigationButtons();
        UpdateSessionSummary();
    }

    private async Task MoveTestCaseToCollectionEndAsync(
        TestCollectionData sourceCollection,
        TestCaseData sourceTestCase,
        TestCollectionData targetCollection)
    {
        if (!CanMoveTestCase(
                sourceTestCase) ||
            ReferenceEquals(
                sourceCollection,
                targetCollection))
        {
            return;
        }

        sourceCollection.Cases.Remove(
            sourceTestCase);

        targetCollection.Cases.Add(
            sourceTestCase);

        var sourceCases =
            sourceCollection.Cases
                .OrderBy(
                    item =>
                        item.SortOrder)
                .ThenBy(
                    item =>
                        item.Number)
                .ToList();

        var targetCases =
            targetCollection.Cases
                .OrderBy(
                    item =>
                        item.SortOrder)
                .ThenBy(
                    item =>
                        item.Number)
                .ToList();

        await NormalizeAndPersistTestCaseOrderAsync(
            sourceCollection,
            sourceCases);

        await NormalizeAndPersistTestCaseOrderAsync(
            targetCollection,
            targetCases);

        RenumberCollectionCases(
            sourceCollection);

        RenumberCollectionCases(
            targetCollection);

        UpdateCollectionState(
            sourceCollection);

        UpdateCollectionState(
            targetCollection);

        BuildTestTree();
        SelectCollection(
            targetCollection);
        UpdateSessionSummary();
    }

    private void NormalizeFolderOrderInMemory(
        string parentFolderKey,
        FolderData? movedFolder,
        FolderData? relativeFolder,
        bool insertAfter)
    {
        var folders =
            _folders
                .Where(
                    folder =>
                        folder.ParentKey ==
                            parentFolderKey &&
                        !ReferenceEquals(
                            folder,
                            movedFolder))
                .OrderBy(
                    folder =>
                        folder.SortOrder)
                .ThenBy(
                    folder =>
                        folder.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        if (movedFolder is not null)
        {
            var targetIndex =
                relativeFolder is null
                    ? folders.Count
                    : folders.IndexOf(
                        relativeFolder);

            if (targetIndex < 0)
            {
                targetIndex =
                    folders.Count;
            }
            else if (insertAfter)
            {
                targetIndex++;
            }

            folders.Insert(
                targetIndex,
                movedFolder);
        }

        for (var index = 0;
             index < folders.Count;
             index++)
        {
            folders[index].SortOrder =
                (index + 1) * 1000;
        }
    }

    private void NormalizeCollectionOrderInMemory(
        string parentFolderKey,
        TestCollectionData? movedCollection,
        TestCollectionData? relativeCollection,
        bool insertAfter)
    {
        var collections =
            _collections
                .Where(
                    collection =>
                        collection.ParentFolderKey ==
                            parentFolderKey &&
                        !ReferenceEquals(
                            collection,
                            movedCollection))
                .OrderBy(
                    collection =>
                        collection.SortOrder)
                .ThenBy(
                    collection =>
                        collection.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        if (movedCollection is not null)
        {
            var targetIndex =
                relativeCollection is null
                    ? collections.Count
                    : collections.IndexOf(
                        relativeCollection);

            if (targetIndex < 0)
            {
                targetIndex =
                    collections.Count;
            }
            else if (insertAfter)
            {
                targetIndex++;
            }

            collections.Insert(
                targetIndex,
                movedCollection);
        }

        for (var index = 0;
             index < collections.Count;
             index++)
        {
            collections[index].SortOrder =
                (index + 1) * 1000;
        }
    }

    private void UpdateFolderBranchTestType(
        FolderData rootFolder,
        string testTypeKey)
    {
        var branchFolderKeys =
            _folders
                .Where(
                    folder =>
                        IsFolderInsideScope(
                            folder.Key,
                            rootFolder.Key))
                .Select(
                    folder =>
                        folder.Key)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        foreach (var folder in _folders.Where(
                     folder =>
                         branchFolderKeys.Contains(
                             folder.Key)))
        {
            folder.TestTypeKey =
                testTypeKey;
        }

        foreach (var collection in _collections.Where(
                     collection =>
                         branchFolderKeys.Contains(
                             collection.ParentFolderKey)))
        {
            collection.TestTypeKey =
                testTypeKey;
        }
    }

    private async Task<bool> ConfirmTestTypeChangeAsync(
        string itemName,
        string currentTestTypeKey,
        string targetTestTypeKey)
    {
        if (string.Equals(
                currentTestTypeKey,
                targetTestTypeKey,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return await ShowDeleteConfirmationAsync(
            "Przenieść do innego rodzaju testów?",
            $"Element „{itemName}” zostanie przeniesiony z „{GetTestTypeDisplayName(currentTestTypeKey)}” do „{GetTestTypeDisplayName(targetTestTypeKey)}”. Przeniesione zostaną również wszystkie jego elementy podrzędne.");
    }

    private async Task PersistCurrentStructureAsync(
        bool replaceProjectData = false)
    {
        var data =
            await _jsonStorageService.LoadAsync();

        if (replaceProjectData)
        {
            data.Folders.RemoveAll(
                folder =>
                    string.Equals(
                        folder.ProjectKey,
                        _projectKey,
                        StringComparison.OrdinalIgnoreCase));

            data.Collections.RemoveAll(
                collection =>
                    string.Equals(
                        collection.ProjectKey,
                        _projectKey,
                        StringComparison.OrdinalIgnoreCase));

            data.TestCases.RemoveAll(
                testCase =>
                    string.Equals(
                        testCase.ProjectKey,
                        _projectKey,
                        StringComparison.OrdinalIgnoreCase));
        }

        foreach (var folder in _folders.Where(
                     folder =>
                         folder.Key !=
                         ProjectRootKey))
        {
            var storedFolder =
                data.Folders.FirstOrDefault(
                    item =>
                        item.ProjectKey ==
                            _projectKey &&
                        item.SectionKey ==
                            folder.Key);

            if (storedFolder is null)
            {
                storedFolder =
                    new TestSectionModel
                    {
                        Id =
                            folder.Id != Guid.Empty
                                ? folder.Id
                                : CreateStableEntityId(
                                    $"folder:{_projectKey}:{folder.Key}"),

                        ProjectKey =
                            _projectKey,

                        SectionKey =
                            folder.Key
                    };

                data.Folders.Add(
                    storedFolder);
            }

            storedFolder.TestTypeKey =
                folder.TestTypeKey;

            storedFolder.ParentSectionKey =
                folder.ParentKey;

            storedFolder.Name =
                folder.Name;

            storedFolder.IsSystem =
                folder.IsSystem;

            storedFolder.RequiresManagerRole =
                folder.RequiresManagerRole;

            storedFolder.SortOrder =
                folder.SortOrder;
        }

        foreach (var collection in _collections)
        {
            var storedCollection =
                data.Collections.FirstOrDefault(
                    item =>
                        item.ProjectKey ==
                            _projectKey &&
                        item.CollectionKey ==
                            collection.Key);

            if (storedCollection is null)
            {
                storedCollection =
                    new TestCollectionModel
                    {
                        Id =
                            collection.Id != Guid.Empty
                                ? collection.Id
                                : CreateStableEntityId(
                                    $"collection:{_projectKey}:{collection.Key}"),

                        ProjectKey =
                            _projectKey,

                        CollectionKey =
                            collection.Key
                    };

                data.Collections.Add(
                    storedCollection);
            }

            storedCollection.TestTypeKey =
                collection.TestTypeKey;

            storedCollection.ParentFolderKey =
                collection.ParentFolderKey;

            storedCollection.Name =
                collection.Name;

            storedCollection.Description =
                collection.Description;

            storedCollection.IsSystem =
                collection.IsSystem;

            storedCollection.RequiresManagerRole =
                collection.RequiresManagerRole;

            storedCollection.SortOrder =
                collection.SortOrder;

            foreach (var testCase in collection.Cases)
            {
                var storedCase =
                    data.TestCases.FirstOrDefault(
                        item =>
                            item.Id ==
                            testCase.Id);

                if (storedCase is null)
                {
                    storedCase =
                        new TestCaseModel
                        {
                            Id =
                                testCase.Id,

                            ProjectKey =
                                _projectKey
                        };

                    data.TestCases.Add(
                        storedCase);
                }

                storedCase.TestTypeKey =
                    collection.TestTypeKey;

                storedCase.SectionKey =
                    collection.Key;

                storedCase.Name =
                    testCase.Name;

                storedCase.Status =
                    _adHocStatusSnapshot is not null &&
                    _adHocStatusSnapshot.TryGetValue(
                        testCase.Id,
                        out var adHocStatus)
                        ? adHocStatus
                        : _activeAssignmentCaseIds is not null
                            ? StatusNone
                            : testCase.Status;

                storedCase.SortOrder =
                    testCase.SortOrder;

                storedCase.Comment = testCase.Comment;
                storedCase.Summary = testCase.Summary;
                storedCase.Preconditions = testCase.Preconditions;
                storedCase.ExternalId = testCase.ExternalId;
                storedCase.SourceVersion = testCase.SourceVersion;
                storedCase.Importance = testCase.Importance;
                storedCase.ExecutionType = testCase.ExecutionType;
                storedCase.EstimatedDuration = testCase.EstimatedDuration;
                storedCase.Platforms = testCase.Platforms.ToList();
                storedCase.Steps = testCase.Steps.Select(CloneStep).ToList();
            }
        }

        await _jsonStorageService.SaveAsync(
            data);
    }

    private async Task AddFolderAsync(
        FolderData parentFolder)
    {
        var model =
            await _userFolderService.AddFolderAsync(
                _projectKey,
                parentFolder.TestTypeKey,
                parentFolder.Key,
                _loggedInLogin);

        var newFolder =
            new FolderData
            {
                Id =
                    model.Id,

                Key =
                    model.SectionKey,

                ParentKey =
                    model.ParentSectionKey,

                Name =
                    model.Name,

                CreatedByLogin =
                    model.CreatedByLogin,

                TestTypeKey =
                    model.TestTypeKey,

                IsSystem =
                    false,

                IsProtected =
                    false,

                SortOrder =
                    model.SortOrder
            };

        _folders.Add(
            newFolder);

        // Nowy folder zawsze trafia na koniec swojej gałęzi.
        var siblings =
            GetSiblingFolders(
                newFolder);

        await NormalizeAndPersistFolderOrderAsync(
            newFolder.ParentKey,
            siblings);

        _pendingTreeSelectionKey =
            newFolder.Key;

        BuildTestTree();
        SelectFolderForCommands(newFolder);

        await RenameFolderAsync(newFolder);
    }

    private List<FolderData> GetSiblingFolders(
        FolderData folder)
    {
        return _folders
            .Where(
                item =>
                    string.Equals(
                        item.ParentKey,
                        folder.ParentKey,
                        StringComparison.OrdinalIgnoreCase))
            .OrderBy(
                item =>
                    item.SortOrder)
            .ThenBy(
                item =>
                    item.Name,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private int GetNextFolderSortOrder(
        string parentFolderKey)
    {
        var maxSortOrder =
            _folders
                .Where(
                    folder =>
                        string.Equals(
                            folder.ParentKey,
                            parentFolderKey,
                            StringComparison.OrdinalIgnoreCase))
                .Select(
                    folder =>
                        folder.SortOrder)
                .DefaultIfEmpty(
                    0)
                .Max();

        return maxSortOrder + 1000;
    }

    private async Task MoveFolderAsync(
        FolderData folder,
        int direction)
    {
        if (!CanMoveFolder(folder) ||
            direction is not -1 and not 1)
        {
            return;
        }

        var siblings =
            GetSiblingFolders(
                folder);

        var currentIndex =
            siblings.IndexOf(
                folder);

        var targetIndex =
            currentIndex + direction;

        if (currentIndex < 0 ||
            targetIndex < 0 ||
            targetIndex >= siblings.Count)
        {
            return;
        }

        siblings.RemoveAt(
            currentIndex);

        siblings.Insert(
            targetIndex,
            folder);

        await NormalizeAndPersistFolderOrderAsync(
            folder.ParentKey,
            siblings);

        _pendingTreeSelectionKey =
            folder.Key;

        BuildTestTree();
    }

    private async Task NormalizeAndPersistFolderOrderAsync(
        string parentFolderKey,
        IReadOnlyList<FolderData> orderedFolders)
    {
        for (var index = 0;
             index < orderedFolders.Count;
             index++)
        {
            orderedFolders[index].SortOrder =
                (index + 1) * 1000;
        }

        var data =
            await _jsonStorageService.LoadAsync();

        foreach (var folder in orderedFolders)
        {
            var storedFolder =
                data.Folders.FirstOrDefault(
                    item =>
                        string.Equals(
                            item.ProjectKey,
                            _projectKey,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            item.SectionKey,
                            folder.Key,
                            StringComparison.OrdinalIgnoreCase));

            if (storedFolder is null)
            {
                storedFolder =
                    new TestSectionModel
                    {
                        Id =
                            folder.Id != Guid.Empty
                                ? folder.Id
                                : CreateStableEntityId(
                                    $"folder:{_projectKey}:{folder.Key}"),

                        ProjectKey =
                            _projectKey,

                        TestTypeKey =
                            folder.TestTypeKey,

                        SectionKey =
                            folder.Key,

                        ParentSectionKey =
                            parentFolderKey,

                        Name =
                            folder.Name,

                        IsSystem =
                            folder.IsSystem,

                        RequiresManagerRole =
                            folder.RequiresManagerRole
                    };

                data.Folders.Add(
                    storedFolder);
            }

            storedFolder.ParentSectionKey =
                parentFolderKey;

            storedFolder.Name =
                folder.Name;

            storedFolder.CreatedByLogin =
                folder.CreatedByLogin;

            storedFolder.RequiresManagerRole =
                folder.RequiresManagerRole;

            storedFolder.SortOrder =
                folder.SortOrder;
        }

        await _jsonStorageService.SaveAsync(
            data);
    }

    private List<TestCollectionData> GetSiblingCollections(
        TestCollectionData collection)
    {
        return _collections
            .Where(
                item =>
                    string.Equals(
                        item.ParentFolderKey,
                        collection.ParentFolderKey,
                        StringComparison.OrdinalIgnoreCase))
            .OrderBy(
                item =>
                    item.SortOrder)
            .ThenBy(
                item =>
                    item.Name,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task MoveCollectionAsync(
        TestCollectionData collection,
        int direction)
    {
        if (!CanMoveCollection(collection) ||
            direction is not -1 and not 1)
        {
            return;
        }

        var siblings =
            GetSiblingCollections(
                collection);

        var currentIndex =
            siblings.IndexOf(
                collection);

        var targetIndex =
            currentIndex + direction;

        if (currentIndex < 0 ||
            targetIndex < 0 ||
            targetIndex >= siblings.Count)
        {
            return;
        }

        siblings.RemoveAt(
            currentIndex);

        siblings.Insert(
            targetIndex,
            collection);

        await NormalizeAndPersistCollectionOrderAsync(
            collection.ParentFolderKey,
            siblings);

        _pendingTreeSelectionKey =
            collection.Key;

        BuildTestTree();
        UpdateNavigationButtons();
    }

    private async Task NormalizeAndPersistCollectionOrderAsync(
        string parentFolderKey,
        IReadOnlyList<TestCollectionData> orderedCollections)
    {
        for (var index = 0;
             index < orderedCollections.Count;
             index++)
        {
            orderedCollections[index].SortOrder =
                (index + 1) * 1000;
        }

        var data =
            await _jsonStorageService.LoadAsync();

        foreach (var collection in orderedCollections)
        {
            var storedCollection =
                data.Collections.FirstOrDefault(
                    item =>
                        string.Equals(
                            item.ProjectKey,
                            _projectKey,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            item.CollectionKey,
                            collection.Key,
                            StringComparison.OrdinalIgnoreCase));

            if (storedCollection is null)
            {
                storedCollection =
                    new TestCollectionModel
                    {
                        Id =
                            collection.Id != Guid.Empty
                                ? collection.Id
                                : CreateStableEntityId(
                                    $"collection:{_projectKey}:{collection.Key}"),

                        ProjectKey =
                            _projectKey,

                        TestTypeKey =
                            collection.TestTypeKey,

                        ParentFolderKey =
                            parentFolderKey,

                        CollectionKey =
                            collection.Key,

                        Name =
                            collection.Name,

                        Description =
                            collection.Description,

                        IsSystem =
                            collection.IsSystem,

                        RequiresManagerRole =
                            collection.RequiresManagerRole
                    };

                data.Collections.Add(
                    storedCollection);
            }

            storedCollection.ParentFolderKey =
                parentFolderKey;

            storedCollection.Name =
                collection.Name;

            storedCollection.Description =
                collection.Description;

            storedCollection.CreatedByLogin =
                collection.CreatedByLogin;

            storedCollection.RequiresManagerRole =
                collection.RequiresManagerRole;

            storedCollection.SortOrder =
                collection.SortOrder;
        }

        await _jsonStorageService.SaveAsync(
            data);
    }

    private async Task MoveTestCaseAsync(
        TestCollectionData collection,
        TestCaseData testCase,
        int direction)
    {
        if (!CanMoveTestCase(
                testCase) ||
            direction is not -1 and not 1)
        {
            return;
        }

        var orderedCases =
            collection.Cases
                .Where(
                    IsCaseVisibleForActiveAssignment)
                .OrderBy(
                    item =>
                        item.SortOrder)
                .ThenBy(
                    item =>
                        item.Number)
                .ToList();

        var currentIndex =
            orderedCases.IndexOf(
                testCase);

        var targetIndex =
            currentIndex + direction;

        if (currentIndex < 0 ||
            targetIndex < 0 ||
            targetIndex >= orderedCases.Count)
        {
            return;
        }

        orderedCases.RemoveAt(
            currentIndex);

        orderedCases.Insert(
            targetIndex,
            testCase);

        await NormalizeAndPersistTestCaseOrderAsync(
            collection,
            orderedCases);

        RenumberCollectionCases(
            collection);

        RenderCurrentCollectionCases();
        UpdateCurrentCollectionProgress();
    }

    private async Task NormalizeAndPersistTestCaseOrderAsync(
        TestCollectionData collection,
        IReadOnlyList<TestCaseData> orderedCases)
    {
        for (var index = 0;
             index < orderedCases.Count;
             index++)
        {
            orderedCases[index].SortOrder =
                (index + 1) * 1000;
        }

        var data =
            await _jsonStorageService.LoadAsync();

        foreach (var testCase in orderedCases)
        {
            var storedCase =
                data.TestCases.FirstOrDefault(
                    item =>
                        item.Id == testCase.Id);

            if (storedCase is null)
            {
                storedCase =
                    new TestCaseModel
                    {
                        Id =
                            testCase.Id,

                        ProjectKey =
                            _projectKey,

                        TestTypeKey =
                            collection.TestTypeKey,

                        SectionKey =
                            collection.Key,

                        Name =
                            testCase.Name,

                        Status =
                            testCase.Status
                    };

                data.TestCases.Add(
                    storedCase);
            }

            storedCase.SortOrder =
                testCase.SortOrder;

            storedCase.ProjectKey =
                _projectKey;

            storedCase.TestTypeKey =
                collection.TestTypeKey;

            storedCase.SectionKey =
                collection.Key;

                storedCase.Name =
                    testCase.Name;

                storedCase.CreatedByLogin =
                    testCase.CreatedByLogin;

            storedCase.Status =
                testCase.Status;
        }

        await _jsonStorageService.SaveAsync(
            data);
    }

    private void SelectFolderForCommands(
        FolderData folder)
    {
        _selectedFolder =
            folder;

        _selectedCollection =
            null;

        _selectedTestCase =
            null;
    }

    private void SelectCollectionForCommands(
        TestCollectionData collection)
    {
        _selectedFolder =
            null;

        _selectedCollection =
            collection;

        _selectedTestCase =
            null;
    }

    private void SelectTestCaseForCommands(
        TestCollectionData collection,
        TestCaseData testCase)
    {
        _selectedFolder =
            null;

        _selectedCollection =
            collection;

        _selectedTestCase =
            testCase;
    }

    private void CopyFolderToClipboard(
        FolderData folder)
    {
        if (folder.Key == ProjectRootKey)
        {
            return;
        }

        _structureClipboard =
            CreateFolderClipboardItem(
                folder);

        SelectFolderForCommands(
            folder);

        BuildTestTree();
    }

    private FolderClipboardItem CreateFolderClipboardItem(
        FolderData folder)
    {
        var childFolders =
            _folders
                .Where(
                    child =>
                        string.Equals(
                            child.ParentKey,
                            folder.Key,
                            StringComparison.OrdinalIgnoreCase))
                .OrderBy(
                    child =>
                        child.SortOrder)
                .ThenBy(
                    child =>
                        child.Name,
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    CreateFolderClipboardItem)
                .ToList();

        var collections =
            _collections
                .Where(
                    collection =>
                        string.Equals(
                            collection.ParentFolderKey,
                            folder.Key,
                            StringComparison.OrdinalIgnoreCase))
                .OrderBy(
                    collection =>
                        collection.SortOrder)
                .ThenBy(
                    collection =>
                        collection.Name,
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    CreateCollectionClipboardItem)
                .ToList();

        return new FolderClipboardItem(
            folder.Name,
            folder.TestTypeKey,
            childFolders,
            collections);
    }

    private void CopyCollectionToClipboard(
        TestCollectionData collection)
    {
        _structureClipboard =
            CreateCollectionClipboardItem(
                collection);

        SelectCollectionForCommands(
            collection);

        BuildTestTree();
    }

    private static CollectionClipboardItem
        CreateCollectionClipboardItem(
            TestCollectionData collection)
    {
        var cases =
            collection.Cases
                .OrderBy(
                    testCase =>
                        testCase.SortOrder)
                .ThenBy(
                    testCase =>
                        testCase.Number)
                .Select(
                    testCase =>
                        new TestCaseClipboardItem(
                            testCase.Name,
                            testCase.Summary,
                            testCase.Preconditions,
                            testCase.ExternalId,
                            testCase.SourceVersion,
                            testCase.Importance,
                            testCase.ExecutionType,
                            testCase.EstimatedDuration,
                            testCase.Platforms.ToList(),
                            testCase.Steps.Select(CloneStep).ToList()))
                .ToList();

        return new CollectionClipboardItem(
            collection.Name,
            collection.Description,
            cases);
    }

    private void CopyTestCaseToClipboard(
        TestCollectionData collection,
        TestCaseData testCase)
    {
        _structureClipboard =
            new TestCaseClipboardItem(
                testCase.Name,
                testCase.Summary,
                testCase.Preconditions,
                testCase.ExternalId,
                testCase.SourceVersion,
                testCase.Importance,
                testCase.ExecutionType,
                testCase.EstimatedDuration,
                testCase.Platforms.ToList(),
                testCase.Steps.Select(CloneStep).ToList());

        SelectTestCaseForCommands(
            collection,
            testCase);

        BuildTestTree();
    }

    private bool CanPasteIntoFolder(
        FolderData targetFolder)
    {
        return _structureClipboard switch
        {
            FolderClipboardItem =>
                true,

            CollectionClipboardItem =>
                targetFolder.Key !=
                ProjectRootKey,

            _ =>
                false
        };
    }

    private async Task PasteIntoFolderAsync(
        FolderData targetFolder)
    {
        var canPaste =
            _structureClipboard is
                FolderClipboardItem ||
            (_structureClipboard is
                 CollectionClipboardItem &&
             targetFolder.Key !=
             ProjectRootKey);

        if (!canPaste)
        {
            return;
        }

        CaptureUndoSnapshot();

        string? pastedKey =
            null;

        switch (_structureClipboard)
        {
            case FolderClipboardItem folderClipboard:
            {
                var pastedFolder =
                    PasteFolderClipboardItem(
                        folderClipboard,
                        targetFolder,
                        isTopLevelCopy: true);

                pastedKey =
                    pastedFolder.Key;

                break;
            }

            case CollectionClipboardItem
                collectionClipboard
                when targetFolder.Key !=
                     ProjectRootKey:
            {
                var pastedCollection =
                    PasteCollectionClipboardItem(
                        collectionClipboard,
                        targetFolder,
                        createCopyName: true);

                pastedKey =
                    pastedCollection.Key;

                break;
            }

            default:
                return;
        }

        await PersistCurrentStructureAsync();

        RefreshCollectionPaths();

        _pendingTreeSelectionKey =
            pastedKey;

        BuildTestTree();
        UpdateSessionSummary();
    }

    private FolderData PasteFolderClipboardItem(
        FolderClipboardItem clipboardItem,
        FolderData targetParent,
        bool isTopLevelCopy)
    {
        var targetTestTypeKey =
            targetParent.Key ==
            ProjectRootKey
                ? clipboardItem.TestTypeKey
                : targetParent.TestTypeKey;

        var folderName =
            isTopLevelCopy
                ? CreateUniqueCopyName(
                    clipboardItem.Name,
                    _folders
                        .Where(
                            folder =>
                                string.Equals(
                                    folder.ParentKey,
                                    targetParent.Key,
                                    StringComparison.OrdinalIgnoreCase))
                        .Select(
                            folder =>
                                folder.Name))
                : clipboardItem.Name;

        var folder =
            new FolderData
            {
                Id =
                    Guid.NewGuid(),

                Key =
                    Guid.NewGuid()
                        .ToString("N"),

                ParentKey =
                    targetParent.Key,

                Name =
                    folderName,

                CreatedByLogin =
                    _loggedInLogin,

                TestTypeKey =
                    targetTestTypeKey,

                IsSystem =
                    false,

                IsProtected =
                    false,

                SortOrder =
                    GetNextFolderSortOrder(
                        targetParent.Key)
            };

        _folders.Add(
            folder);

        foreach (var collectionClipboard in
                 clipboardItem.Collections)
        {
            PasteCollectionClipboardItem(
                collectionClipboard,
                folder,
                createCopyName: false);
        }

        foreach (var childFolderClipboard in
                 clipboardItem.ChildFolders)
        {
            PasteFolderClipboardItem(
                childFolderClipboard,
                folder,
                isTopLevelCopy: false);
        }

        return folder;
    }

    private TestCollectionData PasteCollectionClipboardItem(
        CollectionClipboardItem clipboardItem,
        FolderData targetFolder,
        bool createCopyName)
    {
        var name =
            createCopyName
                ? CreateUniqueCopyName(
                    clipboardItem.Name,
                    _collections
                        .Where(
                            collection =>
                                string.Equals(
                                    collection.ParentFolderKey,
                                    targetFolder.Key,
                                    StringComparison.OrdinalIgnoreCase))
                        .Select(
                            collection =>
                                collection.Name))
                : clipboardItem.Name;

        var collection =
            new TestCollectionData
            {
                Id =
                    Guid.NewGuid(),

                Key =
                    Guid.NewGuid()
                        .ToString("N"),

                ParentFolderKey =
                    targetFolder.Key,

                Name =
                    name,

                Description =
                    clipboardItem.Description,

                CreatedByLogin =
                    _loggedInLogin,

                Path =
                    BuildFolderPath(
                        targetFolder.Key),

                TestTypeKey =
                    targetFolder.TestTypeKey,

                IsSystem =
                    false,

                IsProtected =
                    false,

                SortOrder =
                    GetNextCollectionSortOrder(
                        targetFolder.Key)
            };

        foreach (var testCaseClipboard in
                 clipboardItem.TestCases)
        {
            collection.Cases.Add(
                CreateTestCaseFromClipboard(
                    testCaseClipboard,
                    collection.Cases.Count));
        }

        _collections.Add(
            collection);

        return collection;
    }

    private int GetNextCollectionSortOrder(
        string parentFolderKey)
    {
        return _collections
                   .Where(
                       collection =>
                           string.Equals(
                               collection.ParentFolderKey,
                               parentFolderKey,
                               StringComparison.OrdinalIgnoreCase))
                   .Select(
                       collection =>
                           collection.SortOrder)
                   .DefaultIfEmpty(
                       0)
                   .Max() +
               1000;
    }

    private TestCaseData CreateTestCaseFromClipboard(
        TestCaseClipboardItem clipboardItem,
        int currentCaseCount)
    {
        return new TestCaseData
        {
            Id =
                Guid.NewGuid(),

            Number =
                currentCaseCount + 1,

            Name =
                clipboardItem.Name,

            CreatedByLogin =
                _loggedInLogin,

            IsSystem =
                false,

            IsProtected =
                false,

            SortOrder =
                (currentCaseCount + 1) *
                1000,

            Status =
                StatusNone,

            Summary = clipboardItem.Summary,
            Preconditions = clipboardItem.Preconditions,
            ExternalId = clipboardItem.ExternalId,
            SourceVersion = clipboardItem.SourceVersion,
            Importance = clipboardItem.Importance,
            ExecutionType = clipboardItem.ExecutionType,
            EstimatedDuration = clipboardItem.EstimatedDuration,
            Platforms = clipboardItem.Platforms?.ToList() ?? new List<string>(),
            Steps = clipboardItem.Steps?.Select(CloneStep).ToList() ?? new List<TestStepModel>()
        };
    }

    private async Task PasteTestCaseIntoCollectionAsync(
        TestCollectionData collection,
        TestCaseData? insertAfterTestCase = null)
    {
        if (_structureClipboard is not
            TestCaseClipboardItem clipboardItem)
        {
            return;
        }

        CaptureUndoSnapshot();

        var copiedName =
            CreateUniqueCopyName(
                clipboardItem.Name,
                collection.Cases.Select(
                    testCase =>
                        testCase.Name));

        var copiedCase =
            CreateTestCaseFromClipboard(
                clipboardItem with
                {
                    Name = copiedName
                },
                collection.Cases.Count);

        collection.Cases.Add(
            copiedCase);

        if (insertAfterTestCase is not null)
        {
            var orderedCases =
                collection.Cases
                    .OrderBy(
                        testCase =>
                            testCase.SortOrder)
                    .ThenBy(
                        testCase =>
                            testCase.Number)
                    .ToList();

            orderedCases.Remove(
                copiedCase);

            var targetIndex =
                orderedCases.IndexOf(
                    insertAfterTestCase);

            orderedCases.Insert(
                targetIndex >= 0
                    ? targetIndex + 1
                    : orderedCases.Count,
                copiedCase);

            for (var index = 0;
                 index < orderedCases.Count;
                 index++)
            {
                orderedCases[index].SortOrder =
                    (index + 1) *
                    1000;
            }
        }

        RenumberCollectionCases(
            collection);

        await PersistCurrentStructureAsync();

        UpdateCollectionState(
            collection);

        UpdateSessionSummary();
        RenderCurrentCollectionCases();
        UpdateCurrentCollectionProgress();
        UpdateActiveCollectionHighlight();
    }

    private async Task PasteCollectionNextToSelectionAsync(
        TestCollectionData selectedCollection)
    {
        if (_structureClipboard is not
            CollectionClipboardItem clipboardItem)
        {
            return;
        }

        var parentFolder =
            _folders.FirstOrDefault(
                folder =>
                    string.Equals(
                        folder.Key,
                        selectedCollection.ParentFolderKey,
                        StringComparison.OrdinalIgnoreCase));

        if (parentFolder is null)
        {
            return;
        }

        CaptureUndoSnapshot();

        var pastedCollection =
            PasteCollectionClipboardItem(
                clipboardItem,
                parentFolder,
                createCopyName: true);

        var siblings =
            _collections
                .Where(
                    collection =>
                        string.Equals(
                            collection.ParentFolderKey,
                            parentFolder.Key,
                            StringComparison.OrdinalIgnoreCase))
                .OrderBy(
                    collection =>
                        collection.SortOrder)
                .ThenBy(
                    collection =>
                        collection.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        siblings.Remove(
            pastedCollection);

        var selectedIndex =
            siblings.IndexOf(
                selectedCollection);

        siblings.Insert(
            selectedIndex >= 0
                ? selectedIndex + 1
                : siblings.Count,
            pastedCollection);

        for (var index = 0;
             index < siblings.Count;
             index++)
        {
            siblings[index].SortOrder =
                (index + 1) *
                1000;
        }

        await PersistCurrentStructureAsync();

        _pendingTreeSelectionKey =
            pastedCollection.Key;

        BuildTestTree();
        UpdateSessionSummary();

        SelectCollection(
            pastedCollection,
            revealInTree: true);

        SelectCollectionForCommands(
            pastedCollection);
    }

    private async Task PasteFolderNextToSelectionAsync(
        FolderData selectedFolder)
    {
        if (_structureClipboard is not
            FolderClipboardItem clipboardItem)
        {
            return;
        }

        var parentFolder =
            _folders.FirstOrDefault(
                folder =>
                    string.Equals(
                        folder.Key,
                        selectedFolder.ParentKey,
                        StringComparison.OrdinalIgnoreCase));

        if (parentFolder is null)
        {
            return;
        }

        CaptureUndoSnapshot();

        var pastedFolder =
            PasteFolderClipboardItem(
                clipboardItem,
                parentFolder,
                isTopLevelCopy: true);

        var siblings =
            _folders
                .Where(
                    folder =>
                        string.Equals(
                            folder.ParentKey,
                            parentFolder.Key,
                            StringComparison.OrdinalIgnoreCase))
                .OrderBy(
                    folder =>
                        folder.SortOrder)
                .ThenBy(
                    folder =>
                        folder.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        siblings.Remove(
            pastedFolder);

        var selectedIndex =
            siblings.IndexOf(
                selectedFolder);

        siblings.Insert(
            selectedIndex >= 0
                ? selectedIndex + 1
                : siblings.Count,
            pastedFolder);

        for (var index = 0;
             index < siblings.Count;
             index++)
        {
            siblings[index].SortOrder =
                (index + 1) *
                1000;
        }

        await PersistCurrentStructureAsync();

        RefreshCollectionPaths();

        _pendingTreeSelectionKey =
            pastedFolder.Key;

        BuildTestTree();
        UpdateSessionSummary();

        SelectFolderForCommands(
            pastedFolder);
    }

    private async Task DuplicateCollectionAsync(
        TestCollectionData sourceCollection)
    {
        var duplicateName =
            CreateUniqueCopyName(
                sourceCollection.Name,
                _collections
                    .Where(
                        collection =>
                            string.Equals(
                                collection.ParentFolderKey,
                                sourceCollection.ParentFolderKey,
                                StringComparison.OrdinalIgnoreCase))
                    .Select(
                        collection =>
                            collection.Name));

        var model =
            await _userCollectionService.AddCollectionAsync(
                _projectKey,
                sourceCollection.TestTypeKey,
                sourceCollection.ParentFolderKey,
                duplicateName,
                _loggedInLogin);

        var duplicateCollection =
            new TestCollectionData
            {
                Id =
                    model.Id,

                Key =
                    model.CollectionKey,

                ParentFolderKey =
                    model.ParentFolderKey,

                Name =
                    model.Name,

                CreatedByLogin =
                    model.CreatedByLogin,

                Path =
                    BuildFolderPath(
                        model.ParentFolderKey),

                TestTypeKey =
                    model.TestTypeKey,

                IsSystem =
                    false,

                IsProtected =
                    false,

                SortOrder =
                    model.SortOrder
            };

        var sourceCases =
            sourceCollection.Cases
                .OrderBy(
                    testCase =>
                        testCase.SortOrder)
                .ThenBy(
                    testCase =>
                        testCase.Number)
                .ToList();

        foreach (var sourceCase in sourceCases)
        {
            var caseModel =
                await _userTestCaseService.AddTestCaseAsync(
                    _projectKey,
                    duplicateCollection.TestTypeKey,
                    duplicateCollection.Key,
                    sourceCase.Name,
                    _loggedInLogin);

            duplicateCollection.Cases.Add(
                new TestCaseData
                {
                    Id =
                        caseModel.Id,

                    Number =
                        duplicateCollection.Cases.Count + 1,

                    Name =
                        caseModel.Name,

                    CreatedByLogin =
                        caseModel.CreatedByLogin,

                    IsSystem =
                        false,

                    IsProtected =
                        false,

                    SortOrder =
                        caseModel.SortOrder,

                    Status =
                        StatusNone
                });
        }

        _collections.Add(
            duplicateCollection);

        var siblings =
            GetSiblingCollections(
                sourceCollection);

        siblings.Remove(
            duplicateCollection);

        var sourceIndex =
            siblings.IndexOf(
                sourceCollection);

        siblings.Insert(
            sourceIndex + 1,
            duplicateCollection);

        await NormalizeAndPersistCollectionOrderAsync(
            sourceCollection.ParentFolderKey,
            siblings);

        _pendingTreeSelectionKey =
            duplicateCollection.Key;

        BuildTestTree();
        UpdateSessionSummary();

        SelectCollection(
            duplicateCollection);
    }

    private async Task DuplicateTestCaseAsync(
        TestCollectionData collection,
        TestCaseData sourceTestCase)
    {
        var duplicateName =
            CreateUniqueCopyName(
                sourceTestCase.Name,
                collection.Cases.Select(
                    testCase =>
                        testCase.Name));

        var model =
            await _userTestCaseService.AddTestCaseAsync(
                _projectKey,
                collection.TestTypeKey,
                collection.Key,
                duplicateName,
                _loggedInLogin);

        var duplicateCase =
            new TestCaseData
            {
                Id =
                    model.Id,

                Number =
                    collection.Cases.Count + 1,

                Name =
                    model.Name,

                CreatedByLogin =
                    model.CreatedByLogin,

                IsSystem =
                    false,

                IsProtected =
                    false,

                SortOrder =
                    model.SortOrder,

                Status =
                    StatusNone
            };

        collection.Cases.Add(
            duplicateCase);

        var orderedCases =
            collection.Cases
                .Where(
                    IsCaseVisibleForActiveAssignment)
                .OrderBy(
                    testCase =>
                        testCase.SortOrder)
                .ThenBy(
                    testCase =>
                        testCase.Number)
                .ToList();

        orderedCases.Remove(
            duplicateCase);

        var sourceIndex =
            orderedCases.IndexOf(
                sourceTestCase);

        orderedCases.Insert(
            sourceIndex + 1,
            duplicateCase);

        await NormalizeAndPersistTestCaseOrderAsync(
            collection,
            orderedCases);

        RenumberCollectionCases(
            collection);

        UpdateCollectionState(
            collection);

        UpdateSessionSummary();
        RenderCurrentCollectionCases();
        UpdateCurrentCollectionProgress();
        UpdateActiveCollectionHighlight();
    }

    private static string CreateUniqueCopyName(
        string originalName,
        IEnumerable<string> existingNames)
    {
        var names =
            existingNames.ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        var sourceName =
            originalName.Trim();

        var copyMarkerIndex =
            sourceName.LastIndexOf(
                " - kopia",
                StringComparison.OrdinalIgnoreCase);

        var originalBaseName =
            copyMarkerIndex >= 0
                ? sourceName[..copyMarkerIndex].TrimEnd()
                : sourceName;

        var firstCopyName =
            $"{originalBaseName} - kopia";

        if (!names.Contains(
                firstCopyName))
        {
            return firstCopyName;
        }

        var copyNumber =
            2;

        while (names.Contains(
                   $"{firstCopyName} ({copyNumber})"))
        {
            copyNumber++;
        }

        return $"{firstCopyName} ({copyNumber})";
    }

    private async Task AddCollectionAsync(
        FolderData parentFolder)
    {
        var name =
            CreateUniqueCollectionName(
                parentFolder.Key);

        var model =
            await _userCollectionService.AddCollectionAsync(
                _projectKey,
                parentFolder.TestTypeKey,
                parentFolder.Key,
                name,
                _loggedInLogin);

        var newCollection =
            new TestCollectionData
            {
                Id =
                    model.Id,

                Key =
                    model.CollectionKey,

                ParentFolderKey =
                    model.ParentFolderKey,

                Name =
                    model.Name,

                CreatedByLogin =
                    model.CreatedByLogin,

                Path =
                    BuildFolderPath(
                        model.ParentFolderKey),

                TestTypeKey =
                    model.TestTypeKey,

                IsSystem =
                    false,

                IsProtected =
                    false,

                SortOrder =
                    model.SortOrder
            };

        _collections.Add(
            newCollection);

        // Nowy zbiór zawsze pojawia się na końcu wskazanego folderu,
        // a później można go dowolnie przesuwać PPM.
        var siblings =
            GetSiblingCollections(
                newCollection);

        await NormalizeAndPersistCollectionOrderAsync(
            parentFolder.Key,
            siblings);

        _pendingTreeSelectionKey =
            newCollection.Key;

        BuildTestTree();
        SelectCollectionForCommands(newCollection);
        UpdateSessionSummary();

        await RenameCollectionAsync(newCollection);
    }

    private string CreateUniqueCollectionName(
        string parentFolderKey)
    {
        const string baseName =
            "Nowy zbiór przypadków";

        var existingNames =
            _collections
                .Where(
                    collection =>
                        string.Equals(
                            collection.ParentFolderKey,
                            parentFolderKey,
                            StringComparison.OrdinalIgnoreCase))
                .Select(
                    collection =>
                        collection.Name)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        if (!existingNames.Contains(
                baseName))
        {
            return baseName;
        }

        var suffix =
            1;

        while (existingNames.Contains(
                   $"{baseName} ({suffix})"))
        {
            suffix++;
        }

        return $"{baseName} ({suffix})";
    }

    private async Task RenameFolderAsync(
        FolderData folder)
    {
        if (!CanModifyFolder(folder))
        {
            return;
        }

        var newName =
            await ShowRenameDialogAsync(
                "Zmień nazwę folderu",
                folder.Name);

        if (newName is null)
        {
            return;
        }

        if (folder.Id != Guid.Empty)
        {
            var renamed =
                await _userFolderService.RenameFolderAsync(
                    folder.Id,
                    newName);

            if (!renamed)
            {
                return;
            }
        }

        folder.Name =
            newName;

        RefreshCollectionPaths();
        await PersistCurrentStructureAsync();

        _pendingTreeSelectionKey =
            folder.Key;

        BuildTestTree();
    }

    private async Task RenameCollectionAsync(
        TestCollectionData collection)
    {
        if (!CanModifyCollection(collection))
        {
            return;
        }

        var newName =
            await ShowRenameDialogAsync(
                "Zmień nazwę zbioru przypadków",
                collection.Name);

        if (newName is null)
        {
            return;
        }

        if (collection.Id != Guid.Empty)
        {
            var renamed =
                await _userCollectionService.RenameCollectionAsync(
                    collection.Id,
                    newName);

            if (!renamed)
            {
                return;
            }
        }

        collection.Name =
            newName;

        await PersistCurrentStructureAsync();
        BuildTestTree();

        if (_currentCollectionIndex >= 0 &&
            ReferenceEquals(
                collection,
                _collections[_currentCollectionIndex]))
        {
            ShowCurrentCollectionHeader(
                collection);
        }
    }

    private async Task DeleteFolderAsync(
        FolderData folder)
    {
        if (!CanModifyFolder(folder))
        {
            return;
        }

        var deletionStats =
            GetFolderDeletionStats(
                folder.Key);

        var message =
            deletionStats.TotalItems == 0
                ? $"Czy na pewno chcesz usunąć folder „{folder.Name}”?"
                :
                    $"Folder „{folder.Name}” nie jest pusty.\n\n" +
                    $"Podfoldery: {deletionStats.FolderCount}\n" +
                    $"Zbiory przypadków: {deletionStats.CollectionCount}\n" +
                    $"Przypadki testowe: {deletionStats.TestCaseCount}\n\n" +
                    "Usunięcie folderu spowoduje trwałe usunięcie całej jego zawartości.";

        var confirmed =
            await ShowDeleteConfirmationAsync(
                "Usuń folder",
                message);

        if (!confirmed)
        {
            return;
        }

        if (folder.RequiresManagerRole)
        {
            await _assignmentService.RequestStructureDeletionAsync(
                _projectKey,
                "Folder",
                folder.Key,
                folder.Name,
                _testerName);

            await ShowInformationAsync(
                "Wysłano prośbę o usunięcie",
                "Główna gałąź zostanie usunięta dopiero po zatwierdzeniu przez Lidera lub Admina.");
            return;
        }

        if (folder.Id != Guid.Empty)
        {
            var deleted =
                await _userFolderService.DeleteFolderAsync(
                    folder.Id);

            if (!deleted)
            {
                return;
            }
        }

        var removedCurrentCollection =
            _currentCollectionIndex >= 0 &&
            IsCollectionInsideFolderBranch(
                _collections[_currentCollectionIndex],
                folder.Key);

        RemoveFolderBranchFromMemory(
            folder);

        _folders.Remove(
            folder);

        if (removedCurrentCollection)
        {
            _currentCollectionIndex =
                -1;

            ShowWelcomeScreen();
        }

        BuildTestTree();
        UpdateSessionSummary();
    }

    private async Task DeleteCollectionAsync(
        TestCollectionData collection)
    {
        if (!CanModifyCollection(collection))
        {
            return;
        }

        var message =
            collection.Cases.Count == 0
                ? $"Czy na pewno chcesz usunąć zbiór „{collection.Name}”?"
                :
                    $"Zbiór „{collection.Name}” zawiera {collection.Cases.Count} przypadków testowych.\n\n" +
                    "Usunięcie zbioru spowoduje trwałe usunięcie całej jego zawartości.";

        var confirmed =
            await ShowDeleteConfirmationAsync(
                "Usuń zbiór przypadków",
                message);

        if (!confirmed)
        {
            return;
        }

        if (collection.RequiresManagerRole)
        {
            await _assignmentService.RequestStructureDeletionAsync(
                _projectKey,
                "Collection",
                collection.Key,
                collection.Name,
                _testerName);

            await ShowInformationAsync(
                "Wysłano prośbę o usunięcie",
                "Główny zbiór zostanie usunięty dopiero po zatwierdzeniu przez Lidera lub Admina.");
            return;
        }

        if (collection.Id != Guid.Empty)
        {
            var deleted =
                await _userCollectionService.DeleteCollectionAsync(
                    collection.Id);

            if (!deleted)
            {
                return;
            }
        }

        var wasCurrent =
            _currentCollectionIndex >= 0 &&
            ReferenceEquals(
                collection,
                _collections[_currentCollectionIndex]);

        _collections.Remove(
            collection);

        _currentCollectionIndex =
            -1;

        BuildTestTree();
        UpdateSessionSummary();

        if (wasCurrent)
        {
            ShowWelcomeScreen();
        }
    }

    private async Task<bool> ShowDeleteConfirmationAsync(
        string title,
        string message)
    {
        var ownerWindow =
            TopLevel.GetTopLevel(
                this)
            as Window;

        if (ownerWindow is null)
        {
            return false;
        }

        var dialog =
            new ConfirmDeleteWindow(
                title,
                message);

        return await dialog.ShowDialog<bool>(
            ownerWindow);
    }

    private FolderDeletionStats GetFolderDeletionStats(
        string folderKey)
    {
        var descendantFolderKeys =
            GetDescendantFolderKeys(
                folderKey);

        var branchFolderKeys =
            descendantFolderKeys
                .Append(
                    folderKey)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        var collections =
            _collections
                .Where(
                    collection =>
                        branchFolderKeys.Contains(
                            collection.ParentFolderKey))
                .ToList();

        return new FolderDeletionStats(
            descendantFolderKeys.Count,
            collections.Count,
            collections.Sum(
                collection =>
                    collection.Cases.Count));
    }

    private List<string> GetDescendantFolderKeys(
        string parentFolderKey)
    {
        var result =
            new List<string>();

        var directChildren =
            _folders
                .Where(
                    folder =>
                        string.Equals(
                            folder.ParentKey,
                            parentFolderKey,
                            StringComparison.OrdinalIgnoreCase))
                .ToList();

        foreach (var child in directChildren)
        {
            result.Add(
                child.Key);

            result.AddRange(
                GetDescendantFolderKeys(
                    child.Key));
        }

        return result;
    }

    private bool IsCollectionInsideFolderBranch(
        TestCollectionData collection,
        string folderKey)
    {
        if (string.Equals(
                collection.ParentFolderKey,
                folderKey,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var descendantFolderKeys =
            GetDescendantFolderKeys(
                folderKey);

        return descendantFolderKeys.Contains(
            collection.ParentFolderKey,
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<string?> ShowRenameDialogAsync(
        string title,
        string currentName)
    {
        var ownerWindow =
            TopLevel.GetTopLevel(
                this)
            as Window;

        if (ownerWindow is null)
        {
            return null;
        }

        var dialog =
            new RenameItemWindow(
                title,
                currentName);

        var accepted =
            await dialog.ShowDialog<bool>(
                ownerWindow);

        if (!accepted)
        {
            return null;
        }

        return dialog.NewName.Trim();
    }

    private async Task AddUserTestCaseAsync(
        TestCollectionData collection)
    {
        var ownerWindow =
            TopLevel.GetTopLevel(
                this)
            as Window;

        if (ownerWindow is null)
        {
            return;
        }

        var dialog =
            new AddTestCaseWindow();

        var wasAdded =
            await dialog.ShowDialog<bool>(
                ownerWindow);

        if (!wasAdded)
        {
            return;
        }

        var name =
            dialog.TestCaseName.Trim();

        if (string.IsNullOrWhiteSpace(
                name))
        {
            return;
        }

        var model =
            await _userTestCaseService.AddTestCaseAsync(
                _projectKey,
                collection.TestTypeKey,
                collection.Key,
                name,
                _loggedInLogin);

        collection.Cases.Add(
            new TestCaseData
            {
                Id =
                    model.Id,

                Number =
                    collection.Cases.Count + 1,

                Name =
                    model.Name,

                CreatedByLogin =
                    model.CreatedByLogin,

                IsSystem =
                    false,

                IsProtected =
                    false,

                SortOrder =
                    model.SortOrder
            });

        var orderedCases =
            collection.Cases
                .OrderBy(
                    testCase =>
                        testCase.SortOrder)
                .ThenBy(
                    testCase =>
                        testCase.Number)
                .ToList();

        await NormalizeAndPersistTestCaseOrderAsync(
            collection,
            orderedCases);

        RenumberCollectionCases(
            collection);

        UpdateCollectionState(
            collection);

        UpdateSessionSummary();

        if (_currentCollectionIndex >= 0 &&
            ReferenceEquals(
                collection,
                _collections[_currentCollectionIndex]))
        {
            RenderCurrentCollectionCases();
            UpdateCurrentCollectionProgress();
        }
    }

    private void SelectCollection(
        TestCollectionData collection,
        bool revealInTree = false,
        bool expandPath = true)
    {
        HideInlineDashboard();

        var index =
            _collections.IndexOf(
                collection);

        if (index < 0)
        {
            return;
        }

        _currentCollectionIndex =
            index;

        ClearFolderWorkspace();

        if (_welcomePanel is not null)
        {
            _welcomePanel.IsVisible =
                false;
        }

        if (_summaryPanel is not null)
        {
            _summaryPanel.IsVisible =
                false;
        }

        if (_testExecutionPanel is not null)
        {
            _testExecutionPanel.IsVisible =
                true;
        }

        ShowCurrentCollectionHeader(
            collection);

        RenderCurrentCollectionCases();
        UpdateCurrentCollectionProgress();
        UpdateNavigationButtons();
        UpdateSessionSummary();
        if (expandPath)
        {
            ExpandPathToCollection(
                collection.Key);
        }
        UpdateActiveCollectionHighlight();

        if (revealInTree)
        {
            Dispatcher.UIThread.Post(
                () =>
                {
                    RevealCollectionInTreeGently(
                        collection);
                },
                DispatcherPriority.Loaded);
        }

        _ = TrackCurrentLocationAsync(
            collection);
    }

    private void ShowCurrentCollectionHeader(
        TestCollectionData collection)
    {
        if (_currentSectionTitleTextBlock is not null)
        {
            _currentSectionTitleTextBlock.Text =
                collection.Name;
        }

        if (_currentSectionPathTextBlock is not null)
        {
            _currentSectionPathTextBlock.Text =
                collection.Path;
        }

        UpdateCollectionDescriptionDisplay(
            collection);
    }

    private void UpdateCollectionDescriptionDisplay(
        TestCollectionData collection)
    {
        var hasDescription =
            !string.IsNullOrWhiteSpace(
                collection.Description);

        if (_addCollectionDescriptionButton is not null)
        {
            _addCollectionDescriptionButton.IsVisible =
                !hasDescription;
        }

        if (_collectionDescriptionPanel is not null)
        {
            _collectionDescriptionPanel.IsVisible =
                hasDescription;
        }

        if (_currentCollectionDescriptionTextBlock is not null)
        {
            _currentCollectionDescriptionTextBlock.Text =
                collection.Description;
        }
    }

    private async void CollectionDescriptionButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var collection =
            GetCurrentCollection();

        var ownerWindow =
            TopLevel.GetTopLevel(
                this)
            as Window;

        if (collection is null ||
            ownerWindow is null)
        {
            return;
        }

        var dialog =
            new CollectionDescriptionWindow(
                collection.Description);

        var accepted =
            await dialog.ShowDialog<bool>(
                ownerWindow);

        if (!accepted)
        {
            return;
        }

        collection.Description =
            dialog.Description;

        UpdateCollectionDescriptionDisplay(
            collection);

        await _userCollectionService.SaveDescriptionAsync(
            new TestCollectionModel
            {
                Id =
                    collection.Id,

                ProjectKey =
                    _projectKey,

                TestTypeKey =
                    collection.TestTypeKey,

                ParentFolderKey =
                    collection.ParentFolderKey,

                CollectionKey =
                    collection.Key,

                Name =
                    collection.Name,

                Description =
                    collection.Description,

                IsSystem =
                    collection.IsSystem,

                SortOrder =
                    collection.SortOrder
            });
    }

    private void RenderCurrentCollectionCases()
    {
        if (_testCasesStackPanel is null ||
            _currentCollectionIndex < 0)
        {
            return;
        }

        _testCasesStackPanel.Children.Clear();

        var collection =
            _collections[
                _currentCollectionIndex];

        RenumberCollectionCases(
            collection);

        var orderedCases =
            collection.Cases
                .OrderBy(
                    testCase =>
                        testCase.SortOrder)
                .ThenBy(
                    testCase =>
                        testCase.Number)
                .ToList();

        foreach (var testCase in orderedCases)
        {
            var row =
                new TestCaseRow
                {
                    TestCaseId =
                        testCase.Id,

                    IsUserDefined =
                        !testCase.IsSystem,

                    CanRename =
                        CanModifyTestCase(testCase),

                    Number =
                        testCase.Number,

                    TestCaseName =
                        testCase.Name,

                    AllowPendingStatus =
                        _activeAssignmentCaseIds is null,

                    BlockedComment =
                        testCase.Comment,

                    Status =
                        testCase.Status,

                    CanMoveUp =
                        CanMoveTestCase(testCase) &&
                        orderedCases.IndexOf(
                            testCase) > 0,

                    CanMoveDown =
                        CanMoveTestCase(testCase) &&
                        orderedCases.IndexOf(
                            testCase) <
                        orderedCases.Count - 1
                };

            row.StatusChanged +=
                async (_, newStatus) =>
                {
                    if (string.Equals(newStatus, StatusBlocked, StringComparison.OrdinalIgnoreCase) &&
                        string.IsNullOrWhiteSpace(row.BlockedComment))
                    {
                        row.FlashBlockedCommentValidation();
                        UpdateNavigationButtons();
                        return;
                    }

                    var previousStatus =
                        testCase.Status;

                    testCase.Status =
                        newStatus;

                    testCase.Comment =
                        row.BlockedComment;

                    UpdateCollectionState(
                        collection);

                    UpdateCurrentCollectionProgress();
                    UpdateSessionSummary();
                    UpdateActiveCollectionHighlight();

                    TryAutoScrollAfterSequentialCompletion(
                        collection,
                        testCase,
                        previousStatus,
                        row);

                    if (_activeAssignmentIdByCaseId.TryGetValue(
                            testCase.Id,
                            out var assignmentId))
                    {
                        await PersistAssignmentCaseStatusAsync(
                            assignmentId,
                            testCase.Id,
                            newStatus,
                            testCase.Comment);
                    }
                    else
                    {
                        await _userTestCaseService.SaveStatusAsync(
                            testCase.Id,
                            _projectKey,
                            collection.TestTypeKey,
                            collection.Key,
                            testCase.Name,
                            testCase.SortOrder,
                            newStatus,
                            testCase.Comment);
                    }

                    await TrackResultChangeAsync(
                        testCase.Name);

                    UpdateNavigationButtons();
                };

            row.BlockedValidationChanged +=
                (_, _) => UpdateNavigationButtons();

            row.MoveUpRequested +=
                async (_, _) =>
                {
                    await MoveTestCaseAsync(
                        collection,
                        testCase,
                        -1);
                };

            row.MoveDownRequested +=
                async (_, _) =>
                {
                    await MoveTestCaseAsync(
                        collection,
                        testCase,
                        1);
                };

            row.DragRequested +=
                async (_, triggerEvent) =>
                {
                    if (!CanMoveTestCase(testCase))
                    {
                        return;
                    }

                    var dragData =
                        new DataTransfer();

                    dragData.Add(
                        DataTransferItem.CreateText(
                            $"qa-test-case:{testCase.Id}"));

                    await DragDrop.DoDragDropAsync(
                        triggerEvent,
                        dragData,
                        DragDropEffects.Move);
                };

            DragDrop.SetAllowDrop(
                row,
                CanReorderStructure);

            DragDrop.AddDragOverHandler(
                row,
                (_, eventArgs) =>
                {
                    if (!CanReorderStructure)
                    {
                        eventArgs.DragEffects =
                            DragDropEffects.None;

                        eventArgs.Handled =
                            true;

                        return;
                    }

                    var draggedCase =
                        FindDraggedTestCase(
                            eventArgs);

                    var acceptsDrop =
                        draggedCase is not null &&
                        CanMoveTestCase(
                            draggedCase.Value.TestCase) &&
                        !ReferenceEquals(
                            draggedCase.Value.TestCase,
                            testCase);

                    eventArgs.DragEffects =
                        acceptsDrop
                            ? DragDropEffects.Move
                            : DragDropEffects.None;

                    row.SetDragTarget(
                        acceptsDrop);

                    eventArgs.Handled =
                        true;
                });

            DragDrop.AddDragLeaveHandler(
                row,
                (_, _) =>
                {
                    row.SetDragTarget(
                        false);
                });

            DragDrop.AddDropHandler(
                row,
                async (_, eventArgs) =>
                {
                    // Zatrzymujemy propagację przed pierwszym await, aby ten sam
                    // drop nie został ponownie obsłużony przez zbiór lub folder.
                    eventArgs.Handled =
                        true;

                    row.SetDragTarget(
                        false);

                    if (!CanReorderStructure)
                    {
                        eventArgs.DragEffects =
                            DragDropEffects.None;

                        return;
                    }

                    var draggedCase =
                        FindDraggedTestCase(
                            eventArgs);

                    if (draggedCase is null ||
                        ReferenceEquals(
                            draggedCase.Value.TestCase,
                            testCase))
                    {
                        eventArgs.DragEffects =
                            DragDropEffects.None;

                        return;
                    }

                    var insertAfter =
                        eventArgs
                            .GetPosition(
                                row)
                            .Y >
                        row.Bounds.Height / 2;

                    if (!await ExecuteTreeDropSafelyAsync(
                            () => DropTestCaseAsync(
                                draggedCase.Value.Collection,
                                draggedCase.Value.TestCase,
                                collection,
                                testCase,
                                insertAfter)))
                    {
                        eventArgs.DragEffects =
                            DragDropEffects.None;

                        return;
                    }

                    eventArgs.DragEffects =
                        DragDropEffects.Move;

                });

            row.DuplicateRequested +=
                async (_, _) =>
                {
                    await DuplicateTestCaseAsync(
                        collection,
                        testCase);
                };

            row.SelectedRequested +=
                (_, _) =>
                {
                    SelectTestCaseForCommands(
                        collection,
                        testCase);
                };

            row.CopyRequested +=
                (_, _) =>
                {
                    CopyTestCaseToClipboard(
                        collection,
                        testCase);
                };

            row.DetailsRequested +=
                async (_, _) =>
                {
                    await EditTestCaseDetailsAsync(
                        collection,
                        testCase);
                };

            if (CanModifyTestCase(testCase))
            {
                row.RenameRequested +=
                    async (_, _) =>
                    {
                        await RenameUserTestCaseAsync(
                            collection,
                            testCase);
                    };

                row.DeleteRequested +=
                    async (_, _) =>
                    {
                        await DeleteUserTestCaseAsync(
                            collection,
                            testCase);
                    };
            }

            _testCasesStackPanel.Children.Add(
                row);
        }

        _testCasesStackPanel.Children.Add(
            CreateAddCaseButton(
                collection));

        _testCasesStackPanel.Children.Add(
            CreateAddCaseContextArea(
                collection));
    }

    private void TryAutoScrollAfterSequentialCompletion(
        TestCollectionData collection,
        TestCaseData completedCase,
        string previousStatus,
        TestCaseRow completedRow)
    {
        if (_testCasesScrollViewer is null ||
            _testCasesStackPanel is null ||
            IsFinalStatus(
                previousStatus) ||
            !IsFinalStatus(
                completedCase.Status))
        {
            return;
        }

        var orderedCases =
            collection.Cases
                .OrderBy(
                    testCase =>
                        testCase.SortOrder)
                .ThenBy(
                    testCase =>
                        testCase.Number)
                .ToList();

        var completedIndex =
            orderedCases.IndexOf(
                completedCase);

        if (completedIndex < 0)
        {
            return;
        }

        var isMovingBackwards =
            string.Equals(
                _lastAutoScrollCollectionKey,
                collection.Key,
                StringComparison.OrdinalIgnoreCase) &&
            _lastAutoScrollCaseIndex >= 0 &&
            completedIndex < _lastAutoScrollCaseIndex;

        _lastAutoScrollCollectionKey =
            collection.Key;

        _lastAutoScrollCaseIndex =
            completedIndex;

        if (isMovingBackwards)
        {
            Dispatcher.UIThread.Post(
                () => ScrollCaseWithPreviousRowsVisible(
                    completedRow,
                    3),
                DispatcherPriority.Loaded);

            return;
        }

        if (orderedCases
                .Take(
                    completedIndex)
                .Any(
                    testCase =>
                        !IsFinalStatus(
                            testCase.Status)))
        {
            return;
        }

        var nextIncompleteCase =
            orderedCases
                .Skip(
                    completedIndex + 1)
                .FirstOrDefault(
                    testCase =>
                        !IsFinalStatus(
                            testCase.Status));

        if (nextIncompleteCase is null)
        {
            TryRevealNextSequentialCollection(
                collection);

            return;
        }

        var nextRow =
            _testCasesStackPanel.Children
                .OfType<TestCaseRow>()
                .FirstOrDefault(
                    row =>
                        row.TestCaseId ==
                        nextIncompleteCase.Id);

        if (nextRow is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(
            () => ScrollCompletedCaseOutOfView(
                completedRow,
                nextRow),
            DispatcherPriority.Loaded);
    }

    private void ScrollCaseWithPreviousRowsVisible(
        TestCaseRow selectedRow,
        int previousRowsToShow)
    {
        if (_testCasesScrollViewer is null ||
            _testCasesStackPanel is null)
        {
            return;
        }

        var selectedPosition =
            selectedRow.TranslatePoint(
                new Point(),
                _testCasesStackPanel);

        if (!selectedPosition.HasValue)
        {
            return;
        }

        var visibleRows =
            _testCasesStackPanel.Children
                .OfType<TestCaseRow>()
                .ToList();

        var selectedIndex =
            visibleRows.IndexOf(
                selectedRow);

        var rowStep =
            selectedRow.Bounds.Height;

        if (selectedIndex > 0)
        {
            var previousPosition =
                visibleRows[selectedIndex - 1]
                    .TranslatePoint(
                        new Point(),
                        _testCasesStackPanel);

            if (previousPosition.HasValue)
            {
                rowStep =
                    selectedPosition.Value.Y -
                    previousPosition.Value.Y;
            }
        }

        var targetOffset =
            Math.Max(
                0,
                selectedPosition.Value.Y -
                Math.Max(1, rowStep) *
                previousRowsToShow);

        AnimateTestCaseScroll(
            _testCasesScrollViewer.Offset.Y,
            targetOffset);
    }

    private void ScrollCompletedCaseOutOfView(
        TestCaseRow completedRow,
        TestCaseRow nextRow)
    {
        if (_testCasesScrollViewer is null ||
            _testCasesStackPanel is null)
        {
            return;
        }

        var currentOffset =
            _testCasesScrollViewer.Offset.Y;

        var completedPosition =
            completedRow.TranslatePoint(
                new Point(),
                _testCasesStackPanel);

        var nextPosition =
            nextRow.TranslatePoint(
                new Point(),
                _testCasesStackPanel);

        var oneRowStep =
            completedPosition.HasValue &&
            nextPosition.HasValue
                ? nextPosition.Value.Y -
                  completedPosition.Value.Y
                : completedRow.Bounds.Height;

        var scrollDistance =
            Math.Clamp(
                oneRowStep,
                1,
                Math.Max(
                    1,
                    completedRow.Bounds.Height));

        EnsureSequentialScrollTailSpace(
            scrollDistance);

        // Margin rozszerzający koniec listy wpływa na Extent dopiero po
        // kolejnym przebiegu layoutu. Dopiero wtedy wyliczamy cel animacji.
        Dispatcher.UIThread.Post(
            () =>
            {
                if (_testCasesScrollViewer is null)
                {
                    return;
                }

                var maximumOffset =
                    Math.Max(
                        0,
                        _testCasesScrollViewer.Extent.Height -
                        _testCasesScrollViewer.Viewport.Height);

                var targetOffset =
                    Math.Min(
                        maximumOffset,
                        currentOffset + scrollDistance);

                AnimateTestCaseScroll(
                    currentOffset,
                    targetOffset);
            },
            DispatcherPriority.Loaded);
    }

    private void EnsureSequentialScrollTailSpace(
        double rowStep)
    {
        if (_testCasesScrollViewer is null ||
            _testCasesStackPanel is null)
        {
            return;
        }

        // Pozwala przedostatniemu wierszowi dojść do górnej pozycji, dzięki
        // czemu ostatni przypadek zajmuje dokładnie miejsce kolejnego
        // dwukliku. Po ostatnim przypadku przewijanie nie jest już wywołane.
        var requiredBottomSpace =
            Math.Max(
                0,
                _testCasesScrollViewer.Viewport.Height -
                Math.Max(1, rowStep) * 2 -
                12);

        _testCasesStackPanel.Margin =
            new Thickness(
                0,
                0,
                8,
                requiredBottomSpace);
    }

    private async void AnimateTestCaseScroll(
        double startOffset,
        double targetOffset)
    {
        if (_testCasesScrollViewer is null ||
            Math.Abs(
                targetOffset - startOffset) < 0.5)
        {
            return;
        }

        var animationVersion =
            ++_testCaseScrollAnimationVersion;

        const int stepCount =
            8;

        for (var step = 1;
             step <= stepCount;
             step++)
        {
            await Task.Delay(
                16);

            if (_testCasesScrollViewer is null ||
                animationVersion !=
                _testCaseScrollAnimationVersion)
            {
                return;
            }

            var progress =
                step /
                (double)stepCount;

            var easedProgress =
                1 -
                Math.Pow(
                    1 - progress,
                    3);

            _testCasesScrollViewer.Offset =
                new Vector(
                    _testCasesScrollViewer.Offset.X,
                    startOffset +
                    (targetOffset - startOffset) *
                    easedProgress);
        }
    }

    private void TryRevealNextSequentialCollection(
        TestCollectionData completedCollection)
    {
        if (completedCollection.Cases.Any(
                testCase =>
                    !IsFinalStatus(
                        testCase.Status)))
        {
            return;
        }

        var collections =
            GetCollectionsForTestType(
                completedCollection.TestTypeKey);

        var currentIndex =
            collections.IndexOf(
                completedCollection);

        if (currentIndex < 0 ||
            currentIndex >= collections.Count - 1 ||
            collections
                .Take(
                    currentIndex)
                .SelectMany(
                    collection =>
                        collection.Cases)
                .Any(
                    testCase =>
                        !IsFinalStatus(
                            testCase.Status)))
        {
            return;
        }

        Dispatcher.UIThread.Post(
            () =>
            {
                RevealCollectionInTreeGently(
                    collections[currentIndex + 1]);
            },
            DispatcherPriority.Loaded);
    }

    private void RevealCollectionInTreeGently(
        TestCollectionData collection)
    {
        if (_testTreeScrollViewer is null ||
            collection.TreeItem is null)
        {
            return;
        }

        var itemPosition =
            collection.TreeItem.TranslatePoint(
                new Point(),
                _testTreeScrollViewer);

        if (itemPosition is null)
        {
            return;
        }

        var itemTop =
            itemPosition.Value.Y;

        var itemHeight =
            Math.Max(
                28,
                collection.TreeItem.Bounds.Height);

        var itemBottom =
            itemTop +
            itemHeight;

        var lookAheadDistance =
            Math.Clamp(
                itemHeight * 3.5,
                110,
                160);

        var comfortableBottom =
            _testTreeScrollViewer.Viewport.Height -
            lookAheadDistance;

        const double comfortableTop = 24;

        var delta =
            itemBottom >
            comfortableBottom
                ? itemBottom -
                  comfortableBottom
                : itemTop < comfortableTop
                    ? itemTop - comfortableTop
                    : 0;

        var currentVerticalOffset =
            _testTreeScrollViewer.Offset.Y;

        var maximumVerticalOffset =
            Math.Max(
                0,
                _testTreeScrollViewer.Extent.Height -
                _testTreeScrollViewer.Viewport.Height);

        var targetVerticalOffset =
            Math.Clamp(
                currentVerticalOffset + delta,
                0,
                maximumVerticalOffset);

        // Lewa krawędź drzewa zawiera strzałki, ikony stanu i znacznik
        // aktualnego zbioru. Nie przesuwamy jej poziomo poza widok.
        const double targetHorizontalOffset = 0;

        var currentHorizontalOffset =
            _testTreeScrollViewer.Offset.X;

        if (Math.Abs(
                targetVerticalOffset -
                currentVerticalOffset) < 1 &&
            Math.Abs(
                targetHorizontalOffset -
                currentHorizontalOffset) < 1)
        {
            return;
        }

        AnimateTreeScroll(
            new Vector(
                currentHorizontalOffset,
                currentVerticalOffset),
            new Vector(
                targetHorizontalOffset,
                targetVerticalOffset));
    }

    private async void AnimateTreeScroll(
        Vector startOffset,
        Vector targetOffset)
    {
        if (_testTreeScrollViewer is null ||
            (targetOffset -
             startOffset).Length < 1)
        {
            return;
        }

        var animationVersion =
            ++_treeScrollAnimationVersion;

        const int stepCount = 8;

        for (var step = 1;
             step <= stepCount;
             step++)
        {
            await Task.Delay(
                16);

            if (_testTreeScrollViewer is null ||
                animationVersion !=
                _treeScrollAnimationVersion)
            {
                return;
            }

            var progress =
                step /
                (double)stepCount;

            var easedProgress =
                1 -
                Math.Pow(
                    1 - progress,
                    3);

            _testTreeScrollViewer.Offset =
                new Vector(
                    startOffset.X +
                    (targetOffset.X -
                     startOffset.X) *
                    easedProgress,
                    startOffset.Y +
                    (targetOffset.Y -
                     startOffset.Y) *
                    easedProgress);
        }
    }

    private (TestCollectionData Collection, TestCaseData TestCase)?
        FindDraggedTestCase(
            DragEventArgs eventArgs)
    {
        var value =
            eventArgs.DataTransfer.TryGetText();

        const string prefix =
            "qa-test-case:";

        if (string.IsNullOrWhiteSpace(
                value) ||
            !value.StartsWith(
                prefix,
                StringComparison.Ordinal) ||
            !Guid.TryParse(
                value[prefix.Length..],
                out var testCaseId))
        {
            return null;
        }

        foreach (var collection in _collections)
        {
            var testCase =
                collection.Cases.FirstOrDefault(
                    item =>
                        item.Id ==
                        testCaseId);

            if (testCase is not null)
            {
                return (
                    collection,
                    testCase);
            }
        }

        return null;
    }

    private async Task DropTestCaseAsync(
        TestCollectionData sourceCollection,
        TestCaseData sourceTestCase,
        TestCollectionData targetCollection,
        TestCaseData targetTestCase,
        bool insertAfter)
    {
        if (!CanReorderStructure)
        {
            return;
        }

        var targetCases =
            targetCollection.Cases
                .OrderBy(
                    item =>
                        item.SortOrder)
                .ThenBy(
                    item =>
                        item.Number)
                .ToList();

        var targetIndex =
            targetCases.IndexOf(
                targetTestCase);

        if (targetIndex < 0)
        {
            return;
        }

        var sourceSnapshot =
            sourceCollection.Cases.ToList();
        var targetSnapshot =
            ReferenceEquals(sourceCollection, targetCollection)
                ? sourceSnapshot
                : targetCollection.Cases.ToList();
        var orderSnapshot =
            sourceSnapshot
                .Concat(targetSnapshot)
                .Distinct()
                .ToDictionary(
                    item => item,
                    item => (item.SortOrder, item.Number));

        try
        {
            sourceCollection.Cases.Remove(
                sourceTestCase);

            targetCases.Remove(
                sourceTestCase);

            targetIndex =
                targetCases.IndexOf(
                    targetTestCase);

            if (targetIndex < 0)
            {
                return;
            }

            targetCases.Insert(
                targetIndex +
                    (insertAfter ? 1 : 0),
                sourceTestCase);

            if (!ReferenceEquals(
                    sourceCollection,
                    targetCollection))
            {
                targetCollection.Cases.Add(
                    sourceTestCase);

            var sourceCases =
                sourceCollection.Cases
                    .OrderBy(
                        item =>
                            item.SortOrder)
                    .ThenBy(
                        item =>
                            item.Number)
                    .ToList();

            await NormalizeAndPersistTestCaseOrderAsync(
                sourceCollection,
                sourceCases);

            RenumberCollectionCases(
                sourceCollection);
            }
            else
            {
                targetCollection.Cases.Clear();

                targetCollection.Cases.AddRange(
                    targetCases);
            }

            await NormalizeAndPersistTestCaseOrderAsync(
                targetCollection,
                targetCases);

            RenumberCollectionCases(
                targetCollection);
        }
        catch
        {
            sourceCollection.Cases.Clear();
            sourceCollection.Cases.AddRange(sourceSnapshot);

            if (!ReferenceEquals(sourceCollection, targetCollection))
            {
                targetCollection.Cases.Clear();
                targetCollection.Cases.AddRange(targetSnapshot);
            }

            foreach (var (testCase, order) in orderSnapshot)
            {
                testCase.SortOrder = order.SortOrder;
                testCase.Number = order.Number;
            }

            UpdateCollectionState(sourceCollection);
            UpdateCollectionState(targetCollection);
            throw;
        }

        UpdateCollectionState(
            sourceCollection);

        UpdateCollectionState(
            targetCollection);

        BuildTestTree();
        UpdateSessionSummary();
        RenderCurrentCollectionCases();
        UpdateCurrentCollectionProgress();
    }

    private Control CreateAddCaseButton(
        TestCollectionData collection)
    {
        var button =
            new Button
            {
                Content =
                    "+  " + LocalizationService.T("Structure.AddCase"),

                HorizontalAlignment =
                    HorizontalAlignment.Left,

                Margin =
                    new Thickness(
                        0,
                        4,
                        0,
                        10),

                Padding =
                    new Thickness(
                        18,
                        10),

                CornerRadius =
                    new CornerRadius(10),

                Background =
                    new SolidColorBrush(
                        Color.Parse(
                            "#1828C76F")),

                BorderBrush =
                    new SolidColorBrush(
                        Color.Parse(
                            "#7028C76F")),

                BorderThickness =
                    new Thickness(1),

                Foreground =
                    new SolidColorBrush(
                        Color.Parse(
                            "#28C76F")),

                Cursor =
                    new Cursor(
                        StandardCursorType.Hand)
            };

        button.Click +=
            async (_, _) =>
            {
                await AddUserTestCaseAsync(
                    collection);
            };

        return button;
    }

    private Control CreateAddCaseContextArea(
        TestCollectionData collection)
    {
        var addCaseItem =
            new MenuItem
            {
                Header =
                    LocalizationService.T("Structure.AddCase")
            };

        addCaseItem.Click +=
            async (_, _) =>
            {
                await AddUserTestCaseAsync(
                    collection);
            };

        return new Border
        {
            MinHeight =
                72,

            HorizontalAlignment =
                HorizontalAlignment.Stretch,

            Background =
                Brushes.Transparent,

            ContextMenu =
                new ContextMenu
                {
                    ItemsSource =
                        new[]
                        {
                            addCaseItem
                        }
                }
        };
    }

    private async Task RenameUserTestCaseAsync(
        TestCollectionData collection,
        TestCaseData testCase)
    {
        if (!CanModifyTestCase(testCase))
        {
            return;
        }

        var newName =
            await ShowRenameDialogAsync(
                "Zmień nazwę przypadku",
                testCase.Name);

        if (newName is null)
        {
            return;
        }

        if (!testCase.IsSystem)
        {
            var renamed =
                await _userTestCaseService.RenameTestCaseAsync(
                    testCase.Id,
                    newName);

            if (!renamed)
            {
                return;
            }
        }

        testCase.Name =
            newName;

        await PersistCurrentStructureAsync();
        RenderCurrentCollectionCases();
    }

    private async Task EditTestCaseDetailsAsync(
        TestCollectionData collection,
        TestCaseData testCase)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner is null || !CanModifyTestCase(testCase))
        {
            return;
        }

        var dialog = new TestCaseDetailsWindow(
            testCase.Name,
            testCase.Summary,
            testCase.Preconditions,
            testCase.Platforms,
            testCase.Steps);

        if (!await dialog.ShowDialog<bool>(owner))
        {
            return;
        }

        CaptureUndoSnapshot();
        testCase.Name = dialog.CaseName;
        testCase.Summary = dialog.Summary;
        testCase.Preconditions = dialog.Preconditions;
        testCase.Platforms = dialog.Platforms;
        testCase.Steps = dialog.Steps;

        await PersistCurrentStructureAsync();
        RenderCurrentCollectionCases();
    }

    private async Task DeleteUserTestCaseAsync(
        TestCollectionData collection,
        TestCaseData testCase)
    {
        if (!CanModifyTestCase(testCase))
        {
            return;
        }

        if (!testCase.IsSystem)
        {
            var deleted =
                await _userTestCaseService.DeleteTestCaseAsync(
                    testCase.Id);

            if (!deleted)
            {
                return;
            }
        }

        collection.Cases.Remove(
            testCase);

        RenumberCollectionCases(
            collection);

        UpdateCollectionState(
            collection);

        UpdateSessionSummary();
        RenderCurrentCollectionCases();
        UpdateCurrentCollectionProgress();
        UpdateActiveCollectionHighlight();
    }

    private static void RenumberCollectionCases(
        TestCollectionData collection)
    {
        var orderedCases =
            collection.Cases
                .OrderBy(
                    testCase =>
                        testCase.SortOrder)
                .ThenBy(
                    testCase =>
                        testCase.Number)
                .ToList();

        for (var index = 0;
             index < orderedCases.Count;
             index++)
        {
            orderedCases[index].Number =
                index + 1;
        }
    }

    private void UpdateCurrentCollectionProgress()
    {
        if (_currentCollectionIndex < 0)
        {
            return;
        }

        var collection =
            _collections[
                _currentCollectionIndex];

        var visibleCases =
            collection.Cases
                .Where(
                    IsCaseVisibleForActiveAssignment)
                .ToList();

        var completed =
            visibleCases.Count(
                testCase =>
                    IsFinalStatus(
                        testCase.Status));

        if (_currentSectionProgressTextBlock is not null)
        {
            _currentSectionProgressTextBlock.Text =
                $"{completed} / {visibleCases.Count}";
        }
    }

    private void UpdateCollectionState(
        TestCollectionData collection)
    {
        var completed =
            collection.Cases.Count(
                testCase =>
                    IsFinalStatus(
                        testCase.Status));

        var inProgress =
            collection.Cases.Count(
                testCase =>
                    testCase.Status ==
                    StatusInProgress);

        var failed =
            collection.Cases.Count(
                testCase =>
                    testCase.Status ==
                    StatusFailed);

        var blocked =
            collection.Cases.Count(
                testCase =>
                    testCase.Status ==
                    StatusBlocked);

        var na =
            collection.Cases.Count(
                testCase =>
                    testCase.Status ==
                    StatusNa);

        if (collection.ProgressText is not null)
        {
            collection.ProgressText.Text =
                $"{completed}/{collection.Cases.Count}";
        }

        if (collection.StateIcon is null ||
            collection.HeaderBorder is null)
        {
            return;
        }

        ClearCollectionStateClasses(
            collection.HeaderBorder);

        if (inProgress > 0)
        {
            collection.StateIcon.Text =
                "⌛";

            collection.HeaderBorder.Classes.Add(
                "InProgressRow");

            return;
        }

        if (completed < collection.Cases.Count ||
            collection.Cases.Count == 0)
        {
            collection.StateIcon.Text =
                "○";

            return;
        }

        if (failed > 0)
        {
            collection.StateIcon.Text =
                "✕";

            collection.HeaderBorder.Classes.Add(
                "FailedRow");

            return;
        }

        if (blocked > 0)
        {
            collection.StateIcon.Text =
                "!";

            collection.HeaderBorder.Classes.Add(
                "BlockedRow");

            return;
        }

        if (na > 0)
        {
            collection.StateIcon.Text =
                "⚠";

            collection.HeaderBorder.Classes.Add(
                "WarningRow");

            return;
        }

        collection.StateIcon.Text =
            "✓";

        collection.HeaderBorder.Classes.Add(
            "SuccessRow");
    }

    private static bool IsFinalStatus(
        string status)
    {
        return status == StatusSuccess ||
               status == StatusFailed ||
               status == StatusNa ||
               status == StatusBlocked;
    }

    private static void ClearCollectionStateClasses(
        Border border)
    {
        border.Classes.Remove(
            "InProgressRow");

        border.Classes.Remove(
            "SuccessRow");

        border.Classes.Remove(
            "FailedRow");

        border.Classes.Remove(
            "WarningRow");

        border.Classes.Remove(
            "BlockedRow");
    }

    private void UpdateSessionSummary()
    {
        var currentCollection =
            GetCurrentCollection();

        IEnumerable<TestCollectionData> visibleCollections =
            currentCollection is null
                ? _collections
                : new[]
                {
                    currentCollection
                };

        var visibleCases =
            visibleCollections
                .SelectMany(
                    collection =>
                        collection.Cases)
                .Where(
                    IsCaseVisibleForActiveAssignment)
                .ToList();

        var success =
            visibleCases.Count(
                testCase =>
                    testCase.Status ==
                    StatusSuccess);

        var inProgress =
            visibleCases.Count(
                testCase =>
                    testCase.Status ==
                    StatusInProgress);

        var failed =
            visibleCases.Count(
                testCase =>
                    testCase.Status ==
                    StatusFailed);

        var na =
            visibleCases.Count(
                testCase =>
                    testCase.Status ==
                    StatusNa);

        var blocked =
            visibleCases.Count(
                testCase =>
                    testCase.Status ==
                    StatusBlocked);

        var remaining =
            visibleCases.Count(
                testCase =>
                    testCase.Status ==
                    StatusNone);

        SetText(
            _successCountTextBlock,
            success.ToString());

        SetText(
            _inProgressCountTextBlock,
            inProgress.ToString());

        SetText(
            _failedCountTextBlock,
            failed.ToString());

        SetText(
            _naCountTextBlock,
            na.ToString());

        SetText(
            _blockedCountTextBlock,
            blocked.ToString());

        SetText(
            _remainingCountTextBlock,
            remaining.ToString());

        if (_remainingLabelTextBlock is not null)
        {
            _remainingLabelTextBlock.Text =
                LocalizationService.T("Explorer.Remaining");
        }
    }

    private static string GetTestTypeShortName(
        string testTypeKey)
    {
        return testTypeKey switch
        {
            RegressionTestTypeKey =>
                LocalizationService.IsPolish ? "regresji" : "regression",

            FunctionalTestTypeKey =>
                LocalizationService.IsPolish ? "funkcjonalne" : "functional",

            _ =>
                "test"
        };
    }

    private void UpdateActiveCollectionHighlight()
    {
        foreach (var collection in _collections)
        {
            var isActive =
                _currentCollectionIndex >= 0 &&
                ReferenceEquals(
                    collection,
                    _collections[
                        _currentCollectionIndex]);

            if (collection.HeaderBorder is not null)
            {
                collection.HeaderBorder.Classes.Remove(
                    "ActiveTreeRow");

                if (isActive)
                {
                    collection.HeaderBorder.Classes.Add(
                        "ActiveTreeRow");
                }
            }

            if (collection.ActiveIndicator is not null)
            {
                collection.ActiveIndicator.Background =
                    isActive
                        ? new SolidColorBrush(
                            Color.Parse(
                                "#28C76F"))
                        : Brushes.Transparent;
            }

            if (collection.TreeItem is not null)
            {
                collection.TreeItem.IsSelected =
                    false;
            }
        }

        if (_testTreeView is not null)
        {
            _testTreeView.SelectedItem =
                null;
        }
    }

    private void PreviousSectionButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var currentCollection =
            GetCurrentCollection();

        if (currentCollection is null)
        {
            return;
        }

        var collections =
            GetCollectionsForTestType(
                currentCollection.TestTypeKey);

        var position =
            collections.IndexOf(
                currentCollection);

        if (position <= 0)
        {
            return;
        }

        SelectCollection(
            collections[position - 1]);
    }

    private void NextSectionButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        MoveToNextSection();
    }

    private void ToggleCompactTestTreePanelButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var targetState =
            _treePanelState switch
            {
                TreePanelState.Full =>
                    TreePanelState.Compact,

                TreePanelState.Compact =>
                    TreePanelState.Full,

                _ =>
                    TreePanelState.Compact
            };

        SetTreePanelState(
            targetState);
    }

    private void CollapseTestTreePanelButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        SetTreePanelState(
            _treePanelState ==
                TreePanelState.Collapsed
                ? TreePanelState.Full
                : TreePanelState.Collapsed);
    }

    private void SetTreePanelState(
        TreePanelState state)
    {
        if (_explorerBodyGrid is null ||
            _explorerBodyGrid.ColumnDefinitions.Count < 3)
        {
            return;
        }

        var leftColumn =
            _explorerBodyGrid.ColumnDefinitions[0];

        if (_treePanelState ==
                TreePanelState.Full &&
            leftColumn.ActualWidth > 280)
        {
            _lastTestTreePanelWidth =
                Math.Clamp(
                    leftColumn.ActualWidth,
                    260,
                    460);
        }

        _treePanelState =
            state;

        if (state ==
            TreePanelState.Collapsed)
        {
            leftColumn.MinWidth =
                0;

            leftColumn.MaxWidth =
                0;

            leftColumn.Width =
                new GridLength(0);

            if (_testTreePanelBorder is not null)
            {
                _testTreePanelBorder.IsVisible =
                    false;
            }

            if (_testTreeGridSplitter is not null)
            {
                _testTreeGridSplitter.IsEnabled =
                    false;
            }
        }
        else
        {
            leftColumn.MinWidth =
                260;

            leftColumn.MaxWidth =
                460;

            leftColumn.Width =
                new GridLength(
                    state ==
                        TreePanelState.Compact
                        ? 260
                        : Math.Clamp(
                            _lastTestTreePanelWidth,
                            260,
                            460));

            if (_testTreePanelBorder is not null)
            {
                _testTreePanelBorder.IsVisible =
                    true;
            }

            if (_testTreeGridSplitter is not null)
            {
                _testTreeGridSplitter.IsEnabled =
                    true;
            }
        }

        UpdateTreePanelTypography();
        UpdateTreePanelButtons();
    }

    private double GetCurrentTreePanelWidth()
    {
        if (_explorerBodyGrid is null ||
            _explorerBodyGrid.ColumnDefinitions.Count == 0)
        {
            return 0;
        }

        return _explorerBodyGrid
            .ColumnDefinitions[0]
            .ActualWidth;
    }

    private void UpdateTreePanelTypography()
    {
        if (_testTreePanelBorder is null ||
            _treePanelState ==
                TreePanelState.Collapsed)
        {
            return;
        }

        UpdateTreeCollectionRowWidths();

        var useCompactTypography =
            false;

        if (_testTreeScrollViewer is not null &&
            Math.Abs(_testTreeScrollViewer.Offset.X) > 0.5)
        {
            _testTreeScrollViewer.Offset =
                new Vector(
                    0,
                    _testTreeScrollViewer.Offset.Y);
        }

        if (useCompactTypography ==
            _isCompactTreeTypography)
        {
            return;
        }

        _isCompactTreeTypography =
            useCompactTypography;

        _testTreePanelBorder.Classes.Remove(
            "CompactTreePanel");

        if (useCompactTypography)
        {
            _testTreePanelBorder.Classes.Add(
                "CompactTreePanel");
        }
    }

    private void UpdateTreeCollectionRowWidths()
    {
        var treeScrollViewer =
            _testTreeScrollViewer;

        if (treeScrollViewer is null)
        {
            return;
        }

        var viewportWidth =
            treeScrollViewer.Bounds.Width;

        if (viewportWidth < 120)
        {
            return;
        }

        foreach (var collection in _collections)
        {
            if (collection.HeaderBorder is null)
            {
                continue;
            }

            var rowPosition =
                collection.HeaderBorder.TranslatePoint(
                    new Point(),
                    treeScrollViewer);

            if (!rowPosition.HasValue)
            {
                continue;
            }

            // Liczymy szerokość od faktycznej pozycji wiersza w viewportcie.
            // Wcięcia TreeView zależą od motywu i DPI, więc szacowanie ich na
            // podstawie głębokości potrafiło nadal wypchnąć licznik poza panel.
            var availableRowWidth =
                Math.Max(
                    90,
                    viewportWidth -
                    rowPosition.Value.X -
                    12);

            if (double.IsNaN(
                    collection.HeaderBorder.Width) ||
                Math.Abs(
                    collection.HeaderBorder.Width -
                    availableRowWidth) > 0.5)
            {
                collection.HeaderBorder.Width =
                    availableRowWidth;
            }
        }
    }

    private bool CollectionMatchesTreeSearch(
        TestCollectionData collection)
    {
        if (string.IsNullOrWhiteSpace(
                _testTreeSearchText))
        {
            return true;
        }

        return collection.Name.Contains(
                   _testTreeSearchText,
                   StringComparison.OrdinalIgnoreCase) ||
               collection.Cases.Any(
                   testCase =>
                       testCase.Name.Contains(
                           _testTreeSearchText,
                           StringComparison.OrdinalIgnoreCase));
    }

    private bool FolderContainsTreeSearchMatches(
        string folderKey)
    {
        if (string.IsNullOrWhiteSpace(
                _testTreeSearchText))
        {
            return true;
        }

        if (_collections.Any(
                collection =>
                    string.Equals(
                        collection.ParentFolderKey,
                        folderKey,
                        StringComparison.OrdinalIgnoreCase) &&
                    CollectionMatchesTreeSearch(
                        collection)))
        {
            return true;
        }

        return _folders
            .Where(
                folder =>
                    string.Equals(
                        folder.ParentKey,
                        folderKey,
                        StringComparison.OrdinalIgnoreCase))
            .Any(
                folder =>
                    FolderContainsTreeSearchMatches(
                        folder.Key));
    }

    private void TestTreeSearchTextBox_OnTextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        if (_activeAssignmentCaseIds is not null)
        {
            return;
        }

        _testTreeSearchText =
            _testTreeSearchTextBox?.Text?.Trim() ??
            string.Empty;

        BuildTestTree();
    }

    private bool FolderContainsActiveAssignmentCases(
        string folderKey)
    {
        if (_activeAssignmentCaseIds is null)
        {
            return true;
        }

        var folderKeys =
            _folders
                .Where(
                    folder =>
                        IsFolderInsideScope(
                            folder.Key,
                            folderKey))
                .Select(
                    folder =>
                        folder.Key)
                .Append(
                    folderKey)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        return _collections
            .Where(
                collection =>
                    folderKeys.Contains(
                        collection.ParentFolderKey))
            .SelectMany(
                collection =>
                    collection.Cases)
            .Any(
                testCase =>
                    _activeAssignmentCaseIds.Contains(
                        testCase.Id));
    }

    private void UpdateTreePanelButtons()
    {
        if (_toggleCompactTestTreePanelButton is not null)
        {
            _toggleCompactTestTreePanelButton.Content =
                _treePanelState ==
                    TreePanelState.Full
                    ? "‹"
                    : "›";

            ToolTip.SetTip(
                _toggleCompactTestTreePanelButton,
                _treePanelState ==
                    TreePanelState.Full
                    ? "Włącz kompaktowy panel"
                    : "Rozwiń panel testów");
        }

        if (_collapseTestTreePanelButton is not null)
        {
            _collapseTestTreePanelButton.Content =
                _treePanelState ==
                    TreePanelState.Collapsed
                    ? "≫"
                    : "≪";

            ToolTip.SetTip(
                _collapseTestTreePanelButton,
                _treePanelState ==
                    TreePanelState.Collapsed
                    ? "Przywróć pełny panel testów"
                    : "Ukryj panel testów");
        }
    }

    private void MoveToNextSection()
    {
        var invalidBlockedRow = _testCasesStackPanel?.Children
            .OfType<TestCaseRow>()
            .FirstOrDefault(row => row.HasPendingBlockedComment);
        if (invalidBlockedRow is not null)
        {
            invalidBlockedRow.FlashBlockedCommentValidation();
            return;
        }

        var currentCollection =
            GetCurrentCollection();

        if (currentCollection is null)
        {
            return;
        }

        var collections =
            GetCollectionsForTestType(
                currentCollection.TestTypeKey);

        var position =
            collections.IndexOf(
                currentCollection);

        if (position >= 0 &&
            position < collections.Count - 1)
        {
            SelectCollection(
                collections[position + 1],
                revealInTree: true);

            Dispatcher.UIThread.Post(
                () =>
                {
                    _testCasesScrollViewer?
                        .ScrollToHome();
                },
                DispatcherPriority.Loaded);

            return;
        }

        if (_activeAssignmentCaseIds is not null)
        {
            var firstUnfinishedCollection =
                collections.FirstOrDefault(
                    collection =>
                        collection.Cases
                            .Where(
                                IsCaseVisibleForActiveAssignment)
                            .Any(
                                testCase =>
                                    !IsFinalStatus(
                                        testCase.Status)));

            if (firstUnfinishedCollection is not null)
            {
                SelectCollection(
                    firstUnfinishedCollection,
                    revealInTree: true);

                Dispatcher.UIThread.Post(
                    () =>
                    {
                        _testCasesScrollViewer?
                            .ScrollToHome();
                    },
                    DispatcherPriority.Loaded);

                return;
            }
        }

        ShowSummaryScreen(
            currentCollection.TestTypeKey);
    }

    public bool TryHandleEnter(
        object? eventSource)
    {
        var comboBox =
            eventSource as ComboBox;

        if (comboBox is null &&
            eventSource is Visual sourceVisual)
        {
            comboBox =
                sourceVisual
                    .GetVisualAncestors()
                    .OfType<ComboBox>()
                    .FirstOrDefault();
        }

        if (comboBox?.IsDropDownOpen ==
            true)
        {
            comboBox.IsDropDownOpen =
                false;
        }

        if (_testExecutionPanel?.IsVisible ==
                true &&
            _nextSectionButton?.IsVisible !=
                false &&
            _nextSectionButton?.IsEnabled !=
                false)
        {
            MoveToNextSection();

            return true;
        }

        if (_summaryPanel?.IsVisible ==
            true)
        {
            return true;
        }

        return false;
    }

    private async void ExplorerView_OnPreviewKeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await RefreshAsync();
            return;
        }

        if (e.Key == Key.Enter &&
            _summaryPanel?.IsVisible ==
            true)
        {
            e.Handled =
                true;

            return;
        }

        if (IsTextEditingSource(
                e.Source))
        {
            return;
        }

        if (e.Key == Key.F2)
        {
            if (_selectedTestCase is not null &&
                _selectedCollection is not null &&
                CanModifyTestCase(_selectedTestCase))
            {
                await RenameUserTestCaseAsync(
                    _selectedCollection,
                    _selectedTestCase);
            }
            else if (_selectedCollection is not null &&
                     CanModifyCollection(_selectedCollection))
            {
                await RenameCollectionAsync(
                    _selectedCollection);
            }
            else if (_selectedFolder is not null &&
                CanModifyFolder(_selectedFolder) &&
                _selectedFolder.Key !=
                ProjectRootKey)
            {
                await RenameFolderAsync(
                    _selectedFolder);
            }

            e.Handled =
                true;

            return;
        }

        if (e.Key == Key.Enter &&
            TryHandleEnter(
                e.Source))
        {
            e.Handled =
                true;

            return;
        }

        if (e.Key == Key.Z &&
            e.KeyModifiers.HasFlag(
                KeyModifiers.Control))
        {
            await UndoLastStructureChangeAsync();

            e.Handled =
                true;

            return;
        }

        if (e.Key == Key.C &&
            e.KeyModifiers.HasFlag(
                KeyModifiers.Control))
        {
            if (_selectedTestCase is not null &&
                _selectedCollection is not null)
            {
                CopyTestCaseToClipboard(
                    _selectedCollection,
                    _selectedTestCase);
            }
            else if (_selectedCollection is not null)
            {
                CopyCollectionToClipboard(
                    _selectedCollection);
            }
            else if (_selectedFolder is not null)
            {
                CopyFolderToClipboard(
                    _selectedFolder);
            }

            e.Handled =
                true;

            return;
        }

        if (e.Key == Key.V &&
            e.KeyModifiers.HasFlag(
                KeyModifiers.Control))
        {
            if (_selectedTestCase is not null &&
                _selectedCollection is not null &&
                _structureClipboard is
                    TestCaseClipboardItem)
            {
                await PasteTestCaseIntoCollectionAsync(
                    _selectedCollection,
                    _selectedTestCase);
            }
            else if (_selectedCollection is not null &&
                     _structureClipboard is
                         CollectionClipboardItem)
            {
                await PasteCollectionNextToSelectionAsync(
                    _selectedCollection);
            }
            else if (_selectedCollection is not null &&
                     _structureClipboard is
                         TestCaseClipboardItem)
            {
                await PasteTestCaseIntoCollectionAsync(
                    _selectedCollection);
            }
            else if (_selectedFolder is not null &&
                     _structureClipboard is
                         FolderClipboardItem &&
                     !string.Equals(
                         _selectedFolder.Key,
                         ProjectRootKey,
                         StringComparison.OrdinalIgnoreCase))
            {
                await PasteFolderNextToSelectionAsync(
                    _selectedFolder);
            }
            else if (_selectedFolder is not null)
            {
                await PasteIntoFolderAsync(
                    _selectedFolder);
            }

            e.Handled =
                true;

            return;
        }

        if (e.Key is not Key.Delete and
            not Key.Back)
        {
            return;
        }

        if (_selectedTestCase is not null &&
            _selectedCollection is not null &&
            CanModifyTestCase(_selectedTestCase))
        {
            var confirmed =
                await ShowDeleteConfirmationAsync(
                    "Usuń przypadek",
                    $"Czy na pewno chcesz usunąć przypadek „{_selectedTestCase.Name}”?");

            if (confirmed)
            {
                var collection =
                    _selectedCollection;

                var testCase =
                    _selectedTestCase;

                _selectedTestCase =
                    null;

                await DeleteUserTestCaseAsync(
                    collection,
                    testCase);
            }
        }
        else if (_selectedCollection is not null &&
                 CanModifyCollection(_selectedCollection))
        {
            var collection =
                _selectedCollection;

            _selectedCollection =
                null;

            await DeleteCollectionAsync(
                collection);
        }
        else if (_selectedFolder is not null &&
                 CanModifyFolder(_selectedFolder) &&
                 _selectedFolder.Key !=
                 ProjectRootKey)
        {
            var folder =
                _selectedFolder;

            _selectedFolder =
                null;

            await DeleteFolderAsync(
                folder);
        }

        e.Handled =
            true;
    }

    private static bool IsTextEditingSource(
        object? source)
    {
        if (source is TextBox)
        {
            return true;
        }

        return source is Visual visual &&
               visual
                   .GetVisualAncestors()
                   .OfType<TextBox>()
                   .Any();
    }

    private void UpdateNavigationButtons()
    {
        var currentCollection =
            GetCurrentCollection();

        if (currentCollection is null)
        {
            return;
        }

        var collections =
            GetCollectionsForTestType(
                currentCollection.TestTypeKey);

        var position =
            collections.IndexOf(
                currentCollection);

        if (_previousSectionButton is not null)
        {
            _previousSectionButton.IsEnabled =
                position > 0;
        }

        if (_nextSectionButton is null)
        {
            return;
        }

        var isLastCollection =
            position ==
            collections.Count - 1;

        var isWholeTestTypeComplete =
            collections
                .SelectMany(
                    collection =>
                        collection.Cases)
                .Where(
                    IsCaseVisibleForActiveAssignment)
                .All(
                    testCase =>
                        IsFinalStatus(
                            testCase.Status));

        _nextSectionButton.Content =
            isLastCollection &&
            isWholeTestTypeComplete
                ? LocalizationService.T("Explorer.FinishAndContinue")
                : LocalizationService.T("Explorer.Next");

        _nextSectionButton.MinWidth =
            isLastCollection &&
            isWholeTestTypeComplete
                ? 270
                : 120;

        _nextSectionButton.IsEnabled =
            collections.Count > 0 &&
            !(_testCasesStackPanel?.Children
                .OfType<TestCaseRow>()
                .Any(row => row.HasPendingBlockedComment) ?? false);
    }

    private void ShowSummaryScreen(
        string completedTestTypeKey)
    {
        var firstUnfinishedCollection =
            GetCollectionsForTestType(
                    completedTestTypeKey)
                .FirstOrDefault(
                    collection =>
                        collection.Cases
                            .Where(
                                IsCaseVisibleForActiveAssignment)
                            .Any(
                                testCase =>
                                    !IsFinalStatus(
                                        testCase.Status)));

        if (firstUnfinishedCollection is not null)
        {
            SelectCollection(
                firstUnfinishedCollection,
                revealInTree: true);

            Dispatcher.UIThread.Post(
                () =>
                {
                    _testCasesScrollViewer?
                        .ScrollToHome();
                },
                DispatcherPriority.Loaded);

            return;
        }

        ClearFolderWorkspace();

        _lastCompletedTestTypeKey =
            completedTestTypeKey;

        if (_welcomePanel is not null)
        {
            _welcomePanel.IsVisible =
                false;
        }

        if (_testExecutionPanel is not null)
        {
            _testExecutionPanel.IsVisible =
                false;
        }

        if (_summaryPanel is not null)
        {
            _summaryPanel.IsVisible =
                true;
        }

        var collections =
            GetCollectionsForTestType(
                completedTestTypeKey);

        var cases =
            collections
                .SelectMany(
                    collection =>
                        collection.Cases)
                .Where(
                    IsCaseVisibleForActiveAssignment)
                .ToList();

        SetText(
            _summaryCompletedTitleTextBlock,
            LocalizationService.Format(
                "Explorer.TestTypeCompleted",
                GetTestTypeDisplayName(
                    completedTestTypeKey)));

        SetText(
            _summarySuccessCountTextBlock,
            cases.Count(
                    testCase =>
                        testCase.Status ==
                        StatusSuccess)
                .ToString());

        SetText(
            _summaryInProgressCountTextBlock,
            cases.Count(
                    testCase =>
                        testCase.Status ==
                        StatusInProgress)
                .ToString());

        SetText(
            _summaryFailedCountTextBlock,
            cases.Count(
                    testCase =>
                        testCase.Status ==
                        StatusFailed)
                .ToString());

        SetText(
            _summaryNaCountTextBlock,
            cases.Count(
                    testCase =>
                        testCase.Status ==
                        StatusNa)
                .ToString());

        SetText(
            _summaryBlockedCountTextBlock,
            cases.Count(
                    testCase =>
                        testCase.Status ==
                        StatusBlocked)
                .ToString());

        SetText(
            _summaryRemainingCountTextBlock,
            cases.Count(
                    testCase =>
                        testCase.Status ==
                        StatusNone)
                .ToString());

        var nextTypeKey =
            GetNextTestTypeKey(
                completedTestTypeKey);

        var hasNextType =
            nextTypeKey is not null;

        if (_summaryNextTypePanel is not null)
        {
            _summaryNextTypePanel.IsVisible =
                hasNextType;
        }

        if (_summaryAllDoneTextBlock is not null)
        {
            _summaryAllDoneTextBlock.IsVisible =
                !hasNextType;
        }

        if (nextTypeKey is not null)
        {
            SetText(
                _summaryNextTypeNameTextBlock,
                GetTestTypeDisplayName(
                    nextTypeKey));

            var nextTypeCaseCount =
                GetCollectionsForTestType(
                    nextTypeKey)
                    .SelectMany(
                        collection =>
                            collection.Cases)
                    .Where(
                        IsCaseVisibleForActiveAssignment)
                    .Count();

            SetText(
                _summaryNextTypeCaseCountTextBlock,
                CreateCaseCountText(
                    nextTypeCaseCount));
        }

        if (_summaryContinueButton is not null)
        {
            var canFinishAssignment =
                !hasNextType &&
                _activeAssignmentId.HasValue &&
                _collections
                    .SelectMany(
                        collection =>
                            collection.Cases)
                    .Where(
                        IsCaseVisibleForActiveAssignment)
                    .All(
                        testCase =>
                            IsFinalStatus(
                                testCase.Status));

            _summaryContinueButton.IsVisible =
                hasNextType || canFinishAssignment;

            _summaryContinueButton.Content =
                hasNextType
                    ? LocalizationService.T("Explorer.FinishAndContinue")
                    : LocalizationService.T("Explorer.Finish");

            _summaryContinueButton.MinWidth =
                hasNextType
                    ? 270
                    : 150;

            if (_summaryBackButton is not null)
            {
                _summaryBackButton.IsVisible =
                    !canFinishAssignment;
            }
        }

        UpdateActiveCollectionHighlight();
    }

    private static string CreateCaseCountText(
        int count)
    {
        if (!LocalizationService.IsPolish)
        {
            return count == 1
                ? "1 test case to execute"
                : $"{count} test cases to execute";
        }

        if (count == 1)
        {
            return "1 przypadek do wykonania";
        }

        if (count % 10 is >= 2 and <= 4 &&
            count % 100 is < 12 or > 14)
        {
            return $"{count} przypadki do wykonania";
        }

        return $"{count} przypadków do wykonania";
    }

    private void SummaryBackButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(
                _lastCompletedTestTypeKey))
        {
            ShowWelcomeScreen();

            return;
        }

        var collections =
            GetCollectionsForTestType(
                _lastCompletedTestTypeKey);

        if (collections.Count == 0)
        {
            ShowWelcomeScreen();

            return;
        }

        SelectCollection(
            collections[^1]);
    }

    private async void SummaryContinueButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(
                _lastCompletedTestTypeKey))
        {
            return;
        }

        var nextTypeKey =
            GetNextTestTypeKey(
                _lastCompletedTestTypeKey);

        if (nextTypeKey is null)
        {
            await FinishAssignedTestsAsync();

            return;
        }

        var collections =
            GetCollectionsForTestType(
                nextTypeKey);

        if (collections.Count > 0)
        {
            SelectCollection(
                collections[0]);

            return;
        }

        var folder =
            _folders.FirstOrDefault(
                item =>
                    item.TestTypeKey ==
                        nextTypeKey &&
                    item.ParentKey ==
                        ProjectRootKey);

        if (folder is not null)
        {
            ShowFolderScreen(
                folder,
                _lastCompletedTestTypeKey);

            ExpandPathToFolder(
                folder.Key);
        }
    }

    private async Task PersistAssignmentCaseStatusAsync(
        Guid assignmentId,
        Guid testCaseId,
        string status,
        string comment)
    {
        var write =
            _assignmentService.UpdateAssignmentCaseStatusAsync(
                assignmentId,
                testCaseId,
                status,
                comment);

        _pendingAssignmentStatusWrites.Add(write);

        try
        {
            await write;
        }
        finally
        {
            _pendingAssignmentStatusWrites.Remove(write);
        }
    }

    private async Task AwaitPendingAssignmentStatusWritesAsync()
    {
        var pending =
            _pendingAssignmentStatusWrites.ToArray();

        if (pending.Length > 0)
        {
            await Task.WhenAll(pending);
        }
    }

    private async void DownloadReportButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        await GenerateReportAsync();
    }

    private async Task FinishAssignedTestsAsync(
        bool allowUnfinished = false,
        bool skipCompletionDialog = false)
    {
        if (!_activeAssignmentId.HasValue ||
            _isFinishingAssignedTests)
        {
            return;
        }

        _isFinishingAssignedTests = true;
        _assignmentValidityTimer.Stop();

        try
        {
            await FinishAssignedTestsCoreAsync(
                allowUnfinished,
                skipCompletionDialog);
        }
        finally
        {
            _isFinishingAssignedTests = false;

            if (_activeAssignmentCaseIds is not null)
            {
                _assignmentValidityTimer.Start();
            }
        }
    }

    private async Task FinishAssignedTestsCoreAsync(
        bool allowUnfinished,
        bool skipCompletionDialog)
    {
        if (!_activeAssignmentId.HasValue)
        {
            return;
        }

        var ownerWindow =
            TopLevel.GetTopLevel(this)
            as Window;

        if (ownerWindow is null)
        {
            return;
        }

        await AwaitPendingAssignmentStatusWritesAsync();

        if (!await ReconcileActiveAssignmentsAsync(
                ownerWindow,
                refreshAssignedScreen: false))
        {
            return;
        }

        var completionChoice =
            AssignmentCompletionChoice.FinishWithoutReport;

        if (!skipCompletionDialog)
        {
            var dialog =
                new AssignmentCompletionConfirmationWindow();

            completionChoice =
                await dialog.ShowDialog<AssignmentCompletionChoice>(
                    ownerWindow);

            if (completionChoice ==
                AssignmentCompletionChoice.Cancel)
            {
                return;
            }

        }

        if (completionChoice ==
            AssignmentCompletionChoice.FinishWithReport)
        {
            var reportGenerated =
                await GenerateReportAsync();

            if (!reportGenerated)
            {
                return;
            }
        }

        var completedAssignmentIds =
            _activeAssignments
                .Where(
                    assignment =>
                        assignment.TestCaseIds.Any(
                            testCaseId =>
                                _activeAssignmentCaseIds?.Contains(
                                    testCaseId) == true))
                .Select(
                    assignment =>
                        assignment.Id)
                .Distinct()
                .ToArray();

        var allAssignmentsCompleted = true;
        foreach (var assignmentId in completedAssignmentIds)
        {
            allAssignmentsCompleted &=
                await _assignmentService.CompleteAssignmentAsync(
                    assignmentId,
                    allowUnfinished);
        }

        if (!allAssignmentsCompleted)
        {
            await ReconcileActiveAssignmentsAsync(
                ownerWindow,
                refreshAssignedScreen: true);

            await new OperationResultWindow(
                    false,
                    LocalizationService.T("Assignment.CannotFinishTitle"),
                    LocalizationService.T("Assignment.CannotFinishDescription"))
                .ShowDialog(ownerWindow);
            return;
        }

        if (!allowUnfinished)
        {
            await PlayCompletionCelebrationAsync();
        }

        if (allowUnfinished ||
            _completionCelebrationOverlay is null)
        {
            await new OperationResultWindow(
                true,
                LocalizationService.T("Assignment.CompletedTitle"),
                allowUnfinished
                    ? LocalizationService.T("Assignment.CompletedWithUnfinished")
                    : LocalizationService.T("Assignment.CompletedDescription"))
                .ShowDialog(ownerWindow);
        }

        _activeAssignmentId =
            null;

        _activeAssignmentCaseIds =
            null;

        _activeAssignmentIdByCaseId.Clear();

        await RefreshAssignmentAndNotificationStateAsync();

        if (_activeAssignments.Length > 0)
        {
            await ExecuteLatestAssignmentAsync();
        }
        else
        {
            await RestoreAdHocStateAsync();

            _applicationVersion =
                string.Empty;

            if (_sessionManager is not null &&
                _sessionState is not null)
            {
                await _sessionManager.StartNewSessionAsync(
                    _sessionState,
                    _projectName,
                    string.Empty,
                    _loggedInLogin,
                    "AdHoc");
            }

            if (_testTreeTitleTextBlock is not null)
            {
                _testTreeTitleTextBlock.Text =
                    LocalizationService.T("Explorer.TestsAndCases");
            }

            if (_testTreeSearchTextBox is not null)
            {
                _testTreeSearchTextBox.IsVisible =
                    true;
            }

            if (_projectInfoTextBlock is not null)
            {
                _projectInfoTextBlock.Text =
                    $"{_projectName} • ad-hoc";
            }

            RestoreAdHocLocation();
        }
    }

    private async Task PlayCompletionCelebrationAsync()
    {
        if (_completionCelebrationOverlay is null)
        {
            return;
        }

        _completionCelebrationOverlay.IsVisible =
            true;

        for (var step = 1; step <= 8; step++)
        {
            _completionCelebrationOverlay.Opacity =
                step / 8d;

            await Task.Delay(35);
        }

        await Task.Delay(500);

        for (var step = 7; step >= 0; step--)
        {
            _completionCelebrationOverlay.Opacity =
                step / 8d;

            await Task.Delay(28);
        }

        _completionCelebrationOverlay.IsVisible =
            false;
    }

    private void CaptureAdHocStateBeforeAssignedMode()
    {
        if (_adHocStatusSnapshot is not null)
        {
            return;
        }

        _adHocStatusSnapshot =
            _collections
                .SelectMany(
                    collection =>
                        collection.Cases)
                .ToDictionary(
                    testCase =>
                        testCase.Id,
                    testCase =>
                        testCase.Status);

        _adHocCollectionKeyBeforeAssignedMode =
            GetCurrentCollection()
                ?.Key;

        _adHocWasWelcomeBeforeAssignedMode =
            _welcomePanel?.IsVisible == true;
    }

    private async Task RestoreAdHocStateAsync()
    {
        if (_adHocStatusSnapshot is null)
        {
            return;
        }

        foreach (var collection in _collections)
        {
            foreach (var testCase in collection.Cases)
            {
                testCase.Status =
                    _adHocStatusSnapshot.TryGetValue(
                        testCase.Id,
                        out var status)
                        ? status
                        : StatusNone;
            }

            UpdateCollectionState(
                collection);
        }

        await _userTestCaseService.SaveStatusesAsync(
            _adHocStatusSnapshot);

        UpdateSessionSummary();
    }

    private void RestoreAdHocLocation()
    {
        var previousCollection =
            _adHocWasWelcomeBeforeAssignedMode ||
            string.IsNullOrWhiteSpace(
                _adHocCollectionKeyBeforeAssignedMode)
                ? null
                : _collections.FirstOrDefault(
                    collection =>
                        string.Equals(
                            collection.Key,
                            _adHocCollectionKeyBeforeAssignedMode,
                            StringComparison.OrdinalIgnoreCase));

        _adHocStatusSnapshot =
            null;

        _adHocCollectionKeyBeforeAssignedMode =
            null;

        _adHocWasWelcomeBeforeAssignedMode =
            false;

        BuildTestTree();

        if (previousCollection is not null)
        {
            SelectCollection(
                previousCollection,
                revealInTree: true);
        }
        else
        {
            ShowWelcomeScreen();
        }
    }

    private async Task ExitAssignedModeToAdHocAsync()
    {
        _assignmentValidityTimer.Stop();

        await _assignmentService.MarkAssignmentsPausedAsync(
            _activeAssignments.Select(
                assignment =>
                    assignment.Id));

        _activeAssignmentId =
            null;

        _activeAssignmentCaseIds =
            null;

        _activeAssignmentIdByCaseId.Clear();

        await RestoreAdHocStateAsync();

        _applicationVersion =
            string.Empty;

        if (_sessionManager is not null &&
            _sessionState is not null)
        {
            await _sessionManager.StartNewSessionAsync(
                _sessionState,
                _projectName,
                string.Empty,
                _loggedInLogin,
                "AdHoc");
        }

        if (_testTreeTitleTextBlock is not null)
        {
            _testTreeTitleTextBlock.Text =
                LocalizationService.T("Explorer.TestsAndCases");
        }

        if (_testTreeSearchTextBox is not null)
        {
            _testTreeSearchTextBox.IsVisible =
                true;
        }

        if (_projectInfoTextBlock is not null)
        {
            _projectInfoTextBlock.Text =
                $"{_projectName} • ad-hoc";
        }

        if (_finishEarlyButton is not null)
        {
            _finishEarlyButton.Content =
                LocalizationService.T("Explorer.FinishAndReport");

            ToolTip.SetTip(
                _finishEarlyButton,
                LocalizationService.T("Explorer.FinishAndReportTip"));
        }

        RestoreAdHocLocation();
        await RefreshAssignmentAndNotificationStateAsync();
    }

    private async Task CheckActiveAssignmentValidityAsync()
    {
        if (_isFinishingAssignedTests ||
            _checkingAssignmentValidity ||
            _activeAssignmentCaseIds is null ||
            _activeAssignments.Length == 0)
        {
            return;
        }

        _checkingAssignmentValidity =
            true;

        try
        {
            var currentOwnerWindow =
                TopLevel.GetTopLevel(this)
                as Window;

            if (currentOwnerWindow is not null)
            {
                await ReconcileActiveAssignmentsAsync(
                    currentOwnerWindow,
                    refreshAssignedScreen: true);
            }

            return;
        }

#if false
            var activeAssignmentIds =
                (await _assignmentService.GetActiveAssignmentsForUserAsync(
                    _loggedInLogin))
                .Where(
                    assignment =>
                        string.Equals(
                            assignment.ProjectKey,
                            _projectKey,
                            StringComparison.OrdinalIgnoreCase))
                .Select(
                    assignment =>
                        assignment.Id)
                .ToHashSet();

            var currentAssignmentWasWithdrawn =
                _activeAssignments.Any(
                    assignment =>
                        !activeAssignmentIds.Contains(
                            assignment.Id));

            if (!currentAssignmentWasWithdrawn)
            {
                return;
            }

            var ownerWindow =
                TopLevel.GetTopLevel(this)
                as Window;

            await ExitAssignedModeToAdHocAsync();

            if (ownerWindow is not null)
            {
                await new OperationResultWindow(
                        false,
                        LocalizationService.T("Assignment.PausedTitle"),
                        LocalizationService.T("Assignment.WithdrawnDescription"))
                    .ShowDialog(ownerWindow);
            }
        }
#endif
        finally
        {
            _checkingAssignmentValidity =
                false;
        }
    }

    private async Task<bool> ReconcileActiveAssignmentsAsync(
        Window ownerWindow,
        bool refreshAssignedScreen)
    {
        var previousAssignmentIds =
            _activeAssignments
                .Select(assignment => assignment.Id)
                .ToHashSet();

        var visibleCaseIds =
            _collections
                .SelectMany(collection => collection.Cases)
                .Select(testCase => testCase.Id)
                .ToHashSet();

        var activeAssignments =
            (await _assignmentService.GetActiveAssignmentsForUserAsync(
                _loggedInLogin))
            .Where(assignment =>
                string.Equals(
                    assignment.ProjectKey,
                    _projectKey,
                    StringComparison.OrdinalIgnoreCase))
            .Select(assignment =>
            {
                assignment.TestCaseIds =
                    assignment.TestCaseIds
                        .Where(visibleCaseIds.Contains)
                        .Distinct()
                        .ToList();
                return assignment;
            })
            .Where(assignment => assignment.TestCaseIds.Count > 0)
            .ToArray();

        var currentAssignments =
            previousAssignmentIds.Count == 0
                ? activeAssignments
                : activeAssignments
                    .Where(assignment =>
                        previousAssignmentIds.Contains(assignment.Id))
                    .ToArray();

        if (currentAssignments.Length == 0)
        {
            // Ukończone przypisanie znika z kolejki aktywnej testera.
            // W trakcie finalizacji jest to oczekiwane i nie oznacza
            // wstrzymania ani wycofania sesji przez managera.
            if (_isFinishingAssignedTests)
            {
                return true;
            }

            var unavailableIds =
                previousAssignmentIds.Count > 0
                    ? previousAssignmentIds
                    : _sessionState?.AssignmentIds.ToHashSet() ?? [];

            await ExitAssignedModeToAdHocAsync();
            await ShowUnavailableAssignmentsAsync(
                ownerWindow,
                unavailableIds);
            return false;
        }

        var changed =
            currentAssignments.Length != _activeAssignments.Length ||
            currentAssignments.Any(assignment =>
                !_activeAssignments.Any(previous =>
                    previous.Id == assignment.Id &&
                    previous.TestCaseIds.Count == assignment.TestCaseIds.Count));

        _activeAssignments = currentAssignments;
        _activeAssignmentId = currentAssignments[0].Id;
        _activeAssignmentCaseIds =
            currentAssignments
                .SelectMany(assignment => assignment.TestCaseIds)
                .ToHashSet();

        if (_sessionManager is not null &&
            _sessionState is not null)
        {
            await _sessionManager.UpdateAssignmentContextAsync(
                _sessionState,
                currentAssignments.Select(assignment => assignment.Id));
        }

        if (changed && refreshAssignedScreen)
        {
            await ExecuteLatestAssignmentAsync();
        }

        return true;
    }

    private async Task ShowUnavailableAssignmentsAsync(
        Window ownerWindow,
        IEnumerable<Guid> assignmentIds)
    {
        var unavailable =
            await _assignmentService.GetAssignmentsByIdsAsync(
                assignmentIds);

        var managerLogin =
            unavailable
                .Select(assignment =>
                    string.IsNullOrWhiteSpace(assignment.WithdrawnByLogin)
                        ? assignment.AssignedByLogin
                        : assignment.WithdrawnByLogin)
                .FirstOrDefault(login => !string.IsNullOrWhiteSpace(login));

        var managerText =
            string.IsNullOrWhiteSpace(managerLogin)
                ? LocalizationService.T("Assignment.ContactManager")
                : LocalizationService.Format("Assignment.ContactManagerLogin", managerLogin);

        await new OperationResultWindow(
                false,
                LocalizationService.T("Assignment.PausedTitle"),
                LocalizationService.Format("Assignment.UnavailableDescription", managerText))
            .ShowDialog(ownerWindow);
    }

    private async void FinishEarlyButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_activeAssignmentCaseIds is not null)
        {
            var assignedUnfinishedCount =
                _collections
                    .SelectMany(
                        collection =>
                            collection.Cases)
                    .Where(
                        IsCaseVisibleForActiveAssignment)
                    .Count(
                        testCase =>
                            !IsFinalStatus(
                                testCase.Status));

            if (assignedUnfinishedCount > 0)
            {
                var unfinishedChoice =
                    await ShowUnfinishedAssignmentConfirmationAsync(
                        LocalizationService.T("Assignment.UnfinishedTitle"),
                        LocalizationService.Format("Assignment.UnfinishedDescription", assignedUnfinishedCount),
                        LocalizationService.T("Assignment.SubmitAnyway"),
                        LocalizationService.T("Assignment.PauseAndReturnLater"));

                if (unfinishedChoice ==
                    OperationConfirmationChoice.Alternate)
                {
                    await ExitAssignedModeToAdHocAsync();
                    return;
                }

                if (unfinishedChoice !=
                    OperationConfirmationChoice.Confirm)
                {
                    return;
                }

                await FinishAssignedTestsAsync(
                    allowUnfinished: true,
                    skipCompletionDialog: true);

                return;
            }

            await FinishAssignedTestsAsync();
            return;
        }

        var currentTestTypeKey =
            GetCurrentCollection()
                ?.TestTypeKey;

        var unfinishedCount =
            string.IsNullOrWhiteSpace(
                currentTestTypeKey)
                ? _collections
                    .SelectMany(
                        collection =>
                            collection.Cases)
                    .Where(
                        IsCaseVisibleForActiveAssignment)
                    .Count(
                        testCase =>
                            !IsFinalStatus(
                                testCase.Status))
                : _collections
                    .Where(
                        collection =>
                            string.Equals(
                                collection.TestTypeKey,
                                currentTestTypeKey,
                                StringComparison.OrdinalIgnoreCase))
                    .SelectMany(
                        collection =>
                            collection.Cases)
                    .Where(
                        IsCaseVisibleForActiveAssignment)
                    .Count(
                        testCase =>
                            !IsFinalStatus(
                                testCase.Status));

        if (unfinishedCount > 0)
        {
            var accepted =
                await ShowOperationConfirmationAsync(
                    LocalizationService.T("Report.FinishEarlyTitle"),
                    LocalizationService.Format("Report.FinishEarlyDescription", unfinishedCount),
                    LocalizationService.T("Report.Create"));

            if (!accepted)
            {
                return;
            }
        }

        await GenerateReportAsync();
    }

    private async Task<bool> GenerateReportAsync()
    {
        var ownerWindow =
            TopLevel.GetTopLevel(this)
            as Window;

        if (ownerWindow is null)
        {
            return false;
        }

        var sessionMode =
            string.Equals(
                _sessionState?.SessionMode,
                "Assigned",
                StringComparison.OrdinalIgnoreCase)
                ? "Assigned"
                : "AdHoc";

        var reportDialog =
            new ReportVersionWindow(
                _applicationVersion,
                CreateReportFileNameBase(
                    _applicationVersion));

        var exportRequest =
            await reportDialog.ShowDialog<ReportExportRequest?>(
                ownerWindow);

        if (exportRequest is null)
        {
            return false;
        }

        var savedReportPath =
            await ExportTestReportAsync(
                exportRequest.DirectoryPath,
                exportRequest.ApplicationVersion,
                sessionMode,
                exportRequest.Format,
                exportRequest.FileNameBase,
                exportRequest.IncludeUnfinished);

        if (string.IsNullOrWhiteSpace(
                savedReportPath))
        {
            return false;
        }

        _applicationVersion =
            exportRequest.ApplicationVersion;

        if (_downloadReportButton is not null)
        {
            _downloadReportButton.Content =
                LocalizationService.T("Report.Saved");

            ToolTip.SetTip(
                _downloadReportButton,
                LocalizationService.Format("Report.SavedLocationTip", savedReportPath));
        }

        if (_sessionManager is not null &&
            _sessionState is not null)
        {
            await _sessionManager.UpdateApplicationVersionAsync(
                _sessionState,
                exportRequest.ApplicationVersion);

            await _sessionManager.MarkReportGeneratedAsync(
                _sessionState);
        }

        if (string.Equals(
                sessionMode,
                "AdHoc",
                StringComparison.OrdinalIgnoreCase))
        {
            var clearStatusesDialog =
                new ConfirmDeleteWindow(
                    LocalizationService.T("Report.GeneratedTitle"),
                    LocalizationService.Format("Report.ClearQuestion", savedReportPath),
                    LocalizationService.T("Report.ClearStatuses"));

            var clearStatuses =
                await clearStatusesDialog.ShowDialog<bool>(
                    ownerWindow);

            if (clearStatuses)
            {
                var busyWindow =
                    new BusyOperationWindow(
                        LocalizationService.T("Report.ClearingTitle"),
                        LocalizationService.T("Report.ClearingDescription"),
                        () =>
                            ApplyStatusResetAsync(
                                new ResetStatusesRequest
                                {
                                    ScopeFolderKey = null,
                                    NewStatus = StatusNone,
                                    OnlyPendingAndNa = false
                                }));

                await busyWindow.ShowDialog(
                    ownerWindow);

                if (busyWindow.OperationException is not null)
                {
                    var errorWindow =
                        new OperationResultWindow(
                            false,
                            LocalizationService.T("Report.ClearFailedTitle"),
                            LocalizationService.T("Report.ClearFailedDescription"));

                    await errorWindow.ShowDialog(
                        ownerWindow);

                    return true;
                }

                if (_sessionManager is not null &&
                    _sessionState is not null)
                {
                    await _sessionManager.MarkReportGeneratedAsync(
                        _sessionState);
                }

                var resultWindow =
                    new OperationResultWindow(
                        true,
                        LocalizationService.T("Report.ClearedTitle"),
                        LocalizationService.T("Report.ClearedDescription"));

                await resultWindow.ShowDialog(
                    ownerWindow);
            }
        }

        return true;
    }

    private async Task<bool> ShowOperationConfirmationAsync(
        string title,
        string message,
        string confirmButtonText)
    {
        var ownerWindow =
            TopLevel.GetTopLevel(
                this)
            as Window;

        if (ownerWindow is null)
        {
            return false;
        }

        var dialog =
            new ConfirmDeleteWindow(
                title,
                message,
                confirmButtonText);

        return await dialog.ShowDialog<bool>(
            ownerWindow);
    }

    private async Task<OperationConfirmationChoice> ShowUnfinishedAssignmentConfirmationAsync(
        string title,
        string message,
        string confirmButtonText,
        string alternateButtonText)
    {
        var ownerWindow =
            TopLevel.GetTopLevel(
                this)
            as Window;

        if (ownerWindow is null)
        {
            return OperationConfirmationChoice.Cancel;
        }

        var dialog =
            new ConfirmDeleteWindow(
                title,
                message,
                confirmButtonText,
                alternateButtonText);

        return await dialog.ShowDialog<OperationConfirmationChoice>(
            ownerWindow);
    }

    private async Task<string?> ExportTestReportAsync(
        string directoryPath,
        string applicationVersion,
        string sessionMode,
        TestReportFormat format,
        string fileNameBase,
        bool includeUnfinished)
    {
        var report =
            CreateTestReport(
                applicationVersion,
                sessionMode,
                includeUnfinished);

        return await _testReportExportService.ExportAsync(
            report,
            directoryPath,
            fileNameBase,
            format);
    }

    private TestReport CreateTestReport(
        string applicationVersion,
        string sessionMode,
        bool includeUnfinished)
    {
        var reportCases =
            _collections
                .SelectMany(
                    collection =>
                        collection.Cases
                            .Where(
                                IsCaseVisibleForActiveAssignment)
                            .Select(
                            testCase =>
                                new TestReportCase
                                {
                                    TestType =
                                        collection.TestTypeKey,

                                    Collection =
                                        collection.Name,

                                    Path =
                                        collection.Path,

                                    Name =
                                        testCase.Name,

                                    Status =
                                        testCase.Status,

                                    Comment =
                                        testCase.Comment
                                }))
                .Where(
                    testCase =>
                        includeUnfinished ||
                        IsFinalStatus(
                            testCase.Status))
                .ToList();

        var success =
            reportCases.Count(
                item =>
                    item.Status == StatusSuccess);

        var failed =
            reportCases.Count(
                item =>
                    item.Status == StatusFailed);

        var blocked =
            reportCases.Count(
                item =>
                    item.Status == StatusBlocked);

        var notApplicable =
            reportCases.Count(
                item =>
                    item.Status == StatusNa);

        var inProgress =
            reportCases.Count(
                item =>
                    item.Status == StatusInProgress);

        var notStarted =
            reportCases.Count(
                item =>
                    item.Status == StatusNone);

        var completed =
            success +
            failed +
            blocked +
            notApplicable;

        var completionPercent =
            reportCases.Count == 0
                ? 0
                : Math.Round(
                    completed * 100.0 /
                    reportCases.Count,
                    2);

        return new TestReport
        {
            Metadata =
                new TestReportMetadata
                {
                    SessionId =
                        _sessionState?.SessionId
                        ?? Guid.Empty,

                    SessionMode =
                        sessionMode,

                    ProjectName =
                        _projectName,

                    ApplicationVersion =
                        applicationVersion,

                    TesterLogin =
                        _loggedInLogin,

                    GeneratedAt =
                        DateTimeOffset.Now
                },

            Summary =
                new TestReportSummary
                {
                    Total =
                        reportCases.Count,

                    Success =
                        success,

                    Failed =
                        failed,

                    Blocked =
                        blocked,

                    NotApplicable =
                        notApplicable,

                    InProgress =
                        inProgress,

                    NotStarted =
                        notStarted,

                    CompletionPercent =
                        completionPercent
                },

            TestCases =
                reportCases
        };
    }

    private void ShowWelcomeScreen()
    {
        ClearFolderWorkspace();

        if (_testExecutionPanel is not null)
        {
            _testExecutionPanel.IsVisible =
                false;
        }

        if (_summaryPanel is not null)
        {
            _summaryPanel.IsVisible =
                false;
        }

        if (_welcomePanel is not null)
        {
            _welcomePanel.IsVisible =
                true;
        }

        if (_welcomeTitleTextBlock is not null)
        {
            _welcomeTitleTextBlock.Text =
                LocalizationService.T("Explorer.SelectTestType");
        }

        if (_welcomeDescriptionTextBlock is not null)
        {
            _welcomeDescriptionTextBlock.Text =
                LocalizationService.T("Explorer.SelectCollectionDescription");
        }
    }

    private void ShowFolderScreen(
        FolderData folder,
        string? returnToSummaryTestTypeKey = null)
    {
        _currentCollectionIndex =
            -1;

        _emptyFolderReturnTestTypeKey =
            returnToSummaryTestTypeKey;

        if (_emptyFolderBackButton is not null)
        {
            _emptyFolderBackButton.IsVisible =
                !string.IsNullOrWhiteSpace(
                    returnToSummaryTestTypeKey);
        }

        if (_contentAreaGrid is not null)
        {
            _contentAreaGrid.ContextMenu =
                CreateFolderWorkspaceContextMenu(
                    folder);
        }

        if (_testExecutionPanel is not null)
        {
            _testExecutionPanel.IsVisible =
                false;
        }

        if (_summaryPanel is not null)
        {
            _summaryPanel.IsVisible =
                false;
        }

        if (_welcomePanel is not null)
        {
            _welcomePanel.IsVisible =
                true;
        }

        if (_welcomeTitleTextBlock is not null)
        {
            _welcomeTitleTextBlock.Text =
                folder.Name;
        }

        var hasChildren =
            _folders.Any(
                item =>
                    item.ParentKey ==
                    folder.Key) ||
            _collections.Any(
                item =>
                    item.ParentFolderKey ==
                    folder.Key);

        if (_welcomeDescriptionTextBlock is not null)
        {
            _welcomeDescriptionTextBlock.Text =
                hasChildren
                    ? LocalizationService.T("Structure.SelectChild")
                    : LocalizationService.T("Structure.EmptyFolder");
        }

        UpdateActiveCollectionHighlight();
    }

    private ContextMenu CreateFolderWorkspaceContextMenu(
        FolderData folder)
    {
        var addFolderItem =
            new MenuItem
            {
                Header =
                    LocalizationService.T("Structure.AddFolderHere")
            };

        addFolderItem.Click +=
            async (_, _) =>
            {
                await AddFolderAsync(
                    folder);
            };

        var addCollectionItem =
            new MenuItem
            {
                Header =
                    LocalizationService.T("Structure.AddCollectionHere")
            };

        addCollectionItem.Click +=
            async (_, _) =>
            {
                await AddCollectionAsync(
                    folder);
            };

        return new ContextMenu
        {
            ItemsSource =
                new[]
                {
                    addFolderItem,
                    addCollectionItem
                }
        };
    }

    private void EmptyFolderBackButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(
                _emptyFolderReturnTestTypeKey))
        {
            ShowWelcomeScreen();

            return;
        }

        ShowSummaryScreen(
            _emptyFolderReturnTestTypeKey);
    }

    private void ClearFolderWorkspace()
    {
        _emptyFolderReturnTestTypeKey =
            null;

        if (_emptyFolderBackButton is not null)
        {
            _emptyFolderBackButton.IsVisible =
                false;
        }

        if (_contentAreaGrid is not null)
        {
            _contentAreaGrid.ContextMenu =
                null;
        }
    }

    private TestCollectionData? GetCurrentCollection()
    {
        if (_currentCollectionIndex < 0 ||
            _currentCollectionIndex >=
            _collections.Count)
        {
            return null;
        }

        return _collections[
            _currentCollectionIndex];
    }

    private List<TestCollectionData> GetCollectionsForTestType(
        string testTypeKey)
    {
        var result =
            new List<TestCollectionData>();

        var testTypeRootFolders =
            _folders
                .Where(
                    folder =>
                        string.Equals(
                            folder.ParentKey,
                            ProjectRootKey,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            folder.TestTypeKey,
                            testTypeKey,
                            StringComparison.OrdinalIgnoreCase))
                .OrderBy(
                    folder =>
                        folder.SortOrder)
                .ThenBy(
                    folder =>
                        folder.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        foreach (var rootFolder in testTypeRootFolders)
        {
            AddCollectionsInTreeOrder(
                rootFolder.Key,
                result);
        }

        return _activeAssignmentCaseIds is null
            ? result
            : result
                .Where(
                    collection =>
                        collection.Cases.Any(
                            testCase =>
                                _activeAssignmentCaseIds.Contains(
                                    testCase.Id)))
                .ToList();
    }

    private bool IsCaseVisibleForActiveAssignment(
        TestCaseData testCase)
    {
        return _activeAssignmentCaseIds is null ||
               _activeAssignmentCaseIds.Contains(
                   testCase.Id);
    }

    private void AddCollectionsInTreeOrder(
        string folderKey,
        List<TestCollectionData> result)
    {
        var collectionsInCurrentFolder =
            _collections
                .Where(
                    collection =>
                        string.Equals(
                            collection.ParentFolderKey,
                            folderKey,
                            StringComparison.OrdinalIgnoreCase))
                .OrderBy(
                    collection =>
                        collection.SortOrder)
                .ThenBy(
                    collection =>
                        collection.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        result.AddRange(
            collectionsInCurrentFolder);

        var childFolders =
            _folders
                .Where(
                    folder =>
                        string.Equals(
                            folder.ParentKey,
                            folderKey,
                            StringComparison.OrdinalIgnoreCase))
                .OrderBy(
                    folder =>
                        folder.SortOrder)
                .ThenBy(
                    folder =>
                        folder.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        foreach (var childFolder in childFolders)
        {
            AddCollectionsInTreeOrder(
                childFolder.Key,
                result);
        }
    }

    private static string GetTestTypeDisplayName(
        string testTypeKey)
    {
        if (string.Equals(
                testTypeKey,
                RegressionTestTypeKey,
                StringComparison.OrdinalIgnoreCase))
        {
            return LocalizationService.T(
                "Explorer.RegressionTests");
        }

        if (string.Equals(
                testTypeKey,
                FunctionalTestTypeKey,
                StringComparison.OrdinalIgnoreCase))
        {
            return LocalizationService.T(
                "Explorer.FunctionalTests");
        }

        return LocalizationService.T(
            "Explorer.Test");
    }

    private string? GetNextTestTypeKey(
        string currentTestTypeKey)
    {
        var orderedKeys =
            new[]
            {
                RegressionTestTypeKey,
                FunctionalTestTypeKey
            };

        var currentIndex =
            Array.FindIndex(
                orderedKeys,
                key =>
                    string.Equals(
                        key,
                        currentTestTypeKey,
                        StringComparison.OrdinalIgnoreCase));

        // Nieznany klucz nie może powodować powrotu do pierwszego typu
        // i zapętlenia ekranu podsumowania z ostatnim zbiorem.
        if (currentIndex < 0)
        {
            return null;
        }

        for (var index = currentIndex + 1;
             index < orderedKeys.Length;
             index++)
        {
            if (_activeAssignmentCaseIds is null ||
                GetCollectionsForTestType(
                    orderedKeys[index]).Count > 0)
            {
                return orderedKeys[index];
            }
        }

        return null;
    }

    private void ExpandPathToCollection(
        string collectionKey)
    {
        var collection =
            _collections.FirstOrDefault(
                item =>
                    item.Key ==
                    collectionKey);

        if (collection is null)
        {
            return;
        }

        ExpandPathToFolder(
            collection.ParentFolderKey);
    }

    private void ExpandPathToFolder(
        string folderKey)
    {
        var currentKey =
            folderKey;

        while (!string.IsNullOrWhiteSpace(
                   currentKey))
        {
            var folder =
                _folders.FirstOrDefault(
                    item =>
                        item.Key ==
                        currentKey);

            if (folder is null)
            {
                break;
            }

            if (folder.TreeItem is not null)
            {
                folder.TreeItem.IsExpanded =
                    true;
            }

            currentKey =
                folder.ParentKey;
        }
    }

    private void SelectPendingTreeElement(
        string key)
    {
        var folder =
            _folders.FirstOrDefault(
                item =>
                    item.Key ==
                    key);

        if (folder is not null)
        {
            ExpandPathToFolder(
                folder.ParentKey);

            SelectFolderForCommands(
                folder);

            ShowFolderScreen(
                folder);

            if (_testTreeView is not null &&
                folder.TreeItem is not null)
            {
                _testTreeView.SelectedItem =
                    folder.TreeItem;

                folder.TreeItem.IsSelected =
                    true;
            }

            folder.TreeItem?.BringIntoView();

            return;
        }

        var collection =
            _collections.FirstOrDefault(
                item =>
                    item.Key ==
                    key);

        if (collection is null)
        {
            return;
        }

        ExpandPathToCollection(
            collection.Key);

        SelectCollection(
            collection);

        collection.TreeItem?.BringIntoView();
    }

    private void RemoveFolderBranchFromMemory(
        FolderData folder)
    {
        var childFolders =
            _folders
                .Where(
                    item =>
                        item.ParentKey ==
                        folder.Key)
                .ToList();

        foreach (var childFolder in childFolders)
        {
            RemoveFolderBranchFromMemory(
                childFolder);

            _folders.Remove(
                childFolder);
        }

        var collections =
            _collections
                .Where(
                    item =>
                        item.ParentFolderKey ==
                        folder.Key)
                .ToList();

        foreach (var collection in collections)
        {
            _collections.Remove(
                collection);
        }
    }

    private string BuildFolderPath(
        string folderKey)
    {
        var names =
            new List<string>();

        var currentKey =
            folderKey;

        while (!string.IsNullOrWhiteSpace(
                   currentKey))
        {
            var folder =
                _folders.FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Key,
                            currentKey,
                            StringComparison.OrdinalIgnoreCase));

            if (folder is null)
            {
                break;
            }

            if (folder.Key != ProjectRootKey)
            {
                names.Add(
                    GetFolderDisplayName(
                        folder));
            }

            currentKey =
                folder.ParentKey;
        }

        names.Reverse();

        return names.Count == 0
            ? _projectName
            : string.Join(
                " / ",
                names);
    }

    private void RefreshCollectionPaths()
    {
        foreach (var collection in _collections)
        {
            collection.Path =
                BuildFolderPath(
                    collection.ParentFolderKey);
        }
    }

    private static void SetText(
        TextBlock? textBlock,
        string value)
    {
        if (textBlock is not null)
        {
            textBlock.Text =
                value;
        }
    }


    private async void ResetStatusesButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        CloseSettingsFlyout();

        var ownerWindow =
            TopLevel.GetTopLevel(this)
            as Window;

        if (ownerWindow is null)
        {
            return;
        }

        var scopeOptions =
            _folders
                .Where(
                    folder =>
                        string.Equals(
                            folder.ParentKey,
                            ProjectRootKey,
                            StringComparison.OrdinalIgnoreCase))
                .OrderBy(
                    folder =>
                        folder.SortOrder)
                .ThenBy(
                    folder =>
                        folder.Name,
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    folder =>
                        new ResetScopeOption(
                            folder.Key,
                            folder.Name))
                .ToList();

        var dialog =
            new ResetStatusesWindow(
                scopeOptions);

        var accepted =
            await dialog.ShowDialog<bool>(
                ownerWindow);

        if (!accepted)
        {
            return;
        }

        var busyWindow =
            new BusyOperationWindow(
                LocalizationService.T("ResetStatuses.BusyTitle"),
                LocalizationService.T("ResetStatuses.BusyDescription"),
                () =>
                    ApplyStatusResetAsync(
                        dialog.Request));

        await busyWindow.ShowDialog(
            ownerWindow);

        if (busyWindow.OperationException is not null)
        {
            var errorWindow =
                new OperationResultWindow(
                    false,
                    LocalizationService.T("ResetStatuses.FailedTitle"),
                    LocalizationService.T("ResetStatuses.FailedDescription"));

            await errorWindow.ShowDialog(
                ownerWindow);

            return;
        }

        var resultWindow =
            new OperationResultWindow(
                true,
                LocalizationService.T("ResetStatuses.SuccessTitle"),
                LocalizationService.T("ResetStatuses.SuccessDescription"));

        await resultWindow.ShowDialog(
            ownerWindow);
    }

    private async Task ApplyStatusResetAsync(
        ResetStatusesRequest request)
    {
        var targetCollections =
            _collections
                .Where(
                    collection =>
                        string.IsNullOrWhiteSpace(
                            request.ScopeFolderKey) ||
                        IsFolderInsideScope(
                            collection.ParentFolderKey,
                            request.ScopeFolderKey))
                .ToList();

        foreach (var collection in targetCollections)
        {
            foreach (var testCase in collection.Cases)
            {
                if (_activeAssignmentCaseIds is not null &&
                    !_activeAssignmentCaseIds.Contains(
                        testCase.Id))
                {
                    continue;
                }

                if (request.OnlyPendingAndNa &&
                    testCase.Status != StatusNone &&
                    testCase.Status != StatusNa)
                {
                    continue;
                }

                var newStatus =
                    _activeAssignmentCaseIds is not null &&
                    string.Equals(
                        request.NewStatus,
                        StatusNone,
                        StringComparison.OrdinalIgnoreCase)
                        ? StatusInProgress
                        : request.NewStatus;

                testCase.Status =
                    newStatus;

                testCase.Comment =
                    string.Empty;

                if (_activeAssignmentIdByCaseId.TryGetValue(
                        testCase.Id,
                        out var assignmentId))
                {
                    await _assignmentService.UpdateAssignmentCaseStatusAsync(
                        assignmentId,
                        testCase.Id,
                        newStatus,
                        string.Empty);
                }
                else
                {
                    await _userTestCaseService.SaveStatusAsync(
                        testCase.Id,
                        _projectKey,
                        collection.TestTypeKey,
                        collection.Key,
                        testCase.Name,
                        testCase.SortOrder,
                        testCase.Status);
                }
            }

            UpdateCollectionState(
                collection);
        }

        await TrackResultChangeAsync();

        UpdateSessionSummary();
        UpdateCurrentCollectionProgress();
        UpdateActiveCollectionHighlight();

        if (_currentCollectionIndex >= 0)
        {
            RenderCurrentCollectionCases();
        }
    }

    private bool IsFolderInsideScope(
        string folderKey,
        string scopeFolderKey)
    {
        var currentKey =
            folderKey;

        while (!string.IsNullOrWhiteSpace(
                   currentKey))
        {
            if (string.Equals(
                    currentKey,
                    scopeFolderKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var currentFolder =
                _folders.FirstOrDefault(
                    folder =>
                        string.Equals(
                            folder.Key,
                            currentKey,
                            StringComparison.OrdinalIgnoreCase));

            if (currentFolder is null)
            {
                return false;
            }

            currentKey =
                currentFolder.ParentKey;
        }

        return false;
    }

    private async void ProjectToolsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        CloseSettingsFlyout();

        var ownerWindow =
            TopLevel.GetTopLevel(this)
            as Window;

        if (ownerWindow is null)
        {
            return;
        }

        var dialog =
            new ProjectToolsWindow();

        var action =
            await dialog.ShowDialog<ProjectToolsAction>(
                ownerWindow);

        switch (action)
        {
            case ProjectToolsAction.Import:

                await ImportProjectAsync(
                    ownerWindow);

                break;

            case ProjectToolsAction.Export:

                await ExportProjectAsync(
                    ownerWindow);

                break;
        }
    }

    private bool IsCurrentUserAdministrator =>
        string.Equals(
            _highestSystemRole,
            "Administrator",
            StringComparison.OrdinalIgnoreCase);

    private bool CanAssignTests =>
        IsCurrentUserAdministrator ||
        string.Equals(
            _highestSystemRole,
            "Lider",
            StringComparison.OrdinalIgnoreCase);

    private bool CanConfigureNetworkHost =>
        CanAssignTests;

    private bool CanReorderStructure =>
        _activeAssignmentCaseIds is null;

    private async Task RefreshAssignmentAndNotificationStateAsync()
    {
        _activeAssignments =
            (await _assignmentService.GetActiveAssignmentsForUserAsync(
                _loggedInLogin))
            .Where(
                assignment =>
                    string.Equals(
                        assignment.ProjectKey,
                        _projectKey,
                        StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (_executeAssignedTestsButton is not null)
        {
            _executeAssignedTestsButton.IsVisible =
                _activeAssignments.Length > 0 &&
                _activeAssignmentCaseIds is null;

            if (_executeAssignedTestsLabel is not null)
            {
                _executeAssignedTestsLabel.Text =
                    LocalizationService.T("Assignment.ExecuteTests");
            }

            _executeAssignedTestsButton.ClearValue(
                TemplatedControl.BackgroundProperty);

            _executeAssignedTestsButton.ClearValue(
                TemplatedControl.BorderBrushProperty);

            if (_executeAssignedTestsButton.IsVisible)
            {
                _assignmentGlowTimer.Start();
            }
            else
            {
                _assignmentGlowTimer.Stop();
                _executeAssignedTestsButton.Opacity = 1;
                _executeAssignedTestsButton.ClearValue(
                    TemplatedControl.BorderBrushProperty);
            }
        }

        if (_executeAssignmentPendingDot is not null)
        {
            _executeAssignmentPendingDot.IsVisible =
                _activeAssignments.Length > 0;
        }

        if (_restartAssignedTestsButton is not null)
        {
            _restartAssignedTestsButton.IsVisible =
                _activeAssignmentCaseIds is not null;
        }

        if (_dashboardPendingReportDot is not null)
        {
            var dashboardAssignments =
                (await _assignmentService.GetAssignmentsForDashboardAsync())
                .Where(
                    assignment =>
                        string.Equals(
                            assignment.ProjectKey,
                            _projectKey,
                            StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var pendingReportAssignments =
                dashboardAssignments
                    .GroupBy(
                        assignment =>
                            assignment.ApplicationVersion,
                        StringComparer.OrdinalIgnoreCase)
                    .Where(
                        versionGroup =>
                            versionGroup.Any(
                                assignment =>
                                    assignment.CompletedAt.HasValue &&
                                    !assignment.ReportGeneratedAt.HasValue) &&
                            !versionGroup.Any(
                                assignment =>
                                    assignment.IsActive &&
                                    !assignment.CompletedAt.HasValue))
                    .SelectMany(
                        versionGroup =>
                            versionGroup.Where(
                                assignment =>
                                    assignment.CompletedAt.HasValue &&
                                    !assignment.ReportGeneratedAt.HasValue))
                    .OrderBy(
                        assignment =>
                            assignment.Id)
                    .ToArray();

            _dashboardPendingReportSignature =
                string.Join(
                    ";",
                    pendingReportAssignments.Select(
                        assignment =>
                            assignment.Id.ToString("N")));

            if (string.IsNullOrEmpty(
                    _dashboardPendingReportSignature))
            {
                _acknowledgedDashboardReportSignature =
                    string.Empty;
            }

            _dashboardPendingReportDot.IsVisible =
                CanAssignTests &&
                !string.IsNullOrEmpty(
                    _dashboardPendingReportSignature) &&
                !string.Equals(
                    _dashboardPendingReportSignature,
                    _acknowledgedDashboardReportSignature,
                    StringComparison.Ordinal);
        }

        var unreadCount =
            await _assignmentService.GetUnreadCountAsync(
                _loggedInLogin);

        if (_notificationBadgeBorder is not null)
        {
            _notificationBadgeBorder.IsVisible =
                unreadCount > 0;
        }

        if (_notificationBadgeTextBlock is not null)
        {
            _notificationBadgeTextBlock.Text =
                unreadCount.ToString();
        }
    }

    private async void NetworkSyncButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        CloseSettingsFlyout();
        if (TopLevel.GetTopLevel(this) is not Window ownerWindow)
        {
            return;
        }

        var dialog = new NetworkSyncWindow(
            CanConfigureNetworkHost);
        await dialog.ShowDialog(ownerWindow);
    }

    private async void NotificationFlyout_OnOpened(
        object? sender,
        EventArgs e)
    {
        if (sender is not Flyout flyout ||
            flyout.Content is not Control content)
        {
            return;
        }

        var itemsPanel =
            FindNotificationFlyoutControl<StackPanel>(
                content,
                "NotificationFlyoutItemsPanel");

        var clearButton =
            FindNotificationFlyoutControl<Button>(
                content,
                "NotificationFlyoutClearButton");

        var confirmation =
            FindNotificationFlyoutControl<Grid>(
                content,
                "NotificationFlyoutClearConfirmation");

        if (itemsPanel is null ||
            clearButton is null)
        {
            return;
        }

        if (confirmation is not null)
        {
            confirmation.IsVisible = false;
        }

        clearButton.IsVisible = true;
        itemsPanel.Children.Clear();
        itemsPanel.Children.Add(
            new TextBlock
            {
                Text = LocalizationService.T("Common.Loading"),
                Margin = new Thickness(0, 18),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.Gray
            });

        var notifications =
            await _assignmentService.GetNotificationsForUserAsync(
                _loggedInLogin);

        var activeAssignmentIds =
            (await _assignmentService.GetActiveAssignmentsForUserAsync(
                _loggedInLogin))
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

        itemsPanel.Children.Clear();
        clearButton.IsEnabled =
            notifications.Length > 0;

        if (notifications.Length == 0)
        {
            AddEmptyNotificationFlyoutState(
                itemsPanel);
        }

        foreach (var notification in notifications)
        {
            itemsPanel.Children.Add(
                await CreateNotificationFlyoutItemAsync(
                    notification,
                    newestActiveAssignmentNotificationId));
        }

        await _assignmentService.MarkAllNotificationsReadAsync(
            _loggedInLogin);

        await RefreshAssignmentAndNotificationStateAsync();
    }

    private async Task<Border> CreateNotificationFlyoutItemAsync(
        UserNotificationModel notification,
        Guid? newestActiveAssignmentNotificationId)
    {
        var contentPanel =
            new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock
                    {
                        Text = GetNotificationDisplayTitle(
                            notification.Title),
                        FontSize = 14,
                        FontWeight = FontWeight.Bold
                    },
                    new TextBlock
                    {
                        Text = GetNotificationDisplayMessage(
                            notification.Message),
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 13
                    },
                    new TextBlock
                    {
                        Text = notification.CreatedAt.LocalDateTime
                            .ToString("dd.MM.yyyy HH:mm"),
                        FontSize = 11,
                        Foreground = Brushes.Gray
                    }
                }
            };

        var notificationBorder =
            new Border
            {
                Padding = new Thickness(12),
                Background = notification.IsRead
                    ? new SolidColorBrush(
                        Color.Parse("#0A68726B"))
                    : new SolidColorBrush(
                        Color.Parse("#1828C76F")),
                BorderBrush = new SolidColorBrush(
                    Color.Parse("#4068726B")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Child = contentPanel
            };

        if (notification.StructureChangeRequestId is Guid requestId)
        {
            var request =
                await _assignmentService.GetStructureChangeRequestAsync(
                    requestId);

            if (request?.Status == "Pending")
            {
                var approveButton =
                    new Button
                    {
                        Content = LocalizationService.T(
                            "Notifications.ApproveDeletion"),
                        Classes = { "PrimaryAction" }
                    };

                var rejectButton =
                    new Button
                    {
                        Content = LocalizationService.T(
                            "Notifications.Reject"),
                        Classes = { "SecondaryAction" }
                    };

                var actions =
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Margin = new Thickness(0, 8, 0, 0),
                        Children =
                        {
                            rejectButton,
                            approveButton
                        }
                    };

                async void Resolve(bool approve)
                {
                    var resolved =
                        await _assignmentService.ResolveStructureDeletionAsync(
                            requestId,
                            _loggedInLogin,
                            approve);

                    if (!resolved)
                    {
                        return;
                    }

                    approveButton.IsEnabled = false;
                    rejectButton.IsEnabled = false;
                    approveButton.Content = LocalizationService.T(
                        approve
                            ? "Notifications.Approved"
                            : "Notifications.Rejected");
                }

                approveButton.Click +=
                    (_, _) =>
                        Resolve(true);

                rejectButton.Click +=
                    (_, _) =>
                        Resolve(false);

                contentPanel.Children.Add(
                    actions);
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
                LocalizationService.T(
                    "Notifications.ShowExecuteTip"));

            notificationBorder.PointerPressed +=
                async (_, eventArgs) =>
                {
                    if (!eventArgs
                            .GetCurrentPoint(
                                notificationBorder)
                            .Properties
                            .IsLeftButtonPressed)
                    {
                        return;
                    }

                    _notificationCenterButton?.Flyout?.Hide();
                    await HighlightAssignedTestsButtonAsync();
                };
        }

        return notificationBorder;
    }

    private void NotificationFlyoutClearButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var confirmation =
            FindNotificationFlyoutControl<Grid>(
                button,
                "NotificationFlyoutClearConfirmation");

        if (confirmation is null)
        {
            return;
        }

        button.IsVisible = false;
        confirmation.IsVisible = true;
    }

    private void NotificationFlyoutCancelClearButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var confirmation =
            FindNotificationFlyoutControl<Grid>(
                button,
                "NotificationFlyoutClearConfirmation");

        var clearButton =
            FindNotificationFlyoutControl<Button>(
                button,
                "NotificationFlyoutClearButton");

        if (confirmation is not null)
        {
            confirmation.IsVisible = false;
        }

        if (clearButton is not null)
        {
            clearButton.IsVisible = true;
        }
    }

    private async void NotificationFlyoutConfirmClearButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        var itemsPanel =
            FindNotificationFlyoutControl<StackPanel>(
                button,
                "NotificationFlyoutItemsPanel");

        var confirmation =
            FindNotificationFlyoutControl<Grid>(
                button,
                "NotificationFlyoutClearConfirmation");

        var clearButton =
            FindNotificationFlyoutControl<Button>(
                button,
                "NotificationFlyoutClearButton");

        if (itemsPanel is null)
        {
            return;
        }

        await _assignmentService.ClearNotificationsForUserAsync(
            _loggedInLogin);

        itemsPanel.Children.Clear();
        AddEmptyNotificationFlyoutState(
            itemsPanel);

        if (confirmation is not null)
        {
            confirmation.IsVisible = false;
        }

        if (clearButton is not null)
        {
            clearButton.IsVisible = true;
            clearButton.IsEnabled = false;
        }

        await RefreshAssignmentAndNotificationStateAsync();
    }

    private static void AddEmptyNotificationFlyoutState(
        StackPanel itemsPanel)
    {
        itemsPanel.Children.Add(
            new TextBlock
            {
                Text = LocalizationService.T("Notifications.Empty"),
                Margin = new Thickness(0, 18),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.Gray
            });
    }

    private static T? FindNotificationFlyoutControl<T>(
        Control source,
        string name)
        where T : Control
    {
        if (source is T typedSource &&
            string.Equals(
                typedSource.Name,
                name,
                StringComparison.Ordinal))
        {
            return typedSource;
        }

        return source
            .GetVisualAncestors()
            .Prepend(source)
            .SelectMany(
                ancestor =>
                    ancestor.GetVisualDescendants())
            .OfType<T>()
            .FirstOrDefault(
                control =>
                    string.Equals(
                        control.Name,
                        name,
                        StringComparison.Ordinal));
    }

    private static string GetNotificationDisplayTitle(
        string title)
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

    private static string GetNotificationDisplayMessage(
        string message)
    {
        if (LocalizationService.IsPolish)
        {
            return message;
        }

        var assignment =
            Regex.Match(
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

    private async Task HighlightAssignedTestsButtonAsync()
    {
        if (_executeAssignedTestsButton is null ||
            _executeAssignmentPendingDot is null ||
            !_executeAssignedTestsButton.IsVisible)
        {
            return;
        }

        _executeAssignmentPendingDot.IsVisible = true;
        var animationVersion =
            ++_assignmentDotAnimationVersion;
        var scale =
            _executeAssignmentPendingDot.RenderTransform as ScaleTransform;

        if (scale is null)
        {
            scale = new ScaleTransform();
            _executeAssignmentPendingDot.RenderTransform = scale;
        }

        var scales = new[] { 0.65, 1.0, 1.4, 1.0 };
        var startedAt = DateTime.UtcNow;
        var scaleIndex = 0;

        while (DateTime.UtcNow - startedAt < TimeSpan.FromSeconds(4))
        {
            if (animationVersion != _assignmentDotAnimationVersion)
            {
                return;
            }

            scale.ScaleX = scales[scaleIndex];
            scale.ScaleY = scales[scaleIndex];
            scaleIndex = (scaleIndex + 1) % scales.Length;
            await Task.Delay(105);
        }

        foreach (var delay in new[] { 170, 230, 310, 420, 580 })
        {
            foreach (var nextScale in scales)
            {
                if (animationVersion != _assignmentDotAnimationVersion)
                {
                    return;
                }

                scale.ScaleX = nextScale;
                scale.ScaleY = nextScale;
                await Task.Delay(delay);
            }
        }

        scale.ScaleX = 1;
        scale.ScaleY = 1;
    }

    private async Task OpenAssignmentManagementAsync()
    {
        if (!CanAssignTests)
        {
            return;
        }

        if (_inlineDashboardHost is null)
        {
            return;
        }

        var options =
            _collections
                .OrderBy(
                    collection =>
                        collection.Path)
                .ThenBy(
                    collection =>
                        collection.SortOrder)
                .SelectMany(
                    collection =>
                        collection.Cases.Select(
                            testCase =>
                                new AssignmentCaseOption(
                                    testCase.Id,
                                    $"{collection.Path} / {collection.Name}",
                                    testCase.Name)))
                .ToArray();

        var assignmentController =
            new AssignmentManagementWindow(
                _projectKey,
                _projectName,
                _loggedInLogin,
                options,
                IsCurrentUserAdministrator);

        _inlineDashboardController?.ReleaseInlineContent();
        _inlineDashboardController = null;

        _inlineAssignmentController?.ReleaseInlineContent();
        _inlineAssignmentController = assignmentController;

        var assignmentContent =
            await assignmentController.TakeInlineContentAsync(
                ReturnFromInlineAssignmentAsync);

        _inlineDashboardHost.Content = assignmentContent;
        _inlineDashboardHost.IsVisible = assignmentContent is not null;
    }

    private async Task ReturnFromInlineAssignmentAsync()
    {
        await RefreshAssignmentAndNotificationStateAsync();
        await ShowInlineDashboardAsync();
    }

    private async Task ShowInlineDashboardAsync()
    {
        if (_inlineDashboardHost is null)
        {
            return;
        }

        HideWorkspacePanelsForDashboard();

        _inlineAssignmentController?.ReleaseInlineContent();
        _inlineAssignmentController = null;

        _inlineDashboardController?.ReleaseInlineContent();
        _inlineDashboardController =
            new ProgressDashboardWindow(
                _loggedInLogin,
                _systemRoles,
                CanAssignTests
                    ? OpenAssignmentManagementAsync
                    : null);

        var dashboardContent =
            await _inlineDashboardController.TakeInlineContentAsync();

        _inlineDashboardHost.Content = dashboardContent;
        _inlineDashboardHost.IsVisible = dashboardContent is not null;
    }

    private async void ExecuteAssignedTestsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_activeAssignmentCaseIds is not null)
        {
            var confirmed =
                await ShowOperationConfirmationAsync(
                    LocalizationService.T("Assignment.InterruptTitle"),
                    LocalizationService.T("Assignment.InterruptDescription"),
                    LocalizationService.T("Assignment.InterruptTests"));

            if (confirmed)
            {
                await ExitAssignedModeToAdHocAsync();
            }

            return;
        }

        await ExecuteLatestAssignmentAsync();
    }

    private async void RestartAssignedTestsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_activeAssignmentCaseIds is null ||
            _activeAssignmentCaseIds.Count == 0 ||
            TopLevel.GetTopLevel(this) is not Window ownerWindow)
        {
            return;
        }

        var confirmed =
            await ShowOperationConfirmationAsync(
                LocalizationService.T("Assignment.RestartTitle"),
                LocalizationService.T("Assignment.RestartDescription"),
                LocalizationService.T("Assignment.RestartTests"));

        if (!confirmed)
        {
            return;
        }

        var busyWindow =
            new BusyOperationWindow(
                LocalizationService.T("Assignment.RestartBusyTitle"),
                LocalizationService.T("Assignment.RestartBusyDescription"),
                () =>
                    ApplyStatusResetAsync(
                        new ResetStatusesRequest
                        {
                            ScopeFolderKey = null,
                            NewStatus = StatusInProgress,
                            OnlyPendingAndNa = false
                        }));

        await busyWindow.ShowDialog(
            ownerWindow);

        if (busyWindow.OperationException is not null)
        {
            await new OperationResultWindow(
                    false,
                    LocalizationService.T("Assignment.RestartFailedTitle"),
                    LocalizationService.T("Assignment.RestartFailedDescription"))
                .ShowDialog(ownerWindow);

            return;
        }

        var firstCollection =
            new[]
            {
                RegressionTestTypeKey,
                FunctionalTestTypeKey
            }
            .SelectMany(GetCollectionsForTestType)
            .FirstOrDefault(collection =>
                collection.Cases.Any(IsCaseVisibleForActiveAssignment));

        if (firstCollection is not null)
        {
            SelectCollection(
                firstCollection,
                revealInTree: true);
        }

        await new OperationResultWindow(
                true,
                LocalizationService.T("Assignment.RestartSuccessTitle"),
                LocalizationService.T("Assignment.RestartSuccessDescription"))
            .ShowDialog(ownerWindow);
    }

    public async Task ExecuteLatestAssignmentAsync()
    {
        var ownerWindow =
            TopLevel.GetTopLevel(this)
            as Window;

        if (ownerWindow is null ||
            !await ReconcileActiveAssignmentsAsync(
                ownerWindow,
                refreshAssignedScreen: false))
        {
            return;
        }

        var assignment =
            _activeAssignments.FirstOrDefault();

        if (assignment is null)
        {
            await RefreshAssignmentAndNotificationStateAsync();
            return;
        }

        CaptureAdHocStateBeforeAssignedMode();

        _activeAssignmentCaseIds =
            _activeAssignments
                .SelectMany(
                    item =>
                        item.TestCaseIds)
                .ToHashSet();

        _activeAssignmentIdByCaseId =
            _activeAssignments
                .SelectMany(
                    item =>
                        item.TestCaseIds.Select(
                            testCaseId =>
                                new
                                {
                                    TestCaseId = testCaseId,
                                    AssignmentId = item.Id,
                                    item.UpdatedAt
                                }))
                .GroupBy(
                    item =>
                        item.TestCaseId)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        group
                            .OrderByDescending(
                                item =>
                                    item.UpdatedAt)
                            .First()
                            .AssignmentId);

        _activeAssignmentId =
            assignment.Id;

        await _assignmentService.MarkAssignmentStartedAsync(
            _activeAssignments.Select(
                item =>
                    item.Id));

        var progressByCaseId =
            _activeAssignments
                .SelectMany(
                    item =>
                        item.CaseProgress)
                .GroupBy(
                    progress =>
                        progress.TestCaseId)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        group
                            .OrderByDescending(
                                progress =>
                                    progress.UpdatedAt)
                            .First());

        foreach (var testCase in
                 _collections.SelectMany(
                     collection =>
                         collection.Cases))
        {
            if (!_activeAssignmentCaseIds.Contains(
                    testCase.Id))
            {
                continue;
            }

            testCase.Status =
                progressByCaseId.TryGetValue(
                    testCase.Id,
                    out var savedProgress)
                    ? savedProgress.Status
                    : StatusInProgress;

            testCase.Comment =
                savedProgress?.Comment ?? string.Empty;
        }

        _applicationVersion =
            assignment.ApplicationVersion;

        if (_sessionManager is not null &&
            _sessionState is not null)
        {
            await _sessionManager.MarkSessionStartedAsync(
                _sessionState,
                _projectName,
                _applicationVersion,
                _loggedInLogin,
                "Assigned");

            await _sessionManager.UpdateAssignmentContextAsync(
                _sessionState,
                _activeAssignments.Select(
                    item =>
                        item.Id));
        }

        if (_executeAssignedTestsButton is not null)
        {
            _executeAssignedTestsButton.IsVisible =
                true;

            if (_executeAssignedTestsLabel is not null)
            {
                _executeAssignedTestsLabel.Text =
                    LocalizationService.T("Assignment.InterruptTests");
            }

            _executeAssignedTestsButton.Background =
                new SolidColorBrush(
                    Color.Parse(
                        "#D84C57"));
        }

        if (_restartAssignedTestsButton is not null)
        {
            _restartAssignedTestsButton.IsVisible =
                true;
        }

        if (_finishEarlyButton is not null)
        {
            _finishEarlyButton.Content =
                LocalizationService.T("Assignment.FinishTests");

            ToolTip.SetTip(
                _finishEarlyButton,
                LocalizationService.T("Assignment.FinishTip"));
        }

        _assignmentGlowTimer.Stop();
        _assignmentValidityTimer.Start();

        if (_testTreeTitleTextBlock is not null)
        {
            _testTreeTitleTextBlock.Text =
                LocalizationService.T("Assignment.ExecuteTests");
        }

        if (_testTreeSearchTextBox is not null)
        {
            _testTreeSearchText =
                string.Empty;
            _testTreeSearchTextBox.Text =
                string.Empty;
            _testTreeSearchTextBox.IsVisible =
                false;
        }

        if (_projectInfoTextBlock is not null)
        {
            _projectInfoTextBlock.Text =
                LocalizationService.Format(
                    "Assignment.ExecutionModeSubtitle",
                    _projectName,
                    _applicationVersion);
        }

        BuildTestTree();

        var firstCollection =
            new[]
            {
                RegressionTestTypeKey,
                FunctionalTestTypeKey
            }
            .SelectMany(
                GetCollectionsForTestType)
            .FirstOrDefault();

        if (firstCollection is not null)
        {
            SelectCollection(
                firstCollection,
                revealInTree: true);

            Dispatcher.UIThread.Post(
                () =>
                {
                    if (_testCasesScrollViewer is not null)
                    {
                        _testCasesScrollViewer.Offset =
                            new Vector(
                                _testCasesScrollViewer.Offset.X,
                                0);
                    }

                    if (_testTreeView is not null &&
                        firstCollection.TreeItem is not null)
                    {
                        _testTreeView.ScrollIntoView(
                            firstCollection.TreeItem);
                    }
                },
                DispatcherPriority.Loaded);
        }

        UpdateSessionSummary();

        if (TopLevel.GetTopLevel(this) is Window tutorialOwnerWindow &&
            !await _userProfileService
                .GetSuppressAssignedTestsTutorialAsync(
                    _loggedInLogin))
        {
            var tutorial =
                new AssignedTestsTutorialWindow();

            await tutorial.ShowDialog(tutorialOwnerWindow);

            if (tutorial.DontShowAgain)
            {
                await _userProfileService
                    .SetSuppressAssignedTestsTutorialAsync(
                        _loggedInLogin,
                        true);
            }
        }
    }

    private async void ProgressDashboardButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        _acknowledgedDashboardReportSignature =
            _dashboardPendingReportSignature;

        if (_dashboardPendingReportDot is not null)
        {
            _dashboardPendingReportDot.IsVisible = false;
        }

        await ShowInlineDashboardAsync();
    }

    private void HomeButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        HideInlineDashboard();
        CloseSettingsFlyout();
        _returnToStartAction?.Invoke();
    }

    private void HideWorkspacePanelsForDashboard()
    {
        ClearFolderWorkspace();

        if (_welcomePanel is not null)
        {
            _welcomePanel.IsVisible = false;
        }

        if (_testExecutionPanel is not null)
        {
            _testExecutionPanel.IsVisible = false;
        }

        if (_summaryPanel is not null)
        {
            _summaryPanel.IsVisible = false;
        }

        if (_emptyFolderBackButton is not null)
        {
            _emptyFolderBackButton.IsVisible = false;
        }
    }

    private void HideInlineDashboard()
    {
        if (_inlineDashboardHost is not null)
        {
            _inlineDashboardHost.IsVisible = false;
            _inlineDashboardHost.Content = null;
        }

        _inlineDashboardController?.ReleaseInlineContent();
        _inlineDashboardController = null;
        _inlineAssignmentController?.ReleaseInlineContent();
        _inlineAssignmentController = null;
    }

    private async void AdminTestMenuButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (!CanAssignTests)
        {
            return;
        }

        CloseSettingsFlyout();

        var ownerWindow =
            TopLevel.GetTopLevel(
                this)
            as Window;

        if (ownerWindow is null)
        {
            return;
        }

        var dialog =
            new AdminTestMenuWindow(
                _highestSystemRole,
                _loggedInLogin);

        await dialog.ShowDialog(
            ownerWindow);

        if (dialog.GlobalResetCompleted)
        {
            _logoutAction?.Invoke();
            return;
        }

        await ReloadRoleBadgesAsync();
    }

    private void AdvancedSettingsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        // Controls inside a Flyout live in its own namescope. The generated
        // fields can therefore still be null even though the flyout is open.
        // Resolve both controls from the button's currently materialized
        // visual tree instead of relying on those generated fields.
        if (sender is not Button button)
        {
            return;
        }

        var flyoutContent =
            button.GetVisualAncestors()
                .OfType<StackPanel>()
                .FirstOrDefault(
                    panel =>
                        panel.GetVisualDescendants()
                            .OfType<StackPanel>()
                            .Any(child => child.Name == "AdvancedSettingsPanel"));

        var advancedPanel =
            flyoutContent?
                .GetVisualDescendants()
                .OfType<StackPanel>()
                .FirstOrDefault(panel => panel.Name == "AdvancedSettingsPanel");

        if (advancedPanel is null)
        {
            return;
        }

        advancedPanel.IsVisible =
            !advancedPanel.IsVisible;

        var chevron =
            button.GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(
                    textBlock =>
                        textBlock.Name == "AdvancedSettingsChevronTextBlock");

        if (chevron is not null)
        {
            chevron.Text =
                advancedPanel.IsVisible
                    ? "⌃"
                    : "⌄";
        }
    }

    private async void ResetSettingsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        CloseSettingsFlyout();

        if (!CanAssignTests ||
            TopLevel.GetTopLevel(this) is not Window ownerWindow)
        {
            return;
        }

        var dialog =
            new ResetSettingsWindow(
                _loggedInLogin);

        await dialog.ShowDialog(ownerWindow);

        if (dialog.GlobalResetCompleted)
        {
            _logoutAction?.Invoke();
        }
    }

    private async void AssignmentArchiveButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        CloseSettingsFlyout();

        if (!CanAssignTests ||
            TopLevel.GetTopLevel(this) is not Window ownerWindow)
        {
            return;
        }

        await new AssignmentArchiveWindow()
            .ShowDialog(ownerWindow);
    }

    private async void HelpInfoButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var ownerWindow =
            TopLevel.GetTopLevel(
                this)
            as Window;

        if (ownerWindow is null)
        {
            return;
        }

        var dialog =
            new HelpInfoWindow();

        await dialog.ShowDialog(
            ownerWindow);
    }

    private void CloseSettingsFlyout()
    {
        _projectToolsButton?.Flyout?.Hide();
    }

    private async Task ExportProjectAsync(
        Window ownerWindow)
    {
        var storageProvider =
            ownerWindow.StorageProvider;

        if (!storageProvider.CanSave)
        {
            return;
        }

        var suggestedFileName =
            CreateExportFileName();

        var file =
            await storageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title =
                        LocalizationService.T("ProjectTools.Export"),

                    SuggestedFileName =
                        suggestedFileName,

                    DefaultExtension =
                        "json",

                    FileTypeChoices =
                        new[]
                        {
                            new FilePickerFileType(
                                LocalizationService.T("ProjectTools.JsonFile"))
                            {
                                Patterns =
                                    new[]
                                    {
                                        "*.json"
                                    },

                                MimeTypes =
                                    new[]
                                    {
                                        "application/json"
                                    }
                            }
                        }
                });

        if (file is null)
        {
            return;
        }

        var projectPackage =
            CreateProjectPackage();

        var json =
            JsonSerializer.Serialize(
                projectPackage,
                new JsonSerializerOptions
                {
                    WriteIndented =
                        true
                });

        await using var stream =
            await file.OpenWriteAsync();

        stream.SetLength(0);

        await using var writer =
            new StreamWriter(stream);

        await writer.WriteAsync(
            json);
    }

    private async Task ImportProjectAsync(
        Window ownerWindow)
    {
        var storageProvider =
            ownerWindow.StorageProvider;

        if (!storageProvider.CanOpen)
        {
            return;
        }

        var files =
            await storageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title =
                        LocalizationService.T("ProjectTools.Import"),

                    AllowMultiple =
                        false,

                    FileTypeFilter =
                        new[]
                        {
                            new FilePickerFileType(
                                LocalizationService.T("ProjectTools.JsonFile"))
                            {
                                Patterns =
                                    new[]
                                    {
                                        "*.json"
                                    },

                                MimeTypes =
                                    new[]
                                    {
                                        "application/json"
                                    }
                            }
                        }
                });

        var file =
            files.FirstOrDefault();

        if (file is null)
        {
            return;
        }

        ProjectPackage? projectPackage;

        try
        {
            await using var stream =
                await file.OpenReadAsync();

            projectPackage =
                await JsonSerializer.DeserializeAsync<ProjectPackage>(
                    stream,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive =
                            true
                    });
        }
        catch
        {
            return;
        }

        if (projectPackage is null)
        {
            return;
        }

        var previewWindow =
            new ImportProjectPreviewWindow(
                projectPackage.Metadata);

        var accepted =
            await previewWindow.ShowDialog<bool>(
                ownerWindow);

        if (!accepted)
        {
            return;
        }

        await MergeImportedProjectAsync(
            projectPackage,
            previewWindow.OverwriteStatuses);
    }

    private ProjectPackage CreateProjectPackage()
    {
        var projectPackage =
            new ProjectPackage
            {
                Metadata =
                    new ProjectPackageMetadata
                    {
                        ProjectName =
                            _projectName,

                        ProjectKey =
                            _projectKey,

                        ApplicationVersion =
                            _applicationVersion,

                        TesterName =
                            _testerName,

                        ExportedAt =
                            DateTimeOffset.Now,

                        QaTestCenterVersion =
                            "0.2"
                    }
            };

        projectPackage.Folders.AddRange(
            _folders.Select(
                folder =>
                    new ProjectPackageFolder
                    {
                        Id =
                            folder.Id,

                        Key =
                            folder.Key,

                        ParentKey =
                            folder.ParentKey,

                        Name =
                            folder.Name,

                        TestTypeKey =
                            folder.TestTypeKey,

                        IsSystem =
                            folder.IsSystem,

                        IsProtected =
                            folder.IsProtected,

                        RequiresManagerRole =
                            folder.RequiresManagerRole,

                        CreatedByLogin =
                            folder.CreatedByLogin,

                        SortOrder =
                            folder.SortOrder
                    }));

        projectPackage.Collections.AddRange(
            _collections.Select(
                collection =>
                    new ProjectPackageCollection
                    {
                        Id =
                            collection.Id,

                        Key =
                            collection.Key,

                        ParentFolderKey =
                            collection.ParentFolderKey,

                        Name =
                            collection.Name,

                        Description =
                            collection.Description,

                        TestTypeKey =
                            collection.TestTypeKey,

                        IsSystem =
                            collection.IsSystem,

                        IsProtected =
                            collection.IsProtected,

                        RequiresManagerRole =
                            collection.RequiresManagerRole,

                        CreatedByLogin =
                            collection.CreatedByLogin,

                        SortOrder =
                            collection.SortOrder
                    }));

        projectPackage.TestCases.AddRange(
            _collections.SelectMany(
                collection =>
                    collection.Cases.Select(
                        testCase =>
                            new ProjectPackageTestCase
                            {
                                Id =
                                    testCase.Id,

                                CollectionKey =
                                    collection.Key,

                                TestTypeKey =
                                    collection.TestTypeKey,

                                Name =
                                    testCase.Name,

                                IsSystem =
                                    testCase.IsSystem,

                                IsProtected =
                                    testCase.IsProtected,

                                CreatedByLogin =
                                    testCase.CreatedByLogin,

                                SortOrder =
                                    testCase.SortOrder,

                                Status =
                                    testCase.Status,

                                Comment =
                                    testCase.Comment,

                                Summary = testCase.Summary,
                                Preconditions = testCase.Preconditions,
                                ExternalId = testCase.ExternalId,
                                SourceVersion = testCase.SourceVersion,
                                Importance = testCase.Importance,
                                ExecutionType = testCase.ExecutionType,
                                EstimatedDuration = testCase.EstimatedDuration,
                                Platforms = testCase.Platforms.ToList(),
                                Steps = testCase.Steps.Select(CloneStep).ToList()
                            })));

        return projectPackage;
    }

    private void CaptureUndoSnapshot()
    {
        _undoHistory.Add(
            new UndoHistoryEntry(
                CreateProjectPackage(),
                GetCurrentCollection()?.Key));

        const int maximumHistoryCount = 20;

        if (_undoHistory.Count >
            maximumHistoryCount)
        {
            _undoHistory.RemoveAt(
                0);
        }
    }

    private async Task UndoLastStructureChangeAsync()
    {
        if (_undoHistory.Count == 0)
        {
            return;
        }

        var historyEntry =
            _undoHistory[^1];

        _undoHistory.RemoveAt(
            _undoHistory.Count - 1);

        RestoreProjectPackage(
            historyEntry.Package);

        await PersistCurrentStructureAsync(
            replaceProjectData: true);

        RefreshCollectionPaths();

        foreach (var collection in _collections)
        {
            RenumberCollectionCases(
                collection);

            UpdateCollectionState(
                collection);
        }

        _currentCollectionIndex =
            -1;

        BuildTestTree();
        UpdateSessionSummary();

        var previousCollection =
            _collections.FirstOrDefault(
                collection =>
                    string.Equals(
                        collection.Key,
                        historyEntry.CurrentCollectionKey,
                        StringComparison.OrdinalIgnoreCase));

        if (previousCollection is not null)
        {
            SelectCollection(
                previousCollection,
                revealInTree: true);

            SelectCollectionForCommands(
                previousCollection);
        }
        else
        {
            ShowWelcomeScreen();
        }
    }

    private void RestoreProjectPackage(
        ProjectPackage projectPackage)
    {
        _folders.Clear();
        _collections.Clear();

        _folders.AddRange(
            projectPackage.Folders.Select(
                folder =>
                    new FolderData
                    {
                        Id =
                            folder.Id,

                        Key =
                            folder.Key,

                        ParentKey =
                            folder.ParentKey,

                        Name =
                            folder.Name,

                        TestTypeKey =
                            folder.TestTypeKey,

                        IsSystem =
                            folder.IsSystem,

                        IsProtected =
                            folder.IsProtected,

                        RequiresManagerRole =
                            folder.RequiresManagerRole,

                        CreatedByLogin =
                            folder.CreatedByLogin,

                        SortOrder =
                            folder.SortOrder
                    }));

        foreach (var collectionModel in
                 projectPackage.Collections)
        {
            var collection =
                new TestCollectionData
                {
                    Id =
                        collectionModel.Id,

                    Key =
                        collectionModel.Key,

                    ParentFolderKey =
                        collectionModel.ParentFolderKey,

                    Name =
                        collectionModel.Name,

                    Description =
                        collectionModel.Description,

                    TestTypeKey =
                        collectionModel.TestTypeKey,

                    IsSystem =
                        collectionModel.IsSystem,

                    IsProtected =
                        collectionModel.IsProtected,

                    RequiresManagerRole =
                        collectionModel.RequiresManagerRole,

                    CreatedByLogin =
                        collectionModel.CreatedByLogin,

                    SortOrder =
                        collectionModel.SortOrder
                };

            collection.Cases.AddRange(
                projectPackage.TestCases
                    .Where(
                        testCase =>
                            string.Equals(
                                testCase.CollectionKey,
                                collectionModel.Key,
                                StringComparison.OrdinalIgnoreCase))
                    .OrderBy(
                        testCase =>
                            testCase.SortOrder)
                    .Select(
                        testCase =>
                            new TestCaseData
                            {
                                Id =
                                    testCase.Id,

                                Name =
                                    testCase.Name,

                                IsSystem =
                                    testCase.IsSystem,

                                IsProtected =
                                    testCase.IsProtected,

                                CreatedByLogin =
                                    testCase.CreatedByLogin,

                                SortOrder =
                                    testCase.SortOrder,

                                Status =
                                    testCase.Status,

                                Comment =
                                    testCase.Comment,

                                Summary = testCase.Summary,
                                Preconditions = testCase.Preconditions,
                                ExternalId = testCase.ExternalId,
                                SourceVersion = testCase.SourceVersion,
                                Importance = testCase.Importance,
                                ExecutionType = testCase.ExecutionType,
                                EstimatedDuration = testCase.EstimatedDuration,
                                Platforms = testCase.Platforms.ToList(),
                                Steps = testCase.Steps.Select(CloneStep).ToList()
                            }));

            _collections.Add(
                collection);
        }

        _selectedFolder =
            null;

        _selectedCollection =
            null;

        _selectedTestCase =
            null;
    }

    private async Task MergeImportedProjectAsync(
        ProjectPackage projectPackage,
        bool overwriteStatuses)
    {
        var storedData =
            await _jsonStorageService.LoadAsync();

        foreach (var importedFolder in projectPackage.Folders)
        {
            var existingFolder =
                _folders.FirstOrDefault(
                    folder =>
                        string.Equals(
                            folder.Key,
                            importedFolder.Key,
                            StringComparison.OrdinalIgnoreCase));

            if (existingFolder is not null)
            {
                continue;
            }

            var folderId =
                importedFolder.Id == Guid.Empty
                    ? Guid.NewGuid()
                    : importedFolder.Id;

            var folder =
                new FolderData
                {
                    Id =
                        folderId,

                    Key =
                        importedFolder.Key,

                    ParentKey =
                        importedFolder.ParentKey,

                    Name =
                        importedFolder.Name,

                    TestTypeKey =
                        importedFolder.TestTypeKey,

                    IsSystem =
                        false,

                    IsProtected =
                        false,

                    RequiresManagerRole =
                        importedFolder.RequiresManagerRole,

                    CreatedByLogin =
                        importedFolder.CreatedByLogin,

                    SortOrder =
                        importedFolder.SortOrder
                };

            _folders.Add(
                folder);

            storedData.Folders.Add(
                new TestSectionModel
                {
                    Id =
                        folder.Id,

                    ProjectKey =
                        _projectKey,

                    TestTypeKey =
                        folder.TestTypeKey,

                    SectionKey =
                        folder.Key,

                    ParentSectionKey =
                        folder.ParentKey,

                    Name =
                        folder.Name,

                    IsSystem =
                        false,

                    RequiresManagerRole =
                        folder.RequiresManagerRole,

                    CreatedByLogin =
                        folder.CreatedByLogin,

                    SortOrder =
                        folder.SortOrder
                });
        }

        foreach (var importedCollection in projectPackage.Collections)
        {
            var existingCollection =
                _collections.FirstOrDefault(
                    collection =>
                        string.Equals(
                            collection.Key,
                            importedCollection.Key,
                            StringComparison.OrdinalIgnoreCase));

            if (existingCollection is not null)
            {
                existingCollection.Description =
                    importedCollection.Description ?? string.Empty;

                var storedCollection =
                    storedData.Collections.FirstOrDefault(
                        collection =>
                            string.Equals(
                                collection.ProjectKey,
                                _projectKey,
                                StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(
                                collection.CollectionKey,
                                existingCollection.Key,
                                StringComparison.OrdinalIgnoreCase));

                if (storedCollection is not null)
                {
                    storedCollection.Description =
                        existingCollection.Description;
                }

                continue;
            }

            var collectionId =
                importedCollection.Id == Guid.Empty
                    ? Guid.NewGuid()
                    : importedCollection.Id;

            var collection =
                new TestCollectionData
                {
                    Id =
                        collectionId,

                    Key =
                        importedCollection.Key,

                    ParentFolderKey =
                        importedCollection.ParentFolderKey,

                    Name =
                        importedCollection.Name,

                    Description =
                        importedCollection.Description ?? string.Empty,

                    Path =
                        BuildFolderPath(
                            importedCollection.ParentFolderKey),

                    TestTypeKey =
                        importedCollection.TestTypeKey,

                    IsSystem =
                        false,

                    IsProtected =
                        false,

                    RequiresManagerRole =
                        importedCollection.RequiresManagerRole,

                    CreatedByLogin =
                        importedCollection.CreatedByLogin,

                    SortOrder =
                        importedCollection.SortOrder
                };

            _collections.Add(
                collection);

            storedData.Collections.Add(
                new TestCollectionModel
                {
                    Id =
                        collection.Id,

                    ProjectKey =
                        _projectKey,

                    TestTypeKey =
                        collection.TestTypeKey,

                    ParentFolderKey =
                        collection.ParentFolderKey,

                    CollectionKey =
                        collection.Key,

                    Name =
                        collection.Name,

                    Description =
                        collection.Description,

                    IsSystem =
                        false,

                    RequiresManagerRole =
                        collection.RequiresManagerRole,

                    CreatedByLogin =
                        collection.CreatedByLogin,

                    SortOrder =
                        collection.SortOrder
                });
        }

        foreach (var importedTestCase in projectPackage.TestCases)
        {
            var collection =
                _collections.FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Key,
                            importedTestCase.CollectionKey,
                            StringComparison.OrdinalIgnoreCase));

            if (collection is null)
            {
                continue;
            }

            var existingTestCase =
                collection.Cases.FirstOrDefault(
                    testCase =>
                        testCase.Id ==
                        importedTestCase.Id);

            if (existingTestCase is not null)
            {
                if (overwriteStatuses)
                {
                    existingTestCase.Status =
                        importedTestCase.Status;
                }

                continue;
            }

            var testCaseId =
                importedTestCase.Id == Guid.Empty
                    ? Guid.NewGuid()
                    : importedTestCase.Id;

            var testCase =
                new TestCaseData
                {
                    Id =
                        testCaseId,

                    Number =
                        collection.Cases.Count + 1,

                    Name =
                        importedTestCase.Name,

                    IsSystem =
                        false,

                    IsProtected =
                        false,

                    CreatedByLogin =
                        importedTestCase.CreatedByLogin,

                    SortOrder =
                        importedTestCase.SortOrder,

                    Status =
                        overwriteStatuses
                            ? importedTestCase.Status
                            : StatusNone,

                    Comment =
                        overwriteStatuses
                            ? importedTestCase.Comment
                            : string.Empty,

                    Summary = importedTestCase.Summary,
                    Preconditions = importedTestCase.Preconditions,
                    ExternalId = importedTestCase.ExternalId,
                    SourceVersion = importedTestCase.SourceVersion,
                    Importance = importedTestCase.Importance,
                    ExecutionType = importedTestCase.ExecutionType,
                    EstimatedDuration = importedTestCase.EstimatedDuration,
                    Platforms = importedTestCase.Platforms.ToList(),
                    Steps = importedTestCase.Steps.Select(CloneStep).ToList()
                };

            collection.Cases.Add(
                testCase);

            storedData.TestCases.Add(
                new TestCaseModel
                {
                    Id =
                        testCase.Id,

                    ProjectKey =
                        _projectKey,

                    TestTypeKey =
                        collection.TestTypeKey,

                    SectionKey =
                        collection.Key,

                    Name =
                        testCase.Name,

                    CreatedByLogin =
                        testCase.CreatedByLogin,

                    SortOrder =
                        testCase.SortOrder,

                    Status =
                        testCase.Status,

                    Comment =
                        testCase.Comment,

                    Summary = testCase.Summary,
                    Preconditions = testCase.Preconditions,
                    ExternalId = testCase.ExternalId,
                    SourceVersion = testCase.SourceVersion,
                    Importance = testCase.Importance,
                    ExecutionType = testCase.ExecutionType,
                    EstimatedDuration = testCase.EstimatedDuration,
                    Platforms = testCase.Platforms.ToList(),
                    Steps = testCase.Steps.Select(CloneStep).ToList()
                });
        }

        if (overwriteStatuses)
        {
            foreach (var collection in _collections)
            {
                foreach (var testCase in collection.Cases)
                {
                    var importedTestCase =
                        projectPackage.TestCases.FirstOrDefault(
                            item =>
                                item.Id ==
                                testCase.Id);

                    if (importedTestCase is null)
                    {
                        continue;
                    }

                    testCase.Status =
                        importedTestCase.Status;

                    var storedTestCase =
                        storedData.TestCases.FirstOrDefault(
                            item =>
                                item.Id ==
                                testCase.Id);

                    if (storedTestCase is not null)
                    {
                        storedTestCase.Status =
                            testCase.Status;
                    }
                }
            }
        }

        await _jsonStorageService.SaveAsync(
            storedData);

        RefreshCollectionPaths();
        BuildTestTree();
        UpdateSessionSummary();

        if (_currentCollectionIndex >= 0)
        {
            RenderCurrentCollectionCases();
            UpdateCurrentCollectionProgress();
            UpdateActiveCollectionHighlight();
        }
    }

    private string CreateExportFileName()
    {
        var projectName =
            SanitizeFileNamePart(
                _projectName);

        var applicationVersion =
            SanitizeFileNamePart(
                _applicationVersion);

        var testerName =
            SanitizeFileNamePart(
                _testerName);

        return string.Join(
                   "_",
                   new[]
                   {
                       projectName,
                       applicationVersion,
                       testerName,
                       DateTime.Now.ToString(
                           "yyyy-MM-dd")
                   }
                   .Where(
                       part =>
                           !string.IsNullOrWhiteSpace(part)))
               + ".json";
    }

    private string CreateReportFileNameBase(
        string applicationVersion)
    {
        return string.Join(
                   "_",
                   new[]
                   {
                       "RAPORT",
                       SanitizeFileNamePart(
                           _projectName),
                       SanitizeFileNamePart(
                           applicationVersion),
                       SanitizeFileNamePart(
                           _loggedInLogin),
                       DateTime.Now.ToString(
                           "yyyy-MM-dd_HH-mm-ss")
                   }
                   .Where(
                       part =>
                           !string.IsNullOrWhiteSpace(part)))
               ;
    }

    private static string SanitizeFileNamePart(
        string value)
    {
        var invalidCharacters =
            Path.GetInvalidFileNameChars();

        var cleanedCharacters =
            value
                .Trim()
                .ToUpperInvariant()
                .Select(
                    character =>
                        invalidCharacters.Contains(character) ||
                        char.IsWhiteSpace(character)
                            ? '_'
                            : character)
                .ToArray();

        return new string(
                cleanedCharacters)
            .Trim('_');
    }

    private async void ThemeToggleButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var ownerWindow =
            TopLevel.GetTopLevel(this)
            as Window;

        if (ownerWindow is null)
        {
            return;
        }

        var dialog =
            new ApplicationSettingsWindow();

        await dialog.ShowDialog<bool>(
            ownerWindow);

        _isDarkMode =
            Application.Current?.RequestedThemeVariant ==
            ThemeVariant.Dark;

        UpdateThemeButton();

        RefreshRoleBadges();
    }

    private async void LogoutButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var ownerWindow =
            TopLevel.GetTopLevel(this)
            as Window;

        if (ownerWindow is null)
        {
            return;
        }

        var dialog =
            new ConfirmLogoutWindow(
                _loggedInLogin);

        var confirmed =
            await dialog.ShowDialog<bool>(
                ownerWindow);

        if (!confirmed)
        {
            return;
        }

        _logoutAction?.Invoke();
    }

    private async void RefreshButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        if (_refreshIndicatorBorder is not null)
        {
            _refreshIndicatorBorder.IsVisible = true;
        }
        _refreshIndicatorTimer.Start();

        try
        {
            _userDataLoaded = false;
            await LoadUserDataAsync();
            await RefreshAssignmentAndNotificationStateAsync();
            UpdateSessionSummary();
        }
        finally
        {
            _refreshIndicatorTimer.Stop();
            if (_refreshIndicatorRotateTransform is not null)
            {
                _refreshIndicatorRotateTransform.Angle = 0;
            }
            if (_refreshIndicatorBorder is not null)
            {
                _refreshIndicatorBorder.IsVisible = false;
            }
            _isRefreshing = false;
        }
    }

    private void LoggedInUserPanel_OnPointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        if (sender is not Control control ||
            !e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            return;
        }

        control.ContextMenu?.Open(control);
        e.Handled = true;
    }

    private void BuildRoleBadges()
    {
        if (_roleBadgesPanel is null)
        {
            return;
        }

        RefreshRoleBadges();

        if (_roleBadgesScrollViewer is not null)
        {
            _roleBadgesScrollViewer.PointerWheelChanged +=
                (_, eventArgs) =>
                {
                    var current =
                        _roleBadgesScrollViewer.Offset;

                    _roleBadgesScrollViewer.Offset =
                        new Vector(
                            Math.Max(
                                0,
                                current.X -
                                eventArgs.Delta.Y * 34),
                            current.Y);

                    eventArgs.Handled =
                        true;
                };

            EnableRoleBadgeDragScrolling(
                _roleBadgesScrollViewer);
        }
    }

    private void RefreshRoleBadges()
    {
        if (_roleBadgesPanel is null)
        {
            return;
        }

        var scopedProjectRoles =
            _projectRoleScopeLoaded
                ? _projectRoles.Where(
                    role =>
                        _currentProjectRoleNames.Contains(role))
                : Enumerable.Empty<string>();

        var roles = scopedProjectRoles
            .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Nie chowaj ról na podstawie samej szerokości okna. Ilość miejsca
        // zajmowana przez akcje po lewej zmienia się zależnie od trybu pracy.
        var visibleHeaderActions =
            new[]
            {
                _executeAssignedTestsButton,
                _restartAssignedTestsButton
            }.Where(button => button?.IsVisible == true).ToArray();

        const double fixedHeaderWidth = 620;
        const double overflowButtonWidth = 48;
        const double maximumRolePanelWidth = 900;

        var headerActionsWidth =
            visibleHeaderActions.Sum(
                button =>
                    Math.Max(
                        button!.Bounds.Width,
                        Math.Max(
                            button.DesiredSize.Width,
                            120))) +
            Math.Max(
                0,
                visibleHeaderActions.Length - 1) * 6;

        var roleSpace =
            Math.Min(
                maximumRolePanelWidth,
                Math.Max(
                    0,
                    Bounds.Width -
                    fixedHeaderWidth -
                    headerActionsWidth));

        static double EstimateBadgeWidth(string role) =>
            Math.Max(58, 34 + role.Length * 7.4);

        var allRolesWidth =
            roles.Sum(EstimateBadgeWidth) +
            Math.Max(0, roles.Length - 1) * 6;

        var visibleCount = roles.Length;

        if (allRolesWidth > roleSpace)
        {
            visibleCount = 0;
            var usedWidth = overflowButtonWidth;

            foreach (var role in roles)
            {
                var badgeWidth =
                    EstimateBadgeWidth(role) +
                    (visibleCount > 0 ? 6 : 0);

                if (usedWidth + badgeWidth > roleSpace)
                {
                    break;
                }

                usedWidth += badgeWidth;
                visibleCount++;
            }

            // Najważniejsza rola pozostaje czytelna nawet w wąskim oknie.
            visibleCount =
                Math.Min(
                    roles.Length,
                    Math.Max(1, visibleCount));
        }

        _visibleRoleBadgeCount =
            visibleCount;

        _roleBadgesPanel.Children.Clear();
        _hiddenRoleBadgesPanel?.Children.Clear();

        foreach (var role in roles.Take(visibleCount))
        {
            _roleBadgesPanel.Children.Add(
                CreateRoleBadge(
                    role));
        }

        var hiddenRoles =
            roles.Skip(visibleCount).ToArray();

        if (_roleOverflowButton is not null)
        {
            _roleOverflowButton.IsVisible =
                hiddenRoles.Length > 0;

            ToolTip.SetTip(
                _roleOverflowButton,
                hiddenRoles.Length > 0
                    ? $"Pozostałe role: {string.Join(", ", hiddenRoles)}"
                    : null);
        }

        if (_hiddenRoleBadgesPanel is not null)
        {
            foreach (var role in hiddenRoles)
            {
                _hiddenRoleBadgesPanel.Children.Add(
                    CreateRoleBadge(role));
            }
        }

        return;
    }

    private static void EnableRoleBadgeDragScrolling(
        ScrollViewer scrollViewer)
    {
        Point? dragStart = null;
        Vector startOffset = default;

        scrollViewer.PointerPressed +=
            (_, eventArgs) =>
            {
                var point =
                    eventArgs.GetCurrentPoint(scrollViewer);

                if (!point.Properties.IsLeftButtonPressed)
                {
                    return;
                }

                dragStart = point.Position;
                startOffset = scrollViewer.Offset;
                eventArgs.Pointer.Capture(scrollViewer);
            };

        scrollViewer.PointerMoved +=
            (_, eventArgs) =>
            {
                if (dragStart is null)
                {
                    return;
                }

                var current = eventArgs.GetPosition(scrollViewer);
                scrollViewer.Offset = new Vector(
                    Math.Max(0, startOffset.X + dragStart.Value.X - current.X),
                    startOffset.Y);
            };

        scrollViewer.PointerReleased +=
            (_, eventArgs) =>
            {
                dragStart = null;
                eventArgs.Pointer.Capture(null);
            };
    }

    private Border CreateRoleBadge(
        string role)
    {
        var isAdmin =
            string.Equals(
                role,
                "Admin",
                StringComparison.OrdinalIgnoreCase);

        var (
            background,
            border,
            foreground) =
            role switch
            {
                "Admin" =>
                    ("#5B2530", "#D2A447", "#FFF2CC"),

                "Przełożony" =>
                    ("#8C671C", "#6B4C0F", "#FFF0BD"),

                "Pracownik" =>
                    ("#315F86", "#1E4F78", "#E4F2FC"),

                _ =>
                    ("#315F86", "#1E4F78", "#E4F2FC")
            };

        if (!isAdmin &&
            _projectRoleColors.TryGetValue(role, out var customBorderColor))
        {
            border = customBorderColor;
        }

        if (!isAdmin &&
            _projectRoleBackgroundColors.TryGetValue(role, out var customBackgroundColor))
        {
            background = customBackgroundColor;
        }

        if (!isAdmin &&
            _projectRoleTextColors.TryGetValue(role, out var customTextColor))
        {
            foreground = customTextColor;
        }

        return new Border
        {
            Height = isAdmin ? 40 : 38,
            Padding = new Thickness(isAdmin ? 15 : 13, 0),
            Background = new SolidColorBrush(Color.Parse(background)),
            BorderBrush = new SolidColorBrush(Color.Parse(border)),
            BorderThickness = new Thickness(isAdmin ? 2 : 1),
            CornerRadius = new CornerRadius(isAdmin ? 13 : 12),
            Child = new TextBlock
            {
                Text = isAdmin ? "★  Admin" : role,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = isAdmin ? FontWeight.Bold : FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse(foreground))
            }
        };
    }

    private async Task LoadProjectRoleColorsAsync()
    {
        var definitions = await _userProfileService.GetRoleAndProjectDefinitionsAsync();
        _projectRoleColors.Clear();
        _projectRoleBackgroundColors.Clear();
        _projectRoleTextColors.Clear();
        _currentProjectRoleNames.Clear();

        var currentProject =
            definitions.Projects.FirstOrDefault(
                project =>
                    string.Equals(
                        project.Name,
                        _projectName,
                        StringComparison.OrdinalIgnoreCase));

        foreach (var role in definitions.Roles)
        {
            _projectRoleColors[role.Name] = role.BorderColor;
            if (!string.IsNullOrWhiteSpace(role.BackgroundColor))
            {
                _projectRoleBackgroundColors[role.Name] = role.BackgroundColor;
            }
            if (!string.IsNullOrWhiteSpace(role.TextColor))
            {
                _projectRoleTextColors[role.Name] = role.TextColor;
            }

            if (currentProject is not null &&
                role.ProjectKeys.Contains(
                    currentProject.Key,
                    StringComparer.OrdinalIgnoreCase))
            {
                _currentProjectRoleNames.Add(role.Name);
            }
        }

        _projectRoleScopeLoaded = true;
        RefreshRoleBadges();
    }

    private async Task ReloadRoleBadgesAsync()
    {
        var profiles =
            await _userProfileService.GetProfilesAsync();
        var currentProfile = profiles.FirstOrDefault(profile =>
            string.Equals(
                profile.Login,
                _loggedInLogin,
                StringComparison.OrdinalIgnoreCase));

        if (currentProfile is not null)
        {
            _projectRoles = currentProfile.ProjectRoles
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        await LoadProjectRoleColorsAsync();
    }

    private void UpdateThemeButton()
    {
        if (_themeIconTextBlock is null ||
            _themeToggleButton is null)
        {
            return;
        }

        if (_projectToolsButton is not null)
        {
            ToolTip.SetTip(
                _projectToolsButton,
                LocalizationService.T("Common.Settings"));
        }

        if (_isDarkMode)
        {
            _themeIconTextBlock.Text =
                "☀";

            ToolTip.SetTip(
                _themeToggleButton,
                LocalizationService.T("Common.ApplicationSettings"));
        }
        else
        {
            _themeIconTextBlock.Text =
                "☾";

            ToolTip.SetTip(
                _themeToggleButton,
                LocalizationService.T("Common.ApplicationSettings"));
        }
    }

    private static string GetFolderDisplayName(FolderData folder) =>
        folder.Key switch
        {
            "regression-root" => LocalizationService.T("Explorer.RegressionTests"),
            "functional-root" => LocalizationService.T("Explorer.FunctionalTests"),
            _ => folder.Name
        };

    private void LocalizationService_OnLanguageChanged(
        object? sender,
        EventArgs e)
    {
        if (_loggedInUserTextBlock is not null)
        {
            _loggedInUserTextBlock.Text =
                string.Format(
                    LocalizationService.T("Common.LoggedIn"),
                    _loggedInLogin);
        }

        if (_welcomePanel?.IsVisible == true)
        {
            if (_welcomeTitleTextBlock is not null)
            {
                _welcomeTitleTextBlock.Text =
                    LocalizationService.T("Explorer.SelectTestType");
            }

            if (_welcomeDescriptionTextBlock is not null)
            {
                _welcomeDescriptionTextBlock.Text =
                    LocalizationService.T("Explorer.SelectCollectionDescription");
            }
        }

        if (_executeAssignedTestsLabel is not null)
        {
            _executeAssignedTestsLabel.Text =
                LocalizationService.T(
                    _activeAssignmentCaseIds is null
                        ? "Assignment.ExecuteTests"
                        : "Assignment.InterruptTests");
        }

        if (_finishEarlyButton is not null)
        {
            _finishEarlyButton.Content =
                LocalizationService.T(
                    _activeAssignmentCaseIds is null
                        ? "Explorer.FinishAndReport"
                        : "Assignment.FinishTests");

            ToolTip.SetTip(
                _finishEarlyButton,
                LocalizationService.T(
                    _activeAssignmentCaseIds is null
                        ? "Explorer.FinishAndReportTip"
                        : "Assignment.FinishTip"));
        }

        if (_activeAssignmentCaseIds is not null &&
            _testTreeTitleTextBlock is not null)
        {
            _testTreeTitleTextBlock.Text =
                LocalizationService.T("Assignment.ExecuteTests");
        }

        RefreshCollectionPaths();
        BuildTestTree();

        var currentCollection =
            GetCurrentCollection();

        if (currentCollection is not null)
        {
            ShowCurrentCollectionHeader(
                currentCollection);
        }

        UpdateSessionSummary();
        UpdateNavigationButtons();
        UpdateThemeButton();
    }

    private sealed record FolderDeletionStats(
        int FolderCount,
        int CollectionCount,
        int TestCaseCount)
    {
        public int TotalItems =>
            FolderCount +
            CollectionCount +
                   TestCaseCount;
    }

    private abstract record StructureClipboardItem;

    private sealed record FolderClipboardItem(
        string Name,
        string TestTypeKey,
        IReadOnlyList<FolderClipboardItem> ChildFolders,
        IReadOnlyList<CollectionClipboardItem> Collections)
        : StructureClipboardItem;

    private sealed record CollectionClipboardItem(
        string Name,
        string Description,
        IReadOnlyList<TestCaseClipboardItem> TestCases)
        : StructureClipboardItem;

    private sealed record TestCaseClipboardItem(
        string Name,
        string Summary = "",
        string Preconditions = "",
        string ExternalId = "",
        string SourceVersion = "",
        string Importance = "",
        string ExecutionType = "",
        string EstimatedDuration = "",
        IReadOnlyList<string>? Platforms = null,
        IReadOnlyList<TestStepModel>? Steps = null)
        : StructureClipboardItem;

    private sealed record UndoHistoryEntry(
        ProjectPackage Package,
        string? CurrentCollectionKey);

    private enum TreeDropZone
    {
        None,
        Before,
        Inside,
        After
    }

    private enum TreePanelState
    {
        Full,
        Compact,
        Collapsed
    }

    private sealed class FolderData
    {
        public Guid Id { get; init; }

        public string Key { get; init; } =
            string.Empty;

        public string ParentKey { get; set; } =
            string.Empty;

        public string Name { get; set; } =
            string.Empty;

        public string CreatedByLogin { get; set; } =
            string.Empty;

        public string TestTypeKey { get; set; } =
            string.Empty;

        public bool IsSystem { get; init; }

        public bool IsProtected { get; init; }

        public bool RequiresManagerRole { get; init; }

        public int SortOrder { get; set; }

        public TreeViewItem? TreeItem { get; set; }
    }

    private sealed class TestCollectionData
    {
        public Guid Id { get; init; }

        public string Key { get; init; } =
            string.Empty;

        public string ParentFolderKey { get; set; } =
            string.Empty;

        public string Name { get; set; } =
            string.Empty;

        public string Path { get; set; } =
            string.Empty;

        public string Description { get; set; } =
            string.Empty;

        public string CreatedByLogin { get; set; } =
            string.Empty;

        public string TestTypeKey { get; set; } =
            string.Empty;

        public bool IsSystem { get; init; }

        public bool IsProtected { get; init; }

        public bool RequiresManagerRole { get; init; }

        public int SortOrder { get; set; }

        public List<TestCaseData> Cases { get; init; } =
            new();

        public TreeViewItem? TreeItem { get; set; }

        public Border? HeaderBorder { get; set; }

        public Border? ActiveIndicator { get; set; }

        public TextBlock? StateIcon { get; set; }

        public TextBlock? ProgressText { get; set; }
    }

    private sealed class TestCaseData
    {
        public Guid Id { get; init; }

        public int Number { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public string CreatedByLogin { get; set; } =
            string.Empty;

        public bool IsSystem { get; init; }

        public bool IsProtected { get; init; }

        public int SortOrder { get; set; }

        public string Status { get; set; } =
            StatusNone;

        public string Comment { get; set; } =
            string.Empty;

        public string Summary { get; set; } = string.Empty;
        public string Preconditions { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public string SourceVersion { get; set; } = string.Empty;
        public string Importance { get; set; } = string.Empty;
        public string ExecutionType { get; set; } = string.Empty;
        public string EstimatedDuration { get; set; } = string.Empty;
        public List<string> Platforms { get; set; } = new();
        public List<TestStepModel> Steps { get; set; } = new();
    }
}

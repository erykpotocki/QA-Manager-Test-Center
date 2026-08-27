using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using QARegressionManager.Models;
using QARegressionManager.Services;

namespace QARegressionManager.Views;

public partial class AssignmentManagementWindow : Window
{
    private const string RegressionTestType = "regression";
    private const string FunctionalTestType = "functional";
    private readonly string _projectKey;
    private readonly string _projectName;
    private readonly string _assignedByLogin;
    private readonly IReadOnlyList<AssignmentCaseOption> _caseOptions;
    private readonly bool _includeTestProfiles;
    private readonly AssignmentService _assignmentService =
        new();
    private readonly UserProfileService _profileService =
        new();
    private readonly Dictionary<Guid, CheckBox> _caseCheckBoxes =
        new();
    private readonly Dictionary<string, CheckBox> _groupCheckBoxes =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<Guid> _selectedCaseIds =
        new();
    private readonly List<AssignmentDraft> _drafts =
        new();
    private readonly AssignmentInputPresets _inputPresets =
        AssignmentInputPresetService.Load();

    private TestAssignmentModel[] _activeAssignments =
        Array.Empty<TestAssignmentModel>();
    private bool _synchronizingCheckBoxes;
    private bool _loadingData;
    private bool _sendingAssignments;
    private Control? _inlineContent;
    private Func<Task>? _inlineCloseAction;

    public bool DataChanged { get; private set; }

    public AssignmentManagementWindow()
        : this(
            "PROJECT",
            "Projekt",
            "administrator",
            Array.Empty<AssignmentCaseOption>(),
            true)
    {
    }

    public AssignmentManagementWindow(
        string projectKey,
        string projectName,
        string assignedByLogin,
        IReadOnlyList<AssignmentCaseOption> caseOptions,
        bool includeTestProfiles = true)
    {
        InitializeComponent();

        _projectKey = projectKey;
        _projectName = projectName;
        _assignedByLogin = assignedByLogin;
        _caseOptions = caseOptions;
        _includeTestProfiles = includeTestProfiles;

        AssignmentComboBox.SelectionChanged +=
            (_, _) =>
            {
                if (!_loadingData)
                {
                    LoadSelectedAssignment();
                }
            };

        TestTypeComboBox.SelectionChanged +=
            (_, _) =>
            {
                if (_loadingData)
                {
                    return;
                }

                _selectedCaseIds.Clear();
                ResultTextBlock.IsVisible =
                    false;
                BuildCases();
            };

        SearchTextBox.TextChanged +=
            (_, _) =>
                BuildCases();

        VersionPresetComboBox.SelectionChanged +=
            (_, _) =>
                Dispatcher.UIThread.Post(
                    UpdatePresetDeleteButtons,
                    DispatcherPriority.Background);

        SessionNamePresetComboBox.SelectionChanged +=
            (_, _) =>
                Dispatcher.UIThread.Post(
                    UpdatePresetDeleteButtons,
                    DispatcherPriority.Background);

        VersionPresetComboBox.LostFocus +=
            (_, _) =>
                UpdatePresetDeleteButtons();

        SessionNamePresetComboBox.LostFocus +=
            (_, _) =>
                UpdatePresetDeleteButtons();

        RememberVersionCheckBox.Click +=
            (_, _) =>
                SaveCheckedPresets();

        RememberSessionNameCheckBox.Click +=
            (_, _) =>
                SaveCheckedPresets();

        RefreshPresetItems();

        SelectAllCheckBox.Click +=
            (_, _) =>
            {
                if (_synchronizingCheckBoxes)
                {
                    return;
                }

                var visibleIds =
                    _caseCheckBoxes.Keys.ToArray();

                var shouldSelect =
                    visibleIds.Any(
                        caseId =>
                            !_selectedCaseIds.Contains(
                                caseId));

                SetVisibleCasesChecked(
                    shouldSelect);
            };

        Opened +=
            async (_, _) =>
                await LoadDataAsync();
    }

    public async Task<Control?> TakeInlineContentAsync(
        Func<Task> closeAction)
    {
        if (Content is not Control content)
        {
            return null;
        }

        Content = null;
        _inlineContent = content;
        _inlineCloseAction = closeAction;

        await LoadDataAsync();

        return content;
    }

    public void ReleaseInlineContent()
    {
        _inlineContent = null;
        _inlineCloseAction = null;
    }

    private async Task LoadDataAsync()
    {
        _loadingData =
            true;

        try
        {
            var profiles =
                await _profileService.GetProfilesAsync();

            RecipientComboBox.Items.Clear();

            foreach (var profile in
                     profiles.Where(
                         profile =>
                             _includeTestProfiles ||
                             !IsTestProfileLogin(
                                 profile.Login)))
            {
                RecipientComboBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = profile.Login,
                        Tag = profile.Login
                    });
            }

            _activeAssignments =
                await _assignmentService.GetActiveAssignmentsForProjectAsync(
                    _projectKey);

            TestTypeComboBox.Items.Clear();

            foreach (var testType in
                     _caseOptions
                         .Select(GetTestTypeName)
                         .Where(testType =>
                             !string.IsNullOrWhiteSpace(testType))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                TestTypeComboBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = GetTestTypeDisplayName(testType),
                        Tag = testType
                    });
            }

            TestTypeComboBox.SelectedIndex =
                -1;

            AssignmentComboBox.Items.Clear();
            AssignmentComboBox.Items.Add(
                new ComboBoxItem
                {
                    Content = LocalizationService.T("Assignment.NewSession")
                });

            foreach (var assignment in _activeAssignments)
            {
                AssignmentComboBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = LocalizationService.Format(
                            "Assignment.SessionListItem",
                            string.IsNullOrWhiteSpace(assignment.SessionName)
                                ? assignment.RecipientLogin
                                : assignment.SessionName,
                            assignment.ApplicationVersion,
                            assignment.TestCaseIds.Count),
                        Tag = assignment
                    });
            }

            AssignmentComboBox.SelectedIndex =
                0;

            if (RecipientComboBox.SelectedIndex < 0)
            {
                RecipientComboBox.SelectedIndex =
                    RecipientComboBox.ItemCount > 0
                        ? 0
                        : -1;
            }

            WithdrawButton.IsVisible =
                true;

            WithdrawButton.IsEnabled =
                false;

            _selectedCaseIds.Clear();
            BuildCases();
        }
        finally
        {
            _loadingData =
                false;
        }
    }

    private static bool IsTestProfileLogin(
        string login)
    {
        return login.StartsWith(
                   "tester",
                   StringComparison.OrdinalIgnoreCase) &&
               int.TryParse(
                   login["tester".Length..],
                   out _);
    }

    private void BuildCases()
    {
        CasesPanel.Children.Clear();
        _caseCheckBoxes.Clear();
        _groupCheckBoxes.Clear();

        var query =
            SearchTextBox.Text?.Trim();

        var selectedTestType =
            GetSelectedTestType();

        SearchTextBox.IsEnabled =
            !string.IsNullOrWhiteSpace(selectedTestType);

        SelectAllCheckBox.IsEnabled =
            !string.IsNullOrWhiteSpace(selectedTestType);

        if (string.IsNullOrWhiteSpace(selectedTestType))
        {
            SelectAllCheckBox.IsChecked =
                false;

            CasesPanel.Children.Add(
                new TextBlock
                {
                    Text = LocalizationService.T("Assignment.SelectTypeHint"),
                    Margin = new Thickness(4, 14),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.Gray
                });

            UpdateAssignmentCounts();
            return;
        }

        var availableOptions =
            GetAvailableCaseOptions()
                .Where(
                    option =>
                        string.Equals(
                            GetTestTypeName(option),
                            selectedTestType,
                            StringComparison.OrdinalIgnoreCase))
                .Where(
                    option =>
                        string.IsNullOrWhiteSpace(query) ||
                        option.CollectionName.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase) ||
                        option.TestCaseName.Contains(
                            query,
                            StringComparison.OrdinalIgnoreCase))
                .ToArray();

        foreach (var group in
                 availableOptions.GroupBy(
                     option =>
                         option.CollectionName))
        {
            var groupCheckBox =
                new CheckBox
                {
                    Content = GetCollectionDisplayName(
                        group.Key,
                        selectedTestType),
                    IsThreeState = true,
                    Margin = new Thickness(0),
                    FontSize = 14,
                    FontWeight = FontWeight.Bold
                };

            _groupCheckBoxes[group.Key] =
                groupCheckBox;

            var groupIds =
                group.Select(
                        option =>
                            option.Id)
                    .ToArray();

            var expander =
                new Expander
                {
                    Header = groupCheckBox,
                    IsExpanded =
                        !string.IsNullOrWhiteSpace(query),
                    HorizontalAlignment =
                        HorizontalAlignment.Stretch
                };

            groupCheckBox.Click +=
                (_, _) =>
                {
                    if (_synchronizingCheckBoxes)
                    {
                        return;
                    }

                    var shouldSelect =
                        groupIds.Any(
                            caseId =>
                                !_selectedCaseIds.Contains(
                                    caseId));

                    SetCasesChecked(
                        groupIds,
                        shouldSelect);

                    if (shouldSelect)
                    {
                        expander.IsExpanded =
                            false;

                        Dispatcher.UIThread.Post(
                            () => ScrollToNextAssignmentGroup(
                                expander),
                            DispatcherPriority.Background);
                    }
                };

            var casePanel =
                new StackPanel
                {
                    Spacing = 5,
                    Margin = new Thickness(12, 4, 0, 8)
                };

            foreach (var option in group)
            {
                var checkBox =
                    new CheckBox
                    {
                        Content = option.TestCaseName,
                        IsChecked = _selectedCaseIds.Contains(
                            option.Id),
                        Margin = new Thickness(20, 2, 0, 2)
                    };

                checkBox.IsCheckedChanged +=
                    (_, _) =>
                    {
                        if (_synchronizingCheckBoxes)
                        {
                            return;
                        }

                        if (checkBox.IsChecked == true)
                        {
                            _selectedCaseIds.Add(
                                option.Id);
                        }
                        else
                        {
                            _selectedCaseIds.Remove(
                                option.Id);
                        }

                        UpdateSelectionIndicators();
                    };

                _caseCheckBoxes[option.Id] =
                    checkBox;

                casePanel.Children.Add(
                    checkBox);
            }

            expander.Content =
                casePanel;

            CasesPanel.Children.Add(
                expander);
        }

        if (availableOptions.Length == 0)
        {
            CasesPanel.Children.Add(
                new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(query)
                        ? !_caseOptions.Any(option =>
                              string.Equals(
                                  GetTestTypeName(option),
                                  selectedTestType,
                                  StringComparison.OrdinalIgnoreCase))
                            ? LocalizationService.T("Assignment.EmptyType")
                            : LocalizationService.T("Assignment.AllAlreadyAssigned")
                        : LocalizationService.T("Assignment.NoSearchResults"),
                    Margin = new Thickness(4, 14),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.Gray
                });
        }

        UpdateSelectionIndicators();
        UpdateAssignmentCounts();
    }

    private string? GetSelectedTestType()
    {
        return (TestTypeComboBox.SelectedItem as ComboBoxItem)?.Tag
            ?.ToString();
    }

    private static string GetTestTypeName(
        AssignmentCaseOption option)
    {
        var rawName = option.CollectionName
            .Split(
                " / ",
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .FirstOrDefault() ??
            string.Empty;

        return rawName.Trim().ToLowerInvariant() switch
        {
            "regression" or "regression tests" or "regresja" or
                "testy regresyjne" or "testy regresji" =>
                RegressionTestType,
            "functional" or "functional tests" or "testy funkcjonalne" or
                "testy funkcyjne" =>
                FunctionalTestType,
            _ => rawName
        };
    }

    private static string GetTestTypeDisplayName(string testType) =>
        testType switch
        {
            RegressionTestType => LocalizationService.T("Explorer.RegressionTests"),
            FunctionalTestType => LocalizationService.T("Explorer.FunctionalTests"),
            _ => testType
        };

    private static string GetCollectionDisplayName(
        string collectionName,
        string selectedTestType)
    {
        var parts = collectionName.Split(
            " / ",
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        if (parts.Length > 1 &&
            string.Equals(
                NormalizeTestTypeName(parts[0]),
                selectedTestType,
                StringComparison.OrdinalIgnoreCase))
        {
            return string.Join(" / ", parts.Skip(1));
        }

        return collectionName;
    }

    private static string NormalizeTestTypeName(string rawName) =>
        rawName.Trim().ToLowerInvariant() switch
        {
            "regression" or "regression tests" or "regresja" or
                "testy regresyjne" or "testy regresji" => RegressionTestType,
            "functional" or "functional tests" or "testy funkcjonalne" or
                "testy funkcyjne" => FunctionalTestType,
            _ => rawName
        };

    private IEnumerable<AssignmentCaseOption> GetAvailableCaseOptions()
    {
        var editedAssignment =
            GetSelectedAssignment();

        var unavailableIds =
            _activeAssignments
                .Where(
                    assignment =>
                        (editedAssignment is null ||
                         assignment.Id !=
                         editedAssignment.Id) &&
                        !_drafts.Any(
                            draft =>
                                draft.AssignmentId ==
                                assignment.Id))
                .SelectMany(
                    assignment =>
                        assignment.TestCaseIds)
                .Concat(
                    _drafts
                        .Where(
                            draft =>
                                editedAssignment is null ||
                                draft.AssignmentId !=
                                editedAssignment.Id)
                        .SelectMany(
                            draft =>
                                draft.TestCaseIds))
                .ToHashSet();

        return _caseOptions.Where(
            option =>
                !unavailableIds.Contains(
                    option.Id));
    }

    private void SetVisibleCasesChecked(
        bool isChecked)
    {
        SetCasesChecked(
            _caseCheckBoxes.Keys,
            isChecked);
    }

    private void ScrollToNextAssignmentGroup(
        Expander selectedGroup)
    {
        var groupIndex =
            CasesPanel.Children.IndexOf(
                selectedGroup);

        if (groupIndex < 0 ||
            groupIndex + 1 >=
            CasesPanel.Children.Count)
        {
            return;
        }

        if (CasesPanel.Children[groupIndex + 1] is not Control nextGroup)
        {
            return;
        }

        CasesScrollViewer.Offset =
            new Vector(
                CasesScrollViewer.Offset.X,
                Math.Max(
                    0,
                    nextGroup.Bounds.Y));
    }

    private void SetCasesChecked(
        IEnumerable<Guid> caseIds,
        bool isChecked)
    {
        _synchronizingCheckBoxes =
            true;

        try
        {
            foreach (var caseId in caseIds)
            {
                if (isChecked)
                {
                    _selectedCaseIds.Add(
                        caseId);
                }
                else
                {
                    _selectedCaseIds.Remove(
                        caseId);
                }

                if (_caseCheckBoxes.TryGetValue(
                        caseId,
                        out var checkBox))
                {
                    checkBox.IsChecked =
                        isChecked;
                }
            }
        }
        finally
        {
            _synchronizingCheckBoxes =
                false;
        }

        UpdateSelectionIndicators();
    }

    private void UpdateSelectionIndicators()
    {
        _synchronizingCheckBoxes =
            true;

        try
        {
            SelectAllCheckBox.IsChecked =
                GetAggregateCheckState(
                    _caseCheckBoxes.Keys);

            foreach (var group in _groupCheckBoxes)
            {
                var groupIds =
                    _caseCheckBoxes.Keys
                        .Where(
                            caseId =>
                                _caseOptions.Any(
                                    option =>
                                        option.Id == caseId &&
                                        string.Equals(
                                            option.CollectionName,
                                            group.Key,
                                            StringComparison.OrdinalIgnoreCase)));

                group.Value.IsChecked =
                    GetAggregateCheckState(
                        groupIds);
            }
        }
        finally
        {
            _synchronizingCheckBoxes =
                false;
        }

        UpdateAssignmentCounts();
    }

    private bool? GetAggregateCheckState(
        IEnumerable<Guid> caseIds)
    {
        var ids =
            caseIds.ToArray();

        if (ids.Length == 0)
        {
            return false;
        }

        var checkedCount =
            ids.Count(
                _selectedCaseIds.Contains);

        return checkedCount switch
        {
            0 => false,
            var count when count == ids.Length => true,
            _ => null
        };
    }

    private void LoadSelectedAssignment()
    {
        var assignment =
            GetSelectedAssignment();

        WithdrawButton.IsVisible =
            true;

        WithdrawButton.IsEnabled =
            assignment is not null;

        _selectedCaseIds.Clear();

        if (assignment is null)
        {
            VersionPresetComboBox.Text =
                string.Empty;

            SessionNamePresetComboBox.Text =
                string.Empty;

            TestTypeComboBox.SelectedIndex =
                -1;
        }
        else
        {
            SessionNamePresetComboBox.Text =
                assignment.SessionName;

            VersionPresetComboBox.Text =
                assignment.ApplicationVersion;

            SelectRecipient(
                assignment.RecipientLogin);

            _selectedCaseIds.UnionWith(
                assignment.TestCaseIds);

            var assignmentTestType =
                _caseOptions
                    .Where(option =>
                        assignment.TestCaseIds.Contains(option.Id))
                    .Select(GetTestTypeName)
                    .FirstOrDefault();

            var wasLoading =
                _loadingData;

            _loadingData =
                true;

            SelectTestType(
                assignmentTestType);

            _loadingData =
                wasLoading;
        }

        ResultTextBlock.IsVisible =
            false;

        BuildCases();
    }

    private void SelectTestType(
        string? testType)
    {
        if (string.IsNullOrWhiteSpace(testType))
        {
            TestTypeComboBox.SelectedIndex =
                -1;
            return;
        }

        foreach (var item in TestTypeComboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem &&
                string.Equals(
                    comboBoxItem.Tag?.ToString(),
                    testType,
                    StringComparison.OrdinalIgnoreCase))
            {
                TestTypeComboBox.SelectedItem =
                    comboBoxItem;
                return;
            }
        }

        TestTypeComboBox.SelectedIndex =
            -1;
    }

    private TestAssignmentModel? GetSelectedAssignment()
    {
        return (AssignmentComboBox.SelectedItem as ComboBoxItem)?.Tag
            as TestAssignmentModel;
    }

    private void SelectRecipient(
        string login)
    {
        foreach (var item in RecipientComboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem &&
                string.Equals(
                    comboBoxItem.Tag?.ToString(),
                    login,
                    StringComparison.OrdinalIgnoreCase))
            {
                RecipientComboBox.SelectedItem =
                    comboBoxItem;

                return;
            }
        }
    }

    private void AddToSummaryButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var recipient =
            (RecipientComboBox.SelectedItem as ComboBoxItem)?.Tag
                ?.ToString();

        var version =
            VersionPresetComboBox.Text?.Trim();

        var sessionName =
            SessionNamePresetComboBox.Text?.Trim();

        var selectedIds =
            _selectedCaseIds
                .Where(
                    caseId =>
                        GetAvailableCaseOptions().Any(
                            option =>
                                option.Id ==
                                caseId))
                .ToArray();

        if (string.IsNullOrWhiteSpace(
                recipient))
        {
            ShowResult(
                LocalizationService.T("Assignment.SelectRecipient"),
                false);

            return;
        }

        if (string.IsNullOrWhiteSpace(
                version))
        {
            ShowResult(
                LocalizationService.T("Assignment.VersionRequired"),
                false);

            VersionPresetComboBox.Focus();

            return;
        }

        sessionName =
            string.IsNullOrWhiteSpace(sessionName)
                ? $"{_projectName} — v{version}"
                : sessionName;

        SaveCheckedPresets(
            sessionName,
            version);

        if (selectedIds.Length == 0)
        {
            ShowResult(
                LocalizationService.T("Assignment.SelectAtLeastOne"),
                false);

            return;
        }

        var selectedAssignment =
            GetSelectedAssignment();

        if (selectedAssignment is not null)
        {
            _drafts.RemoveAll(
                draft =>
                    draft.AssignmentId ==
                    selectedAssignment.Id);
        }

        var combinedCount =
            selectedIds.Length;

        if (selectedAssignment is null)
        {
            var matchingDraftIndex =
                _drafts.FindIndex(
                    draft =>
                        draft.AssignmentId is null &&
                        string.Equals(
                            draft.RecipientLogin,
                            recipient,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            draft.ApplicationVersion,
                            version,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            draft.SessionName,
                            sessionName,
                            StringComparison.OrdinalIgnoreCase));

            if (matchingDraftIndex >= 0)
            {
                var matchingDraft =
                    _drafts[matchingDraftIndex];

                var combinedIds =
                    matchingDraft.TestCaseIds
                        .Concat(selectedIds)
                        .Distinct()
                        .ToArray();

                _drafts[matchingDraftIndex] =
                    matchingDraft with
                    {
                        TestCaseIds = combinedIds
                    };

                combinedCount =
                    combinedIds.Length;
            }
            else
            {
                _drafts.Add(
                    new AssignmentDraft(
                        null,
                        recipient,
                        sessionName,
                        version,
                        selectedIds));
            }
        }
        else
        {
            _drafts.Add(
                new AssignmentDraft(
                    selectedAssignment.Id,
                    recipient,
                    sessionName,
                    version,
                    selectedIds));
        }

        ShowResult(
            LocalizationService.Format("Assignment.AddedToSummary", recipient, combinedCount),
            true);

        GoToSummaryButton.IsVisible =
            true;

        _selectedCaseIds.Clear();

        _loadingData =
            true;

        AssignmentComboBox.SelectedIndex =
            0;

        SessionNamePresetComboBox.Text =
            sessionName;

        _loadingData =
            false;

        WithdrawButton.IsVisible =
            true;

        WithdrawButton.IsEnabled =
            false;

        BuildCases();
    }

    private void GoToSummaryButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        ShowSummary();
    }

    private void ShowSummary()
    {
        if (_drafts.Count == 0)
        {
            return;
        }

        EditorPanel.IsVisible =
            false;

        SummaryPanel.IsVisible =
            true;

        SummaryResultTextBlock.IsVisible =
            false;

        BuildDraftSummary();
    }

    private void BuildDraftSummary()
    {
        DraftSummaryPanel.Children.Clear();

        foreach (var draft in _drafts.ToArray())
        {
            var caseNames =
                _caseOptions
                    .Where(
                        option =>
                            draft.TestCaseIds.Contains(
                                option.Id))
                    .Take(3)
                    .Select(
                        option =>
                            option.TestCaseName)
                    .ToArray();

            var description =
                string.Join(
                    ", ",
                    caseNames);

            if (draft.TestCaseIds.Length >
                caseNames.Length)
            {
                description +=
                    LocalizationService.Format("Assignment.AndMore", draft.TestCaseIds.Length - caseNames.Length);
            }

            var removeButton =
                new Button
                {
                    Content = LocalizationService.T("Assignment.Remove"),
                    Height = 38,
                    MinWidth = 82,
                    Padding = new Thickness(16, 0),
                    CornerRadius = new CornerRadius(9),
                    FontSize = 13,
                    FontWeight = FontWeight.SemiBold,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

            removeButton.Click +=
                (_, _) =>
                {
                    _drafts.Remove(
                        draft);

                    BuildDraftSummary();
                    BuildCases();

                    if (_drafts.Count == 0)
                    {
                        BackToEditor();
                    }
                };

            var textPanel =
                new StackPanel
                {
                    Spacing = 3
                };

            textPanel.Children.Add(
                new TextBlock
                {
                    Text = LocalizationService.Format("Assignment.DraftSummary", draft.SessionName, draft.RecipientLogin, draft.ApplicationVersion, draft.TestCaseIds.Length),
                    FontSize = 14,
                    FontWeight = FontWeight.SemiBold
                });

            textPanel.Children.Add(
                new TextBlock
                {
                    Text = description,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                    Foreground = Brushes.Gray
                });

            var row =
                new Grid
                {
                    ColumnDefinitions =
                        new ColumnDefinitions("*,Auto"),
                    ColumnSpacing = 12
                };

            Grid.SetColumn(
                removeButton,
                1);

            row.Children.Add(
                textPanel);

            row.Children.Add(
                removeButton);

            DraftSummaryPanel.Children.Add(
                new Border
                {
                    Padding = new Thickness(14),
                    CornerRadius = new CornerRadius(10),
                    BorderBrush =
                        this.FindResource("CardBorderBrush") as IBrush,
                    BorderThickness = new Thickness(1),
                    Child = row
                });
        }

        UpdateSummaryCounts();
        GoToSummaryButton.IsVisible =
            _drafts.Count > 0;
    }

    private void UpdateSummaryCounts()
    {
        var assignedIds =
            GetCommittedAndDraftAssignedIds();

        SummaryTotalTextBlock.Text =
            _caseOptions.Count.ToString();

        SummaryAssignedTextBlock.Text =
            assignedIds.Count.ToString();

        SummaryRemainingTextBlock.Text =
            Math.Max(
                    0,
                    _caseOptions.Count -
                    assignedIds.Count)
                .ToString();
    }

    private HashSet<Guid> GetCommittedAndDraftAssignedIds()
    {
        var replacedAssignmentIds =
            _drafts
                .Where(
                    draft =>
                        draft.AssignmentId.HasValue)
                .Select(
                    draft =>
                        draft.AssignmentId!.Value)
                .ToHashSet();

        return _activeAssignments
            .Where(
                assignment =>
                    !replacedAssignmentIds.Contains(
                        assignment.Id))
            .SelectMany(
                assignment =>
                    assignment.TestCaseIds)
            .Concat(
                _drafts.SelectMany(
                    draft =>
                        draft.TestCaseIds))
            .ToHashSet();
    }

    private void UpdateAssignmentCounts()
    {
        var selectedTestType =
            GetSelectedTestType();

        if (string.IsNullOrWhiteSpace(selectedTestType))
        {
            AssignmentCountsTextBlock.Text =
                LocalizationService.Format("Assignment.SelectionCounts", 0, 0, 0);

            return;
        }

        var typeCaseIds =
            _caseOptions
                .Where(
                    option =>
                        string.Equals(
                            GetTestTypeName(option),
                            selectedTestType,
                            StringComparison.OrdinalIgnoreCase))
                .Select(option => option.Id)
                .ToHashSet();

        var assignedCount =
            GetCommittedAndDraftAssignedIds()
                .Count(typeCaseIds.Contains);

        AssignmentCountsTextBlock.Text =
            LocalizationService.Format("Assignment.SelectionCounts", _selectedCaseIds.Count, assignedCount, Math.Max(0, typeCaseIds.Count - assignedCount));
    }

    private async void SendAssignmentsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_drafts.Count == 0 ||
            _sendingAssignments)
        {
            return;
        }

        _sendingAssignments =
            true;

        SendAssignmentsButton.IsEnabled =
            false;

        try
        {
            var batchId =
                Guid.NewGuid();

            await _assignmentService.SaveAssignmentsBatchAsync(
                _drafts.Select(
                    draft =>
                        new AssignmentSaveRequest(
                            draft.AssignmentId,
                            _projectKey,
                            _projectName,
                            draft.SessionName,
                            draft.ApplicationVersion,
                            draft.RecipientLogin,
                            _assignedByLogin,
                            draft.TestCaseIds,
                            batchId)));

            var sentCount =
                _drafts.Count;

            var recipientCount =
                _drafts
                    .Select(draft => draft.RecipientLogin)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

            var caseCount =
                _drafts.Sum(draft => draft.TestCaseIds.Count());

            await _assignmentService.SendUserNotificationAsync(
                _assignedByLogin,
                LocalizationService.T("Assignment.SentTitle"),
                LocalizationService.Format("Assignment.SentDescription", sentCount, recipientCount, caseCount));

            _drafts.Clear();
            DataChanged =
                true;

            await CompleteInlineOrCloseAsync();
        }
        catch (Exception exception)
        {
            SummaryResultTextBlock.Text =
                LocalizationService.Format("Assignment.SendFailed", exception.Message);

            SummaryResultTextBlock.Foreground =
                Brushes.IndianRed;

            SummaryResultTextBlock.IsVisible =
                true;
        }
        finally
        {
            _sendingAssignments =
                false;

            SendAssignmentsButton.IsEnabled =
                true;
        }
    }

    private void BackToEditorButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        BackToEditor();
    }

    private async void CancelAllDraftsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_drafts.Count == 0)
        {
            return;
        }

        var confirmation =
            new ConfirmDeleteWindow(
                LocalizationService.T("Assignment.CancelAllTitle"),
                LocalizationService.T("Assignment.CancelAllDescription"),
                LocalizationService.T("Assignment.CancelAll"));

        if (!await confirmation.ShowDialog<bool>(
                GetDialogOwner()))
        {
            return;
        }

        _drafts.Clear();
        _selectedCaseIds.Clear();

        BackToEditor();

        ShowResult(
            LocalizationService.T("Assignment.CancelledAll"),
            true);
    }

    private void BackToEditor()
    {
        SummaryPanel.IsVisible =
            false;

        EditorPanel.IsVisible =
            true;

        GoToSummaryButton.IsVisible =
            _drafts.Count > 0;

        BuildCases();
    }

    private async void WithdrawButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        var assignment =
            GetSelectedAssignment();

        if (assignment is null)
        {
            return;
        }

        var wasStarted =
            assignment.StartedAt.HasValue ||
            assignment.CaseProgress.Any(
                progress =>
                    progress.Status is
                        "Success" or
                        "Failed" or
                        "NA" or
                        "Blocked");

        var confirmation =
            new ConfirmDeleteWindow(
                wasStarted
                    ? LocalizationService.T("Assignment.SessionStarted")
                    : LocalizationService.T("Assignment.CancelTitle"),
                wasStarted
                    ? LocalizationService.Format("Assignment.WithdrawStartedQuestion", assignment.RecipientLogin)
                    : LocalizationService.Format("Assignment.RemoveNotStartedQuestion", assignment.RecipientLogin, assignment.ApplicationVersion),
                LocalizationService.T("Assignment.CancelAssignment"));

        if (!await confirmation.ShowDialog<bool>(
                GetDialogOwner()))
        {
            return;
        }

        await _assignmentService.WithdrawAssignmentAsync(
            assignment.Id,
            _assignedByLogin);

        _drafts.RemoveAll(
            draft =>
                draft.AssignmentId ==
                assignment.Id);

        DataChanged =
            true;

        ShowResult(
            LocalizationService.T("Assignment.WithdrawnSuccess"),
            true);

        await LoadDataAsync();
    }

    private void ShowResult(
        string message,
        bool success)
    {
        ResultTextBlock.Text =
            message;

        ResultTextBlock.Foreground =
            success
                ? Brushes.SeaGreen
                : Brushes.IndianRed;

        ResultTextBlock.IsVisible =
            true;
    }

    private void RefreshPresetItems()
    {
        var sessionName =
            SessionNamePresetComboBox.Text;
        var version =
            VersionPresetComboBox.Text;

        SessionNamePresetComboBox.ItemsSource =
            _inputPresets.SessionNames.ToArray();
        VersionPresetComboBox.ItemsSource =
            _inputPresets.Versions.ToArray();

        SessionNamePresetComboBox.Text =
            sessionName;
        VersionPresetComboBox.Text =
            version;

        UpdatePresetDeleteButtons();
    }

    private void UpdatePresetDeleteButtons()
    {
        DeleteSavedSessionNameButton.IsEnabled =
            ContainsPreset(
                _inputPresets.SessionNames,
                SessionNamePresetComboBox.Text);

        DeleteSavedVersionButton.IsEnabled =
            ContainsPreset(
                _inputPresets.Versions,
                VersionPresetComboBox.Text);
    }

    private void SaveCheckedPresets(
        string? sessionName = null,
        string? version = null)
    {
        var changed =
            false;

        if (RememberSessionNameCheckBox.IsChecked == true)
        {
            changed |=
                AddPreset(
                    _inputPresets.SessionNames,
                    sessionName ?? SessionNamePresetComboBox.Text);
        }

        if (RememberVersionCheckBox.IsChecked == true)
        {
            changed |=
                AddPreset(
                    _inputPresets.Versions,
                    version ?? VersionPresetComboBox.Text);
        }

        if (!changed)
        {
            UpdatePresetDeleteButtons();
            return;
        }

        AssignmentInputPresetService.Save(
            _inputPresets);
        RefreshPresetItems();
    }

    private static bool AddPreset(
        List<string> presets,
        string? value)
    {
        var normalized =
            value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var existingIndex =
            presets.FindIndex(
                item =>
                    string.Equals(
                        item,
                        normalized,
                        StringComparison.OrdinalIgnoreCase));

        if (existingIndex == 0)
        {
            return false;
        }

        if (existingIndex > 0)
        {
            presets.RemoveAt(
                existingIndex);
        }

        presets.Insert(
            0,
            normalized);

        return true;
    }

    private static bool ContainsPreset(
        IEnumerable<string> presets,
        string? value)
    {
        var normalized =
            value?.Trim();

        return !string.IsNullOrWhiteSpace(normalized) &&
               presets.Any(
                   item =>
                       string.Equals(
                           item,
                           normalized,
                           StringComparison.OrdinalIgnoreCase));
    }

    private void DeleteSavedSessionNameButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        DeletePreset(
            _inputPresets.SessionNames,
            SessionNamePresetComboBox.Text);
    }

    private void DeleteSavedVersionButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        DeletePreset(
            _inputPresets.Versions,
            VersionPresetComboBox.Text);
    }

    private void DeletePreset(
        List<string> presets,
        string? value)
    {
        var removed =
            presets.RemoveAll(
                item =>
                    string.Equals(
                        item,
                        value?.Trim(),
                        StringComparison.OrdinalIgnoreCase)) > 0;

        if (!removed)
        {
            UpdatePresetDeleteButtons();
            return;
        }

        AssignmentInputPresetService.Save(
            _inputPresets);
        RefreshPresetItems();

        ShowResult(
            LocalizationService.T("Assignment.SavedValueRemoved"),
            true);
    }

    private async void CloseButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        await CompleteInlineOrCloseAsync();
    }

    private Window GetDialogOwner()
    {
        if (_inlineContent is not null &&
            TopLevel.GetTopLevel(_inlineContent) is Window inlineOwner)
        {
            return inlineOwner;
        }

        return this;
    }

    private async Task CompleteInlineOrCloseAsync()
    {
        if (_inlineCloseAction is not null)
        {
            var closeAction = _inlineCloseAction;
            await closeAction();
            return;
        }

        Close(DataChanged);
    }

    protected override void OnKeyDown(
        KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            if (SummaryPanel.IsVisible)
            {
                BackToEditor();
            }
            else
            {
                _ = CompleteInlineOrCloseAsync();
            }

            e.Handled =
                true;
        }
    }

    private sealed record AssignmentDraft(
        Guid? AssignmentId,
        string RecipientLogin,
        string SessionName,
        string ApplicationVersion,
        Guid[] TestCaseIds);
}

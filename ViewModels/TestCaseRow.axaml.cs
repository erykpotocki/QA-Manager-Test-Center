using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;

namespace QARegressionManager.Views;

public partial class TestCaseRow : UserControl
{
    private Border? _rootBorder;
    private TextBlock? _numberTextBlock;
    private TextBlock? _nameTextBlock;
    private ComboBox? _statusComboBox;
    private ComboBoxItem? _pendingStatusComboBoxItem;
    private Border? _blockedCommentBorder;
    private TextBox? _blockedCommentTextBox;
    private TextBlock? _blockedCommentHintTextBlock;
    private Button? _saveBlockedCommentButton;

    private bool _isUserDefined;
    private bool _canRename;
    private bool _allowPendingStatus = true;
    private Point? _dragStartPoint;
    private PointerPressedEventArgs? _dragTriggerEvent;

    public Guid TestCaseId { get; set; }

    private bool _canMoveUp;
    private bool _canMoveDown;

    public bool CanMoveUp
    {
        get => _canMoveUp;

        set
        {
            _canMoveUp = value;
            BuildContextMenu();
        }
    }

    public bool CanMoveDown
    {
        get => _canMoveDown;

        set
        {
            _canMoveDown = value;
            BuildContextMenu();
        }
    }

    public bool IsUserDefined
    {
        get
        {
            return _isUserDefined;
        }

        set
        {
            _isUserDefined =
                value;

            BuildContextMenu();
        }
    }

    public bool CanRename
    {
        get => _canRename;

        set
        {
            _canRename =
                value;

            BuildContextMenu();
        }
    }

    public bool AllowPendingStatus
    {
        get => _allowPendingStatus;

        set
        {
            _allowPendingStatus =
                value;

            if (_pendingStatusComboBoxItem is not null)
            {
                _pendingStatusComboBoxItem.IsVisible =
                    value;

                _pendingStatusComboBoxItem.IsEnabled =
                    value;
            }

            if (!value &&
                string.Equals(
                    ReadStatus(),
                    "None",
                    StringComparison.OrdinalIgnoreCase))
            {
                SetStatus(
                    "InProgress");
            }
        }
    }

    public int Number
    {
        get
        {
            return int.TryParse(
                _numberTextBlock?.Text,
                out var value)
                    ? value
                    : 0;
        }

        set
        {
            if (_numberTextBlock is not null)
            {
                _numberTextBlock.Text =
                    value.ToString("00");
            }
        }
    }

    public string TestCaseName
    {
        get
        {
            return _nameTextBlock?.Text ??
                   string.Empty;
        }

        set
        {
            if (_nameTextBlock is not null)
            {
                _nameTextBlock.Text =
                    value;
            }
        }
    }

    public string Status
    {
        get
        {
            return ReadStatus();
        }

        set
        {
            SetStatus(
                value);

            ApplyStatusStyle(
                value);
        }
    }

    public string BlockedComment
    {
        get => _blockedCommentTextBox?.Text?.Trim() ?? string.Empty;
        set
        {
            if (_blockedCommentTextBox is not null)
            {
                _blockedCommentTextBox.Text = value ?? string.Empty;
            }
        }
    }

    public bool HasPendingBlockedComment =>
        string.Equals(ReadStatus(), "Blocked", StringComparison.OrdinalIgnoreCase) &&
        string.IsNullOrWhiteSpace(BlockedComment);

    public event EventHandler<string>? StatusChanged;
    public event EventHandler? BlockedValidationChanged;
    public event EventHandler? QuickCompleted;
    public event EventHandler? MoveUpRequested;
    public event EventHandler? MoveDownRequested;
    public event EventHandler? SelectedRequested;
    public event EventHandler? CopyRequested;
    public event EventHandler? DuplicateRequested;
    public event EventHandler? RenameRequested;
    public event EventHandler? DetailsRequested;
    public event EventHandler? DeleteRequested;
    public event EventHandler<PointerPressedEventArgs>? DragRequested;

    public TestCaseRow()
    {
        InitializeComponent();
        FindControls();

        if (_statusComboBox is not null)
        {
            _statusComboBox.SelectionChanged +=
                StatusComboBox_OnSelectionChanged;
        }

        if (_blockedCommentTextBox is not null)
        {
            _blockedCommentTextBox.TextChanged += BlockedCommentTextBox_OnTextChanged;
            _blockedCommentTextBox.KeyDown += BlockedCommentTextBox_OnKeyDown;
        }

        if (_rootBorder is not null)
        {
            _rootBorder.PointerPressed +=
                RootBorder_OnPointerPressed;

            _rootBorder.PointerMoved +=
                RootBorder_OnPointerMoved;

            _rootBorder.PointerReleased +=
                RootBorder_OnPointerReleased;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(
            this);
    }

    private void FindControls()
    {
        _rootBorder =
            this.FindControl<Border>(
                "RootBorder");

        _numberTextBlock =
            this.FindControl<TextBlock>(
                "NumberTextBlock");

        _nameTextBlock =
            this.FindControl<TextBlock>(
                "NameTextBlock");

        _statusComboBox =
            this.FindControl<ComboBox>(
                "StatusComboBox");

        _pendingStatusComboBoxItem =
            this.FindControl<ComboBoxItem>(
                "PendingStatusComboBoxItem");

        _blockedCommentBorder = this.FindControl<Border>("BlockedCommentBorder");
        _blockedCommentTextBox = this.FindControl<TextBox>("BlockedCommentTextBox");
        _blockedCommentHintTextBlock = this.FindControl<TextBlock>("BlockedCommentHintTextBlock");
        _saveBlockedCommentButton = this.FindControl<Button>("SaveBlockedCommentButton");
    }


    private void RootBorder_OnPointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        var point =
            e.GetCurrentPoint(
                _rootBorder);

        if (point.Properties.IsLeftButtonPressed &&
            e.Source is not ComboBox &&
            e.Source is not ComboBoxItem)
        {
            SelectedRequested?.Invoke(
                this,
                EventArgs.Empty);

            _dragStartPoint =
                e.GetPosition(
                    this);

            _dragTriggerEvent =
                e;
        }

        if (e.ClickCount != 2)
        {
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (e.Source is ComboBox ||
            e.Source is ComboBoxItem)
        {
            return;
        }

        var newStatus =
            e.KeyModifiers.HasFlag(
                KeyModifiers.Shift)
                ? "Failed"
                : "Success";

        Status =
            newStatus;

        QuickCompleted?.Invoke(
            this,
            EventArgs.Empty);

        e.Handled =
            true;
    }

    private void RootBorder_OnPointerMoved(
        object? sender,
        PointerEventArgs e)
    {
        if (_dragStartPoint is null ||
            _dragTriggerEvent is null)
        {
            return;
        }

        var point =
            e.GetCurrentPoint(
                this);

        if (!point.Properties.IsLeftButtonPressed)
        {
            ClearDragStart();

            return;
        }

        var currentPosition =
            e.GetPosition(
                this);

        var delta =
            currentPosition -
            _dragStartPoint.Value;

        if (Math.Abs(delta.X) < 6 &&
            Math.Abs(delta.Y) < 6)
        {
            return;
        }

        var triggerEvent =
            _dragTriggerEvent;

        ClearDragStart();

        DragRequested?.Invoke(
            this,
            triggerEvent);
    }

    private void RootBorder_OnPointerReleased(
        object? sender,
        PointerReleasedEventArgs e)
    {
        ClearDragStart();
    }

    private void ClearDragStart()
    {
        _dragStartPoint =
            null;

        _dragTriggerEvent =
            null;
    }

    public void SetDragTarget(
        bool isTarget)
    {
        if (_rootBorder is null)
        {
            return;
        }

        _rootBorder.Classes.Set(
            "DragTargetRow",
            isTarget);
    }

    private void StatusComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        var status =
            ReadStatus();

        if (!_allowPendingStatus &&
            string.Equals(
                status,
                "None",
                StringComparison.OrdinalIgnoreCase))
        {
            SetStatus(
                "InProgress");

            return;
        }

        ApplyStatusStyle(
            status);

        var isBlocked = string.Equals(
            status,
            "Blocked",
            StringComparison.OrdinalIgnoreCase);

        if (_blockedCommentBorder is not null)
        {
            _blockedCommentBorder.IsVisible = isBlocked;
        }

        if (isBlocked && string.IsNullOrWhiteSpace(BlockedComment))
        {
            BlockedValidationChanged?.Invoke(this, EventArgs.Empty);
            FlashBlockedCommentValidation();
            _blockedCommentTextBox?.Focus();
            return;
        }

        BlockedValidationChanged?.Invoke(this, EventArgs.Empty);

        StatusChanged?.Invoke(
            this,
            status);
    }

    private void BlockedCommentTextBox_OnTextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        if (_blockedCommentTextBox is not null)
        {
            var original = _blockedCommentTextBox.Text ?? string.Empty;
            var withoutLeadingWhitespace = original.TrimStart();
            if (!string.Equals(original, withoutLeadingWhitespace, StringComparison.Ordinal))
            {
                _blockedCommentTextBox.Text = withoutLeadingWhitespace;
                _blockedCommentTextBox.CaretIndex = withoutLeadingWhitespace.Length;
                return;
            }
        }

        if (!string.Equals(
                ReadStatus(),
                "Blocked",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var isValid = !string.IsNullOrWhiteSpace(BlockedComment);

        if (_saveBlockedCommentButton is not null)
        {
            _saveBlockedCommentButton.IsEnabled = isValid;
            _saveBlockedCommentButton.Content = "ZAPISZ";
        }

        if (_blockedCommentHintTextBlock is not null)
        {
            _blockedCommentHintTextBlock.Text =
                "Pole jest wymagane przed zapisaniem statusu.";
            _blockedCommentHintTextBlock.Foreground =
                new SolidColorBrush(
                    Color.Parse("#DC4C56"));
            _blockedCommentHintTextBlock.IsVisible = !isValid;
        }

        BlockedValidationChanged?.Invoke(this, EventArgs.Empty);
        if (isValid)
        {
            StatusChanged?.Invoke(this, "Blocked");
        }
    }

    private void BlockedCommentTextBox_OnKeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter ||
            e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            return;
        }

        ConfirmBlockedComment();
        e.Handled = true;
    }

    private void SaveBlockedCommentButton_OnClick(
        object? sender,
        RoutedEventArgs e) =>
        ConfirmBlockedComment();

    private void ConfirmBlockedComment()
    {
        if (HasPendingBlockedComment)
        {
            FlashBlockedCommentValidation();
            return;
        }

        StatusChanged?.Invoke(this, "Blocked");
        BlockedValidationChanged?.Invoke(this, EventArgs.Empty);

        if (_saveBlockedCommentButton is not null)
        {
            _saveBlockedCommentButton.Content = "ZAPISANO";
        }

        if (_blockedCommentHintTextBlock is not null)
        {
            _blockedCommentHintTextBlock.Text =
                "Komentarz zapisany automatycznie.";
            _blockedCommentHintTextBlock.Foreground =
                new SolidColorBrush(
                    Color.Parse("#169B50"));
            _blockedCommentHintTextBlock.IsVisible = true;
        }
    }

    public void FlashBlockedCommentValidation()
    {
        if (_blockedCommentBorder is null || !HasPendingBlockedComment)
        {
            return;
        }

        var ticks = 0;
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };

        timer.Tick += (_, _) =>
        {
            ticks++;
            _blockedCommentBorder.BorderBrush = ticks % 2 == 0
                ? new SolidColorBrush(Color.Parse("#A84A16"))
                : Brushes.Red;
            _blockedCommentBorder.BorderThickness =
                ticks % 2 == 0 ? new Thickness(1) : new Thickness(2);

            if (ticks < 6)
            {
                return;
            }

            timer.Stop();
            _blockedCommentBorder.BorderBrush =
                new SolidColorBrush(Color.Parse("#A84A16"));
            _blockedCommentBorder.BorderThickness = new Thickness(1);
        };

        timer.Start();
    }

    private void BuildContextMenu()
    {
        var moveUpItem =
            new MenuItem
            {
                Header =
                    "Przenieś wyżej",

                IsEnabled =
                    CanMoveUp
            };

        moveUpItem.Click +=
            (_, _) =>
            {
                MoveUpRequested?.Invoke(
                    this,
                    EventArgs.Empty);
            };

        var moveDownItem =
            new MenuItem
            {
                Header =
                    "Przenieś niżej",

                IsEnabled =
                    CanMoveDown
            };

        moveDownItem.Click +=
            (_, _) =>
            {
                MoveDownRequested?.Invoke(
                    this,
                    EventArgs.Empty);
            };

        var duplicateItem =
            new MenuItem
            {
                Header =
                    "Duplikuj przypadek"
            };

        duplicateItem.Click +=
            (_, _) =>
            {
                DuplicateRequested?.Invoke(
                    this,
                    EventArgs.Empty);
            };

        var copyItem =
            new MenuItem
            {
                Header =
                    "Kopiuj przypadek"
            };

        copyItem.Click +=
            (_, _) =>
            {
                CopyRequested?.Invoke(
                    this,
                    EventArgs.Empty);
            };

        var menuItems =
            new System.Collections.Generic.List<MenuItem>
            {
                copyItem,
                duplicateItem,
                moveUpItem,
                moveDownItem
            };

        var detailsItem = new MenuItem
        {
            Header = "Edytuj szczegóły"
        };

        detailsItem.Click += (_, _) =>
            DetailsRequested?.Invoke(this, EventArgs.Empty);

        menuItems.Insert(0, detailsItem);

        if (_canRename)
        {
            var renameItem =
                new MenuItem
                {
                    Header =
                        "Zmień nazwę"
                };

            renameItem.Click +=
                (_, _) =>
                {
                    RenameRequested?.Invoke(
                        this,
                        EventArgs.Empty);
                };

            menuItems.Add(
                renameItem);
        }

        if (_isUserDefined)
        {
            var deleteItem =
                new MenuItem
                {
                    Header =
                        "Usuń przypadek"
                };

            deleteItem.Click +=
                (_, _) =>
                {
                    DeleteRequested?.Invoke(
                        this,
                        EventArgs.Empty);
                };

            menuItems.Add(
                deleteItem);
        }

        ContextMenu =
            new ContextMenu
            {
                ItemsSource =
                    menuItems
            };
    }

    private string ReadStatus()
    {
        if (_statusComboBox?.SelectedItem is
                ComboBoxItem selectedItem &&
            selectedItem.Tag is string status)
        {
            return status;
        }

        return "None";
    }

    private void SetStatus(
        string status)
    {
        if (_statusComboBox is null)
        {
            return;
        }

        if (!_allowPendingStatus &&
            (string.Equals(
                 status,
                 "None",
                 StringComparison.OrdinalIgnoreCase) ||
             string.Equals(
                 status,
                 "Pending",
                 StringComparison.OrdinalIgnoreCase)))
        {
            status =
                "InProgress";
        }

        _statusComboBox.SelectedIndex =
            status switch
            {
                "InProgress" => 1,
                "Success" => 2,
                "Failed" => 3,
                "NA" => 4,
                "Blocked" => 5,
                _ => 0
            };
    }

    private void ApplyStatusStyle(
        string status)
    {
        if (_rootBorder is null)
        {
            return;
        }

        _rootBorder.Classes.Remove(
            "InProgressRow");

        _rootBorder.Classes.Remove(
            "SuccessRow");

        _rootBorder.Classes.Remove(
            "FailedRow");

        _rootBorder.Classes.Remove(
            "NaRow");

        _rootBorder.Classes.Remove(
            "BlockedRow");

        switch (status)
        {
            case "InProgress":

                _rootBorder.Classes.Add(
                    "InProgressRow");

                break;

            case "Success":

                _rootBorder.Classes.Add(
                    "SuccessRow");

                break;

            case "Failed":

                _rootBorder.Classes.Add(
                    "FailedRow");

                break;

            case "NA":

                _rootBorder.Classes.Add(
                    "NaRow");

                break;

            case "Blocked":

                _rootBorder.Classes.Add(
                    "BlockedRow");

                break;
        }
    }
}

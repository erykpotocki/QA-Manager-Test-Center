using System;

namespace QARegressionManager.Services;

public sealed class NavigationService
{
    public int CurrentIndex { get; private set; } = -1;

    public int ItemCount { get; private set; }

    public bool HasCurrentItem =>
        CurrentIndex >= 0 &&
        CurrentIndex < ItemCount;

    public bool CanGoPrevious =>
        HasCurrentItem &&
        CurrentIndex > 0;

    public bool CanGoNext =>
        HasCurrentItem &&
        CurrentIndex < ItemCount - 1;

    public bool IsLastItem =>
        HasCurrentItem &&
        CurrentIndex == ItemCount - 1;

    public event EventHandler<int>? CurrentIndexChanged;

    public void Initialize(
        int itemCount,
        int startIndex = -1)
    {
        if (itemCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(itemCount),
                "Liczba elementów nie może być ujemna.");
        }

        ItemCount = itemCount;

        if (itemCount == 0)
        {
            SetCurrentIndex(-1);
            return;
        }

        if (startIndex < -1 ||
            startIndex >= itemCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startIndex),
                "Indeks początkowy znajduje się poza zakresem.");
        }

        SetCurrentIndex(startIndex);
    }

    public bool Select(int index)
    {
        if (index < 0 ||
            index >= ItemCount)
        {
            return false;
        }

        SetCurrentIndex(index);
        return true;
    }

    public bool MoveNext()
    {
        if (!CanGoNext)
        {
            return false;
        }

        SetCurrentIndex(CurrentIndex + 1);
        return true;
    }

    public bool MovePrevious()
    {
        if (!CanGoPrevious)
        {
            return false;
        }

        SetCurrentIndex(CurrentIndex - 1);
        return true;
    }

    public void ClearSelection()
    {
        SetCurrentIndex(-1);
    }

    private void SetCurrentIndex(int index)
    {
        if (CurrentIndex == index)
        {
            return;
        }

        CurrentIndex = index;

        CurrentIndexChanged?.Invoke(
            this,
            CurrentIndex);
    }
}
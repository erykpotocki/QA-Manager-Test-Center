using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace QARegressionManager.Services;

public sealed class TestTreeBuilder
{
    public TreeViewItem CreateProjectItem(
        string projectName,
        IEnumerable<TreeViewItem> testTypes)
    {
        var projectItem = CreateFolderItem(
            $"▰ {projectName}",
            true);

        foreach (var testType in testTypes)
        {
            projectItem.Items.Add(testType);
        }

        return projectItem;
    }

    public TreeViewItem CreateFolderItem(
        string name,
        bool isExpanded = false)
    {
        return new TreeViewItem
        {
            Header = $"▰ {name}",
            IsExpanded = isExpanded
        };
    }

    public TreeViewItem CreatePlaceholderTestType(
        string name)
    {
        var item = CreateFolderItem(name);

        item.Items.Add(
            new TreeViewItem
            {
                Header = "Przypadek testowy 1"
            });

        item.Items.Add(
            new TreeViewItem
            {
                Header = "Przypadek testowy 2"
            });

        return item;
    }

    public TreeViewItem CreateSectionItem(
        string sectionName,
        int completed,
        int total,
        Border headerBorder,
        TextBlock stateIcon,
        TextBlock progressText)
    {
        stateIcon.Text = "○";
        stateIcon.Width = 20;
        stateIcon.VerticalAlignment = VerticalAlignment.Center;

        progressText.Text = $"{completed}/{total}";
        progressText.Margin = new Thickness(12, 0, 0, 0);
        progressText.VerticalAlignment = VerticalAlignment.Center;

        var nameText = new TextBlock
        {
            Text = sectionName,
            VerticalAlignment = VerticalAlignment.Center
        };

        DockPanel.SetDock(
            stateIcon,
            Dock.Left);

        DockPanel.SetDock(
            progressText,
            Dock.Right);

        var headerPanel = new DockPanel
        {
            LastChildFill = true
        };

        headerPanel.Children.Add(stateIcon);
        headerPanel.Children.Add(progressText);
        headerPanel.Children.Add(nameText);

        headerBorder.Child = headerPanel;
        headerBorder.MinWidth = 230;
        headerBorder.Classes.Add("TreeProgressRow");

        return new TreeViewItem
        {
            Header = headerBorder
        };
    }
}
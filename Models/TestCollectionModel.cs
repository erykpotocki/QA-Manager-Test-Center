using System;

namespace QARegressionManager.Models;

public sealed class TestCollectionModel
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public string ProjectKey { get; set; } =
        string.Empty;

    public string TestTypeKey { get; set; } =
        string.Empty;

    public string ParentFolderKey { get; set; } =
        string.Empty;

    public string CollectionKey { get; set; } =
        string.Empty;

    public string Name { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public string CreatedByLogin { get; set; } =
        string.Empty;

    public bool IsSystem { get; set; }

    public bool RequiresManagerRole { get; set; }

    public int SortOrder { get; set; }
}

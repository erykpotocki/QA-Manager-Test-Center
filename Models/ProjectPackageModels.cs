using System;
using System.Collections.Generic;

namespace QARegressionManager.Models;

public sealed class ProjectPackage
{
    public ProjectPackageMetadata Metadata { get; set; } = new();
    public List<ProjectPackageFolder> Folders { get; set; } = new();
    public List<ProjectPackageCollection> Collections { get; set; } = new();
    public List<ProjectPackageTestCase> TestCases { get; set; } = new();
}

public sealed class ProjectPackageMetadata
{
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = string.Empty;
    public string TesterName { get; set; } = string.Empty;
    public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.Now;
    public string QaTestCenterVersion { get; set; } = "0.2";
}

public sealed class ProjectPackageFolder
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string ParentKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TestTypeKey { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsProtected { get; set; }
    public bool RequiresManagerRole { get; set; }
    public string CreatedByLogin { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class ProjectPackageCollection
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string ParentFolderKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TestTypeKey { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsProtected { get; set; }
    public bool RequiresManagerRole { get; set; }
    public string CreatedByLogin { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class ProjectPackageTestCase
{
    public Guid Id { get; set; }
    public string CollectionKey { get; set; } = string.Empty;
    public string TestTypeKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsProtected { get; set; }
    public string CreatedByLogin { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string Status { get; set; } = "None";
    public string Comment { get; set; } = string.Empty;
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

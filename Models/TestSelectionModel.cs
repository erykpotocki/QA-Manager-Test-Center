using System;
using System.Collections.Generic;

namespace QARegressionManager.Models;

public sealed class TestSectionModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ProjectKey { get; set; } = string.Empty;

    public string TestTypeKey { get; set; } = string.Empty;

    public string SectionKey { get; set; } = string.Empty;

    public string ParentSectionKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string CreatedByLogin { get; set; } = string.Empty;

    public bool IsSystem { get; set; }

    public bool RequiresManagerRole { get; set; }

    public int SortOrder { get; set; }

    public List<TestCaseModel> TestCases { get; set; } = new();
}

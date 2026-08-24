using System;

namespace QARegressionManager.Models;

public sealed class TestCaseModel
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public string ProjectKey { get; set; } =
        string.Empty;

    public string TestTypeKey { get; set; } =
        string.Empty;

    public string SectionKey { get; set; } =
        string.Empty;

    public string Name { get; set; } =
        string.Empty;

    public string CreatedByLogin { get; set; } =
        string.Empty;

    public int SortOrder { get; set; }

    public string Status { get; set; } =
        "None";

    public string Comment { get; set; } =
        string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Preconditions { get; set; } = string.Empty;

    public string ExternalId { get; set; } = string.Empty;

    public string SourceVersion { get; set; } = string.Empty;

    public string Importance { get; set; } = string.Empty;

    public string ExecutionType { get; set; } = string.Empty;

    public string EstimatedDuration { get; set; } = string.Empty;

    public System.Collections.Generic.List<string> Platforms { get; set; } = new();

    public System.Collections.Generic.List<TestStepModel> Steps { get; set; } = new();
}

public sealed class TestStepModel
{
    public int Number { get; set; }
    public string Actions { get; set; } = string.Empty;
    public string ExpectedResults { get; set; } = string.Empty;
    public string ExecutionType { get; set; } = string.Empty;
}

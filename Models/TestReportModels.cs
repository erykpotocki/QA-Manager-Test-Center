using System;
using System.Collections.Generic;

namespace QARegressionManager.Models;

public sealed class TestReport
{
    public TestReportMetadata Metadata { get; set; } =
        new();

    public TestReportSummary Summary { get; set; } =
        new();

    public List<TestReportCase> TestCases { get; set; } =
        new();
}

public sealed class TestReportMetadata
{
    public Guid SessionId { get; set; }

    public string SessionMode { get; set; } =
        "AdHoc";

    public string ProjectName { get; set; } =
        string.Empty;

    public string ApplicationVersion { get; set; } =
        string.Empty;

    public string TesterLogin { get; set; } =
        string.Empty;

    public DateTimeOffset GeneratedAt { get; set; } =
        DateTimeOffset.Now;
}

public sealed class TestReportSummary
{
    public int Total { get; set; }

    public int Success { get; set; }

    public int Failed { get; set; }

    public int Blocked { get; set; }

    public int NotApplicable { get; set; }

    public int InProgress { get; set; }

    public int NotStarted { get; set; }

    public double CompletionPercent { get; set; }
}

public sealed class TestReportCase
{
    public string TestType { get; set; } =
        string.Empty;

    public string Collection { get; set; } =
        string.Empty;

    public string Path { get; set; } =
        string.Empty;

    public string Name { get; set; } =
        string.Empty;

    public string Status { get; set; } =
        string.Empty;

    public string Comment { get; set; } =
        string.Empty;
}

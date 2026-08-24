using System.Collections.Generic;

namespace QARegressionManager.Models;

public sealed class FunctionalTestSeed
{
    public List<TestSectionModel> Folders { get; } = new();
    public List<TestCollectionModel> Collections { get; } = new();
    public List<TestCaseModel> TestCases { get; } = new();
}

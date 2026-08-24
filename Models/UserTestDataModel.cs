using System.Collections.Generic;

namespace QARegressionManager.Models;

public sealed class UserTestDataModel
{
    public List<string> AppliedDataMigrations { get; set; } =
        new();

    public List<TestSectionModel> Folders { get; set; } =
        new();

    public List<TestCollectionModel> Collections { get; set; } =
        new();

    public List<TestCaseModel> TestCases { get; set; } =
        new();
}

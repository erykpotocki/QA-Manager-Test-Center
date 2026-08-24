using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QARegressionManager.Models;

namespace QARegressionManager.Services;

public sealed class UserTestCaseService
{
    private static readonly SemaphoreSlim SaveLock =
        new(1, 1);

    private readonly JsonStorageService _storageService;

    public UserTestCaseService(
        JsonStorageService storageService)
    {
        _storageService =
            storageService;
    }

    public Task<UserTestDataModel> LoadAsync()
    {
        return _storageService.LoadAsync();
    }

    public async Task<TestCaseModel> AddTestCaseAsync(
        string projectKey,
        string testTypeKey,
        string sectionKey,
        string name,
        string createdByLogin)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Nazwa przypadku nie może być pusta.",
                nameof(name));
        }

        var data =
            await _storageService.LoadAsync();

        var nextSortOrder =
            data.TestCases
                .Where(
                    item =>
                        string.Equals(
                            item.ProjectKey,
                            projectKey,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            item.TestTypeKey,
                            testTypeKey,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            item.SectionKey,
                            sectionKey,
                            StringComparison.OrdinalIgnoreCase))
                .Select(
                    item =>
                        item.SortOrder)
                .DefaultIfEmpty(0)
                .Max() + 1000;

        var testCase =
            new TestCaseModel
            {
                Id =
                    Guid.NewGuid(),

                ProjectKey =
                    projectKey,

                TestTypeKey =
                    testTypeKey,

                SectionKey =
                    sectionKey,

                Name =
                    name.Trim(),

                CreatedByLogin =
                    createdByLogin?.Trim() ?? string.Empty,

                SortOrder =
                    nextSortOrder,

                Status =
                    "None"
            };

        data.TestCases.Add(
            testCase);

        await _storageService.SaveAsync(
            data);

        return testCase;
    }

    public async Task SaveStatusAsync(
        Guid testCaseId,
        string projectKey,
        string testTypeKey,
        string sectionKey,
        string name,
        int sortOrder,
        string status,
        string comment = "")
    {
        await SaveLock.WaitAsync();

        try
        {
            var data =
                await _storageService.LoadAsync();

            var testCase =
                data.TestCases.FirstOrDefault(
                    item =>
                        item.Id ==
                        testCaseId);

            if (testCase is null)
            {
                testCase =
                    new TestCaseModel
                    {
                        Id =
                            testCaseId,

                        ProjectKey =
                            projectKey,

                        TestTypeKey =
                            testTypeKey,

                        SectionKey =
                            sectionKey,

                        Name =
                            name,

                        CreatedByLogin =
                            string.Empty,

                        SortOrder =
                            sortOrder,

                        Status =
                            status,

                        Comment =
                            comment
                    };

                data.TestCases.Add(
                    testCase);
            }
            else
            {
                testCase.Status =
                    status;

                testCase.Name =
                    name;

                testCase.SortOrder =
                    sortOrder;

                testCase.Comment =
                    comment;
            }

            await _storageService.SaveAsync(
                data);
        }
        finally
        {
            SaveLock.Release();
        }
    }

    public async Task SaveStatusesAsync(
        IReadOnlyDictionary<Guid, string> statuses)
    {
        if (statuses.Count == 0)
        {
            return;
        }

        await SaveLock.WaitAsync();

        try
        {
            var data =
                await _storageService.LoadAsync();

            foreach (var testCase in data.TestCases)
            {
                if (statuses.TryGetValue(
                        testCase.Id,
                        out var status))
                {
                    testCase.Status =
                        status;
                }
            }

            await _storageService.SaveAsync(
                data);
        }
        finally
        {
            SaveLock.Release();
        }
    }

    public async Task<bool> UpdateSortOrderAsync(
        Guid testCaseId,
        int sortOrder)
    {
        var data =
            await _storageService.LoadAsync();

        var testCase =
            data.TestCases.FirstOrDefault(
                item =>
                    item.Id ==
                    testCaseId);

        if (testCase is null)
        {
            return false;
        }

        testCase.SortOrder =
            sortOrder;

        await _storageService.SaveAsync(
            data);

        return true;
    }

    public async Task<bool> RenameTestCaseAsync(
        Guid testCaseId,
        string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return false;
        }

        var data =
            await _storageService.LoadAsync();

        var testCase =
            data.TestCases.FirstOrDefault(
                item =>
                    item.Id ==
                    testCaseId);

        if (testCase is null)
        {
            return false;
        }

        testCase.Name =
            newName.Trim();

        await _storageService.SaveAsync(
            data);

        return true;
    }

    public async Task<bool> DeleteTestCaseAsync(
        Guid testCaseId)
    {
        var data =
            await _storageService.LoadAsync();

        var testCase =
            data.TestCases.FirstOrDefault(
                item =>
                    item.Id ==
                    testCaseId);

        if (testCase is null)
        {
            return false;
        }

        data.TestCases.Remove(
            testCase);

        await _storageService.SaveAsync(
            data);

        return true;
    }
}

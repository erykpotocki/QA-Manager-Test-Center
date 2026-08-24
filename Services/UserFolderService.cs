using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QARegressionManager.Models;

namespace QARegressionManager.Services;

public sealed class UserFolderService
{
    private readonly JsonStorageService _storageService;

    public UserFolderService(
        JsonStorageService storageService)
    {
        _storageService =
            storageService;
    }

    public async Task<IReadOnlyList<TestSectionModel>>
        GetFoldersAsync(
            string projectKey,
            string testTypeKey)
    {
        var data =
            await _storageService.LoadAsync();

        return data.Folders
            .Where(
                folder =>
                    string.Equals(
                        folder.ProjectKey,
                        projectKey,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        folder.TestTypeKey,
                        testTypeKey,
                        StringComparison.OrdinalIgnoreCase))
            .OrderBy(
                folder =>
                    folder.SortOrder)
            .ThenBy(
                folder =>
                    folder.Name,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<TestSectionModel>
        AddFolderAsync(
            string projectKey,
            string testTypeKey,
            string parentSectionKey,
            string createdByLogin)
    {
        var data =
            await _storageService.LoadAsync();

        var siblingFolders =
            data.Folders
                .Where(
                    folder =>
                        string.Equals(
                            folder.ProjectKey,
                            projectKey,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            folder.TestTypeKey,
                            testTypeKey,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            folder.ParentSectionKey,
                            parentSectionKey,
                            StringComparison.OrdinalIgnoreCase))
                .ToList();

        var folderName =
            CreateUniqueFolderName(
                siblingFolders);

        var nextSortOrder =
            siblingFolders
                .Select(
                    folder =>
                        folder.SortOrder)
                .DefaultIfEmpty(
                    0)
                .Max() + 1000;

        var folder =
            new TestSectionModel
            {
                Id =
                    Guid.NewGuid(),

                ProjectKey =
                    projectKey,

                TestTypeKey =
                    testTypeKey,

                SectionKey =
                    Guid.NewGuid()
                        .ToString("N"),

                ParentSectionKey =
                    parentSectionKey,

                Name =
                    folderName,

                CreatedByLogin =
                    createdByLogin?.Trim() ?? string.Empty,

                IsSystem =
                    false,

                SortOrder =
                    nextSortOrder
            };

        data.Folders.Add(
            folder);

        await _storageService.SaveAsync(
            data);

        return folder;
    }

    public async Task<bool>
        UpdateSortOrderAsync(
            Guid folderId,
            int sortOrder)
    {
        var data =
            await _storageService.LoadAsync();

        var folder =
            data.Folders.FirstOrDefault(
                item =>
                    item.Id ==
                    folderId);

        if (folder is null)
        {
            return false;
        }

        folder.SortOrder =
            sortOrder;

        await _storageService.SaveAsync(
            data);

        return true;
    }

    public async Task<bool>
        DeleteFolderAsync(
            Guid folderId)
    {
        var data =
            await _storageService.LoadAsync();

        var folder =
            data.Folders.FirstOrDefault(
                item =>
                    item.Id ==
                    folderId);

        if (folder is null)
        {
            return false;
        }

        if (folder.IsSystem)
        {
            return false;
        }

        var folderKeysToDelete =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                folder.SectionKey
            };

        var foundNewFolder =
            true;

        while (foundNewFolder)
        {
            foundNewFolder =
                false;

            foreach (var candidate in data.Folders)
            {
                if (folderKeysToDelete.Contains(
                        candidate.SectionKey))
                {
                    continue;
                }

                if (!folderKeysToDelete.Contains(
                        candidate.ParentSectionKey))
                {
                    continue;
                }

                folderKeysToDelete.Add(
                    candidate.SectionKey);

                foundNewFolder =
                    true;
            }
        }

        var collectionKeysToDelete =
            data.Collections
                .Where(
                    collection =>
                        folderKeysToDelete.Contains(
                            collection.ParentFolderKey))
                .Select(
                    collection =>
                        collection.CollectionKey)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        data.TestCases.RemoveAll(
            testCase =>
                collectionKeysToDelete.Contains(
                    testCase.SectionKey));

        data.Collections.RemoveAll(
            collection =>
                folderKeysToDelete.Contains(
                    collection.ParentFolderKey));

        data.Folders.RemoveAll(
            item =>
                folderKeysToDelete.Contains(
                    item.SectionKey));

        await _storageService.SaveAsync(
            data);

        return true;
    }

    public async Task<bool>
        RenameFolderAsync(
            Guid folderId,
            string newName)
    {
        if (string.IsNullOrWhiteSpace(
                newName))
        {
            return false;
        }

        var data =
            await _storageService.LoadAsync();

        var folder =
            data.Folders.FirstOrDefault(
                item =>
                    item.Id ==
                    folderId);

        if (folder is null)
        {
            return false;
        }

        if (folder.IsSystem)
        {
            return false;
        }

        folder.Name =
            newName.Trim();

        await _storageService.SaveAsync(
            data);

        return true;
    }

    private static string CreateUniqueFolderName(
        IReadOnlyCollection<TestSectionModel>
            siblingFolders)
    {
        const string baseName =
            "Nowy folder";

        var existingNames =
            siblingFolders
                .Select(
                    folder =>
                        folder.Name)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        if (!existingNames.Contains(
                baseName))
        {
            return baseName;
        }

        var suffix =
            1;

        while (existingNames.Contains(
                   $"{baseName} ({suffix})"))
        {
            suffix++;
        }

        return $"{baseName} ({suffix})";
    }
}

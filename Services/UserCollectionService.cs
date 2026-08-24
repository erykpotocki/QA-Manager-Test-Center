using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QARegressionManager.Models;

namespace QARegressionManager.Services;

public sealed class UserCollectionService
{
    private const int MaximumDescriptionLength = 180;

    private readonly JsonStorageService _storageService;

    public UserCollectionService(
        JsonStorageService storageService)
    {
        _storageService =
            storageService;
    }

    public async Task<IReadOnlyList<TestCollectionModel>>
        GetCollectionsAsync(
            string projectKey,
            string testTypeKey)
    {
        var data =
            await _storageService.LoadAsync();

        return data.Collections
            .Where(
                collection =>
                    string.Equals(
                        collection.ProjectKey,
                        projectKey,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        collection.TestTypeKey,
                        testTypeKey,
                        StringComparison.OrdinalIgnoreCase))
            .OrderBy(
                collection =>
                    collection.SortOrder)
            .ThenBy(
                collection =>
                    collection.Name,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<TestCollectionModel>
        AddCollectionAsync(
            string projectKey,
            string testTypeKey,
            string parentFolderKey,
            string name,
            string createdByLogin)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Nazwa zbioru nie może być pusta.",
                nameof(name));
        }

        var data =
            await _storageService.LoadAsync();

        var nextSortOrder =
            data.Collections
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
                            item.ParentFolderKey,
                            parentFolderKey,
                            StringComparison.OrdinalIgnoreCase))
                .Select(
                    item =>
                        item.SortOrder)
                .DefaultIfEmpty(0)
                .Max() + 1000;

        var collection =
            new TestCollectionModel
            {
                Id =
                    Guid.NewGuid(),

                ProjectKey =
                    projectKey,

                TestTypeKey =
                    testTypeKey,

                ParentFolderKey =
                    parentFolderKey,

                CollectionKey =
                    Guid.NewGuid()
                        .ToString("N"),

                Name =
                    name.Trim(),

                CreatedByLogin =
                    createdByLogin?.Trim() ?? string.Empty,

                IsSystem =
                    false,

                SortOrder =
                    nextSortOrder
            };

        data.Collections.Add(
            collection);

        await _storageService.SaveAsync(
            data);

        return collection;
    }

    public async Task<bool>
        UpdateSortOrderAsync(
            Guid collectionId,
            int sortOrder)
    {
        var data =
            await _storageService.LoadAsync();

        var collection =
            data.Collections.FirstOrDefault(
                item =>
                    item.Id ==
                    collectionId);

        if (collection is null)
        {
            return false;
        }

        collection.SortOrder =
            sortOrder;

        await _storageService.SaveAsync(
            data);

        return true;
    }

    public async Task<bool>
        SaveDescriptionAsync(
            TestCollectionModel collectionSnapshot)
    {
        var data =
            await _storageService.LoadAsync();

        var collection =
            data.Collections.FirstOrDefault(
                item =>
                    string.Equals(
                        item.ProjectKey,
                        collectionSnapshot.ProjectKey,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        item.CollectionKey,
                        collectionSnapshot.CollectionKey,
                        StringComparison.OrdinalIgnoreCase));

        if (collection is null)
        {
            collection =
                new TestCollectionModel
                {
                    Id =
                        collectionSnapshot.Id != Guid.Empty
                            ? collectionSnapshot.Id
                            : Guid.NewGuid(),

                    ProjectKey =
                        collectionSnapshot.ProjectKey,

                    TestTypeKey =
                        collectionSnapshot.TestTypeKey,

                    ParentFolderKey =
                        collectionSnapshot.ParentFolderKey,

                    CollectionKey =
                        collectionSnapshot.CollectionKey,

                    Name =
                        collectionSnapshot.Name,

                    CreatedByLogin =
                        collectionSnapshot.CreatedByLogin,

                    IsSystem =
                        collectionSnapshot.IsSystem,

                    SortOrder =
                        collectionSnapshot.SortOrder
                };

            data.Collections.Add(
                collection);
        }

        var normalizedDescription =
            (collectionSnapshot.Description ?? string.Empty)
                .Trim();

        collection.Description =
            normalizedDescription[
                ..Math.Min(
                    normalizedDescription.Length,
                    MaximumDescriptionLength)];

        await _storageService.SaveAsync(
            data);

        return true;
    }

    public async Task<bool>
        RenameCollectionAsync(
            Guid collectionId,
            string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return false;
        }

        var data =
            await _storageService.LoadAsync();

        var collection =
            data.Collections.FirstOrDefault(
                item =>
                    item.Id ==
                    collectionId);

        if (collection is null)
        {
            return false;
        }

        collection.Name =
            newName.Trim();

        await _storageService.SaveAsync(
            data);

        return true;
    }

    public async Task<bool>
        DeleteCollectionAsync(
            Guid collectionId)
    {
        var data =
            await _storageService.LoadAsync();

        var collection =
            data.Collections.FirstOrDefault(
                item =>
                    item.Id ==
                    collectionId);

        if (collection is null)
        {
            return false;
        }

        data.TestCases.RemoveAll(
            item =>
                string.Equals(
                    item.SectionKey,
                    collection.CollectionKey,
                    StringComparison.OrdinalIgnoreCase));

        data.Collections.Remove(
            collection);

        await _storageService.SaveAsync(
            data);

        return true;
    }
}

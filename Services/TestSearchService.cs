using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QARegressionManager.Services;

public sealed class TestSearchService
{
    public bool Matches(
        int testNumber,
        string testName,
        string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var searchableText = Normalize(
            $"{testNumber} {testNumber:00} {testName}");

        var searchParts = Normalize(searchText)
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        return searchParts.All(
            part => searchableText.Contains(part));
    }

    private static string Normalize(string value)
    {
        var decomposed = value
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var result = new StringBuilder();

        foreach (var character in decomposed)
        {
            var category =
                CharUnicodeInfo.GetUnicodeCategory(character);

            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            AppendNormalizedCharacter(
                result,
                character);
        }

        return result
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }

    private static void AppendNormalizedCharacter(
        StringBuilder result,
        char character)
    {
        switch (character)
        {
            case 'ł':
                result.Append('l');
                break;

            case 'ß':
                result.Append("ss");
                break;

            case 'æ':
                result.Append("ae");
                break;

            case 'œ':
                result.Append("oe");
                break;

            case 'ø':
                result.Append('o');
                break;

            case 'đ':
                result.Append('d');
                break;

            case 'þ':
                result.Append("th");
                break;

            case '!':
                result.Append('1');
                break;

            case '@':
                result.Append('2');
                break;

            case '#':
                result.Append('3');
                break;

            case '$':
                result.Append('4');
                break;

            case '%':
                result.Append('5');
                break;

            case '^':
                result.Append('6');
                break;

            case '&':
                result.Append('7');
                break;

            case '*':
                result.Append('8');
                break;

            case '(':
                result.Append('9');
                break;

            case ')':
                result.Append('0');
                break;

            default:
                result.Append(character);
                break;
        }
    }
}
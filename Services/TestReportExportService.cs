using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using QARegressionManager.Models;
using QARegressionManager.Views;
using MigraColor = MigraDoc.DocumentObjectModel.Color;

namespace QARegressionManager.Services;

public sealed class TestReportExportService
{
    private static readonly object PdfFontLock =
        new();

    private static bool _pdfFontsConfigured;

    public async Task<string?> ExportAsync(
        TestReport report,
        string directoryPath,
        string fileNameBase,
        TestReportFormat format)
    {
        try
        {
            Directory.CreateDirectory(
                directoryPath);

            return format switch
            {
                TestReportFormat.Excel =>
                    await Task.Run(
                        () =>
                            ExportExcel(
                                report,
                                directoryPath,
                                fileNameBase)),

                TestReportFormat.Pdf =>
                    await Task.Run(
                        () =>
                            ExportPdf(
                                report,
                                directoryPath,
                                fileNameBase)),

                _ =>
                    await ExportJsonAsync(
                        report,
                        directoryPath,
                        fileNameBase)
            };
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string> ExportJsonAsync(
        TestReport report,
        string directoryPath,
        string fileNameBase)
    {
        var outputPath =
            CreateUniqueOutputPath(
                directoryPath,
                fileNameBase,
                ".json");

        var json =
            JsonSerializer.Serialize(
                report,
                new JsonSerializerOptions
                {
                    WriteIndented =
                        true
                });

        await File.WriteAllTextAsync(
            outputPath,
            json);

        return outputPath;
    }

    private static string ExportExcel(
        TestReport report,
        string directoryPath,
        string fileNameBase)
    {
        var outputPath =
            CreateUniqueOutputPath(
                directoryPath,
                fileNameBase,
                ".xlsx");

        using var workbook =
            new XLWorkbook();

        workbook.Properties.Title =
            "Raport z wykonania testów";

        workbook.Properties.Subject =
            $"{report.Metadata.ProjectName} - {report.Metadata.ApplicationVersion}";

        workbook.Properties.Author =
            report.Metadata.TesterLogin;

        var summarySheet =
            workbook.Worksheets.Add(
                "Podsumowanie");

        var resultsSheet =
            workbook.Worksheets.Add(
                "Wyniki testów");

        BuildResultsSheet(
            resultsSheet,
            report);

        BuildSummarySheet(
            summarySheet,
            report,
            Math.Max(
                2,
                report.TestCases.Count +
                1));

        summarySheet.Position =
            1;

        summarySheet.TabColor =
            XLColor.FromHtml(
                "#28C76F");

        resultsSheet.TabColor =
            XLColor.FromHtml(
                "#68726B");

        workbook.SaveAs(
            outputPath);

        return outputPath;
    }

    private static void BuildSummarySheet(
        IXLWorksheet sheet,
        TestReport report,
        int lastResultsRow)
    {
        sheet.ShowGridLines =
            false;

        sheet.Style.Font.FontName =
            "Arial";

        sheet.Style.Font.FontSize =
            10;

        sheet.Column(
                1)
            .Width =
            17;

        sheet.Column(
                2)
            .Width =
            18;

        sheet.Column(
                3)
            .Width =
            14;

        sheet.Column(
                4)
            .Width =
            4;

        sheet.Column(
                5)
            .Width =
            22;

        sheet.Column(
                6)
            .Width =
            18;

        sheet.Columns(
                7,
                8)
            .Width =
            14;

        var titleRange =
            sheet.Range(
                "A1:H2");

        titleRange.Merge();

        var title =
            titleRange.FirstCell();

        title.Value =
            "RAPORT TESTÓW";

        title.Style.Font.Bold =
            true;

        title.Style.Font.FontSize =
            18;

        title.Style.Font.FontColor =
            XLColor.FromHtml(
                "#19944D");

        title.Style.Fill.BackgroundColor =
            XLColor.FromHtml(
                "#E8F7ED");

        title.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Left;

        title.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        title.Style.Alignment.SetIndent(
            1);

        titleRange.Style.Fill.BackgroundColor =
            XLColor.FromHtml(
                "#E8F7ED");

        titleRange.Style.Border.BottomBorder =
            XLBorderStyleValues.Thin;

        titleRange.Style.Border.BottomBorderColor =
            XLColor.FromHtml(
                "#BEE8CD");

        sheet.Row(
                1)
            .Height =
            30;

        sheet.Row(
                2)
            .Height =
            18;

        var authorCell =
            sheet.Cell(
                "H3");

        authorCell.Value =
            "© 2026 Eryk Potocki";

        authorCell.Style.Font.FontSize =
            8;

        authorCell.Style.Font.FontColor =
            XLColor.FromHtml(
                "#68726B");

        authorCell.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Right;

        WriteMetadataRow(
            sheet,
            4,
            "Projekt",
            report.Metadata.ProjectName,
            "Wersja",
            report.Metadata.ApplicationVersion);

        WriteMetadataRow(
            sheet,
            5,
            "Tester",
            report.Metadata.TesterLogin,
            "Tryb",
            GetReadableSessionMode(
                report.Metadata.SessionMode));

        WriteMetadataRow(
            sheet,
            6,
            "Wygenerowano",
            report.Metadata.GeneratedAt.LocalDateTime.ToString(
                "dd.MM.yyyy HH:mm",
                CultureInfo.GetCultureInfo(
                    "pl-PL")),
            "Identyfikator sesji",
            report.Metadata.SessionId == Guid.Empty
                ? "-"
                : report.Metadata.SessionId.ToString());

        sheet.Range(
                "A9:H9")
            .Merge();

        var summaryHeader =
            sheet.Cell(
                "A9");

        summaryHeader.Value =
            "PODSUMOWANIE";

        summaryHeader.Style.Font.Bold =
            true;

        summaryHeader.Style.Font.FontSize =
            14;

        summaryHeader.Style.Font.FontColor =
            XLColor.FromHtml(
                "#17221B");

        CreateSummaryCard(
            sheet,
            "A11:B13",
            "Łącznie",
            $"=COUNTA('Wyniki testów'!$A$2:$A${lastResultsRow})",
            "#E9EEF5",
            "#26384A",
            "0");

        CreateSummaryCard(
            sheet,
            "C11:D13",
            "Wykonane",
            $"=COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Sukces\")+COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Niepowodzenie\")+COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Zablokowany\")+COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Nie dotyczy\")",
            "#E8F2FF",
            "#1F6FBF",
            "0");

        CreateSummaryCard(
            sheet,
            "E11:F13",
            "Niewykonane",
            $"=COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Niewykonany\")+COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"W trakcie\")",
            "#F1F3F2",
            "#68726B",
            "0");

        CreateSummaryCard(
            sheet,
            "G11:H13",
            "Postęp",
            $"=IFERROR((COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Sukces\")+COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Niepowodzenie\")+COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Zablokowany\")+COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Nie dotyczy\"))/COUNTA('Wyniki testów'!$A$2:$A${lastResultsRow}),0)",
            "#E8F7ED",
            "#19944D",
            "0.0%");

        CreateSummaryCard(
            sheet,
            "A15:B17",
            "Sukces",
            $"=COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Sukces\")",
            "#E8F7ED",
            "#19944D",
            "0");

        CreateSummaryCard(
            sheet,
            "C15:D17",
            "Niepowodzenie",
            $"=COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Niepowodzenie\")",
            "#FDEBEC",
            "#B3262D",
            "0");

        CreateSummaryCard(
            sheet,
            "E15:F17",
            "Zablokowane",
            $"=COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Zablokowany\")",
            "#FFF0DE",
            "#B65D0A",
            "0");

        CreateSummaryCard(
            sheet,
            "G15:H17",
            "Nie dotyczy",
            $"=COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Nie dotyczy\")",
            "#FFF7D8",
            "#8A6D00",
            "0");

        sheet.Range(
                "A20:D20")
            .Merge();

        sheet.Cell(
                "A20")
            .Value =
            "Wyniki według statusu";

        sheet.Cell(
                "A20")
            .Style.Font.Bold =
            true;

        var statuses =
            new[]
            {
                (
                    "Sukces",
                    "#E8F7ED",
                    "#19944D"),
                (
                    "Niepowodzenie",
                    "#FDEBEC",
                    "#B3262D"),
                (
                    "Zablokowany",
                    "#FFF0DE",
                    "#B65D0A"),
                (
                    "Nie dotyczy",
                    "#FFF7D8",
                    "#8A6D00"),
                (
                    "W trakcie",
                    "#E8F2FF",
                    "#1F6FBF"),
                (
                    "Niewykonany",
                    "#F1F3F2",
                    "#68726B")
            };

        for (var index = 0;
             index < statuses.Length;
             index++)
        {
            var row =
                22 +
                index;

            var (
                status,
                fill,
                font) =
                statuses[index];

            sheet.Range(
                    row,
                    1,
                    row,
                    3)
                .Merge();

            sheet.Cell(
                    row,
                    1)
                .Value =
                status;

            sheet.Cell(
                    row,
                    4)
                .FormulaA1 =
                $"COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"{status}\")";

            var range =
                sheet.Range(
                    row,
                    1,
                    row,
                    4);

            range.Style.Fill.BackgroundColor =
                XLColor.FromHtml(
                    fill);

            range.Style.Font.FontColor =
                XLColor.FromHtml(
                    font);

            range.Style.Border.BottomBorder =
                XLBorderStyleValues.Thin;

            range.Style.Border.BottomBorderColor =
                XLColor.White;

            sheet.Cell(
                    row,
                    4)
                .Style.Font.Bold =
                true;

            sheet.Cell(
                    row,
                    4)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
        }

        sheet.Range(
                "F20:H20")
            .Merge();

        sheet.Cell(
                "F20")
            .Value =
            "Postęp wykonania";

        sheet.Cell(
                "F20")
            .Style.Font.Bold =
            true;

        sheet.Range(
                "F22:H25")
            .Merge();

        sheet.Cell(
                "F22")
            .FormulaA1 =
            $"=IFERROR((COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Sukces\")+COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Niepowodzenie\")+COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Zablokowany\")+COUNTIF('Wyniki testów'!$F$2:$F${lastResultsRow},\"Nie dotyczy\"))/COUNTA('Wyniki testów'!$A$2:$A${lastResultsRow}),0)";

        sheet.Cell(
                "F22")
            .Style.NumberFormat.Format =
            "0%";

        sheet.Cell(
                "F22")
            .Style.Font.Bold =
            true;

        sheet.Cell(
                "F22")
            .Style.Font.FontSize =
            28;

        sheet.Cell(
                "F22")
            .Style.Font.FontColor =
            XLColor.FromHtml(
                "#19944D");

        sheet.Cell(
                "F22")
            .Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        sheet.Cell(
                "F22")
            .Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        sheet.Range(
                "F22:H25")
            .Style.Fill.BackgroundColor =
            XLColor.FromHtml(
                "#E8F7ED");

        sheet.Range(
                "F22:H25")
            .Style.Border.OutsideBorder =
            XLBorderStyleValues.Thin;

        sheet.Range(
                "F22:H25")
            .Style.Border.OutsideBorderColor =
            XLColor.FromHtml(
                "#BEE8CD");

        sheet.Range(
                "A19:H28")
            .Clear(
                XLClearOptions.All);

        sheet.SheetView.FreezeRows(
            2);

        sheet.PageSetup.PageOrientation =
            XLPageOrientation.Landscape;

        sheet.PageSetup.PagesWide =
            1;

        sheet.PageSetup.PagesTall =
            1;

        sheet.PageSetup.Margins.Top =
            0.4;

        sheet.PageSetup.Margins.Bottom =
            0.4;

        sheet.PageSetup.CenterHorizontally =
            true;
    }

    private static void BuildResultsSheet(
        IXLWorksheet sheet,
        TestReport report)
    {
        sheet.ShowGridLines =
            false;

        sheet.Style.Font.FontName =
            "Arial";

        sheet.Style.Font.FontSize =
            10;

        var headers =
            new[]
            {
                "Lp.",
                "Rodzaj testów",
                "Zbiór",
                "Ścieżka",
                "Przypadek testowy",
                "Wynik",
                "Komentarz"
            };

        for (var column = 0;
             column < headers.Length;
             column++)
        {
            sheet.Cell(
                    1,
                    column +
                    1)
                .Value =
                headers[column];
        }

        for (var index = 0;
             index < report.TestCases.Count;
             index++)
        {
            var testCase =
                report.TestCases[index];

            var row =
                index +
                2;

            sheet.Cell(
                    row,
                    1)
                .Value =
                index +
                1;

            sheet.Cell(
                    row,
                    2)
                .Value =
                GetReadableTestType(
                    testCase.TestType);

            sheet.Cell(
                    row,
                    3)
                .Value =
                testCase.Collection;

            sheet.Cell(
                    row,
                    4)
                .Value =
                testCase.Path;

            sheet.Cell(
                    row,
                    5)
                .Value =
                testCase.Name;

            var readableStatus =
                GetReadableStatus(
                    testCase.Status);

            var resultCell =
                sheet.Cell(
                    row,
                    6);

            resultCell.Value =
                readableStatus;

            ApplyExcelStatusStyle(
                resultCell,
                readableStatus);

            sheet.Cell(row, 7).Value = testCase.Comment;
        }

        var lastRow =
            Math.Max(
                2,
                report.TestCases.Count +
                1);

        var table =
            sheet.Range(
                    1,
                    1,
                    lastRow,
                    7)
                .CreateTable(
                    "WynikiTestow");

        table.Theme =
            XLTableTheme.TableStyleMedium4;

        table.ShowAutoFilter =
            true;

        var headerRange =
            sheet.Range(
                1,
                1,
                1,
                7);

        headerRange.Style.Fill.BackgroundColor =
            XLColor.FromHtml(
                "#19944D");

        headerRange.Style.Font.FontColor =
            XLColor.White;

        headerRange.Style.Font.Bold =
            true;

        headerRange.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        sheet.Row(
                1)
            .Height =
            26;

        sheet.SheetView.FreezeRows(
            1);

        sheet.Column(
                1)
            .Width =
            7;

        sheet.Column(
                2)
            .Width =
            20;

        sheet.Column(
                3)
            .Width =
            26;

        sheet.Column(
                4)
            .Width =
            34;

        sheet.Column(
                5)
            .Width =
            52;

        sheet.Column(
                6)
            .Width =
            19;

        sheet.Column(7).Width = 42;

        sheet.Range(
                2,
                2,
                lastRow,
                7)
            .Style.Alignment.WrapText =
            true;

        sheet.Range(
                2,
                1,
                lastRow,
                1)
            .Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        sheet.Range(
                2,
                6,
                lastRow,
                6)
            .Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        sheet.Rows(
                2,
                lastRow)
            .Height =
            36;

        sheet.Range(
                2,
                1,
                lastRow,
                6)
            .Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        sheet.PageSetup.PageOrientation =
            XLPageOrientation.Landscape;

        sheet.PageSetup.PagesWide =
            1;

        sheet.PageSetup.PagesTall =
            0;

        sheet.PageSetup.Margins.Top =
            0.4;

        sheet.PageSetup.Margins.Bottom =
            0.4;

        sheet.PageSetup.SetRowsToRepeatAtTop(
            1,
            1);
    }

    private static void WriteMetadataRow(
        IXLWorksheet sheet,
        int row,
        string leftLabel,
        string leftValue,
        string rightLabel,
        string rightValue)
    {
        sheet.Cell(
                row,
                1)
            .Value =
            leftLabel;

        sheet.Range(
                row,
                2,
                row,
                3)
            .Merge();

        sheet.Cell(
                row,
                2)
            .Value =
            leftValue;

        sheet.Cell(
                row,
                5)
            .Value =
            rightLabel;

        sheet.Range(
                row,
                6,
                row,
                8)
            .Merge();

        sheet.Cell(
                row,
                6)
            .Value =
            rightValue;

        sheet.Cell(
                row,
                1)
            .Style.Font.Bold =
            true;

        sheet.Cell(
                row,
                5)
            .Style.Font.Bold =
            true;

        sheet.Cell(
                row,
                1)
            .Style.Font.FontColor =
            XLColor.FromHtml(
                "#68726B");

        sheet.Cell(
                row,
                5)
            .Style.Font.FontColor =
            XLColor.FromHtml(
                "#68726B");

        sheet.Cell(
                row,
                1)
            .Style.Fill.BackgroundColor =
            XLColor.FromHtml(
                "#F1F3F2");

        sheet.Cell(
                row,
                5)
            .Style.Fill.BackgroundColor =
            XLColor.FromHtml(
                "#F1F3F2");

        sheet.Range(
                row,
                2,
                row,
                3)
            .Style.Alignment.SetIndent(
                1);

        sheet.Range(
                row,
                6,
                row,
                8)
            .Style.Alignment.SetIndent(
                1);

        sheet.Range(
                row,
                1,
                row,
                3)
            .Style.Border.BottomBorder =
            XLBorderStyleValues.Thin;

        sheet.Range(
                row,
                5,
                row,
                8)
            .Style.Border.BottomBorder =
            XLBorderStyleValues.Thin;

        sheet.Range(
                row,
                1,
                row,
                3)
            .Style.Border.BottomBorderColor =
            XLColor.FromHtml(
                "#E2E7E4");

        sheet.Range(
                row,
                5,
                row,
                8)
            .Style.Border.BottomBorderColor =
            XLColor.FromHtml(
                "#E2E7E4");

        sheet.Range(
                row,
                1,
                row,
                8)
            .Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        sheet.Row(
                row)
            .Height =
            26;
    }

    private static void CreateSummaryCard(
        IXLWorksheet sheet,
        string rangeAddress,
        string label,
        string formula,
        string fillColor,
        string fontColor,
        string numberFormat)
    {
        var range =
            sheet.Range(
                rangeAddress);

        var firstRow =
            range.FirstRow()
                .RowNumber();

        var lastRow =
            range.LastRow()
                .RowNumber();

        var firstColumn =
            range.FirstColumn()
                .ColumnNumber();

        var lastColumn =
            range.LastColumn()
                .ColumnNumber();

        var labelRange =
            sheet.Range(
                firstRow,
                firstColumn,
                firstRow,
                lastColumn);

        labelRange.Merge();

        labelRange.FirstCell()
            .Value =
            label;

        var valueRange =
            sheet.Range(
                firstRow +
                1,
                firstColumn,
                lastRow,
                lastColumn);

        valueRange.Merge();

        valueRange.FirstCell()
            .FormulaA1 =
            formula;

        valueRange.FirstCell()
            .Style.NumberFormat.Format =
            numberFormat;

        range.Style.Fill.BackgroundColor =
            XLColor.FromHtml(
                fillColor);

        range.Style.Font.FontColor =
            XLColor.FromHtml(
                fontColor);

        range.Style.Border.OutsideBorder =
            XLBorderStyleValues.Thin;

        range.Style.Border.OutsideBorderColor =
            XLColor.FromHtml(
                fillColor);

        range.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        range.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        labelRange.FirstCell()
            .Style.Font.Bold =
            true;

        valueRange.FirstCell()
            .Style.Font.Bold =
            true;

        valueRange.FirstCell()
            .Style.Font.FontSize =
            22;
    }

    private static void ApplyExcelStatusStyle(
        IXLCell cell,
        string status)
    {
        var (
            fill,
            font) =
            GetStatusColors(
                status);

        cell.Style.Fill.BackgroundColor =
            XLColor.FromHtml(
                fill);

        cell.Style.Font.FontColor =
            XLColor.FromHtml(
                font);

        cell.Style.Font.Bold =
            true;
    }

    private static string ExportPdf(
        TestReport report,
        string directoryPath,
        string fileNameBase)
    {
        EnsurePdfFontsConfigured();

        var outputPath =
            CreateUniqueOutputPath(
                directoryPath,
                fileNameBase,
                ".pdf");

        var document =
            BuildPdfDocument(
                report);

        var renderer =
            new PdfDocumentRenderer
            {
                Document =
                    document
            };

        renderer.RenderDocument();
        renderer.Save(
            outputPath);

        return outputPath;
    }

    private static string CreateUniqueOutputPath(
        string directoryPath,
        string fileNameBase,
        string extension)
    {
        var outputPath =
            Path.Combine(
                directoryPath,
                fileNameBase +
                extension);

        if (!File.Exists(
                outputPath))
        {
            return outputPath;
        }

        for (var copyNumber = 1;
             ;
             copyNumber++)
        {
            outputPath =
                Path.Combine(
                    directoryPath,
                    $"{fileNameBase} ({copyNumber}){extension}");

            if (!File.Exists(
                    outputPath))
            {
                return outputPath;
            }
        }
    }

    private static void EnsurePdfFontsConfigured()
    {
        lock (PdfFontLock)
        {
            if (_pdfFontsConfigured)
            {
                return;
            }

            GlobalFontSettings.UseWindowsFontsUnderWindows =
                true;

            _pdfFontsConfigured =
                true;
        }
    }

    private static Document BuildPdfDocument(
        TestReport report)
    {
        var document =
            new Document();

        document.Info.Title =
            "Raport z wykonania testów";

        document.Info.Author =
            report.Metadata.TesterLogin;

        var normalStyle =
            document.Styles[
                StyleNames.Normal]!;

        normalStyle.Font.Name =
            "Arial";

        normalStyle.Font.Size =
            9;

        normalStyle.Font.Color =
            PdfColor(
                "#17221B");

        var section =
            document.AddSection();

        section.PageSetup.PageFormat =
            PageFormat.A4;

        section.PageSetup.Orientation =
            Orientation.Portrait;

        section.PageSetup.TopMargin =
            Unit.FromCentimeter(
                2.0);

        section.PageSetup.BottomMargin =
            Unit.FromCentimeter(
                1.5);

        section.PageSetup.LeftMargin =
            Unit.FromCentimeter(
                1.5);

        section.PageSetup.RightMargin =
            Unit.FromCentimeter(
                1.5);

        BuildPdfHeaderAndFooter(
            section,
            report);

        var title =
            section.AddParagraph();

        title.Format.SpaceAfter =
            Unit.FromCentimeter(
                0.15);

        var titleText =
            title.AddFormattedText(
                "Raport z wykonania testów");

        titleText.Bold =
            true;

        titleText.Font.Size =
            22;

        titleText.Font.Color =
            PdfColor(
                "#17221B");

        var subtitle =
            section.AddParagraph(
                "Czytelne podsumowanie przebiegu i wyników sesji testowej.");

        subtitle.Format.SpaceAfter =
            Unit.FromCentimeter(
                0.55);

        subtitle.Format.Font.Color =
            PdfColor(
                "#68726B");

        BuildPdfMetadata(
            section,
            report);

        var summaryTitle =
            section.AddParagraph(
                "Podsumowanie");

        summaryTitle.Format.SpaceBefore =
            Unit.FromCentimeter(
                0.55);

        summaryTitle.Format.SpaceAfter =
            Unit.FromCentimeter(
                0.2);

        summaryTitle.Format.Font.Bold =
            true;

        summaryTitle.Format.Font.Size =
            14;

        BuildPdfSummary(
            section,
            report);

        var resultsTitle =
            section.AddParagraph(
                "Szczegóły testów");

        resultsTitle.Format.SpaceBefore =
            Unit.FromCentimeter(
                0.6);

        resultsTitle.Format.SpaceAfter =
            Unit.FromCentimeter(
                0.2);

        resultsTitle.Format.Font.Bold =
            true;

        resultsTitle.Format.Font.Size =
            14;

        BuildPdfResults(
            section,
            report);

        return document;
    }

    private static void BuildPdfHeaderAndFooter(
        Section section,
        TestReport report)
    {
        var header =
            section.Headers.Primary.AddParagraph(
                "QA Manager • © 2026 Eryk Potocki");

        header.Format.Font.Bold =
            true;

        header.Format.Font.Size =
            9;

        header.Format.Font.Color =
            PdfColor(
                "#19944D");

        header.Format.Alignment =
            ParagraphAlignment.Right;

        var footer =
            section.Footers.Primary.AddParagraph();

        footer.Format.Font.Size =
            8;

        footer.Format.Font.Color =
            PdfColor(
                "#68726B");

        footer.AddText(
            $"{report.Metadata.ProjectName} | {report.Metadata.ApplicationVersion} | strona ");

        footer.AddPageField();

        footer.AddText(
            " z ");

        footer.AddNumPagesField();

        footer.AddText(
            " | © 2026 Eryk Potocki");
    }

    private static void BuildPdfMetadata(
        Section section,
        TestReport report)
    {
        var table =
            section.AddTable();

        table.Borders.Width =
            0.4;

        table.Borders.Color =
            PdfColor(
                "#D8E0DB");

        table.AddColumn(
            Unit.FromCentimeter(
                2.7));

        table.AddColumn(
            Unit.FromCentimeter(
                5.8));

        table.AddColumn(
            Unit.FromCentimeter(
                2.7));

        table.AddColumn(
            Unit.FromCentimeter(
                5.8));

        AddPdfMetadataRow(
            table,
            "Projekt",
            report.Metadata.ProjectName,
            "Wersja",
            report.Metadata.ApplicationVersion);

        AddPdfMetadataRow(
            table,
            "Tester",
            report.Metadata.TesterLogin,
            "Tryb",
            GetReadableSessionMode(
                report.Metadata.SessionMode));

        AddPdfMetadataRow(
            table,
            "Wygenerowano",
            report.Metadata.GeneratedAt.LocalDateTime.ToString(
                "dd.MM.yyyy HH:mm",
                CultureInfo.GetCultureInfo(
                    "pl-PL")),
            "Sesja",
            report.Metadata.SessionId == Guid.Empty
                ? "-"
                : report.Metadata.SessionId.ToString());
    }

    private static void AddPdfMetadataRow(
        Table table,
        string leftLabel,
        string leftValue,
        string rightLabel,
        string rightValue)
    {
        var row =
            table.AddRow();

        row.TopPadding =
            Unit.FromPoint(
                5);

        row.BottomPadding =
            Unit.FromPoint(
                5);

        row.Cells[0].Shading.Color =
            PdfColor(
                "#F1F3F2");

        row.Cells[2].Shading.Color =
            PdfColor(
                "#F1F3F2");

        var leftLabelText =
            row.Cells[0]
                .AddParagraph(
                    leftLabel);

        leftLabelText.Format.Font.Bold =
            true;

        row.Cells[1]
            .AddParagraph(
                leftValue);

        var rightLabelText =
            row.Cells[2]
                .AddParagraph(
                    rightLabel);

        rightLabelText.Format.Font.Bold =
            true;

        row.Cells[3]
            .AddParagraph(
                rightValue);
    }

    private static void BuildPdfSummary(
        Section section,
        TestReport report)
    {
        var table =
            section.AddTable();

        for (var index = 0;
             index < 4;
             index++)
        {
            table.AddColumn(
                Unit.FromCentimeter(
                    4.25));
        }

        var row =
            table.AddRow();

        row.Height =
            Unit.FromCentimeter(
                1.55);

        AddPdfSummaryCell(
            row.Cells[0],
            "Łącznie",
            report.Summary.Total,
            "#E9EEF5",
            "#26384A");

        AddPdfSummaryCell(
            row.Cells[1],
            "Wykonane",
            report.Summary.Success +
            report.Summary.Failed +
            report.Summary.Blocked +
            report.Summary.NotApplicable,
            "#E8F2FF",
            "#1F6FBF");

        AddPdfSummaryCell(
            row.Cells[2],
            "Niewykonane",
            report.Summary.NotStarted +
            report.Summary.InProgress,
            "#F1F3F2",
            "#68726B");

        AddPdfSummaryCell(
            row.Cells[3],
            "Postęp",
            $"{report.Summary.CompletionPercent:0.#}%",
            "#E8F7ED",
            "#19944D");

        var secondRow =
            table.AddRow();

        secondRow.Height =
            Unit.FromCentimeter(
                1.55);

        AddPdfSummaryCell(
            secondRow.Cells[0],
            "Sukces",
            report.Summary.Success,
            "#E8F7ED",
            "#19944D");

        AddPdfSummaryCell(
            secondRow.Cells[1],
            "Niepowodzenie",
            report.Summary.Failed,
            "#FDEBEC",
            "#B3262D");

        AddPdfSummaryCell(
            secondRow.Cells[2],
            "Zablokowane",
            report.Summary.Blocked,
            "#FFF0DE",
            "#B65D0A");

        AddPdfSummaryCell(
            secondRow.Cells[3],
            "Nie dotyczy",
            report.Summary.NotApplicable,
            "#FFF7D8",
            "#8A6D00");
    }

    private static void AddPdfSummaryCell(
        Cell cell,
        string label,
        object value,
        string fillColor,
        string fontColor)
    {
        cell.Shading.Color =
            PdfColor(
                fillColor);

        cell.Borders.Color =
            PdfColor(
                "#FFFFFF");

        var labelParagraph =
            cell.AddParagraph(
                label);

        labelParagraph.Format.Font.Bold =
            true;

        labelParagraph.Format.Alignment =
            ParagraphAlignment.Center;

        var valueParagraph =
            cell.AddParagraph(
                Convert.ToString(
                    value,
                    CultureInfo.InvariantCulture)
                ?? string.Empty);

        valueParagraph.Format.Font.Bold =
            true;

        valueParagraph.Format.Font.Size =
            16;

        valueParagraph.Format.Font.Color =
            PdfColor(
                fontColor);

        valueParagraph.Format.Alignment =
            ParagraphAlignment.Center;
    }

    private static void BuildPdfResults(
        Section section,
        TestReport report)
    {
        var table =
            section.AddTable();

        table.Borders.Width =
            0.35;

        table.Borders.Color =
            PdfColor(
                "#D8E0DB");

        table.AddColumn(
            Unit.FromCentimeter(
                0.8));

        table.AddColumn(
            Unit.FromCentimeter(
                2.6));

        table.AddColumn(
            Unit.FromCentimeter(
                3.1));

        table.AddColumn(
            Unit.FromCentimeter(
                5.4));

        table.AddColumn(
            Unit.FromCentimeter(
                2.4));

        table.AddColumn(
            Unit.FromCentimeter(
                2.7));

        var header =
            table.AddRow();

        header.HeadingFormat =
            true;

        header.Shading.Color =
            PdfColor(
                "#19944D");

        header.TopPadding =
            Unit.FromPoint(
                5);

        header.BottomPadding =
            Unit.FromPoint(
                5);

        var headers =
            new[]
            {
                "Lp.",
                "Rodzaj",
                "Zbiór",
                "Przypadek testowy",
                "Wynik",
                "Komentarz"
            };

        for (var index = 0;
             index < headers.Length;
             index++)
        {
            var paragraph =
                header.Cells[index]
                    .AddParagraph(
                        headers[index]);

            paragraph.Format.Font.Bold =
                true;

            paragraph.Format.Font.Color =
                Colors.White;

            paragraph.Format.Alignment =
                index == 3
                    ? ParagraphAlignment.Left
                    : ParagraphAlignment.Center;
        }

        for (var index = 0;
             index < report.TestCases.Count;
             index++)
        {
            var testCase =
                report.TestCases[index];

            var row =
                table.AddRow();

            row.TopPadding =
                Unit.FromPoint(
                    4);

            row.BottomPadding =
                Unit.FromPoint(
                    4);

            if (index % 2 != 0)
            {
                row.Shading.Color =
                    PdfColor(
                        "#F7F9F8");
            }

            AddPdfTableText(
                row.Cells[0],
                (index +
                 1).ToString(
                    CultureInfo.InvariantCulture),
                ParagraphAlignment.Center);

            AddPdfTableText(
                row.Cells[1],
                GetReadableTestType(
                    testCase.TestType),
                ParagraphAlignment.Left);

            AddPdfTableText(
                row.Cells[2],
                testCase.Collection,
                ParagraphAlignment.Left);

            AddPdfTableText(
                row.Cells[3],
                testCase.Name,
                ParagraphAlignment.Left);

            var readableStatus =
                GetReadableStatus(
                    testCase.Status);

            var statusParagraph =
                AddPdfTableText(
                    row.Cells[4],
                    readableStatus,
                    ParagraphAlignment.Center);

            var (
                fill,
                font) =
                GetStatusColors(
                    readableStatus);

            row.Cells[4].Shading.Color =
                PdfColor(
                    fill);

            statusParagraph.Format.Font.Bold =
                true;

            AddPdfTableText(
                row.Cells[5],
                testCase.Comment,
                ParagraphAlignment.Left);

            statusParagraph.Format.Font.Color =
                PdfColor(
                    font);
        }

        if (report.TestCases.Count == 0)
        {
            var emptyRow =
                table.AddRow();

            emptyRow.Cells[0].MergeRight =
                4;

            var paragraph =
                emptyRow.Cells[0]
                    .AddParagraph(
                        "Brak przypadków testowych w raporcie.");

            paragraph.Format.Alignment =
                ParagraphAlignment.Center;

            paragraph.Format.Font.Color =
                PdfColor(
                    "#68726B");
        }
    }

    private static Paragraph AddPdfTableText(
        Cell cell,
        string text,
        ParagraphAlignment alignment)
    {
        var paragraph =
            cell.AddParagraph(
                text);

        paragraph.Format.Alignment =
            alignment;

        return paragraph;
    }

    private static MigraColor PdfColor(
        string hexColor)
    {
        var hex =
            hexColor.TrimStart(
                '#');

        return MigraColor.FromRgb(
            byte.Parse(
                hex[..2],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture),
            byte.Parse(
                hex.Substring(
                    2,
                    2),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture),
            byte.Parse(
                hex.Substring(
                    4,
                    2),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture));
    }

    private static string GetReadableSessionMode(
        string sessionMode)
    {
        return string.Equals(
            sessionMode,
            "Assigned",
            StringComparison.OrdinalIgnoreCase)
            ? "Sesja przypisana"
            : "Testy ad-hoc";
    }

    private static string GetReadableTestType(
        string testType)
    {
        return testType.ToUpperInvariant() switch
        {
            "PROJECT" =>
                "Testy projektu",

            "REGRESSION" =>
                "Regresja",

            "FUNCTIONAL" =>
                "Testy funkcjonalne",

            _ =>
                testType
        };
    }

    private static string GetReadableStatus(
        string status)
    {
        return status switch
        {
            "Success" =>
                "Sukces",

            "Failed" =>
                "Niepowodzenie",

            "Blocked" =>
                "Zablokowany",

            "NA" =>
                "Nie dotyczy",

            "InProgress" =>
                "W trakcie",

            "None" =>
                "Niewykonany",

            _ =>
                status
        };
    }

    private static (
        string Fill,
        string Font)
        GetStatusColors(
            string status)
    {
        return status switch
        {
            "Sukces" =>
                (
                    "#E8F7ED",
                    "#19944D"),

            "Niepowodzenie" =>
                (
                    "#FDEBEC",
                    "#B3262D"),

            "Zablokowany" =>
                (
                    "#FFF0DE",
                    "#B65D0A"),

            "Nie dotyczy" =>
                (
                    "#FFF7D8",
                    "#8A6D00"),

            "W trakcie" =>
                (
                    "#E8F2FF",
                    "#1F6FBF"),

            _ =>
                (
                    "#F1F3F2",
                    "#68726B")
        };
    }
}

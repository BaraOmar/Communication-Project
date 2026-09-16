using ClosedXML.Excel;
using CommunicationProject.Services.CommunicationLinks.Import;
using CommunicationProject.Services.ExcelImport;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationProject.Controllers;




public class ExcelViewerController : Controller
{
    private readonly ExcelCommunicationLinkImportService
    _communicationLinkImportService;

    public ExcelViewerController(
    ExcelCommunicationLinkImportService communicationLinkImportService)
    {
        _communicationLinkImportService =
            communicationLinkImportService;
    }
    private const long MaximumFileSize =
        10L * 1024L * 1024L;

    private const long MaximumRequestSize =
        100L * 1024L * 1024L;


    [HttpGet]
    public IActionResult Index()
    {
        var profiles = ExcelImportProfiles.All;

        var model = new ExcelUploadViewModel
        {
            Profiles = profiles,

            Files = profiles
                .Select(profile =>
                    new ExcelFileUploadItemViewModel
                    {
                        ProfileKey = profile.Key,
                        ProfileName = profile.Name
                    })
                .ToList()
        };

        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(
        MultipartBodyLengthLimit = MaximumRequestSize)]
    public IActionResult Index(
        ExcelUploadViewModel model)
    {
        model.Profiles = ExcelImportProfiles.All;


        // Restore profile names after model binding.
        foreach (var fileItem in model.Files)
        {
            var profile =
                model.Profiles.FirstOrDefault(
                    x => x.Key == fileItem.ProfileKey);

            if (profile != null)
            {
                fileItem.ProfileName =
                    profile.Name;
            }
        }


        var uploadedFiles =
            model.Files
                .Where(x =>
                    x.ExcelFile != null &&
                    x.ExcelFile.Length > 0)
                .ToList();


        if (uploadedFiles.Count == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "Select at least one Excel file.");

            return View(model);
        }


        for (int index = 0;
             index < model.Files.Count;
             index++)
        {
            var fileItem =
                model.Files[index];

            if (fileItem.ExcelFile == null ||
                fileItem.ExcelFile.Length == 0)
            {
                continue;
            }


            var profile =
                model.Profiles.FirstOrDefault(
                    x => x.Key == fileItem.ProfileKey);

            if (profile == null)
            {
                ModelState.AddModelError(
                    $"Files[{index}].ExcelFile",
                    "Invalid Excel import type.");

                continue;
            }


            if (!ValidateFile(
                    fileItem.ExcelFile,
                    index))
            {
                continue;
            }


            try
            {
                ReadExcelFile(
                    fileItem,
                    profile,
                    index);
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    $"Files[{index}].ExcelFile",
                    $"The file '{fileItem.ExcelFile.FileName}' " +
                    "could not be read. Make sure it is a valid .xlsx file.");
            }
        }


        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportCommunicationLinks(
    ExcelFileUploadItemViewModel fileItem,
    CancellationToken cancellationToken)
    {
        if (fileItem.Headers.Count == 0 ||
            fileItem.Rows.Count == 0)
        {
            TempData["ErrorMessage"] =
                "There is no Communication Links data to import.";

            return RedirectToAction(nameof(Index));
        }

        try
        {
            List<ExcelCommunicationLinkRow> rows =
                ExcelCommunicationLinkParser.Parse(fileItem);

            ExcelCommunicationLinkImportResult result =
                await _communicationLinkImportService
                    .ImportAsync(
                        rows,
                        cancellationToken);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] =
                    result.Message;
            }
            else
            {
                TempData["ErrorMessage"] =
                    result.Message;
            }
        }
        catch (InvalidDataException exception)
        {
            TempData["ErrorMessage"] =
                exception.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] =
                "The Communication Links data could not be imported.";
        }

        return RedirectToAction(nameof(Index));
    }
    private void ReadExcelFile(
        ExcelFileUploadItemViewModel fileItem,
        ExcelImportProfile profile,
        int index)
    {
        using var stream =
            fileItem.ExcelFile!.OpenReadStream();

        using var workbook =
            new XLWorkbook(stream);

        var worksheet =
            workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
        {
            ModelState.AddModelError(
                $"Files[{index}].ExcelFile",
                "The Excel file does not contain a worksheet.");

            return;
        }


        var usedRange =
            worksheet.RangeUsed();

        if (usedRange == null)
        {
            ModelState.AddModelError(
                $"Files[{index}].ExcelFile",
                "The worksheet is empty.");

            return;
        }


        fileItem.FileName =
            fileItem.ExcelFile.FileName;

        fileItem.WorksheetName =
            worksheet.Name;


        int firstUsedRow =
            usedRange.RangeAddress
                .FirstAddress.RowNumber;

        int lastRow =
            usedRange.RangeAddress
                .LastAddress.RowNumber;

        int firstColumn =
            usedRange.RangeAddress
                .FirstAddress.ColumnNumber;

        int lastColumn =
            usedRange.RangeAddress
                .LastAddress.ColumnNumber;


        int headerRow =
            profile.HeaderRow;


        if (headerRow < firstUsedRow ||
            headerRow > lastRow)
        {
            ModelState.AddModelError(
                $"Files[{index}].ExcelFile",
                $"Header row {headerRow} is outside the used Excel range.");

            return;
        }


        // Read headers.
        for (int column = firstColumn;
             column <= lastColumn;
             column++)
        {
            string header =
                worksheet
                    .Cell(headerRow, column)
                    .GetFormattedString()
                    .Trim();

            fileItem.Headers.Add(header);
        }


        // Validate required columns.
        if (profile.RequiredColumns.Count > 0)
        {
            var missingColumns =
                profile.RequiredColumns
                    .Where(required =>
                        !fileItem.Headers.Any(actual =>
                            string.Equals(
                                actual,
                                required,
                                StringComparison.OrdinalIgnoreCase)))
                    .ToList();


            if (missingColumns.Count > 0)
            {
                ModelState.AddModelError(
                    $"Files[{index}].ExcelFile",
                    $"'{fileItem.ExcelFile.FileName}' does not match " +
                    $"{profile.Name}. Missing columns: " +
                    string.Join(", ", missingColumns));

                fileItem.Headers.Clear();

                return;
            }
        }


        // Read rows below the header.
        for (int row = headerRow + 1;
             row <= lastRow;
             row++)
        {
            var rowValues =
                new List<string>();

            bool containsData = false;


            for (int column = firstColumn;
                 column <= lastColumn;
                 column++)
            {
                string value =
                    worksheet
                        .Cell(row, column)
                        .GetFormattedString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    containsData = true;
                }

                rowValues.Add(value);
            }


            // Ignore completely empty rows.
            if (containsData)
            {
                fileItem.Rows.Add(rowValues);
            }
        }
    }


    private bool ValidateFile(
        IFormFile file,
        int index)
    {
        bool valid = true;


        string extension =
            Path.GetExtension(
                file.FileName);


        if (!string.Equals(
                extension,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                $"Files[{index}].ExcelFile",
                "Only .xlsx files are allowed.");

            valid = false;
        }


        if (file.Length > MaximumFileSize)
        {
            ModelState.AddModelError(
                $"Files[{index}].ExcelFile",
                $"'{file.FileName}' exceeds the 10 MB limit.");

            valid = false;
        }


        return valid;
    }
}
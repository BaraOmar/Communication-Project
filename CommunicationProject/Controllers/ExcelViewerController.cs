using ClosedXML.Excel;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CommunicationProject.Controllers;

public class ExcelViewerController : Controller
{
    private const long MaximumFileSize =
        10L * 1024L * 1024L;

    [HttpGet]
    public IActionResult Index()
    {
        return View(
            new ExcelUploadViewModel());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(
        MultipartBodyLengthLimit = MaximumFileSize)]
    public IActionResult Index(
        ExcelUploadViewModel model)
    {
        ValidateFile(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            using var stream =
                model.ExcelFile!.OpenReadStream();

            using var workbook =
                new XLWorkbook(stream);

            var worksheet =
                workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                ModelState.AddModelError(
                    nameof(model.ExcelFile),
                    "The Excel file does not contain a worksheet.");

                return View(model);
            }

            var usedRange =
                worksheet.RangeUsed();

            if (usedRange == null)
            {
                ModelState.AddModelError(
                    nameof(model.ExcelFile),
                    "The worksheet is empty.");

                return View(model);
            }

            model.FileName =
                model.ExcelFile.FileName;

            model.WorksheetName =
                worksheet.Name;

            int firstRow =
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


            // First used row becomes the table header.
            for (int column = firstColumn;
                 column <= lastColumn;
                 column++)
            {
                string header =
                    worksheet
                        .Cell(firstRow, column)
                        .GetFormattedString();

                model.Headers.Add(header);
            }


            // Remaining rows become table data.
            for (int row = firstRow + 1;
                 row <= lastRow;
                 row++)
            {
                var rowValues =
                    new List<string>();

                for (int column = firstColumn;
                     column <= lastColumn;
                     column++)
                {
                    string value =
                        worksheet
                            .Cell(row, column)
                            .GetFormattedString();

                    rowValues.Add(value);
                }

                model.Rows.Add(rowValues);
            }
        }
        catch (Exception)
        {
            ModelState.AddModelError(
                nameof(model.ExcelFile),
                "The Excel file could not be read. " +
                "Make sure it is a valid .xlsx file.");
        }

        return View(model);
    }


    private void ValidateFile(
        ExcelUploadViewModel model)
    {
        if (model.ExcelFile == null ||
            model.ExcelFile.Length == 0)
        {
            ModelState.AddModelError(
                nameof(model.ExcelFile),
                "Select an Excel file.");

            return;
        }

        string extension =
            Path.GetExtension(
                model.ExcelFile.FileName);

        if (!string.Equals(
                extension,
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.ExcelFile),
                "Only .xlsx files are allowed.");
        }

        if (model.ExcelFile.Length >
            MaximumFileSize)
        {
            ModelState.AddModelError(
                nameof(model.ExcelFile),
                "The Excel file must not exceed 10 MB.");
        }
    }
}
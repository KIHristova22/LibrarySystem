using ClosedXML.Excel;
using Library.Data;
using Library.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Library.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly LibraryContext _context;

        private static readonly string[] MonthNames =
        {
            "January", "February", "March", "April",
            "May", "June", "July", "August",
            "September", "October", "November", "December"
        };

        public ReportsController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = await BuildReportsViewModelAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel()
        {
            var model = await BuildReportsViewModelAsync();

            using var workbook = new XLWorkbook();

            var statisticsSheet = workbook.Worksheets.Add("Borrow Statistics");
            statisticsSheet.Cell("A1").Value = $"Borrow Statistics - {model.Year}";
            statisticsSheet.Cell("A1").Style.Font.Bold = true;
            statisticsSheet.Cell("A1").Style.Font.FontSize = 16;

            statisticsSheet.Cell("A3").Value = "Month";
            statisticsSheet.Cell("B3").Value = "Borrows";

            for (var i = 0; i < model.MonthlyStatistics.Count; i++)
            {
                var row = i + 4;
                statisticsSheet.Cell(row, 1).Value = model.MonthlyStatistics[i].Month;
                statisticsSheet.Cell(row, 2).Value = model.MonthlyStatistics[i].BorrowCount;
            }

            var summaryStartRow = model.MonthlyStatistics.Count + 5;
            statisticsSheet.Cell(summaryStartRow, 1).Value = "Total borrows";
            statisticsSheet.Cell(summaryStartRow, 2).Value = model.TotalBorrows;
            statisticsSheet.Cell(summaryStartRow + 1, 1).Value = "Active loans";
            statisticsSheet.Cell(summaryStartRow + 1, 2).Value = model.ActiveLoans;
            statisticsSheet.Cell(summaryStartRow + 2, 1).Value = "Returned borrows";
            statisticsSheet.Cell(summaryStartRow + 2, 2).Value = model.ReturnedBorrows;
            statisticsSheet.Cell(summaryStartRow + 3, 1).Value = "Registered users";
            statisticsSheet.Cell(summaryStartRow + 3, 2).Value = model.TotalRegisteredUsers;

            FormatHeader(statisticsSheet.Range("A3:B3"));
            statisticsSheet.SheetView.FreezeRows(3);
            statisticsSheet.Columns().AdjustToContents();

            var booksSheet = workbook.Worksheets.Add("Top Books");
            booksSheet.Cell("A1").Value = $"Top Borrowed Books - {model.Year}";
            booksSheet.Cell("A1").Style.Font.Bold = true;
            booksSheet.Cell("A1").Style.Font.FontSize = 16;

            booksSheet.Cell("A3").Value = "Book";
            booksSheet.Cell("B3").Value = "Author";
            booksSheet.Cell("C3").Value = "Borrows";

            for (var i = 0; i < model.TopBooks.Count; i++)
            {
                var row = i + 4;
                booksSheet.Cell(row, 1).Value = model.TopBooks[i].Title;
                booksSheet.Cell(row, 2).Value = model.TopBooks[i].AuthorName;
                booksSheet.Cell(row, 3).Value = model.TopBooks[i].BorrowCount;
            }

            FormatHeader(booksSheet.Range("A3:C3"));
            booksSheet.SheetView.FreezeRows(3);
            booksSheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Library_Report.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportToPdf()
        {
            var model = await BuildReportsViewModelAsync();

            // QuestPDF Community is available for qualifying use cases.
            QuestPDF.Settings.License = LicenseType.Community;

            var generatedAt = DateTime.Now;

            var pdfBytes = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(style => style.FontSize(10));

                    page.Header()
                        .Column(column =>
                        {
                            column.Item()
                                .Text("LibrarySystem")
                                .FontSize(22)
                                .Bold()
                                .FontColor(Colors.Blue.Medium);

                            column.Item()
                                .Text($"Library Report — {model.Year}")
                                .FontSize(14)
                                .SemiBold();

                            column.Item()
                                .Text($"Generated: {generatedAt:yyyy-MM-dd HH:mm}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);
                        });

                    page.Content()
                        .PaddingVertical(20)
                        .Column(column =>
                        {
                            column.Spacing(12);

                            column.Item()
                                .Text("Summary Statistics")
                                .FontSize(14)
                                .Bold();

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("Metric");
                                    header.Cell().Element(HeaderCell).Text("Value");
                                });

                                AddPdfTableRow(table, "Total borrows", model.TotalBorrows.ToString());
                                AddPdfTableRow(table, "Active loans", model.ActiveLoans.ToString());
                                AddPdfTableRow(table, "Returned borrows", model.ReturnedBorrows.ToString());
                                AddPdfTableRow(table, "Registered users", model.TotalRegisteredUsers.ToString());
                                AddPdfTableRow(table, "Top category", model.TopCategory);
                            });

                            column.Item()
                                .PaddingTop(10)
                                .Text("Top Borrowed Books")
                                .FontSize(14)
                                .Bold();

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("Book");
                                    header.Cell().Element(HeaderCell).Text("Author");
                                    header.Cell().Element(HeaderCell).Text("Borrows");
                                });

                                if (model.TopBooks.Count == 0)
                                {
                                    table.Cell().Element(BodyCell).Text("No borrowing data");
                                    table.Cell().Element(BodyCell).Text("-");
                                    table.Cell().Element(BodyCell).Text("0");
                                }
                                else
                                {
                                    foreach (var book in model.TopBooks)
                                    {
                                        table.Cell().Element(BodyCell).Text(book.Title);
                                        table.Cell().Element(BodyCell).Text(book.AuthorName);
                                        table.Cell().Element(BodyCell).Text(book.BorrowCount.ToString());
                                    }
                                }
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("LibrarySystem • Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", "Library_Report.pdf");
        }

        private async Task<ReportsViewModel> BuildReportsViewModelAsync()
        {
            var year = DateTime.Now.Year;

            var totalBorrows = await _context.BorrowRecords.CountAsync();
            var activeLoans = await _context.BorrowRecords
                .CountAsync(record => !record.IsReturned);
            var returnedBorrows = await _context.BorrowRecords
                .CountAsync(record => record.IsReturned);
            var registeredUsers = await _context.Users.CountAsync();

            var monthlyCounts = await _context.BorrowRecords
                .Where(record => record.BorrowDate.Year == year)
                .GroupBy(record => record.BorrowDate.Month)
                .Select(group => new
                {
                    Month = group.Key,
                    Count = group.Count()
                })
                .ToListAsync();

            var monthlyCountLookup = monthlyCounts.ToDictionary(
                item => item.Month,
                item => item.Count);

            var monthlyStatistics = Enumerable.Range(1, 12)
                .Select(month => new MonthlyBorrowStat
                {
                    Month = MonthNames[month - 1],
                    BorrowCount = monthlyCountLookup.GetValueOrDefault(month)
                })
                .ToList();

            var topBookCounts = await _context.BorrowRecords
                .GroupBy(record => record.BookId)
                .Select(group => new
                {
                    BookId = group.Key,
                    BorrowCount = group.Count()
                })
                .OrderByDescending(item => item.BorrowCount)
                .ThenBy(item => item.BookId)
                .Take(5)
                .ToListAsync();

            var topBookIds = topBookCounts.Select(item => item.BookId).ToList();

            var books = await _context.Books
                .Include(book => book.Author)
                .Where(book => topBookIds.Contains(book.BookId))
                .ToListAsync();

            var booksById = books.ToDictionary(book => book.BookId);

            var topBooks = topBookCounts
                .Where(item => booksById.ContainsKey(item.BookId))
                .Select(item =>
                {
                    var book = booksById[item.BookId];

                    return new TopBookStat
                    {
                        Title = book.Title,
                        AuthorName = book.Author == null
                            ? "Unknown"
                            : $"{book.Author.FirstName} {book.Author.LastName}",
                        BorrowCount = item.BorrowCount
                    };
                })
                .ToList();

            var categoryCounts = await _context.BorrowRecords
                .GroupBy(record => record.Book.CategoryId)
                .Select(group => new
                {
                    CategoryId = group.Key,
                    BorrowCount = group.Count()
                })
                .OrderByDescending(item => item.BorrowCount)
                .ThenBy(item => item.CategoryId)
                .ToListAsync();

            var topCategory = "No data";

            if (categoryCounts.Count > 0)
            {
                var categoryId = categoryCounts[0].CategoryId;
                topCategory = await _context.Categories
                    .Where(category => category.CategoryId == categoryId)
                    .Select(category => category.Name)
                    .FirstOrDefaultAsync() ?? "Uncategorized";
            }

            return new ReportsViewModel
            {
                Year = year,
                TotalBorrows = totalBorrows,
                ActiveLoans = activeLoans,
                ReturnedBorrows = returnedBorrows,
                TotalRegisteredUsers = registeredUsers,
                TopCategory = topCategory,
                MonthlyStatistics = monthlyStatistics,
                TopBooks = topBooks
            };
        }

        private static void FormatHeader(IXLRange range)
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .Background(Colors.Blue.Medium)
                .Padding(6)
                .DefaultTextStyle(style => style
                    .FontColor(Colors.White)
                    .SemiBold());
        }

        private static IContainer BodyCell(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(6);
        }

        private static void AddPdfTableRow(
            TableDescriptor table,
            string metric,
            string value)
        {
            table.Cell().Element(BodyCell).Text(metric);
            table.Cell().Element(BodyCell).Text(value);
        }
    }
}
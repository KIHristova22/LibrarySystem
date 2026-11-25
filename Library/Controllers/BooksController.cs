using System.Security.Claims;
using Library.Models;
using Library.Repositories;
using Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Library.Controllers
{
    public class BooksController : Controller
    {
        private readonly IBookRepository _bookRepository;
        private readonly ILogger<BooksController> _logger;
        private readonly IBookService _bookService;

        private readonly string _uploadsFolder =
            @"C:/Users/krist/Documents/LibrarySystem/Library/UploadedImages";

        private static readonly HashSet<string> AllowedImageExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

        private const long MaxImageSizeBytes = 5 * 1024 * 1024;

        public BooksController(
            IBookRepository bookRepository,
            ILogger<BooksController> logger,
            IBookService bookService)
        {
            _bookRepository = bookRepository;
            _logger = logger;
            _bookService = bookService;
        }

        public async Task<IActionResult> Index(
            int page = 1,
            string? searchString = null,
            int? categoryId = null,
            bool? onlyAvailable = null,
            string? sortOrder = null)
        {
            const int pageSize = 5;

            page = Math.Max(page, 1);
            searchString = searchString?.Trim();

            var activeBorrowedBookIds = await _bookRepository.GetActiveBorrowedBookIdsAsync();

            var baseQuery = _bookRepository.QueryWithDetails();

            var query = _bookService.ApplyFiltersAndSort(
                baseQuery,
                activeBorrowedBookIds,
                searchString,
                categoryId,
                onlyAvailable,
                sortOrder);

            var totalItems = await query.CountAsync();

            var books = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(
                1,
                (int)Math.Ceiling(totalItems / (double)pageSize));

            ViewBag.BorrowedBookIds = activeBorrowedBookIds;

            ViewBag.Categories = await _bookRepository.GetCategoriesOrderedAsync();

            ViewBag.SearchString = searchString;
            ViewBag.CategoryId = categoryId;
            ViewBag.OnlyAvailable = onlyAvailable == true;
            ViewBag.SortOrder = sortOrder ?? string.Empty;

            return View(books);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Borrow(int id)
        {
            if (User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            await using var transaction =
                await _bookRepository.BeginTransactionAsync();

            try
            {
                var result = await _bookService.BorrowBookAsync(
                    id,
                    userId,
                    User.IsInRole("Admin"));

                if (result == BorrowResult.BookNotFound)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                if (result == BorrowResult.Forbidden)
                {
                    await transaction.RollbackAsync();
                    return Forbid();
                }

                if (result == BorrowResult.AlreadyBorrowed)
                {
                    await transaction.RollbackAsync();

                    TempData["Error"] = "Тази книга вече е заета.";
                    return RedirectToAction(nameof(Index));
                }

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Book {BookId} was borrowed by user {UserId}.",
                    id,
                    userId);

                TempData["Success"] = "Книгата е заета успешно.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync();

                _logger.LogError(
                    exception,
                    "Failed to borrow book {BookId} for user {UserId}.",
                    id,
                    userId);

                throw;
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _bookRepository.FindByIdWithDetailsAsync(id.Value);

            if (book == null)
            {
                return NotFound();
            }

            ViewBag.IsBorrowed = await _bookRepository.IsCurrentlyBorrowedAsync(id.Value);

            return View(book);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int? categoryId)
        {
            await PopulateDropdownsAsync(selectedCategoryId: categoryId);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Book book)
        {
            ValidateImageFile(book.ImageFile);

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(book.AuthorId, book.CategoryId);

                return View(book);
            }

            try
            {
                if (book.ImageFile is { Length: > 0 })
                {
                    book.ImageUrl = await SaveImageAsync(book.ImageFile);
                }

                await _bookRepository.AddAsync(book);
                await _bookRepository.SaveChangesAsync();

                _logger.LogInformation(
                    "Admin created book {BookId}: {BookTitle}",
                    book.BookId,
                    book.Title);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to create book {BookTitle}.",
                    book.Title);

                throw;
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _bookRepository.FindByIdAsync(id.Value);

            if (book == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync(book.AuthorId, book.CategoryId);

            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Book book)
        {
            if (id != book.BookId)
            {
                return NotFound();
            }

            var existingBook = await _bookRepository.FindByIdAsync(id);

            if (existingBook == null)
            {
                return NotFound();
            }

            ValidateImageFile(book.ImageFile);

            if (!ModelState.IsValid)
            {
                book.ImageUrl = existingBook.ImageUrl;

                await PopulateDropdownsAsync(book.AuthorId, book.CategoryId);

                return View(book);
            }

            try
            {
                existingBook.Title = book.Title;
                existingBook.Price = book.Price;
                existingBook.AuthorId = book.AuthorId;
                existingBook.CategoryId = book.CategoryId;

                var oldImageUrl = existingBook.ImageUrl;
                var hasNewImage = book.ImageFile is { Length: > 0 };

                if (hasNewImage)
                {
                    existingBook.ImageUrl = await SaveImageAsync(book.ImageFile!);
                }

                await _bookRepository.SaveChangesAsync();

                if (hasNewImage)
                {
                    DeleteStoredImage(oldImageUrl);
                }

                _logger.LogInformation(
                    "Admin updated book {BookId}: {BookTitle}",
                    existingBook.BookId,
                    existingBook.Title);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to update book {BookId}.",
                    id);

                throw;
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _bookRepository.FindByIdWithDetailsAsync(id.Value);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _bookRepository.FindByIdAsync(id);

            if (book != null)
            {
                var imageUrl = book.ImageUrl;

                try
                {
                    await _bookRepository.RemoveAsync(book);
                    await _bookRepository.SaveChangesAsync();

                    DeleteStoredImage(imageUrl);

                    _logger.LogInformation(
                        "Admin deleted book {BookId}.",
                        id);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to delete book {BookId}.",
                        id);

                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private void ValidateImageFile(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return;
            }

            var extension = Path.GetExtension(Path.GetFileName(imageFile.FileName));

            if (!AllowedImageExtensions.Contains(extension))
            {
                ModelState.AddModelError(
                    nameof(Book.ImageFile),
                    "Only .jpg, .jpeg, and .png files are allowed.");
            }

            if (imageFile.Length > MaxImageSizeBytes)
            {
                ModelState.AddModelError(
                    nameof(Book.ImageFile),
                    "The image must be 5 MB or smaller.");
            }
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            Directory.CreateDirectory(_uploadsFolder);

            var extension = Path.GetExtension(Path.GetFileName(imageFile.FileName))
                .ToLowerInvariant();

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(_uploadsFolder, fileName);

            await using var fileStream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            await imageFile.CopyToAsync(fileStream);

            return $"/uploads/{fileName}";
        }

        private void DeleteStoredImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            var fileName = Path.GetFileName(imageUrl);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var filePath = Path.Combine(_uploadsFolder, fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        private async Task PopulateDropdownsAsync(
            int? selectedAuthorId = null,
            int? selectedCategoryId = null)
        {
            var authors = await _bookRepository.GetAuthorsForDropdownAsync();
            var categories = await _bookRepository.GetCategoriesOrderedAsync();

            ViewBag.Authors = new SelectList(
                authors,
                nameof(AuthorOption.AuthorId),
                nameof(AuthorOption.Name),
                selectedAuthorId);

            ViewBag.Categories = new SelectList(
                categories,
                "CategoryId",
                "Name",
                selectedCategoryId);
        }
    }
}

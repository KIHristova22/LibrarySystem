using System.Security.Claims;
using Library.Data;
using Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Library.Controllers
{
    public class BooksController : Controller
    {
        private readonly LibraryContext _context;
        private readonly string _uploadsFolder =
            @"C:/Users/krist/Documents/LibrarySystem/Library/UploadedImages";

        private static readonly HashSet<string> AllowedImageExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

        private const long MaxImageSizeBytes = 5 * 1024 * 1024;

        public BooksController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 5;

            var query = _context.Books
                .Include(book => book.Author)
                .Include(book => book.Category);

            var totalItems = await query.CountAsync();

            var books = await query
                .OrderBy(book => book.BookId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.BorrowedBookIds = await _context.BorrowRecords
                .Where(record => !record.IsReturned)
                .Select(record => record.BookId)
                .ToListAsync();

            return View(books);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Borrow(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var isAlreadyBorrowed = await _context.BorrowRecords
                .AnyAsync(record => record.BookId == id && !record.IsReturned);

            if (isAlreadyBorrowed)
            {
                TempData["Error"] = "This book is already borrowed.";
                return RedirectToAction(nameof(Index));
            }

            _context.BorrowRecords.Add(new BorrowRecord
            {
                BookId = book.BookId,
                UserId = userId,
                BorrowDate = DateTime.Now,
                IsReturned = false
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Book borrowed successfully.";
            return RedirectToAction(nameof(MyBooks));
        }

        [Authorize]
        public async Task<IActionResult> MyBooks()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var records = await _context.BorrowRecords
                .Where(record => record.UserId == userId && !record.IsReturned)
                .Include(record => record.Book)
                    .ThenInclude(book => book.Author)
                .Include(record => record.Book)
                    .ThenInclude(book => book.Category)
                .OrderByDescending(record => record.BorrowDate)
                .ToListAsync();

            return View(records);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Return(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var record = await _context.BorrowRecords
                .FirstOrDefaultAsync(item =>
                    item.BorrowRecordId == id &&
                    item.UserId == userId &&
                    !item.IsReturned);

            if (record == null)
            {
                return NotFound();
            }

            record.IsReturned = true;
            record.ReturnDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Book returned successfully.";
            return RedirectToAction(nameof(MyBooks));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
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

            if (book.ImageFile is { Length: > 0 })
            {
                book.ImageUrl = await SaveImageAsync(book.ImageFile);
            }

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FindAsync(id);
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

            var existingBook = await _context.Books.FindAsync(id);
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

            await _context.SaveChangesAsync();

            if (hasNewImage)
            {
                DeleteStoredImage(oldImageUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(item => item.Author)
                .Include(item => item.Category)
                .FirstOrDefaultAsync(item => item.BookId == id);

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
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                var imageUrl = book.ImageUrl;

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();

                DeleteStoredImage(imageUrl);
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
            var authors = await _context.Authors
                .Select(author => new
                {
                    author.AuthorId,
                    Name = author.FirstName + " " + author.LastName
                })
                .ToListAsync();

            var categories = await _context.Categories.ToListAsync();

            ViewBag.Authors = new SelectList(
                authors,
                "AuthorId",
                "Name",
                selectedAuthorId);

            ViewBag.Categories = new SelectList(
                categories,
                "CategoryId",
                "Name",
                selectedCategoryId);
        }
    }
}
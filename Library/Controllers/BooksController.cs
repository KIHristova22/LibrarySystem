using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Library.Data;
using Library.Models;
using System.IO;

namespace Library.Controllers
{
    public class BooksController : Controller
    {
        private readonly LibraryContext _context;
        private readonly string _uploadsFolder = @"C:/Users/krist/Documents/LibrarySystem/Library/UploadedImages";

        public BooksController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 5;

            var query = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category);

            int totalItems = await query.CountAsync();

            var books = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(books);
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
            if (book.ImageFile != null && book.ImageFile.Length > 0)
            {
                var permittedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(book.ImageFile.FileName).ToLowerInvariant();

                if (string.IsNullOrEmpty(extension) || !permittedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ImageFile", "Invalid file format. Only .jpg, .jpeg, and .png are allowed.");
                    await PopulateDropdownsAsync(book.AuthorId, book.CategoryId);
                    return View(book);
                }

                if (!Directory.Exists(_uploadsFolder))
                {
                    Directory.CreateDirectory(_uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + extension;
                string filePath = Path.Combine(_uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await book.ImageFile.CopyToAsync(fileStream);
                }

                book.ImageUrl = "/uploads/" + uniqueFileName;
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(book.AuthorId, book.CategoryId);
                return View(book);
            }

            _context.Add(book);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            await PopulateDropdownsAsync(book.AuthorId, book.CategoryId);
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Book book)
        {
            if (id != book.BookId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBook = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.BookId == id);
                    if (existingBook == null) return NotFound();

                    if (book.ImageFile != null && book.ImageFile.Length > 0)
                    {
                        var permittedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                        var extension = Path.GetExtension(book.ImageFile.FileName).ToLowerInvariant();

                        if (string.IsNullOrEmpty(extension) || !permittedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("ImageFile", "Invalid file format. Only .jpg, .jpeg, and .png are allowed.");
                            await PopulateDropdownsAsync(book.AuthorId, book.CategoryId);
                            return View(book);
                        }

                        if (!Directory.Exists(_uploadsFolder))
                        {
                            Directory.CreateDirectory(_uploadsFolder);
                        }

                        string uniqueFileName = Guid.NewGuid().ToString() + extension;
                        string filePath = Path.Combine(_uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await book.ImageFile.CopyToAsync(fileStream);
                        }

                        string oldFilePath = string.IsNullOrEmpty(existingBook.ImageUrl)
                            ? null
                            : Path.Combine(_uploadsFolder, Path.GetFileName(existingBook.ImageUrl));

                        if (!string.IsNullOrEmpty(oldFilePath) && System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }

                        book.ImageUrl = "/uploads/" + uniqueFileName;
                    }
                    else
                    {
                        book.ImageUrl = existingBook.ImageUrl;
                    }

                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Books.Any(e => e.BookId == book.BookId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(book.AuthorId, book.CategoryId);
            return View(book);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.BookId == id);

            if (book == null) return NotFound();

            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                if (!string.IsNullOrEmpty(book.ImageUrl))
                {
                    string fileName = Path.GetFileName(book.ImageUrl);
                    string oldFilePath = Path.Combine(_uploadsFolder, fileName);

                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(int? selectedAuthorId = null, int? selectedCategoryId = null)
        {
            var authors = await _context.Authors
                .Select(a => new { a.AuthorId, Name = a.FirstName + " " + a.LastName })
                .ToListAsync();

            var categories = await _context.Categories.ToListAsync();

            ViewBag.Authors = new SelectList(authors, "AuthorId", "Name", selectedAuthorId);
            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name", selectedCategoryId);
        }
    }
}
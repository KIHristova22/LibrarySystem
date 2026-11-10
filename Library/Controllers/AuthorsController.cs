using Library.Data;
using Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.Controllers
{
    public class AuthorsController : Controller
    {
        private readonly LibraryContext _context;

        public AuthorsController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var authors = await _context.Authors
                .Include(author => author.Books)
                .OrderBy(author => author.LastName)
                .ThenBy(author => author.FirstName)
                .ToListAsync();

            return View(authors);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new Author());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Author author)
        {
            author.FirstName = author.FirstName?.Trim() ?? string.Empty;
            author.LastName = author.LastName?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(author);
            }

            var alreadyExists = await _context.Authors.AnyAsync(existing =>
                existing.FirstName.ToLower() == author.FirstName.ToLower() &&
                existing.LastName.ToLower() == author.LastName.ToLower());

            if (alreadyExists)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "An author with this name already exists.");

                return View(author);
            }

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Author {author.FirstName} {author.LastName} was added successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors.FindAsync(id);

            if (author == null)
            {
                return NotFound();
            }

            return View(author);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Author author)
        {
            if (id != author.AuthorId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(author);
            }

            _context.Update(author);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Author updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors
                .FirstOrDefaultAsync(item => item.AuthorId == id);

            if (author == null)
            {
                return NotFound();
            }

            return View(author);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var author = await _context.Authors.FindAsync(id);

            if (author != null)
            {
                _context.Authors.Remove(author);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Author deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
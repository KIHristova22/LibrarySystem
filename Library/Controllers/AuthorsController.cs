using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library.Data;
using Library.Models;

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
            var authors = await _context.Authors.Include(a => a.Books).ToListAsync();
            return View(authors);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Author author)
        {
            if (ModelState.IsValid)
            {
                _context.Add(author);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(author);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var author = await _context.Authors.FindAsync(id);
            if (author == null) return NotFound();
            return View(author);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Author author)
        {
            if (id != author.AuthorId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(author);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(author);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var author = await _context.Authors.FirstOrDefaultAsync(m => m.AuthorId == id);
            if (author == null) return NotFound();
            return View(author);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var author = await _context.Authors.FindAsync(id);
            if (author != null)
            {
                _context.Authors.Remove(author);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
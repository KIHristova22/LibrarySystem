using Library.Data;
using Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly LibraryContext _context;

        public CategoriesController(LibraryContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(category => category.Books)
                .OrderBy(category => category.Name)
                .ToListAsync();

            return View(categories);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            Category category,
            string? returnUrl = null)
        {
            category.Name = category.Name?.Trim() ?? string.Empty;
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            var normalizedName = category.Name.ToLower();

            var alreadyExists = await _context.Categories
                .AnyAsync(existing => existing.Name.ToLower() == normalizedName);

            if (alreadyExists)
            {
                ModelState.AddModelError(
                    nameof(category.Name),
                    "A category with this name already exists.");

                return View(category);
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Category “{category.Name}” was added successfully.";

            // The book form can request a return with the new category selected.
            if (returnUrl == "/Books/Create")
            {
                return RedirectToAction(
                    "Create",
                    "Books",
                    new { categoryId = category.CategoryId });
            }

            // Only redirect to other local URLs; otherwise return to the categories list.
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

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

            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.CategoryId)
            {
                return NotFound();
            }

            category.Name = category.Name?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            var normalizedName = category.Name.ToLower();

            var alreadyExists = await _context.Categories
                .AnyAsync(existing =>
                    existing.CategoryId != id &&
                    existing.Name.ToLower() == normalizedName);

            if (alreadyExists)
            {
                ModelState.AddModelError(
                    nameof(category.Name),
                    "A category with this name already exists.");

                return View(category);
            }

            _context.Update(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Category updated successfully.";
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

            var category = await _context.Categories
                .FirstOrDefaultAsync(item => item.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Category deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
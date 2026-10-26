using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Library.Data;

namespace Library.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly LibraryContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(LibraryContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == currentUser.Email);

            if (customer == null)
            {
                return View("NoProfile");
            }

            var myLoans = await _context.Loans
                .Include(l => l.Book)
                .Where(l => l.CustomerId == customer.CustomerId) 
                .ToListAsync();

            return View(myLoans);
        }
    }
}
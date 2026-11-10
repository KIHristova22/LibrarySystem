using System.IO;
using Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly string _profileImagesFolder;

        private static readonly HashSet<string> AllowedProfileExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".png" };

        private const long MaxProfilePictureSizeBytes = 5 * 1024 * 1024;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _profileImagesFolder = Path.Combine(
                environment.ContentRootPath,
                "UploadedImages",
                "Profiles");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string email,
            string password,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            var user = await _userManager.FindByEmailAsync(email);

            if (user != null && !string.IsNullOrEmpty(user.UserName))
            {
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName,
                    password,
                    isPersistent: false,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return LocalRedirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Books");
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "User");

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await _userManager.DeleteAsync(user);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Books");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var model = new ProfileViewModel
            {
                Email = user.Email,
                UserName = user.UserName,
                ProfilePictureUrl = user.ProfilePictureUrl
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var image = model.ProfilePicture;

            if (image == null || image.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(model.ProfilePicture),
                    "Choose a JPG or PNG image to upload.");
            }
            else
            {
                var extension = Path.GetExtension(
                    Path.GetFileName(image.FileName));

                if (!AllowedProfileExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        nameof(model.ProfilePicture),
                        "Only .jpg and .png files are allowed.");
                }

                if (image.Length > MaxProfilePictureSizeBytes)
                {
                    ModelState.AddModelError(
                        nameof(model.ProfilePicture),
                        "The image must be 5 MB or smaller.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.Email = user.Email;
                model.UserName = user.UserName;
                model.ProfilePictureUrl = user.ProfilePictureUrl;
                return View(model);
            }

            Directory.CreateDirectory(_profileImagesFolder);

            var fileExtension = Path.GetExtension(
                Path.GetFileName(image!.FileName)).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{fileExtension}";
            var filePath = Path.Combine(_profileImagesFolder, fileName);

            await using (var stream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await image.CopyToAsync(stream);
            }

            var oldPictureUrl = user.ProfilePictureUrl;
            user.ProfilePictureUrl = $"/uploads/Profiles/{fileName}";

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                System.IO.File.Delete(filePath);

                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                model.Email = user.Email;
                model.UserName = user.UserName;
                model.ProfilePictureUrl = oldPictureUrl;
                return View(model);
            }

            DeletePreviousProfilePicture(oldPictureUrl);
            TempData["ProfileMessage"] = "Your profile picture has been updated.";

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Books");
        }

        private void DeletePreviousProfilePicture(string? imageUrl)
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

            var filePath = Path.Combine(_profileImagesFolder, fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Library.Models
{
    public class ProfileViewModel
    {
        public string? Email { get; set; }

        public string? UserName { get; set; }

        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "Profile picture")]
        public IFormFile? ProfilePicture { get; set; }
    }
}
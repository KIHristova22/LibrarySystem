using System.ComponentModel.DataAnnotations;

namespace Library.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
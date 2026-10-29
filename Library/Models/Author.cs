using System.ComponentModel.DataAnnotations;

namespace Library.Models
{
    public class Author
    {
        public int AuthorId { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
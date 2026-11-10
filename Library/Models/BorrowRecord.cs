using Microsoft.AspNetCore.Identity;

namespace Library.Models
{
    public class BorrowRecord
    {
        public int BorrowRecordId { get; set; }

        public int BookId { get; set; }
        public Book Book { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;
        public IdentityUser User { get; set; } = null!;

        public DateTime BorrowDate { get; set; } = DateTime.Now;
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; } = false;
    }
}
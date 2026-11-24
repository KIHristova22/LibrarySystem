using Library.Data;
using Library.Models;
using Microsoft.EntityFrameworkCore;

namespace Library.Services
{
    public class BorrowRepository : IBorrowRepository
    {
        private readonly LibraryContext _context;

        public BorrowRepository(LibraryContext context)
        {
            _context = context;
        }

        public Task<bool> BookExistsAsync(int bookId)
        {
            return _context.Books.AnyAsync(book => book.BookId == bookId);
        }

        public Task<bool> HasActiveBorrowAsync(int bookId)
        {
            return _context.BorrowRecords.AnyAsync(record =>
                record.BookId == bookId && !record.IsReturned);
        }

        public async Task AddBorrowRecordAsync(BorrowRecord record)
        {
            await _context.BorrowRecords.AddAsync(record);
        }

        public Task<BorrowRecord?> GetActiveBorrowAsync(int bookId, string userId)
        {
            return _context.BorrowRecords.FirstOrDefaultAsync(record =>
                record.BookId == bookId &&
                record.UserId == userId &&
                !record.IsReturned);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}

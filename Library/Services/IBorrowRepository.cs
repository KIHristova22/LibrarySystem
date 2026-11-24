using Library.Models;

namespace Library.Services
{
    public interface IBorrowRepository
    {
        Task<bool> BookExistsAsync(int bookId);

        Task<bool> HasActiveBorrowAsync(int bookId);

        Task AddBorrowRecordAsync(BorrowRecord record);

        Task<BorrowRecord?> GetActiveBorrowAsync(int bookId, string userId);

        Task SaveChangesAsync();
    }
}

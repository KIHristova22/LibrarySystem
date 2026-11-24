using Library.Models;

namespace Library.Services
{
    public interface IBookService
    {
        Task<BorrowResult> BorrowBookAsync(int bookId, string? userId, bool isAdmin);

        Task<ReturnResult> ReturnBookAsync(int bookId, string? userId);

        bool IsBookAvailable(int bookId, IEnumerable<BorrowRecord> records);

        bool IsOverdue(BorrowRecord record, DateTime currentDate, int loanPeriodDays);

        IQueryable<Book> ApplyFiltersAndSort(
            IQueryable<Book> books,
            IReadOnlyCollection<int> borrowedBookIds,
            string? searchString,
            int? categoryId,
            bool? onlyAvailable,
            string? sortOrder);
    }
}

using Library.Models;
using Library.Strategies;

namespace Library.Services
{
    public class BookService : IBookService
    {
        private readonly IBorrowRepository _repository;
        private readonly IBookSortStrategyResolver _sortStrategyResolver;

        public BookService(
            IBorrowRepository repository,
            IBookSortStrategyResolver sortStrategyResolver)
        {
            _repository = repository;
            _sortStrategyResolver = sortStrategyResolver;
        }

        public async Task<BorrowResult> BorrowBookAsync(int bookId, string? userId, bool isAdmin)
        {
            if (isAdmin || string.IsNullOrEmpty(userId))
            {
                return BorrowResult.Forbidden;
            }

            if (!await _repository.BookExistsAsync(bookId))
            {
                return BorrowResult.BookNotFound;
            }

            if (await _repository.HasActiveBorrowAsync(bookId))
            {
                return BorrowResult.AlreadyBorrowed;
            }

            var record = new BorrowRecord
            {
                BookId = bookId,
                UserId = userId,
                BorrowDate = DateTime.Now,
                IsReturned = false
            };

            await _repository.AddBorrowRecordAsync(record);
            await _repository.SaveChangesAsync();

            return BorrowResult.Success;
        }

        public async Task<ReturnResult> ReturnBookAsync(int bookId, string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return ReturnResult.Forbidden;
            }

            var record = await _repository.GetActiveBorrowAsync(bookId, userId);

            if (record == null)
            {
                return ReturnResult.NotBorrowed;
            }

            record.IsReturned = true;
            record.ReturnDate = DateTime.Now;

            await _repository.SaveChangesAsync();

            return ReturnResult.Success;
        }

        public bool IsBookAvailable(int bookId, IEnumerable<BorrowRecord> records)
        {
            return !records.Any(record =>
                record.BookId == bookId && !record.IsReturned);
        }

        public bool IsOverdue(BorrowRecord record, DateTime currentDate, int loanPeriodDays)
        {
            if (record.IsReturned)
            {
                return false;
            }

            return record.BorrowDate.AddDays(loanPeriodDays) < currentDate;
        }

        public IQueryable<Book> ApplyFiltersAndSort(
            IQueryable<Book> books,
            IReadOnlyCollection<int> borrowedBookIds,
            string? searchString,
            int? categoryId,
            bool? onlyAvailable,
            string? sortOrder)
        {
            var query = books;

            var trimmedSearch = searchString?.Trim();

            if (!string.IsNullOrWhiteSpace(trimmedSearch))
            {
                query = query.Where(book => book.Title.Contains(trimmedSearch));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(book => book.CategoryId == categoryId.Value);
            }

            if (onlyAvailable == true)
            {
                query = query.Where(book => !borrowedBookIds.Contains(book.BookId));
            }

            var strategy = _sortStrategyResolver.Resolve(sortOrder);
            return strategy.Apply(query);
        }
    }
}

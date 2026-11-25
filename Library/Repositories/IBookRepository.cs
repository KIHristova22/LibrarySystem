using Library.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Library.Repositories
{
    public interface IBookRepository
    {
        IQueryable<Book> QueryWithDetails();

        Task<Book?> FindByIdWithDetailsAsync(int id);

        Task<Book?> FindByIdAsync(int id);

        Task AddAsync(Book book);

        Task LoadNavigationPropertiesAsync(Book book);

        Task RemoveAsync(Book book);

        Task SaveChangesAsync();

        Task<IDbContextTransaction> BeginTransactionAsync();

        Task<List<int>> GetActiveBorrowedBookIdsAsync();

        Task<bool> IsCurrentlyBorrowedAsync(int bookId);

        Task<List<Category>> GetCategoriesOrderedAsync();

        Task<List<AuthorOption>> GetAuthorsForDropdownAsync();
    }
}

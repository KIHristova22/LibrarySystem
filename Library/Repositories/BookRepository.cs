using Library.Data;
using Library.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Library.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly LibraryContext _context;

        public BookRepository(LibraryContext context)
        {
            _context = context;
        }

        public IQueryable<Book> QueryWithDetails()
        {
            return _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .AsQueryable();
        }

        public Task<Book?> FindByIdWithDetailsAsync(int id)
        {
            return _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.BookId == id);
        }

        public async Task<Book?> FindByIdAsync(int id)
        {
            return await _context.Books.FindAsync(id);
        }

        public async Task AddAsync(Book book)
        {
            await _context.Books.AddAsync(book);
        }

        public async Task LoadNavigationPropertiesAsync(Book book)
        {
            await _context.Entry(book).Reference(b => b.Author).LoadAsync();
            await _context.Entry(book).Reference(b => b.Category).LoadAsync();
        }

        public Task RemoveAsync(Book book)
        {
            _context.Books.Remove(book);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return _context.Database.BeginTransactionAsync();
        }

        public Task<List<int>> GetActiveBorrowedBookIdsAsync()
        {
            return _context.BorrowRecords
                .Where(r => !r.IsReturned)
                .Select(r => r.BookId)
                .ToListAsync();
        }

        public Task<bool> IsCurrentlyBorrowedAsync(int bookId)
        {
            return _context.BorrowRecords
                .AnyAsync(r => r.BookId == bookId && !r.IsReturned);
        }

        public Task<List<Category>> GetCategoriesOrderedAsync()
        {
            return _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<AuthorOption>> GetAuthorsForDropdownAsync()
        {
            var authors = await _context.Authors
                .OrderBy(a => a.FirstName)
                .ThenBy(a => a.LastName)
                .Select(a => new { a.AuthorId, Name = a.FirstName + " " + a.LastName })
                .ToListAsync();

            return authors
                .Select(a => new AuthorOption(a.AuthorId, a.Name))
                .ToList();
        }
    }
}

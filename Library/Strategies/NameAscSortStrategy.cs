using Library.Models;

namespace Library.Strategies
{
    public class NameAscSortStrategy : IBookSortStrategy
    {
        public string SortKey => "name_asc";

        public IQueryable<Book> Apply(IQueryable<Book> query)
        {
            return query
                .OrderBy(book => book.Title)
                .ThenBy(book => book.BookId);
        }
    }
}

using Library.Models;

namespace Library.Strategies
{
    public class NameDescSortStrategy : IBookSortStrategy
    {
        public string SortKey => "name_desc";

        public IQueryable<Book> Apply(IQueryable<Book> query)
        {
            return query
                .OrderByDescending(book => book.Title)
                .ThenByDescending(book => book.BookId);
        }
    }
}

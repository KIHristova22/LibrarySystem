using Library.Models;

namespace Library.Strategies
{
    public class DateAscSortStrategy : IBookSortStrategy
    {
        public string SortKey => "date";

        public IQueryable<Book> Apply(IQueryable<Book> query)
        {
            return query.OrderBy(book => book.BookId);
        }
    }
}

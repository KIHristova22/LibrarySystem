using Library.Models;

namespace Library.Strategies
{
    public class DateDescSortStrategy : IBookSortStrategy
    {
        public string SortKey => "date_desc";

        public IQueryable<Book> Apply(IQueryable<Book> query)
        {
            return query.OrderByDescending(book => book.BookId);
        }
    }
}

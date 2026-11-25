using Library.Models;

namespace Library.Strategies
{
    public class PriceDescSortStrategy : IBookSortStrategy
    {
        public string SortKey => "price_desc";

        public IQueryable<Book> Apply(IQueryable<Book> query)
        {
            return query
                .OrderByDescending(book => book.Price)
                .ThenBy(book => book.BookId);
        }
    }
}

using Library.Models;

namespace Library.Strategies
{
    public class PriceAscSortStrategy : IBookSortStrategy
    {
        public string SortKey => "price";

        public IQueryable<Book> Apply(IQueryable<Book> query)
        {
            return query
                .OrderBy(book => book.Price)
                .ThenBy(book => book.BookId);
        }
    }
}

using Library.Models;

namespace Library.Strategies
{
    public interface IBookSortStrategy
    {
        string SortKey { get; }

        IQueryable<Book> Apply(IQueryable<Book> query);
    }
}

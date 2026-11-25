namespace Library.Strategies
{
    public interface IBookSortStrategyResolver
    {
        IBookSortStrategy Resolve(string? sortKey);
    }
}

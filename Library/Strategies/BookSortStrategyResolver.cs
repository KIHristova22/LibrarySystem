namespace Library.Strategies
{
    public class BookSortStrategyResolver : IBookSortStrategyResolver
    {
        private readonly IReadOnlyDictionary<string, IBookSortStrategy> _strategies;
        private readonly IBookSortStrategy _defaultStrategy;

        public BookSortStrategyResolver(IEnumerable<IBookSortStrategy> strategies)
        {
            var list = strategies.ToList();
            _strategies = list.ToDictionary(s => s.SortKey, StringComparer.OrdinalIgnoreCase);
            _defaultStrategy = _strategies.TryGetValue("name_asc", out var def)
                ? def
                : list.First();
        }

        public IBookSortStrategy Resolve(string? sortKey)
        {
            if (!string.IsNullOrEmpty(sortKey) &&
                _strategies.TryGetValue(sortKey, out var strategy))
            {
                return strategy;
            }

            return _defaultStrategy;
        }
    }
}

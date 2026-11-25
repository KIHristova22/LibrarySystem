internal sealed class BookDto
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
}

internal sealed class BookWriteDto
{
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AuthorId { get; set; }
    public int CategoryId { get; set; }
}

internal sealed class CategoryDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal sealed class AuthorDto
{
    public int AuthorId { get; set; }
    public string FullName { get; set; } = string.Empty;
}

internal sealed class BorrowApiRequestDto
{
    public string UserId { get; set; } = string.Empty;
}

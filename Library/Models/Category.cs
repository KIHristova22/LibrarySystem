namespace Library.Models;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; }=string.Empty;
    public List<Book> Books { get; set; } = new();
}
using System.Net;
using System.Net.Http.Json;

const string BaseUrl = "http://localhost:5256";

using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };

while (true)
{
    Console.WriteLine();
    Console.WriteLine("=== Library System API Client ===");
    Console.WriteLine("1. List all books");
    Console.WriteLine("2. View book details");
    Console.WriteLine("3. Search / filter books");
    Console.WriteLine("4. Add a new book");
    Console.WriteLine("5. Borrow a book");
    Console.WriteLine("6. Delete a book");
    Console.WriteLine("0. Exit");
    Console.Write("\nChoice: ");

    var choice = Console.ReadLine()?.Trim();

    switch (choice)
    {
        case "1":
            await ListBooksAsync(client);
            break;
        case "2":
            await ViewBookDetailsAsync(client);
            break;
        case "3":
            await SearchBooksAsync(client);
            break;
        case "4":
            await AddBookAsync(client);
            break;
        case "5":
            await BorrowBookAsync(client);
            break;
        case "6":
            await DeleteBookAsync(client);
            break;
        case "0":
            return;
        default:
            Console.WriteLine("Invalid choice. Please enter a number from the menu.");
            break;
    }
}

static async Task ListBooksAsync(HttpClient client)
{
    try
    {
        var books = await client.GetFromJsonAsync<List<BookDto>>("api/books");
        PrintBooks(books);
    }
    catch (HttpRequestException)
    {
        Console.WriteLine("Error: could not connect to the API server. Make sure the web app is running on http://localhost:5256.");
    }
}

static async Task ViewBookDetailsAsync(HttpClient client)
{
    Console.Write("Enter book ID: ");

    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Invalid ID.");
        return;
    }

    try
    {
        var response = await client.GetAsync($"api/books/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Book #{id} not found.");
            return;
        }

        response.EnsureSuccessStatusCode();

        var book = await response.Content.ReadFromJsonAsync<BookDto>();

        if (book == null)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"ID        : {book.BookId}");
        Console.WriteLine($"Title     : {book.Title}");
        Console.WriteLine($"Author    : {book.AuthorName}");
        Console.WriteLine($"Category  : {book.CategoryName}");
        Console.WriteLine($"Price     : {book.Price:C}");
        Console.WriteLine($"Available : {(book.IsAvailable ? "Yes" : "No")}");
    }
    catch (HttpRequestException)
    {
        Console.WriteLine("Error: could not connect to the API server.");
    }
}

static async Task SearchBooksAsync(HttpClient client)
{
    Console.Write("Search title (leave blank to skip): ");
    var search = Console.ReadLine()?.Trim();

    Console.Write("Category ID (leave blank to skip): ");
    var catInput = Console.ReadLine()?.Trim();

    Console.Write("Only available books? (y/n, leave blank to skip): ");
    var availInput = Console.ReadLine()?.Trim().ToLower();

    var queryParts = new List<string>();

    if (!string.IsNullOrEmpty(search))
    {
        queryParts.Add($"search={Uri.EscapeDataString(search)}");
    }

    if (!string.IsNullOrEmpty(catInput) && int.TryParse(catInput, out var catId))
    {
        queryParts.Add($"categoryId={catId}");
    }

    if (availInput == "y")
    {
        queryParts.Add("onlyAvailable=true");
    }

    var url = "api/books" + (queryParts.Count > 0 ? "?" + string.Join("&", queryParts) : "");

    try
    {
        var books = await client.GetFromJsonAsync<List<BookDto>>(url);
        PrintBooks(books);
    }
    catch (HttpRequestException)
    {
        Console.WriteLine("Error: could not connect to the API server.");
    }
}

static async Task AddBookAsync(HttpClient client)
{
    try
    {
        var categories = await client.GetFromJsonAsync<List<CategoryDto>>("api/categories");

        if (categories != null && categories.Count > 0)
        {
            Console.WriteLine("\nAvailable categories:");
            foreach (var cat in categories)
            {
                Console.WriteLine($"  {cat.CategoryId,3}. {cat.Name}");
            }
        }

        var authors = await client.GetFromJsonAsync<List<AuthorDto>>("api/authors");

        if (authors != null && authors.Count > 0)
        {
            Console.WriteLine("\nAvailable authors:");
            foreach (var author in authors)
            {
                Console.WriteLine($"  {author.AuthorId,3}. {author.FullName}");
            }
        }

        Console.Write("\nTitle: ");
        var title = Console.ReadLine()?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(title))
        {
            Console.WriteLine("Title is required.");
            return;
        }

        Console.Write("Price: ");

        if (!decimal.TryParse(Console.ReadLine(), out var price))
        {
            Console.WriteLine("Invalid price.");
            return;
        }

        Console.Write("Author ID: ");

        if (!int.TryParse(Console.ReadLine(), out var authorId))
        {
            Console.WriteLine("Invalid author ID.");
            return;
        }

        Console.Write("Category ID: ");

        if (!int.TryParse(Console.ReadLine(), out var categoryId))
        {
            Console.WriteLine("Invalid category ID.");
            return;
        }

        var dto = new BookWriteDto
        {
            Title = title,
            Price = price,
            AuthorId = authorId,
            CategoryId = categoryId
        };

        var response = await client.PostAsJsonAsync("api/books", dto);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Book added successfully.");
        }
        else
        {
            Console.WriteLine($"Failed to add book. Status: {(int)response.StatusCode} {response.StatusCode}");
        }
    }
    catch (HttpRequestException)
    {
        Console.WriteLine("Error: could not connect to the API server.");
    }
}

static async Task BorrowBookAsync(HttpClient client)
{
    Console.Write("Book ID: ");

    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Invalid ID.");
        return;
    }

    Console.Write("Your user ID: ");
    var userId = Console.ReadLine()?.Trim() ?? string.Empty;

    if (string.IsNullOrEmpty(userId))
    {
        Console.WriteLine("User ID is required.");
        return;
    }

    try
    {
        var dto = new BorrowApiRequestDto { UserId = userId };
        var response = await client.PostAsJsonAsync($"api/books/{id}/borrow", dto);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Book #{id} not found.");
            return;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            Console.WriteLine("Could not borrow: book may already be borrowed or user ID is invalid.");
            return;
        }

        response.EnsureSuccessStatusCode();
        Console.WriteLine("Book borrowed successfully.");
    }
    catch (HttpRequestException)
    {
        Console.WriteLine("Error: could not connect to the API server.");
    }
}

static async Task DeleteBookAsync(HttpClient client)
{
    Console.Write("Book ID: ");

    if (!int.TryParse(Console.ReadLine(), out var id))
    {
        Console.WriteLine("Invalid ID.");
        return;
    }

    Console.Write($"Confirm delete book #{id}? (y/n): ");

    if (Console.ReadLine()?.Trim().ToLower() != "y")
    {
        Console.WriteLine("Cancelled.");
        return;
    }

    try
    {
        var response = await client.DeleteAsync($"api/books/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Book #{id} not found.");
            return;
        }

        response.EnsureSuccessStatusCode();
        Console.WriteLine("Book deleted successfully.");
    }
    catch (HttpRequestException)
    {
        Console.WriteLine("Error: could not connect to the API server.");
    }
}

static void PrintBooks(List<BookDto>? books)
{
    if (books == null || books.Count == 0)
    {
        Console.WriteLine("No books found.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine($"{"ID",-5} {"Title",-35} {"Author",-25} {"Category",-20} {"Price",8}  Available");
    Console.WriteLine(new string('-', 102));

    foreach (var book in books)
    {
        Console.WriteLine(
            $"{book.BookId,-5} " +
            $"{Clip(book.Title, 33),-35} " +
            $"{Clip(book.AuthorName, 23),-25} " +
            $"{Clip(book.CategoryName, 18),-20} " +
            $"{book.Price,8:C}  " +
            $"{(book.IsAvailable ? "Yes" : "No")}");
    }

    Console.WriteLine($"\n{books.Count} book(s) found.");
}

static string Clip(string s, int max) =>
    s.Length <= max ? s : s[..max] + "..";

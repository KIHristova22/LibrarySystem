using Library.Dtos;
using Library.Models;
using Library.Repositories;
using Library.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.Controllers.Api
{
    [Route("api/books")]
    [ApiController]
    public class BooksApiController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;
        private readonly IBorrowRepository _borrowRepository;

        public BooksApiController(
            IBookRepository bookRepository,
            IBorrowRepository borrowRepository)
        {
            _bookRepository = bookRepository;
            _borrowRepository = borrowRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks(
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] bool? onlyAvailable = null)
        {
            var activeBorrowedIds = await _bookRepository.GetActiveBorrowedBookIdsAsync();

            var query = _bookRepository.QueryWithDetails();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b => b.Title.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(b => b.CategoryId == categoryId.Value);
            }

            if (onlyAvailable == true)
            {
                query = query.Where(b => !activeBorrowedIds.Contains(b.BookId));
            }

            var books = await query.OrderBy(b => b.Title).ToListAsync();

            return Ok(books.Select(b => ToDto(b, activeBorrowedIds)));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<BookDto>> GetBook(int id)
        {
            var book = await _bookRepository.FindByIdWithDetailsAsync(id);

            if (book == null)
            {
                return NotFound();
            }

            var isBorrowed = await _bookRepository.IsCurrentlyBorrowedAsync(id);
            var borrowedIds = isBorrowed ? new[] { id } : Array.Empty<int>();

            return Ok(ToDto(book, borrowedIds));
        }

        [HttpPost]
        public async Task<ActionResult<BookDto>> CreateBook([FromBody] BookWriteDto dto)
        {
            var book = new Book
            {
                Title = dto.Title,
                Price = dto.Price,
                AuthorId = dto.AuthorId,
                CategoryId = dto.CategoryId
            };

            await _bookRepository.AddAsync(book);
            await _bookRepository.SaveChangesAsync();

            await _bookRepository.LoadNavigationPropertiesAsync(book);

            return CreatedAtAction(
                nameof(GetBook),
                new { id = book.BookId },
                ToDto(book, Array.Empty<int>()));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateBook(int id, [FromBody] BookWriteDto dto)
        {
            var book = await _bookRepository.FindByIdAsync(id);

            if (book == null)
            {
                return NotFound();
            }

            book.Title = dto.Title;
            book.Price = dto.Price;
            book.AuthorId = dto.AuthorId;
            book.CategoryId = dto.CategoryId;

            await _bookRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _bookRepository.FindByIdAsync(id);

            if (book == null)
            {
                return NotFound();
            }

            await _bookRepository.RemoveAsync(book);
            await _bookRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("{id:int}/borrow")]
        public async Task<IActionResult> BorrowBook(int id, [FromBody] BorrowApiRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserId))
            {
                return BadRequest(new { message = "UserId is required." });
            }

            var book = await _bookRepository.FindByIdAsync(id);

            if (book == null)
            {
                return NotFound(new { message = "Book not found." });
            }

            var isBorrowed = await _borrowRepository.HasActiveBorrowAsync(id);

            if (isBorrowed)
            {
                return BadRequest(new { message = "Book is already borrowed." });
            }

            var record = new BorrowRecord
            {
                BookId = id,
                UserId = dto.UserId,
                BorrowDate = DateTime.Now,
                IsReturned = false
            };

            await _borrowRepository.AddBorrowRecordAsync(record);
            await _borrowRepository.SaveChangesAsync();

            return Ok(new { message = "Book borrowed successfully." });
        }

        private static BookDto ToDto(Book book, ICollection<int> borrowedIds)
        {
            return new BookDto
            {
                BookId = book.BookId,
                Title = book.Title,
                Price = book.Price,
                AuthorId = book.AuthorId,
                AuthorName = book.Author != null
                    ? $"{book.Author.FirstName} {book.Author.LastName}"
                    : string.Empty,
                CategoryId = book.CategoryId,
                CategoryName = book.Category?.Name ?? string.Empty,
                ImageUrl = book.ImageUrl,
                IsAvailable = !borrowedIds.Contains(book.BookId)
            };
        }
    }
}

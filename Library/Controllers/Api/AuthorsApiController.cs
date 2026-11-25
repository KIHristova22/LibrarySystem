using Library.Data;
using Library.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Library.Controllers.Api
{
    [Route("api/authors")]
    [ApiController]
    public class AuthorsApiController : ControllerBase
    {
        private readonly LibraryContext _context;

        public AuthorsApiController(LibraryContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuthorDto>>> GetAuthors()
        {
            var authors = await _context.Authors
                .OrderBy(a => a.LastName)
                .ThenBy(a => a.FirstName)
                .Select(a => new AuthorDto
                {
                    AuthorId = a.AuthorId,
                    FullName = a.FirstName + " " + a.LastName
                })
                .ToListAsync();

            return Ok(authors);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AuthorDto>> GetAuthor(int id)
        {
            var author = await _context.Authors
                .FirstOrDefaultAsync(a => a.AuthorId == id);

            if (author == null)
            {
                return NotFound();
            }

            return Ok(new AuthorDto
            {
                AuthorId = author.AuthorId,
                FullName = $"{author.FirstName} {author.LastName}"
            });
        }
    }
}

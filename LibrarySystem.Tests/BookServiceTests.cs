using Library.Models;
using Library.Services;
using Moq;

namespace LibrarySystem.Tests
{
    public class BookServiceTests
    {
        private static BookService CreateService(Mock<IBorrowRepository> repository)
        {
            return new BookService(repository.Object);
        }

        [Fact]
        public async Task BorrowBookAsync_AvailableBook_ReturnsSuccessAndSaves()
        {
            var repository = new Mock<IBorrowRepository>();
            repository.Setup(r => r.BookExistsAsync(1)).ReturnsAsync(true);
            repository.Setup(r => r.HasActiveBorrowAsync(1)).ReturnsAsync(false);

            var service = CreateService(repository);

            var result = await service.BorrowBookAsync(1, "user-1", false);

            Assert.Equal(BorrowResult.Success, result);
            repository.Verify(
                r => r.AddBorrowRecordAsync(It.Is<BorrowRecord>(record =>
                    record.BookId == 1 &&
                    record.UserId == "user-1" &&
                    !record.IsReturned)),
                Times.Once);
            repository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task BorrowBookAsync_AlreadyBorrowedBook_ReturnsAlreadyBorrowedAndDoesNotSave()
        {
            var repository = new Mock<IBorrowRepository>();
            repository.Setup(r => r.BookExistsAsync(1)).ReturnsAsync(true);
            repository.Setup(r => r.HasActiveBorrowAsync(1)).ReturnsAsync(true);

            var service = CreateService(repository);

            var result = await service.BorrowBookAsync(1, "user-1", false);

            Assert.Equal(BorrowResult.AlreadyBorrowed, result);
            repository.Verify(
                r => r.AddBorrowRecordAsync(It.IsAny<BorrowRecord>()),
                Times.Never);
            repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task BorrowBookAsync_MissingBook_ReturnsBookNotFound()
        {
            var repository = new Mock<IBorrowRepository>();
            repository.Setup(r => r.BookExistsAsync(99)).ReturnsAsync(false);

            var service = CreateService(repository);

            var result = await service.BorrowBookAsync(99, "user-1", false);

            Assert.Equal(BorrowResult.BookNotFound, result);
            repository.Verify(r => r.HasActiveBorrowAsync(It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData(true, "user-1")]
        [InlineData(false, null)]
        [InlineData(false, "")]
        public async Task BorrowBookAsync_InvalidRoleOrUser_ReturnsForbidden(
            bool isAdmin,
            string? userId)
        {
            var repository = new Mock<IBorrowRepository>();

            var service = CreateService(repository);

            var result = await service.BorrowBookAsync(1, userId, isAdmin);

            Assert.Equal(BorrowResult.Forbidden, result);
            repository.Verify(r => r.BookExistsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ReturnBookAsync_ActiveBorrow_MarksReturnedAndSaves()
        {
            var record = new BorrowRecord
            {
                BorrowRecordId = 5,
                BookId = 1,
                UserId = "user-1",
                IsReturned = false
            };

            var repository = new Mock<IBorrowRepository>();
            repository.Setup(r => r.GetActiveBorrowAsync(1, "user-1"))
                .ReturnsAsync(record);

            var service = CreateService(repository);

            var result = await service.ReturnBookAsync(1, "user-1");

            Assert.Equal(ReturnResult.Success, result);
            Assert.True(record.IsReturned);
            Assert.NotNull(record.ReturnDate);
            repository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ReturnBookAsync_NoActiveBorrow_ReturnsNotBorrowed()
        {
            var repository = new Mock<IBorrowRepository>();
            repository.Setup(r => r.GetActiveBorrowAsync(1, "user-1"))
                .ReturnsAsync((BorrowRecord?)null);

            var service = CreateService(repository);

            var result = await service.ReturnBookAsync(1, "user-1");

            Assert.Equal(ReturnResult.NotBorrowed, result);
            repository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public void IsBookAvailable_WithActiveBorrow_ReturnsFalse()
        {
            var records = new List<BorrowRecord>
            {
                new() { BookId = 1, IsReturned = true },
                new() { BookId = 1, IsReturned = false }
            };

            var service = CreateService(new Mock<IBorrowRepository>());

            Assert.False(service.IsBookAvailable(1, records));
            Assert.True(service.IsBookAvailable(2, records));
        }

        [Theory]
        [InlineData(20, false, true)]
        [InlineData(5, false, false)]
        [InlineData(20, true, false)]
        public void IsOverdue_EvaluatesLoanPeriodAndReturnState(
            int daysSinceBorrow,
            bool isReturned,
            bool expectedOverdue)
        {
            var currentDate = new DateTime(2026, 1, 31);

            var record = new BorrowRecord
            {
                BorrowDate = currentDate.AddDays(-daysSinceBorrow),
                IsReturned = isReturned
            };

            var service = CreateService(new Mock<IBorrowRepository>());

            var result = service.IsOverdue(record, currentDate, 14);

            Assert.Equal(expectedOverdue, result);
        }

        [Fact]
        public void ApplyFiltersAndSort_FiltersByCategoryAndAvailability()
        {
            var service = CreateService(new Mock<IBorrowRepository>());
            var books = BuildBooks().AsQueryable();
            var borrowedIds = new List<int> { 2 };

            var result = service
                .ApplyFiltersAndSort(books, borrowedIds, null, 10, true, null)
                .ToList();

            Assert.Single(result);
            Assert.Equal(1, result[0].BookId);
        }

        [Theory]
        [InlineData("price", new[] { 1, 3, 2 })]
        [InlineData("price_desc", new[] { 2, 3, 1 })]
        [InlineData("name_desc", new[] { 2, 3, 1 })]
        public void ApplyFiltersAndSort_SortsBooksByRequestedOrder(
            string sortOrder,
            int[] expectedOrder)
        {
            var service = CreateService(new Mock<IBorrowRepository>());
            var books = BuildBooks().AsQueryable();
            var borrowedIds = new List<int>();

            var result = service
                .ApplyFiltersAndSort(books, borrowedIds, null, null, null, sortOrder)
                .Select(book => book.BookId)
                .ToList();

            Assert.Equal(expectedOrder, result);
        }

        private static List<Book> BuildBooks()
        {
            return new List<Book>
            {
                new() { BookId = 1, Title = "Alpha", Price = 10m, CategoryId = 10 },
                new() { BookId = 2, Title = "Gamma", Price = 30m, CategoryId = 10 },
                new() { BookId = 3, Title = "Beta", Price = 20m, CategoryId = 20 }
            };
        }
    }
}

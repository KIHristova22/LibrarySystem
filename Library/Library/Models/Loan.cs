namespace Library.Models;

public class Loan
{
    public int LoanId { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int BookId { get; set; }
    public Book? Book { get; set; }
    public DateTime LoanDate { get; set; } =DateTime.Now;
    public DateTime DueDate { get; set; } = DateTime.Now.AddDays(14);
    public DateTime? ReturnDate { get; set; }
    public bool IsReturned => ReturnDate != null;
}
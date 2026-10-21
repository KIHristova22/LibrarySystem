namespace Library.Models;

public class Customer
{
    public int CustomerId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public List<Loan> Loans { get; set; }
    public int Id { get; set; }
}
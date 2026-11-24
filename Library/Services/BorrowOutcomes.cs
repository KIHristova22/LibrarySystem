namespace Library.Services
{
    public enum BorrowResult
    {
        Success,
        BookNotFound,
        AlreadyBorrowed,
        Forbidden
    }

    public enum ReturnResult
    {
        Success,
        NotBorrowed,
        Forbidden
    }
}

namespace Library.ViewModels
{
    public class ReportsViewModel
    {
        public int Year { get; set; }
        public int TotalBorrows { get; set; }
        public int ActiveLoans { get; set; }
        public int ReturnedBorrows { get; set; }
        public int TotalRegisteredUsers { get; set; }
        public string TopCategory { get; set; } = "No data";

        public List<MonthlyBorrowStat> MonthlyStatistics { get; set; } = new();
        public List<TopBookStat> TopBooks { get; set; } = new();
    }

    public class MonthlyBorrowStat
    {
        public string Month { get; set; } = string.Empty;
        public int BorrowCount { get; set; }
    }

    public class TopBookStat
    {
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int BorrowCount { get; set; }
    }
}
namespace CommunicationProject.ViewModels
{
    public class UserListViewModel
    {
        public List<UserItemViewModel> Users { get; set; } = new();

        public string? Search { get; set; }
        public string? Role { get; set; }

        public int PageNumber { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public class UserItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
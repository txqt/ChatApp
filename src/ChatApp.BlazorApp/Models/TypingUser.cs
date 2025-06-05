namespace ChatApp.BlazorApp.Models
{
    public class TypingUser
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }
}
namespace ChatApp.BlazorApp.Models
{
    public class CreateGroupChatRequest
    {
        public string ChatName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<int> MemberIds { get; set; } = new();
        public bool AllowMembersToAddOthers { get; set; } = true;
        public bool AllowMembersToEditInfo { get; set; } = false;
        public int? MaxMembers { get; set; }
    }
}
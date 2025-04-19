namespace ComunApi.Models.Intermediares
{
    public class UserCommunityRole
    {
        public int UserId { get; set; }

        public int CommunityId { get; set; }
        public Community Community { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }
    }
}

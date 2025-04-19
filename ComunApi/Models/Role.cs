using ComunApi.Models.Intermediares;

namespace ComunApi.Models
{
    public class Role
    {
        public int Id { get; set; }
        public int CommunityId { get; set;}
        public Community Community { get; set; }
        public string RoleName { get; set; }
        public bool CanDeleteThreads { get; set; } = false;
        public bool CanDeleteResponses { get; set; } = false;
        public bool CanBanUsers { get; set; } = false;
        public ICollection<UserCommunityRole> UserCommunityRoles { get; set; }
    }
}

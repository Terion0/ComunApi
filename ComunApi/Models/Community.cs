using ComunApi.Models.Intermediares;

namespace ComunApi.Models
{
    public class Community
    {
        public int Id { get; set; }
        public string ComName { get; set; }
        public string ComPicture { get; set; }
        public string ComBanner { get; set; }
        public string ComDescription { get; set; }
        public DateTime CreatedAt { get; set; } 
        public DateTime UpdatedAt { get; set; }

        public int CreatorId { get; set; }

        public ICollection<ThreadCom> Threads { get; set; } = new List<ThreadCom>();
        public ICollection<UserCommunityRole> UserRoles { get; set; } = new List<UserCommunityRole>();
    }
}

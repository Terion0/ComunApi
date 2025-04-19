namespace ComunApi.Models.DTO.DTORoles
{
    public class RoleCreateDTO
    {
        public int CommunityId { get; set; }
        public string RoleName { get; set; }
        public bool CanDeleteThreads { get; set; }
        public bool CanDeleteResponses { get; set; }
        public bool CanBanUsers { get; set; }
    }
}

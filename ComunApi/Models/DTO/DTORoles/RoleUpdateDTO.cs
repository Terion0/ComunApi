namespace ComunApi.Models.DTO.DTORoles
{
    public class RoleUpdateDTO
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
        public int CreatorId { get; set; }
        public bool CanDeleteThreads { get; set; } 
        public bool CanDeleteResponses { get; set; } 
        public bool CanBanUsers { get; set; } 
    }
}

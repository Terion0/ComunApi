namespace ComunApi.Models.DTO.DTOCommunity
{
    public class CommunityCreateDTO
    {
        public string ComName { get; set; }   
        public string ComDescription { get; set; }
        public IFormFile? profileImage { get; set; } 
        public IFormFile? bannerImage { get; set; }
    }
}

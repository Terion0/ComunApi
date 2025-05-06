namespace ComunApi.Models.DTO.DTOCommunity
{
    public class CommunityUpdateDTO
    {
        public int Id { get; set; }
        public string ComName { get; set; }
        public string ComDescription { get; set; }
        public IFormFile? newProfileImage { get; set; }
        public IFormFile? newBannerImage { get; set; }
    }
}

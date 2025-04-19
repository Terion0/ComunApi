using ComunApi.Models.DTO.DTOThread;

namespace ComunApi.Models.DTO.DTOCommunity
{
    public class CommunityDetailDTO
    {
        public int Id { get; set; }
        public string ComName { get; set; }
        public string ComPicture { get; set; }
        public string ComBanner { get; set; }
        public string ComDescription { get; set; }
        public int CreatorId { get; set; }
    }
}

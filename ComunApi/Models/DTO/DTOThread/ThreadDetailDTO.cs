using ComunApi.Models.DTO.DTOCommunity;

namespace ComunApi.Models.DTO.DTOThread
{
    public class ThreadDetailDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public CommunityDetailDTO Community { get; set; }
        public int CreatorId { get; set; }
        public ICollection<string> Images { get; set; }
    }
}

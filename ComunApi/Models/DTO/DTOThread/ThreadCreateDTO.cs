namespace ComunApi.Models.DTO.DTOThread
{
    public class ThreadCreateDTO
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public int CommunityId { get; set; }
        public ICollection<string> Images { get; set; }
    }
}

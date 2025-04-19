namespace ComunApi.Models.DTO.DTOResponse
{
    public class ResponseDetailDTO
    {
        public int Id { get; set; }
        public int CreatorId { get; set; }
        public string Content { get; set; }
        public bool IsDeleted { get; set; }
        public int Responses { get; set; } = 0;
    }
}

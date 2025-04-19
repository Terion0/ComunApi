namespace ComunApi.Models.DTO.DTOResponse
{
    public class ResponseCreateDTO
    {
        public string Content { get; set; }  
        public int ThreadId { get; set; }
        public int ParentId { get; set; } = -1;
    }
}

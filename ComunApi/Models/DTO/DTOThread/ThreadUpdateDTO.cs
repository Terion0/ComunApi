namespace ComunApi.Models.DTO.DTOThread
{
    public class ThreadUpdateDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public ICollection<string> Images { get; set; }
    }
}

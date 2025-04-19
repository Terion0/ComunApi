using System.Drawing;

namespace ComunApi.Models.DTO.DTOThread
{
    public class ThreadListDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public ICollection<string> Images { get; set; }
    }
}

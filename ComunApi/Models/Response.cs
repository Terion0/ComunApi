using Microsoft.Extensions.Hosting;

namespace ComunApi.Models
{
    public class Response
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }  
        public DateTime UpdatedAt { get; set; }
        public int CountResponses { get; set; } = -1;

        public int ThreadId { get; set; }
        public ThreadCom Thread { get; set; }

        public int CreatorId { get; set; }

        public int ParentId { get; set; }
        public Response ParentResponse { get; set; }
        public ICollection<Response> Responses { get; set; }  

    }
}

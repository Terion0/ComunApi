using ComunApi.Models.Intermediares;
using Microsoft.Extensions.Hosting;

namespace ComunApi.Models
{
    public class ThreadCom
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int CommunityId { get; set; }
        public Community Community { get; set; }

        public int CreatorId { get; set; }

        public ICollection<Response> Responses { get; set; } = new List<Response>();
        public ICollection<ThreadImage> Images { get; set; }
    }
}

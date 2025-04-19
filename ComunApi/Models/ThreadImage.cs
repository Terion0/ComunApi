namespace ComunApi.Models
{
    public class ThreadImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }

        public int ThreadId { get; set; }
        public ThreadCom Thread { get; set; }
    }
}

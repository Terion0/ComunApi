namespace ComunApi.Models.Intermediares
{
    public class ThreadLikes
    {
        public int ThreadId { get; set; }
        public ThreadCom thread { get; set; }

        public int UserId { get; set; }
    }
}

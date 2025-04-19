namespace ComunApi.Models.Intermediares
{
    public class CommunitySubscriptions
    {
        public int CommunityId { get; set; }
        public Community Community { get; set; }

        public int UserId { get; set; }
      
    }
}

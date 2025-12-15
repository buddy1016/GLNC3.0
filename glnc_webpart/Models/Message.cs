namespace glnc_webpart.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiveId { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public DateTime SendTime { get; set; }
        public DateTime? ReceiveTime { get; set; }
        public User? Sender { get; set; }
        public User? Receiver { get; set; }
    }
}


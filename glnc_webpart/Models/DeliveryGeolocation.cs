namespace glnc_webpart.Models
{
    public class DeliveryGeolocation
    {
        public int Id { get; set; }
        public double SignLati { get; set; }
        public double SignLongi { get; set; }
        public double SignAlti { get; set; }
        public int DeliveryId { get; set; }
        public int UserId { get; set; }
        public Delivery? Delivery { get; set; }
        public User? User { get; set; }
    }
}


namespace glnc_webpart.Models
{
    public class AttendanceTracking
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
        public double Lati { get; set; }
        public double Longi { get; set; }
        public double Alti { get; set; }
        public byte Type { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}


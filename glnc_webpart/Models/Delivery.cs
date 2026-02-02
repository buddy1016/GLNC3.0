namespace glnc_webpart.Models
{
    public class Delivery
    {
        public int Id { get; set; }
        public DateTime DateTimeAppointment { get; set; }
        public DateTime DateTimeLeave { get; set; }
        public DateTime? DateTimeAccept { get; set; }
        public DateTime? DateTimeArrival { get; set; }
        public string? Description { get; set; }
        public string? SignClient { get; set; }
        public int? SatisfactionClient { get; set; }
        public bool ReturnFlag { get; set; } = false;
        public int TruckId { get; set; }
        public string Client { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Contacts { get; set; } = string.Empty;
        public string Invoice { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public double Weight { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string? InvoiceImage { get; set; }
        public int UserId { get; set; }
        public Supplier? Supplier { get; set; }
        public User? User { get; set; }
        public Truck? Truck { get; set; }
    }
}


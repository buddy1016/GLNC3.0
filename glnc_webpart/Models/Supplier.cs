namespace glnc_webpart.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public bool Check { get; set; } = false;
    }
}


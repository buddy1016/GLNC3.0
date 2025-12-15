using glnc_webpart.Models;
using Microsoft.EntityFrameworkCore;

namespace glnc_webpart.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<AttendanceTracking> AttendanceTrackings { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<DeliveryGeolocation> DeliveryGeolocations { get; set; }
        public DbSet<DriverGeolocation> DriverGeolocations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Truck> Trucks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(30).HasColumnName("name");
                entity.Property(e => e.Password).IsRequired().HasMaxLength(50).HasColumnName("password");
                entity.Property(e => e.Role).IsRequired().HasColumnName("role").HasDefaultValue(1);
            });

            // Configure AttendanceTracking
            modelBuilder.Entity<AttendanceTracking>(entity =>
            {
                entity.ToTable("attendance_tracking");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Time).IsRequired().HasColumnName("time");
                entity.Property(e => e.Lati).IsRequired().HasColumnName("lati");
                entity.Property(e => e.Longi).IsRequired().HasColumnName("longi");
                entity.Property(e => e.Alti).IsRequired().HasColumnName("alti");
                entity.Property(e => e.Type).IsRequired().HasColumnName("type");
                entity.Property(e => e.UserId).IsRequired().HasColumnName("user_id");
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Delivery
            modelBuilder.Entity<Delivery>(entity =>
            {
                entity.ToTable("delivery");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.DateTimeAppointment).IsRequired().HasColumnName("date_time_appointment");
                entity.Property(e => e.DateTimeLeave).IsRequired().HasColumnName("date_time_leave");
                entity.Property(e => e.DateTimeAccept).HasColumnName("date_time_accept");
                entity.Property(e => e.DateTimeArrival).HasColumnName("date_time_arrival");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.SignClient).HasColumnName("sign_client");
                entity.Property(e => e.SatisfactionClient).HasColumnName("satisfaction_client");
                entity.Property(e => e.ReturnFlag).IsRequired().HasColumnName("return_flag").HasDefaultValue(false);
                entity.Property(e => e.TruckId).IsRequired().HasColumnName("truck_id");
                entity.Property(e => e.Client).IsRequired().HasMaxLength(250).HasColumnName("client");
                entity.Property(e => e.Address).IsRequired().HasMaxLength(250).HasColumnName("address");
                entity.Property(e => e.Contacts).IsRequired().HasMaxLength(250).HasColumnName("contacts");
                entity.Property(e => e.Invoice).IsRequired().HasMaxLength(250).HasColumnName("invoice");
                entity.Property(e => e.SupplierId).IsRequired().HasColumnName("supplier_id");
                entity.Property(e => e.Weight).IsRequired().HasColumnName("weight");
                entity.Property(e => e.Comment).IsRequired().HasColumnName("comment");
                entity.Property(e => e.InvoiceImage).HasColumnName("invoice_image");
                entity.Property(e => e.UserId).IsRequired().HasColumnName("user_id");
                entity.HasOne(e => e.Supplier)
                    .WithMany()
                    .HasForeignKey(e => e.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Truck)
                    .WithMany()
                    .HasForeignKey(e => e.TruckId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure DeliveryGeolocation
            modelBuilder.Entity<DeliveryGeolocation>(entity =>
            {
                entity.ToTable("delivery_geolocation");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SignLati).IsRequired().HasColumnName("sign_lati");
                entity.Property(e => e.SignLongi).IsRequired().HasColumnName("sign_longi");
                entity.Property(e => e.SignAlti).IsRequired().HasColumnName("sign_alti");
                entity.Property(e => e.DeliveryId).IsRequired().HasColumnName("delivery_id");
                entity.Property(e => e.UserId).IsRequired().HasColumnName("user_id");
                entity.HasOne(e => e.Delivery)
                    .WithMany()
                    .HasForeignKey(e => e.DeliveryId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure DriverGeolocation
            modelBuilder.Entity<DriverGeolocation>(entity =>
            {
                entity.ToTable("drivergeolocation");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Lati).IsRequired().HasColumnName("lati");
                entity.Property(e => e.Longi).IsRequired().HasColumnName("longi");
                entity.Property(e => e.Alti).IsRequired().HasColumnName("alti");
                entity.Property(e => e.DateTime).IsRequired().HasColumnName("date_time");
            });

            // Configure Message
            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("message");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SenderId).IsRequired().HasColumnName("sender_id");
                entity.Property(e => e.ReceiveId).IsRequired().HasColumnName("receive_id");
                entity.Property(e => e.MessageText).IsRequired().HasColumnName("message");
                entity.Property(e => e.SendTime).IsRequired().HasColumnName("sendtime");
                entity.Property(e => e.ReceiveTime).HasColumnName("receivetime");
                entity.HasOne(e => e.Sender)
                    .WithMany()
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Receiver)
                    .WithMany()
                    .HasForeignKey(e => e.ReceiveId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Supplier
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.ToTable("supplier");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.SupplierName).IsRequired().HasMaxLength(30).HasColumnName("supplier_name");
                entity.Property(e => e.Mail).IsRequired().HasMaxLength(250).HasColumnName("mail");
                entity.Property(e => e.Check).HasColumnName("check").HasDefaultValue(false);
            });

            // Configure Truck
            modelBuilder.Entity<Truck>(entity =>
            {
                entity.ToTable("trucks");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.License).IsRequired().HasMaxLength(250).HasColumnName("license");
                entity.Property(e => e.Brand).IsRequired().HasMaxLength(50).HasColumnName("brand");
                entity.Property(e => e.Model).IsRequired().HasMaxLength(50).HasColumnName("model");
                entity.Property(e => e.Color).IsRequired().HasMaxLength(10).HasColumnName("color");
            });
        }
    }
}


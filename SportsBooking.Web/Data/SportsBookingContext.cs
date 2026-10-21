using Microsoft.EntityFrameworkCore;
using SportsBooking.Web.Models;

namespace SportsBooking.Web.Data;

// Класовете отговарят на таблиците от SportsBooking_Simple.sql.
public class SportsBookingContext : DbContext
{
    public SportsBookingContext(DbContextOptions<SportsBookingContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<SportsCenter> SportsCenters => Set<SportsCenter>();
    public DbSet<Field> Fields => Set<Field>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.FullName).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false);
        });

        modelBuilder.Entity<Sport>(e =>
        {
            e.ToTable("Sports");
            e.HasIndex(x => x.SportName).IsUnique();
            e.Property(x => x.SportName).HasMaxLength(50);
        });

        modelBuilder.Entity<SportsCenter>(e =>
        {
            e.ToTable("SportsCenters");
            e.Property(x => x.CenterName).HasMaxLength(100);
            e.Property(x => x.City).HasMaxLength(50);
        });

        modelBuilder.Entity<Field>(e =>
        {
            e.ToTable("Fields");
            e.Property(x => x.FieldName).HasMaxLength(50);
            e.Property(x => x.PricePerHour).HasPrecision(8, 2);
            e.HasOne(x => x.Center).WithMany(x => x.Fields)
                .HasForeignKey(x => x.CenterID).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Sport).WithMany(x => x.Fields)
                .HasForeignKey(x => x.SportID).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.ToTable("Bookings");
            e.Property(x => x.BookingDate).HasColumnType("date");
            e.Property(x => x.StartTime).HasColumnType("time");
            e.Property(x => x.EndTime).HasColumnType("time");
            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Активна");
            e.HasOne(x => x.User).WithMany(x => x.Bookings)
                .HasForeignKey(x => x.UserID).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Field).WithMany(x => x.Bookings)
                .HasForeignKey(x => x.FieldID).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("Payments");
            e.HasIndex(x => x.BookingID).IsUnique();
            e.Property(x => x.Amount).HasPrecision(8, 2);
            e.Property(x => x.PaymentMethod).HasMaxLength(20);
            e.HasOne(x => x.Booking).WithOne(x => x.Payment)
                .HasForeignKey<Payment>(x => x.BookingID).OnDelete(DeleteBehavior.NoAction);
        });
    }
}

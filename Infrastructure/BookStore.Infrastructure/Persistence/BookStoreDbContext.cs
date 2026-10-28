using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Persistence;

public sealed class BookStoreDbContext(DbContextOptions<BookStoreDbContext> options) : DbContext(options)
{
    public DbSet<BookCategory> Categories => Set<BookCategory>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
    public DbSet<UserRole> Roles => Set<UserRole>();
    public DbSet<UserAccount> Accounts => Set<UserAccount>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<PurchaseReceiptLine> PurchaseReceiptLines => Set<PurchaseReceiptLine>();
    public DbSet<SalesOrder> Orders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> OrderLines => Set<SalesOrderLine>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<BookPromotion> BookPromotions => Set<BookPromotion>();
    public DbSet<OrderPromotion> OrderPromotions => Set<OrderPromotion>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<PromotionVoucher> PromotionVouchers => Set<PromotionVoucher>();
    public DbSet<SystemAuditLog> SystemAuditLogs => Set<SystemAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BookCategory>(entity =>
        {
            entity.ToTable("LoaiSach");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("MaLoai").HasMaxLength(20);
            entity.Property(x => x.Name).HasColumnName("TenLoai").HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("Sach");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("MaSach").HasMaxLength(30);
            entity.Property(x => x.Title).HasColumnName("TenSach").HasMaxLength(250).IsRequired();
            entity.Property(x => x.StockQuantity).HasColumnName("SLCo");
            entity.Property(x => x.SalePrice).HasColumnName("DGBan").HasPrecision(18, 2);
            entity.Property(x => x.IsActive).HasColumnName("TrangThai").HasDefaultValue(true);
            entity.Property(x => x.Unit).HasColumnName("DVTinh").HasMaxLength(30);
            entity.Property(x => x.CategoryId).HasColumnName("MaLoai").HasMaxLength(20);
            entity.Property(x => x.PublisherId).HasColumnName("IDNhaXuatBan");
            entity.HasIndex(x => x.Title);
            entity.HasOne(x => x.Category).WithMany(x => x.Books).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Publisher).WithMany(x => x.Books).HasForeignKey(x => x.PublisherId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Publisher>(entity =>
        {
            entity.ToTable("NhaXuatBan");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("IDNhaXuatBan");
            entity.Property(x => x.Name).HasColumnName("TenNhaXuatBan").HasMaxLength(180).IsRequired();
            entity.Property(x => x.Address).HasColumnName("DiaChi").HasMaxLength(300);
        });

        modelBuilder.Entity<Author>(entity =>
        {
            entity.ToTable("TacGia");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("IDTacGia");
            entity.Property(x => x.Name).HasColumnName("TenTacGia").HasMaxLength(180).IsRequired();
            entity.Property(x => x.Biography).HasColumnName("TieuSu").HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<BookAuthor>(entity =>
        {
            entity.ToTable("TacGiaSach");
            entity.HasKey(x => new { x.BookId, x.AuthorId });
            entity.Property(x => x.BookId).HasColumnName("MaSach").HasMaxLength(30);
            entity.Property(x => x.AuthorId).HasColumnName("IDTacGia");
            entity.HasOne(x => x.Book).WithMany(x => x.Authors).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Author).WithMany(x => x.Books).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("VaiTro");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("IDVaiTro");
            entity.Property(x => x.Name).HasColumnName("TenVaiTro").HasMaxLength(60).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("TaiKhoan");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("IDTaiKhoan");
            entity.Property(x => x.UserName).HasColumnName("TenDangNhap").HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("MatKhauHash").HasMaxLength(500).IsRequired();
            entity.Property(x => x.Email).HasColumnName("Email").HasMaxLength(254).IsRequired();
            entity.Property(x => x.Status).HasColumnName("TrangThai").HasMaxLength(30).IsRequired();
            entity.Property(x => x.RoleId).HasColumnName("IDVaiTro");
            entity.HasIndex(x => x.UserName).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasOne(x => x.Role).WithMany(x => x.Accounts).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("NV");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("MaNV").HasMaxLength(20);
            entity.Property(x => x.LastName).HasColumnName("HoLot").HasMaxLength(100).IsRequired();
            entity.Property(x => x.FirstName).HasColumnName("Ten").HasMaxLength(60).IsRequired();
            entity.Property(x => x.BirthDate).HasColumnName("NS");
            entity.Property(x => x.AvatarPath).HasColumnName("AnhChanDung").HasMaxLength(500);
            entity.Property(x => x.Address).HasColumnName("DiaChi").HasMaxLength(300);
            entity.Property(x => x.AccountStatus).HasColumnName("TrangThaiTaiKhoan").HasMaxLength(30);
            entity.Property(x => x.BaseSalary).HasColumnName("LuongCB").HasPrecision(18, 2);
            entity.Property(x => x.UserAccountId).HasColumnName("IDTaiKhoan");
            entity.HasIndex(x => x.UserAccountId).IsUnique().HasFilter("[IDTaiKhoan] IS NOT NULL");
            entity.HasOne(x => x.UserAccount).WithOne(x => x.Employee).HasForeignKey<Employee>(x => x.UserAccountId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("KH");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("MaKH").HasMaxLength(20);
            entity.Property(x => x.FullName).HasColumnName("HoTen").HasMaxLength(180).IsRequired();
            entity.Property(x => x.UserAccountId).HasColumnName("IDTaiKhoan");
            entity.Property(x => x.PhoneNumber).HasColumnName("SoDienThoai").HasMaxLength(30);
            entity.Property(x => x.ShippingAddress).HasColumnName("DiaChiGiaoHang").HasMaxLength(500);
            entity.HasIndex(x => x.UserAccountId).IsUnique().HasFilter("[IDTaiKhoan] IS NOT NULL");
            entity.HasOne(x => x.UserAccount).WithOne(x => x.Customer).HasForeignKey<Customer>(x => x.UserAccountId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("NCC");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("MaNCC").HasMaxLength(20);
            entity.Property(x => x.Name).HasColumnName("TenNCC").HasMaxLength(180).IsRequired();
            entity.Property(x => x.PhoneNumber).HasColumnName("SoDienThoai").HasMaxLength(30);
        });

        ConfigureReceipt(modelBuilder);
        ConfigureOrders(modelBuilder);
        ConfigurePromotions(modelBuilder);
        modelBuilder.Entity<SystemAuditLog>(entity =>
        {
            entity.ToTable("SystemAuditLog");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.OccurredAt).HasColumnType("datetime2");
            entity.Property(x => x.ActorName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ActorCode).HasMaxLength(30);
            entity.Property(x => x.ActorRole).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Area).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Resource).HasMaxLength(300).IsRequired();
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.HasIndex(x => x.OccurredAt);
        });
    }

    private static void ConfigureReceipt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseReceipt>(entity =>
        {
            entity.ToTable("PNH");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("MaPNH").HasMaxLength(30);
            entity.Property(x => x.CreatedAt).HasColumnName("NgayLap");
            entity.Property(x => x.EmployeeId).HasColumnName("MaNV").HasMaxLength(20);
            entity.Property(x => x.SupplierId).HasColumnName("MaNCC").HasMaxLength(20);
            entity.Property(x => x.TotalAmount).HasColumnName("TongTien").HasPrecision(18, 2);
            entity.Property(x => x.Status).HasColumnName("TrangThai").HasMaxLength(30).HasDefaultValue("Posted");
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseReceiptLine>(entity =>
        {
            entity.ToTable("CTPNH");
            entity.HasKey(x => new { x.ReceiptId, x.BookId });
            entity.Property(x => x.ReceiptId).HasColumnName("MaPNH").HasMaxLength(30);
            entity.Property(x => x.BookId).HasColumnName("MaSach").HasMaxLength(30);
            entity.Property(x => x.Quantity).HasColumnName("SL");
            entity.Property(x => x.UnitCost).HasColumnName("DG").HasPrecision(18, 2);
            entity.Property(x => x.LineTotal).HasColumnName("ThanhTien").HasPrecision(18, 2);
            entity.HasOne(x => x.Receipt).WithMany(x => x.Lines).HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.ToTable("HD");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("MaHD").HasMaxLength(30);
            entity.Property(x => x.CreatedAt).HasColumnName("NgayLapHD");
            entity.Property(x => x.EmployeeId).HasColumnName("MaNV").HasMaxLength(20);
            entity.Property(x => x.CustomerId).HasColumnName("MaKH").HasMaxLength(20);
            entity.Property(x => x.TotalAmount).HasColumnName("TongTien").HasPrecision(18, 2);
            entity.Property(x => x.DiscountAmount).HasColumnName("TongGiamGia").HasPrecision(18, 2);
            entity.Property(x => x.OrderStatus).HasColumnName("TrangThaiDonHang").HasMaxLength(30);
            entity.Property(x => x.PaymentStatus).HasColumnName("TrangThaiThanhToan").HasMaxLength(30);
            entity.Property(x => x.PaymentMethod).HasColumnName("PhuongThucThanhToan").HasMaxLength(40);
            entity.Property(x => x.ShippingAddress).HasColumnName("DiaChiGiaoHang").HasMaxLength(500);
            entity.Property(x => x.ReceiverPhone).HasColumnName("SDTNhanHang").HasMaxLength(30);
            entity.Property(x => x.VoucherId).HasColumnName("MaVoucher").HasMaxLength(40);
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Customer).WithMany(x => x.Orders).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Voucher).WithMany(x => x.Orders).HasForeignKey(x => x.VoucherId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.CreatedAt, x.OrderStatus });
        });

        modelBuilder.Entity<SalesOrderLine>(entity =>
        {
            entity.ToTable("CTHD");
            entity.HasKey(x => new { x.OrderId, x.BookId });
            entity.Property(x => x.OrderId).HasColumnName("MaHD").HasMaxLength(30);
            entity.Property(x => x.BookId).HasColumnName("MaSach").HasMaxLength(30);
            entity.Property(x => x.Quantity).HasColumnName("SL");
            entity.Property(x => x.UnitPrice).HasColumnName("DG").HasPrecision(18, 2);
            entity.Property(x => x.LineTotal).HasColumnName("ThanhTien").HasPrecision(18, 2);
            entity.HasOne(x => x.Order).WithMany(x => x.Lines).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("GioHang");
            entity.HasKey(x => new { x.CustomerId, x.BookId });
            entity.Property(x => x.CustomerId).HasColumnName("MaKH").HasMaxLength(20);
            entity.Property(x => x.BookId).HasColumnName("MaSach").HasMaxLength(30);
            entity.Property(x => x.Quantity).HasColumnName("SoLuong");
            entity.HasOne(x => x.Customer).WithMany(x => x.CartItems).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePromotions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("CTKM");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("MaCTKM").HasMaxLength(30);
            entity.Property(x => x.Name).HasColumnName("TenCTKM").HasMaxLength(180).IsRequired();
            entity.Property(x => x.StartsAt).HasColumnName("BeginDate");
            entity.Property(x => x.EndsAt).HasColumnName("EndDate");
        });

        modelBuilder.Entity<BookPromotion>(entity =>
        {
            entity.ToTable("KMTheoSach");
            entity.HasKey(x => new { x.PromotionId, x.BookId });
            entity.Property(x => x.PromotionId).HasColumnName("MaCTKM").HasMaxLength(30);
            entity.Property(x => x.BookId).HasColumnName("MaSach").HasMaxLength(30);
            entity.Property(x => x.DiscountPercent).HasColumnName("PhanTramGiam").HasPrecision(5, 2);
            entity.HasOne(x => x.Promotion).WithMany(x => x.Books).HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderPromotion>(entity =>
        {
            entity.ToTable("KMTheoHD");
            entity.HasKey(x => new { x.PromotionId, x.MinimumOrderAmount });
            entity.Property(x => x.PromotionId).HasColumnName("MaCTKM").HasMaxLength(30);
            entity.Property(x => x.MinimumOrderAmount).HasColumnName("MucTienHD").HasPrecision(18, 2);
            entity.Property(x => x.DiscountPercent).HasColumnName("PhanTramGiam").HasPrecision(5, 2);
            entity.HasOne(x => x.Promotion).WithMany(x => x.OrderDiscounts).HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.ToTable("Voucher");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("MaVoucher").HasMaxLength(40);
            entity.Property(x => x.Status).HasColumnName("TrangThai").HasMaxLength(30);
            entity.Property(x => x.ExpiresAt).HasColumnName("HanSuDung");
            entity.Property(x => x.MaxUsageCount).HasColumnName("SoLanToiDa");
            entity.Property(x => x.UsageCount).HasColumnName("SoLanDaSuDung").HasDefaultValue(0);
        });

        modelBuilder.Entity<PromotionVoucher>(entity =>
        {
            entity.ToTable("KMTheoVoucher");
            entity.HasKey(x => new { x.PromotionId, x.VoucherId });
            entity.Property(x => x.PromotionId).HasColumnName("MaCTKM").HasMaxLength(30);
            entity.Property(x => x.VoucherId).HasColumnName("MaVoucher").HasMaxLength(40);
            entity.Property(x => x.DiscountPercent).HasColumnName("PhanTramGiam").HasPrecision(5, 2);
            entity.HasOne(x => x.Promotion).WithMany(x => x.Vouchers).HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Voucher).WithMany(x => x.Promotions).HasForeignKey(x => x.VoucherId).OnDelete(DeleteBehavior.Cascade);
        });
    }

}

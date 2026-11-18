namespace TrangSach.DAO;

/// <summary>
/// Lớp tổng hợp DAO theo kiến trúc 3 tầng (GUI - BUS - DAO).
/// Chỉ là container chứa các DAO cụ thể; không giữ trạng thái kết nối.
/// </summary>
public class BookStoreDao : IDisposable
{
    public CatalogDao Catalog { get; } = new();
    public InventoryDao Inventory { get; } = new();
    public OrderDao Orders { get; } = new();
    public UserDao Users { get; } = new();
    public PromotionDao Promotions { get; } = new();
    public ReportDao Reports { get; } = new();

    public static string ConnectionCaption => DbConnection.ConnectionCaption;

    /// <summary>Kiểm tra kết nối SQL Server khi khởi động.</summary>
    public static void Initialize() => DbConnection.TestConnection();

    public static BookStoreDao Create() => new();

    public void Dispose() { }
}

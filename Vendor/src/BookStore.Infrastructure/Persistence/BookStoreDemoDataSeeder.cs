using BookStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Persistence;

/// <summary>
/// Adds a small, clearly identified set of sample commerce records so admin reports
/// can be explored in a fresh local database. Safe to run on every API startup.
/// </summary>
internal static class BookStoreDemoDataSeeder
{
    private const string OrderPrefix = "DEMO-HD-";
    private const string ReceiptPrefix = "DEMO-PNH-";

    public static async Task SeedAsync(BookStoreDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Orders.AnyAsync(order => order.Id.StartsWith(OrderPrefix), cancellationToken))
            return;

        var books = await db.Books.Where(book => book.IsActive).OrderBy(book => book.Id).Take(6).ToListAsync(cancellationToken);
        if (books.Count < 2)
            return;

        var customers = new List<Customer>();
        for (var i = 1; i <= 8; i++)
        {
            var id = $"DEMO-KH-{i:00}";
            var customer = await db.Customers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (customer is null)
            {
                customer = new Customer
                {
                    Id = id,
                    FullName = $"Khách hàng demo {i:00}",
                    PhoneNumber = $"09000000{i:00}",
                    ShippingAddress = $"{i * 10} Đường Sách, Quận {i % 5 + 1}, TP. Hồ Chí Minh"
                };
                db.Customers.Add(customer);
            }
            customers.Add(customer);
        }

        var employee = await db.Employees.SingleOrDefaultAsync(item => item.Id == "DEMO-NV-01", cancellationToken);
        if (employee is null)
        {
            employee = new Employee
            {
                Id = "DEMO-NV-01",
                LastName = "Nhân viên",
                FirstName = "Demo",
                AccountStatus = "Active",
                BaseSalary = 0m
            };
            db.Employees.Add(employee);
        }

        var supplier = await db.Suppliers.SingleOrDefaultAsync(item => item.Id == "DEMO-NCC-01", cancellationToken);
        if (supplier is null)
        {
            supplier = new Supplier { Id = "DEMO-NCC-01", Name = "Nhà cung cấp mẫu Trang Sách", PhoneNumber = "02800000001" };
            db.Suppliers.Add(supplier);
        }

        var today = DateTime.UtcNow.Date;
        var orderStatuses = new[] { "Completed", "Completed", "Pending", "Completed", "Confirmed", "Completed", "Cancelled", "Shipping", "Completed", "Completed" };
        var orders = new List<SalesOrder>();

        // One order per day for the current month gives the 7-day and 30-day charts visible data.
        for (var dayOffset = 0; dayOffset < 30; dayOffset++)
        {
            var index = dayOffset;
            orders.Add(CreateOrder(index, today.AddDays(-dayOffset).AddHours(9 + index % 9), orderStatuses[index % orderStatuses.Length], books, customers));
        }

        // Add a year of history so the 90-day and 1-year filters are useful too.
        for (var i = 0; i < 30; i++)
        {
            var index = 30 + i;
            var daysAgo = 38 + i * 11;
            orders.Add(CreateOrder(index, today.AddDays(-daysAgo).AddHours(10 + i % 7), orderStatuses[(index * 3) % orderStatuses.Length], books, customers));
        }

        // Seed receipt lines before reducing on-hand stock for non-cancelled orders.
        var receipts = new List<PurchaseReceipt>();
        var receiptDates = Enumerable.Range(0, 10).Select(i => today.AddDays(-i * 3))
            .Concat(Enumerable.Range(0, 15).Select(i => today.AddDays(-(39 + i * 22))))
            .OrderBy(date => date).ToArray();
        for (var i = 0; i < receiptDates.Length; i++)
        {
            var firstBook = books[i % books.Count];
            var secondBook = books[(i + 2) % books.Count];
            var lines = new[] { firstBook, secondBook }.Select((book, lineIndex) =>
            {
                var quantity = 4 + (i + lineIndex * 2) % 5;
                var unitCost = decimal.Round(book.SalePrice * 0.62m, 2);
                book.StockQuantity += quantity;
                return new PurchaseReceiptLine
                {
                    Book = book,
                    Quantity = quantity,
                    UnitCost = unitCost,
                    LineTotal = unitCost * quantity
                };
            }).ToList();

            receipts.Add(new PurchaseReceipt
            {
                Id = $"{ReceiptPrefix}{i + 1:0000}",
                CreatedAt = receiptDates[i].AddHours(8 + i % 8),
                Employee = employee,
                Supplier = supplier,
                TotalAmount = lines.Sum(line => line.LineTotal),
                Lines = lines
            });
        }

        foreach (var order in orders)
        {
            if (order.OrderStatus != "Cancelled")
            {
                foreach (var line in order.Lines)
                    line.Book.StockQuantity -= line.Quantity;
            }
        }

        db.Orders.AddRange(orders);
        db.PurchaseReceipts.AddRange(receipts);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static SalesOrder CreateOrder(int index, DateTime createdAt, string status, IReadOnlyList<Book> books, IReadOnlyList<Customer> customers)
    {
        var firstIndex = index % books.Count;
        var secondIndex = (index * 3 + 2) % books.Count;
        if (secondIndex == firstIndex)
            secondIndex = (secondIndex + 1) % books.Count;

        var selectedBooks = new[] { books[firstIndex], books[secondIndex] };
        var lines = selectedBooks.Select((book, lineIndex) =>
        {
            var quantity = 1 + (index + lineIndex) % 3;
            return new SalesOrderLine
            {
                Book = book,
                Quantity = quantity,
                UnitPrice = book.SalePrice,
                LineTotal = book.SalePrice * quantity
            };
        }).ToList();

        var subtotal = lines.Sum(line => line.LineTotal);
        var discount = index % 5 == 0 ? decimal.Round(subtotal * 0.05m, 0) : 0m;
        return new SalesOrder
        {
            Id = $"{OrderPrefix}{index + 1:0000}",
            CreatedAt = createdAt,
            Customer = customers[index % customers.Count],
            TotalAmount = subtotal - discount,
            DiscountAmount = discount,
            OrderStatus = status,
            PaymentStatus = status == "Completed" ? "Paid" : "Unpaid",
            PaymentMethod = "CashOnDelivery",
            ShippingAddress = $"{index + 1} Đường Sách, TP. Hồ Chí Minh",
            ReceiverPhone = $"090100{index:0000}",
            Lines = lines
        };
    }
}

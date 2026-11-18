
namespace TrangSach.Shared.DTO
{
    public class EmployeeYearSalesDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Orders { get; set; }
        public decimal Revenue { get; set; }

        public EmployeeYearSalesDto()
        {
        }

        public EmployeeYearSalesDto(string employeeId, string employeeName, int year, int orders, decimal revenue)
        {
            this.EmployeeId = employeeId;
            this.EmployeeName = employeeName;
            this.Year = year;
            this.Orders = orders;
            this.Revenue = revenue;
        }
    }
}


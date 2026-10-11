using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DiscountsAndVoucherUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SoLanDaSuDung",
                table: "Voucher",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SoLanToiDa",
                table: "Voucher",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TongGiamGia",
                table: "HD",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoLanDaSuDung",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "SoLanToiDa",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "TongGiamGia",
                table: "HD");
        }
    }
}

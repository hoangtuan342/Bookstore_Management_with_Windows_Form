using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CTKM",
                columns: table => new
                {
                    MaCTKM = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TenCTKM = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    BeginDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CTKM", x => x.MaCTKM);
                });

            migrationBuilder.CreateTable(
                name: "LoaiSach",
                columns: table => new
                {
                    MaLoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenLoai = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiSach", x => x.MaLoai);
                });

            migrationBuilder.CreateTable(
                name: "NCC",
                columns: table => new
                {
                    MaNCC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenNCC = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NCC", x => x.MaNCC);
                });

            migrationBuilder.CreateTable(
                name: "NhaXuatBan",
                columns: table => new
                {
                    IDNhaXuatBan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenNhaXuatBan = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhaXuatBan", x => x.IDNhaXuatBan);
                });

            migrationBuilder.CreateTable(
                name: "TacGia",
                columns: table => new
                {
                    IDTacGia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenTacGia = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    TieuSu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TacGia", x => x.IDTacGia);
                });

            migrationBuilder.CreateTable(
                name: "VaiTro",
                columns: table => new
                {
                    IDVaiTro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenVaiTro = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaiTro", x => x.IDVaiTro);
                });

            migrationBuilder.CreateTable(
                name: "Voucher",
                columns: table => new
                {
                    MaVoucher = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    HanSuDung = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voucher", x => x.MaVoucher);
                });

            migrationBuilder.CreateTable(
                name: "KMTheoHD",
                columns: table => new
                {
                    MaCTKM = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MucTienHD = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PhanTramGiam = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KMTheoHD", x => new { x.MaCTKM, x.MucTienHD });
                    table.ForeignKey(
                        name: "FK_KMTheoHD_CTKM_MaCTKM",
                        column: x => x.MaCTKM,
                        principalTable: "CTKM",
                        principalColumn: "MaCTKM",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sach",
                columns: table => new
                {
                    MaSach = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TenSach = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SLCo = table.Column<int>(type: "int", nullable: false),
                    DGBan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DVTinh = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaLoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IDNhaXuatBan = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sach", x => x.MaSach);
                    table.ForeignKey(
                        name: "FK_Sach_LoaiSach_MaLoai",
                        column: x => x.MaLoai,
                        principalTable: "LoaiSach",
                        principalColumn: "MaLoai",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sach_NhaXuatBan_IDNhaXuatBan",
                        column: x => x.IDNhaXuatBan,
                        principalTable: "NhaXuatBan",
                        principalColumn: "IDNhaXuatBan",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    IDTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MatKhauHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IDVaiTro = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.IDTaiKhoan);
                    table.ForeignKey(
                        name: "FK_TaiKhoan_VaiTro_IDVaiTro",
                        column: x => x.IDVaiTro,
                        principalTable: "VaiTro",
                        principalColumn: "IDVaiTro",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KMTheoVoucher",
                columns: table => new
                {
                    MaCTKM = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaVoucher = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PhanTramGiam = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KMTheoVoucher", x => new { x.MaCTKM, x.MaVoucher });
                    table.ForeignKey(
                        name: "FK_KMTheoVoucher_CTKM_MaCTKM",
                        column: x => x.MaCTKM,
                        principalTable: "CTKM",
                        principalColumn: "MaCTKM",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KMTheoVoucher_Voucher_MaVoucher",
                        column: x => x.MaVoucher,
                        principalTable: "Voucher",
                        principalColumn: "MaVoucher",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KMTheoSach",
                columns: table => new
                {
                    MaCTKM = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaSach = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhanTramGiam = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KMTheoSach", x => new { x.MaCTKM, x.MaSach });
                    table.ForeignKey(
                        name: "FK_KMTheoSach_CTKM_MaCTKM",
                        column: x => x.MaCTKM,
                        principalTable: "CTKM",
                        principalColumn: "MaCTKM",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KMTheoSach_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TacGiaSach",
                columns: table => new
                {
                    MaSach = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IDTacGia = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TacGiaSach", x => new { x.MaSach, x.IDTacGia });
                    table.ForeignKey(
                        name: "FK_TacGiaSach_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TacGiaSach_TacGia_IDTacGia",
                        column: x => x.IDTacGia,
                        principalTable: "TacGia",
                        principalColumn: "IDTacGia",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KH",
                columns: table => new
                {
                    MaKH = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    IDTaiKhoan = table.Column<int>(type: "int", nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DiaChiGiaoHang = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KH", x => x.MaKH);
                    table.ForeignKey(
                        name: "FK_KH_TaiKhoan_IDTaiKhoan",
                        column: x => x.IDTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "IDTaiKhoan",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NV",
                columns: table => new
                {
                    MaNV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoLot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    NS = table.Column<DateOnly>(type: "date", nullable: true),
                    AnhChanDung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TrangThaiTaiKhoan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LuongCB = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IDTaiKhoan = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NV", x => x.MaNV);
                    table.ForeignKey(
                        name: "FK_NV_TaiKhoan_IDTaiKhoan",
                        column: x => x.IDTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "IDTaiKhoan",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GioHang",
                columns: table => new
                {
                    MaKH = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaSach = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GioHang", x => new { x.MaKH, x.MaSach });
                    table.ForeignKey(
                        name: "FK_GioHang_KH_MaKH",
                        column: x => x.MaKH,
                        principalTable: "KH",
                        principalColumn: "MaKH",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GioHang_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HD",
                columns: table => new
                {
                    MaHD = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayLapHD = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaNV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MaKH = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TrangThaiDonHang = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TrangThaiThanhToan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhuongThucThanhToan = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DiaChiGiaoHang = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SDTNhanHang = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaVoucher = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HD", x => x.MaHD);
                    table.ForeignKey(
                        name: "FK_HD_KH_MaKH",
                        column: x => x.MaKH,
                        principalTable: "KH",
                        principalColumn: "MaKH",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HD_NV_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NV",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HD_Voucher_MaVoucher",
                        column: x => x.MaVoucher,
                        principalTable: "Voucher",
                        principalColumn: "MaVoucher",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PNH",
                columns: table => new
                {
                    MaPNH = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayLap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaNV = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaNCC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PNH", x => x.MaPNH);
                    table.ForeignKey(
                        name: "FK_PNH_NCC_MaNCC",
                        column: x => x.MaNCC,
                        principalTable: "NCC",
                        principalColumn: "MaNCC",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PNH_NV_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NV",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CTHD",
                columns: table => new
                {
                    MaHD = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaSach = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SL = table.Column<int>(type: "int", nullable: false),
                    DG = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CTHD", x => new { x.MaHD, x.MaSach });
                    table.ForeignKey(
                        name: "FK_CTHD_HD_MaHD",
                        column: x => x.MaHD,
                        principalTable: "HD",
                        principalColumn: "MaHD",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CTHD_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CTPNH",
                columns: table => new
                {
                    MaPNH = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaSach = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SL = table.Column<int>(type: "int", nullable: false),
                    DG = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CTPNH", x => new { x.MaPNH, x.MaSach });
                    table.ForeignKey(
                        name: "FK_CTPNH_PNH_MaPNH",
                        column: x => x.MaPNH,
                        principalTable: "PNH",
                        principalColumn: "MaPNH",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CTPNH_Sach_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sach",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CTHD_MaSach",
                table: "CTHD",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_CTPNH_MaSach",
                table: "CTPNH",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_GioHang_MaSach",
                table: "GioHang",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_HD_MaKH",
                table: "HD",
                column: "MaKH");

            migrationBuilder.CreateIndex(
                name: "IX_HD_MaNV",
                table: "HD",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_HD_MaVoucher",
                table: "HD",
                column: "MaVoucher");

            migrationBuilder.CreateIndex(
                name: "IX_HD_NgayLapHD_TrangThaiDonHang",
                table: "HD",
                columns: new[] { "NgayLapHD", "TrangThaiDonHang" });

            migrationBuilder.CreateIndex(
                name: "IX_KH_IDTaiKhoan",
                table: "KH",
                column: "IDTaiKhoan",
                unique: true,
                filter: "[IDTaiKhoan] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KMTheoSach_MaSach",
                table: "KMTheoSach",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_KMTheoVoucher_MaVoucher",
                table: "KMTheoVoucher",
                column: "MaVoucher");

            migrationBuilder.CreateIndex(
                name: "IX_NV_IDTaiKhoan",
                table: "NV",
                column: "IDTaiKhoan",
                unique: true,
                filter: "[IDTaiKhoan] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PNH_MaNCC",
                table: "PNH",
                column: "MaNCC");

            migrationBuilder.CreateIndex(
                name: "IX_PNH_MaNV",
                table: "PNH",
                column: "MaNV");

            migrationBuilder.CreateIndex(
                name: "IX_Sach_IDNhaXuatBan",
                table: "Sach",
                column: "IDNhaXuatBan");

            migrationBuilder.CreateIndex(
                name: "IX_Sach_MaLoai",
                table: "Sach",
                column: "MaLoai");

            migrationBuilder.CreateIndex(
                name: "IX_Sach_TenSach",
                table: "Sach",
                column: "TenSach");

            migrationBuilder.CreateIndex(
                name: "IX_TacGiaSach_IDTacGia",
                table: "TacGiaSach",
                column: "IDTacGia");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_Email",
                table: "TaiKhoan",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_IDVaiTro",
                table: "TaiKhoan",
                column: "IDVaiTro");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_TenDangNhap",
                table: "TaiKhoan",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaiTro_TenVaiTro",
                table: "VaiTro",
                column: "TenVaiTro",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CTHD");

            migrationBuilder.DropTable(
                name: "CTPNH");

            migrationBuilder.DropTable(
                name: "GioHang");

            migrationBuilder.DropTable(
                name: "KMTheoHD");

            migrationBuilder.DropTable(
                name: "KMTheoSach");

            migrationBuilder.DropTable(
                name: "KMTheoVoucher");

            migrationBuilder.DropTable(
                name: "TacGiaSach");

            migrationBuilder.DropTable(
                name: "HD");

            migrationBuilder.DropTable(
                name: "PNH");

            migrationBuilder.DropTable(
                name: "CTKM");

            migrationBuilder.DropTable(
                name: "Sach");

            migrationBuilder.DropTable(
                name: "TacGia");

            migrationBuilder.DropTable(
                name: "KH");

            migrationBuilder.DropTable(
                name: "Voucher");

            migrationBuilder.DropTable(
                name: "NCC");

            migrationBuilder.DropTable(
                name: "NV");

            migrationBuilder.DropTable(
                name: "LoaiSach");

            migrationBuilder.DropTable(
                name: "NhaXuatBan");

            migrationBuilder.DropTable(
                name: "TaiKhoan");

            migrationBuilder.DropTable(
                name: "VaiTro");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GAC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyBrandTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TableBrandHeader_Ar",
                table: "WarrantyPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableBrandHeader_En",
                table: "WarrantyPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableExtRoadsideHeader_Ar",
                table: "WarrantyPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableExtRoadsideHeader_En",
                table: "WarrantyPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableExtWarrantyHeader_Ar",
                table: "WarrantyPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableExtWarrantyHeader_En",
                table: "WarrantyPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableMfrRoadsideHeader_Ar",
                table: "WarrantyPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableMfrRoadsideHeader_En",
                table: "WarrantyPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableMfrWarrantyHeader_Ar",
                table: "WarrantyPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableMfrWarrantyHeader_En",
                table: "WarrantyPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TablePolicyHeader_Ar",
                table: "WarrantyPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TablePolicyHeader_En",
                table: "WarrantyPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WarrantyBrandRows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarrantyPageId = table.Column<int>(type: "int", nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ManufacturerWarranty_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManufacturerWarranty_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManufacturerRoadside_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ManufacturerRoadside_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtendedWarranty_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtendedWarranty_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtendedRoadside_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtendedRoadside_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PolicyUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyBrandRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyBrandRows_WarrantyPages_WarrantyPageId",
                        column: x => x.WarrantyPageId,
                        principalTable: "WarrantyPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyBrandRows_WarrantyPageId",
                table: "WarrantyBrandRows",
                column: "WarrantyPageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarrantyBrandRows");

            migrationBuilder.DropColumn(
                name: "TableBrandHeader_Ar",
                table: "WarrantyPages");

            migrationBuilder.DropColumn(
                name: "TableBrandHeader_En",
                table: "WarrantyPages");

            migrationBuilder.DropColumn(
                name: "TableExtRoadsideHeader_Ar",
                table: "WarrantyPages");

            migrationBuilder.DropColumn(
                name: "TableExtRoadsideHeader_En",
                table: "WarrantyPages");

            migrationBuilder.DropColumn(
                name: "TableExtWarrantyHeader_Ar",
                table: "WarrantyPages");

            migrationBuilder.DropColumn(
                name: "TableExtWarrantyHeader_En",
                table: "WarrantyPages");

            migrationBuilder.DropColumn(
                name: "TableMfrRoadsideHeader_Ar",
                table: "WarrantyPages");

            migrationBuilder.DropColumn(
                name: "TableMfrRoadsideHeader_En",
                table: "WarrantyPages");

            migrationBuilder.DropColumn(
                name: "TableMfrWarrantyHeader_Ar",
                table: "WarrantyPages");

            migrationBuilder.DropColumn(
                name: "TableMfrWarrantyHeader_En",
                table: "WarrantyPages");

            migrationBuilder.DropColumn(
                name: "TablePolicyHeader_Ar",
                table: "WarrantyPages");

            migrationBuilder.DropColumn(
                name: "TablePolicyHeader_En",
                table: "WarrantyPages");
        }
    }
}

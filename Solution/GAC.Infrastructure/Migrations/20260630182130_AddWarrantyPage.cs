using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GAC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WarrantyBookletPdf",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WarrantyPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BannerImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BannerLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BannerLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Intro_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Intro_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermsImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TermsNote_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermsNote_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtendedHeading_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtendedHeading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtendedIntro_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtendedIntro_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtendedTableHtml_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtendedTableHtml_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyCallouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarrantyPageId = table.Column<int>(type: "int", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Text_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Text_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyCallouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyCallouts_WarrantyPages_WarrantyPageId",
                        column: x => x.WarrantyPageId,
                        principalTable: "WarrantyPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyCallouts_WarrantyPageId",
                table: "WarrantyCallouts",
                column: "WarrantyPageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarrantyCallouts");

            migrationBuilder.DropTable(
                name: "WarrantyPages");

            migrationBuilder.DropColumn(
                name: "WarrantyBookletPdf",
                table: "Vehicles");
        }
    }
}

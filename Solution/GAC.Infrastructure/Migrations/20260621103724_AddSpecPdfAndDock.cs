using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GAC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecPdfAndDock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpecPdf",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DockItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Label_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Label_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LinkType = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DockItems", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DockItems");

            migrationBuilder.DropColumn(
                name: "SpecPdf",
                table: "Vehicles");
        }
    }
}

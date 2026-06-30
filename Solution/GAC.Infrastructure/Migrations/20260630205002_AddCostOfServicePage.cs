using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GAC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCostOfServicePage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CostOfServicePages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ButtonLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ButtonLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ButtonUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TableHeadLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TableHeadLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FooterNote_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FooterNote_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostOfServicePages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CostServiceModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CostOfServicePageId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostServiceModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostServiceModels_CostOfServicePages_CostOfServicePageId",
                        column: x => x.CostOfServicePageId,
                        principalTable: "CostOfServicePages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostServiceRows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CostOfServicePageId = table.Column<int>(type: "int", nullable: false),
                    Label_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Label_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostServiceRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostServiceRows_CostOfServicePages_CostOfServicePageId",
                        column: x => x.CostOfServicePageId,
                        principalTable: "CostOfServicePages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostServiceCells",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CostServiceModelId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostServiceCells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostServiceCells_CostServiceModels_CostServiceModelId",
                        column: x => x.CostServiceModelId,
                        principalTable: "CostServiceModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostServiceCells_CostServiceModelId",
                table: "CostServiceCells",
                column: "CostServiceModelId");

            migrationBuilder.CreateIndex(
                name: "IX_CostServiceModels_CostOfServicePageId",
                table: "CostServiceModels",
                column: "CostOfServicePageId");

            migrationBuilder.CreateIndex(
                name: "IX_CostServiceRows_CostOfServicePageId",
                table: "CostServiceRows",
                column: "CostOfServicePageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostServiceCells");

            migrationBuilder.DropTable(
                name: "CostServiceRows");

            migrationBuilder.DropTable(
                name: "CostServiceModels");

            migrationBuilder.DropTable(
                name: "CostOfServicePages");
        }
    }
}

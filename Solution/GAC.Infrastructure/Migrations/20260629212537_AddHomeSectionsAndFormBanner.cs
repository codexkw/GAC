using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GAC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeSectionsAndFormBanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerImagePath",
                table: "FormPages",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DualCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HomePageId = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ButtonText_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ButtonText_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DualCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DualCards_HomePages_HomePageId",
                        column: x => x.HomePageId,
                        principalTable: "HomePages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromoSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HomePageId = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CtaText_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CtaText_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CtaLink = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromoSections_HomePages_HomePageId",
                        column: x => x.HomePageId,
                        principalTable: "HomePages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromoCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromoSectionId = table.Column<int>(type: "int", nullable: false),
                    Text_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Text_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromoCampaigns_PromoSections_PromoSectionId",
                        column: x => x.PromoSectionId,
                        principalTable: "PromoSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DualCards_HomePageId",
                table: "DualCards",
                column: "HomePageId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoCampaigns_PromoSectionId",
                table: "PromoCampaigns",
                column: "PromoSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromoSections_HomePageId",
                table: "PromoSections",
                column: "HomePageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DualCards");

            migrationBuilder.DropTable(
                name: "PromoCampaigns");

            migrationBuilder.DropTable(
                name: "PromoSections");

            migrationBuilder.DropColumn(
                name: "BannerImagePath",
                table: "FormPages");
        }
    }
}

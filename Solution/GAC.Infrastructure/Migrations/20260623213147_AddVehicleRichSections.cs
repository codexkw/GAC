using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GAC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleRichSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnquiryBgImage",
                table: "Vehicles",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnquiryLead_Ar",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnquiryLead_En",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnquirySub_Ar",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnquirySub_En",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnquiryTitle_Ar",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnquiryTitle_En",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatsNote_Ar",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatsNote_En",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechBannerImage",
                table: "Vehicles",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Trims",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelLabel_Ar",
                table: "Trims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelLabel_En",
                table: "Trims",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroupKey",
                table: "FeatureSections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Lead_Ar",
                table: "FeatureSections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Lead_En",
                table: "FeatureSections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TabLabel_Ar",
                table: "FeatureSections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TabLabel_En",
                table: "FeatureSections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CardItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    Title_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Text_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Text_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardItems_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureBullets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureSectionId = table.Column<int>(type: "int", nullable: false),
                    Label_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Label_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Text_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Text_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureBullets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureBullets_FeatureSections_FeatureSectionId",
                        column: x => x.FeatureSectionId,
                        principalTable: "FeatureSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GalleryTabs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    Label_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Label_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalleryTabs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GalleryTabs_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QualityBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    MainImage = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ThumbImage = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Strapline_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Strapline_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityBlocks_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SafetyToggles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    Title_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Strap_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Strap_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafetyToggles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafetyToggles_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectionHeadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<int>(type: "int", nullable: false),
                    Title_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sub_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sub_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Body_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Body_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionHeadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectionHeadings_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SliderGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SliderGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SliderGroups_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    Label_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Label_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatItems_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrimPriceRows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrimId = table.Column<int>(type: "int", nullable: false),
                    Text_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Text_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrimPriceRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrimPriceRows_Trims_TrimId",
                        column: x => x.TrimId,
                        principalTable: "Trims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarrantyLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    Label_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Label_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarrantyLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarrantyLinks_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GalleryImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GalleryTabId = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Alt_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Alt_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalleryImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GalleryImages_GalleryTabs_GalleryTabId",
                        column: x => x.GalleryTabId,
                        principalTable: "GalleryTabs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SliderSlides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SliderGroupId = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Alt_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Alt_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SliderSlides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SliderSlides_SliderGroups_SliderGroupId",
                        column: x => x.SliderGroupId,
                        principalTable: "SliderGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardItems_VehicleId",
                table: "CardItems",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureBullets_FeatureSectionId",
                table: "FeatureBullets",
                column: "FeatureSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryImages_GalleryTabId",
                table: "GalleryImages",
                column: "GalleryTabId");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryTabs_VehicleId",
                table: "GalleryTabs",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityBlocks_VehicleId",
                table: "QualityBlocks",
                column: "VehicleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SafetyToggles_VehicleId",
                table: "SafetyToggles",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_SectionHeadings_VehicleId",
                table: "SectionHeadings",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_SliderGroups_VehicleId",
                table: "SliderGroups",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_SliderSlides_SliderGroupId",
                table: "SliderSlides",
                column: "SliderGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StatItems_VehicleId",
                table: "StatItems",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_TrimPriceRows_TrimId",
                table: "TrimPriceRows",
                column: "TrimId");

            migrationBuilder.CreateIndex(
                name: "IX_WarrantyLinks_VehicleId",
                table: "WarrantyLinks",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardItems");

            migrationBuilder.DropTable(
                name: "FeatureBullets");

            migrationBuilder.DropTable(
                name: "GalleryImages");

            migrationBuilder.DropTable(
                name: "QualityBlocks");

            migrationBuilder.DropTable(
                name: "SafetyToggles");

            migrationBuilder.DropTable(
                name: "SectionHeadings");

            migrationBuilder.DropTable(
                name: "SliderSlides");

            migrationBuilder.DropTable(
                name: "StatItems");

            migrationBuilder.DropTable(
                name: "TrimPriceRows");

            migrationBuilder.DropTable(
                name: "WarrantyLinks");

            migrationBuilder.DropTable(
                name: "GalleryTabs");

            migrationBuilder.DropTable(
                name: "SliderGroups");

            migrationBuilder.DropColumn(
                name: "EnquiryBgImage",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EnquiryLead_Ar",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EnquiryLead_En",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EnquirySub_Ar",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EnquirySub_En",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EnquiryTitle_Ar",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EnquiryTitle_En",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "StatsNote_Ar",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "StatsNote_En",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "TechBannerImage",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Trims");

            migrationBuilder.DropColumn(
                name: "ModelLabel_Ar",
                table: "Trims");

            migrationBuilder.DropColumn(
                name: "ModelLabel_En",
                table: "Trims");

            migrationBuilder.DropColumn(
                name: "GroupKey",
                table: "FeatureSections");

            migrationBuilder.DropColumn(
                name: "Lead_Ar",
                table: "FeatureSections");

            migrationBuilder.DropColumn(
                name: "Lead_En",
                table: "FeatureSections");

            migrationBuilder.DropColumn(
                name: "TabLabel_Ar",
                table: "FeatureSections");

            migrationBuilder.DropColumn(
                name: "TabLabel_En",
                table: "FeatureSections");
        }
    }
}

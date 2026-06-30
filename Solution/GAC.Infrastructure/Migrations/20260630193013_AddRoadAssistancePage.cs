using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GAC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadAssistancePage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoadAssistancePages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Intro_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Intro_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactLead_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactLead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactText_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactText_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CallButtonLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CallButtonLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadAssistancePages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoadAssistancePages");
        }
    }
}

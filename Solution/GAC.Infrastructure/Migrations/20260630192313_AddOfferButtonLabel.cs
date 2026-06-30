using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GAC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferButtonLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ButtonLabel_Ar",
                table: "Offers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ButtonLabel_En",
                table: "Offers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ButtonLabel_Ar",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "ButtonLabel_En",
                table: "Offers");
        }
    }
}

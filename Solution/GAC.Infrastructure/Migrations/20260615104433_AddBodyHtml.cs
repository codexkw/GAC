using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GAC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBodyHtml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodyHtml_Ar",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyHtml_En",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyHtml_Ar",
                table: "FormPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyHtml_En",
                table: "FormPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyHtml_Ar",
                table: "ContentPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BodyHtml_En",
                table: "ContentPages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyHtml_Ar",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "BodyHtml_En",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "BodyHtml_Ar",
                table: "FormPages");

            migrationBuilder.DropColumn(
                name: "BodyHtml_En",
                table: "FormPages");

            migrationBuilder.DropColumn(
                name: "BodyHtml_Ar",
                table: "ContentPages");

            migrationBuilder.DropColumn(
                name: "BodyHtml_En",
                table: "ContentPages");
        }
    }
}

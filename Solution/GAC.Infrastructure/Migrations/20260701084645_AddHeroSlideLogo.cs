using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GAC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHeroSlideLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoImagePath",
                table: "HeroSlides",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoImagePath",
                table: "HeroSlides");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommonMorphAPI.Migrations
{
    /// <inheritdoc />
    public partial class init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isDeleted",
                table: "WordClasses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isDeleted",
                table: "Slots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "Slots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isDeleted",
                table: "Lemmas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "Lemmas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isDeleted",
                table: "Dialects",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isDeleted",
                table: "WordClasses");

            migrationBuilder.DropColumn(
                name: "isDeleted",
                table: "Slots");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "Slots");

            migrationBuilder.DropColumn(
                name: "isDeleted",
                table: "Lemmas");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "Lemmas");

            migrationBuilder.DropColumn(
                name: "isDeleted",
                table: "Dialects");
        }
    }
}

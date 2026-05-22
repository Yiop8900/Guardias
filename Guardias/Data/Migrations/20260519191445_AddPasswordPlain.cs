using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guardias.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordPlain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "UsuariosEdificio",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<string>(
                name: "PasswordPlain",
                table: "UsuariosEdificio",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordPlain",
                table: "UsuariosEdificio");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "UsuariosEdificio",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}

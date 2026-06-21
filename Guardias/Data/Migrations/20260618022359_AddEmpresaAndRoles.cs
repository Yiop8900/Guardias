using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guardias.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpresaAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "UsuariosEdificio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rol",
                table: "UsuariosEdificio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Edificios",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmpresasAdministradoras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    LimiteAdmins = table.Column<int>(type: "int", nullable: false),
                    LimiteEdificios = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpresasAdministradoras", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosEdificio_EmpresaId",
                table: "UsuariosEdificio",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Edificios_EmpresaId",
                table: "Edificios",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Edificios_EmpresasAdministradoras_EmpresaId",
                table: "Edificios",
                column: "EmpresaId",
                principalTable: "EmpresasAdministradoras",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UsuariosEdificio_EmpresasAdministradoras_EmpresaId",
                table: "UsuariosEdificio",
                column: "EmpresaId",
                principalTable: "EmpresasAdministradoras",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Edificios_EmpresasAdministradoras_EmpresaId",
                table: "Edificios");

            migrationBuilder.DropForeignKey(
                name: "FK_UsuariosEdificio_EmpresasAdministradoras_EmpresaId",
                table: "UsuariosEdificio");

            migrationBuilder.DropTable(
                name: "EmpresasAdministradoras");

            migrationBuilder.DropIndex(
                name: "IX_UsuariosEdificio_EmpresaId",
                table: "UsuariosEdificio");

            migrationBuilder.DropIndex(
                name: "IX_Edificios_EmpresaId",
                table: "Edificios");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "UsuariosEdificio");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "UsuariosEdificio");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Edificios");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guardias.Data.Migrations
{
    /// <inheritdoc />
    public partial class IncidenciaAreaRonda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AreaRondaId",
                table: "Incidencias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incidencias_AreaRondaId",
                table: "Incidencias",
                column: "AreaRondaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Incidencias_AreaRondas_AreaRondaId",
                table: "Incidencias",
                column: "AreaRondaId",
                principalTable: "AreaRondas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidencias_AreaRondas_AreaRondaId",
                table: "Incidencias");

            migrationBuilder.DropIndex(
                name: "IX_Incidencias_AreaRondaId",
                table: "Incidencias");

            migrationBuilder.DropColumn(
                name: "AreaRondaId",
                table: "Incidencias");
        }
    }
}

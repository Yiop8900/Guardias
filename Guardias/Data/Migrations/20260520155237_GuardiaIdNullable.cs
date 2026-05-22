using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guardias.Data.Migrations
{
    /// <inheritdoc />
    public partial class GuardiaIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rondas_Guardias_GuardiaId",
                table: "Rondas");

            migrationBuilder.AlterColumn<int>(
                name: "GuardiaId",
                table: "Rondas",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Rondas_Guardias_GuardiaId",
                table: "Rondas",
                column: "GuardiaId",
                principalTable: "Guardias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rondas_Guardias_GuardiaId",
                table: "Rondas");

            migrationBuilder.AlterColumn<int>(
                name: "GuardiaId",
                table: "Rondas",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Rondas_Guardias_GuardiaId",
                table: "Rondas",
                column: "GuardiaId",
                principalTable: "Guardias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

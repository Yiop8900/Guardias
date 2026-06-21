using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guardias.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTareaArchivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TareaArchivos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TareaId = table.Column<int>(type: "int", nullable: false),
                    NombreOriginal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoMime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EsImagen = table.Column<bool>(type: "bit", nullable: false),
                    SubidoPor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TareaArchivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TareaArchivos_Tareas_TareaId",
                        column: x => x.TareaId,
                        principalTable: "Tareas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TareaArchivos_TareaId",
                table: "TareaArchivos",
                column: "TareaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TareaArchivos");
        }
    }
}

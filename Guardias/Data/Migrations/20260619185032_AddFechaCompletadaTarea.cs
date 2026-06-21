using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Guardias.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFechaCompletadaTarea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCompletada",
                table: "Tareas",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaCompletada",
                table: "Tareas");
        }
    }
}

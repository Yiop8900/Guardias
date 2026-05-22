using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Guardias.Data.Migrations
{
    /// <inheritdoc />
    public partial class CleanStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Incidencias",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Incidencias",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Incidencias",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Rondas",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Rondas",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Tareas",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Tareas",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Tareas",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Tareas",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Guardias",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Rondas",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Rondas",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Rondas",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Edificios",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Guardias",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Guardias",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Guardias",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Edificios",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Edificios",
                keyColumn: "Id",
                keyValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Edificios",
                columns: new[] { "Id", "Activo", "Descripcion", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "Torre principal - lobby y accesos", "Torre A" },
                    { 2, true, "Torre secundaria - oficinas", "Torre B" },
                    { 3, true, "Planta subterranea y accesos vehiculares", "Estacionamiento" }
                });

            migrationBuilder.InsertData(
                table: "Guardias",
                columns: new[] { "Id", "Activo", "Cargo", "EdificioId", "Nombre", "Turno" },
                values: new object[,]
                {
                    { 1, true, "Guardia", 1, "Carlos Ramirez", 0 },
                    { 2, true, "Supervisora", 1, "Ana Martinez", 1 },
                    { 3, true, "Guardia", 2, "Luis Gonzalez", 2 },
                    { 4, true, "Guardia", 3, "Sofia Herrera", 0 }
                });

            migrationBuilder.InsertData(
                table: "Tareas",
                columns: new[] { "Id", "Descripcion", "EdificioId", "Estado", "FechaCreacion", "GuardiaId", "HoraProgramada", "Titulo", "Turno" },
                values: new object[,]
                {
                    { 1, "Revisar estado y vencimiento de extintores.", 1, 0, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new TimeOnly(8, 0, 0), "Verificar extintores", 0 },
                    { 2, "Verificar barreras y accesos del estacionamiento.", 3, 0, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new TimeOnly(7, 30, 0), "Ronda estacionamiento", 3 },
                    { 3, "Comprobar que las camaras esten operativas.", 2, 0, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new TimeOnly(14, 0, 0), "Revisar camaras", 1 }
                });

            migrationBuilder.InsertData(
                table: "Rondas",
                columns: new[] { "Id", "EdificioId", "Estado", "FechaFirma", "FechaHora", "FirmadoPor", "GuardiaId", "ReporteIncidencias" },
                values: new object[,]
                {
                    { 1, 1, 2, new DateTime(2026, 5, 7, 17, 30, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 5, 7, 8, 15, 0, 0, DateTimeKind.Unspecified), "Ana Martinez", 1, null },
                    { 2, 2, 1, null, new DateTime(2026, 5, 7, 14, 10, 0, 0, DateTimeKind.Unspecified), null, 2, null },
                    { 3, 2, 1, null, new DateTime(2026, 5, 7, 22, 5, 0, 0, DateTimeKind.Unspecified), null, 3, null },
                    { 4, 3, 0, null, new DateTime(2026, 5, 8, 8, 0, 0, 0, DateTimeKind.Unspecified), null, 4, null },
                    { 5, 1, 1, null, new DateTime(2026, 5, 8, 8, 20, 0, 0, DateTimeKind.Unspecified), null, 1, null }
                });

            migrationBuilder.InsertData(
                table: "Tareas",
                columns: new[] { "Id", "Descripcion", "EdificioId", "Estado", "FechaCreacion", "GuardiaId", "HoraProgramada", "Titulo", "Turno" },
                values: new object[] { 4, "Registrar entradas y salidas entre 22:00 y 06:00.", 1, 0, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, new TimeOnly(22, 0, 0), "Control acceso nocturno", 2 });

            migrationBuilder.InsertData(
                table: "Incidencias",
                columns: new[] { "Id", "Descripcion", "Estado", "FechaCierre", "FechaCreacion", "NotasCierre", "RondaId", "Severidad" },
                values: new object[,]
                {
                    { 1, "Puerta de emergencia piso 3 no cierra.", 1, null, new DateTime(2026, 5, 7, 14, 25, 0, 0, DateTimeKind.Unspecified), null, 2, 1 },
                    { 2, "Camara del pasillo B sin senal.", 0, null, new DateTime(2026, 5, 7, 22, 30, 0, 0, DateTimeKind.Unspecified), null, 3, 2 },
                    { 3, "Extintor vencido piso 2 Torre A.", 2, new DateTime(2026, 5, 7, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 5, 7, 9, 0, 0, 0, DateTimeKind.Unspecified), "Extintor reemplazado.", 1, 0 }
                });
        }
    }
}

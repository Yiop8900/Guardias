using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Guardias.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Edificios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Edificios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Areas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EdificioId = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Areas_Edificios_EdificioId",
                        column: x => x.EdificioId,
                        principalTable: "Edificios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Guardias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Turno = table.Column<int>(type: "int", nullable: false),
                    EdificioId = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guardias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guardias_Edificios_EdificioId",
                        column: x => x.EdificioId,
                        principalTable: "Edificios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosEdificio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreUsuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EdificioId = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosEdificio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuariosEdificio_Edificios_EdificioId",
                        column: x => x.EdificioId,
                        principalTable: "Edificios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rondas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GuardiaId = table.Column<int>(type: "int", nullable: false),
                    EdificioId = table.Column<int>(type: "int", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    ReporteIncidencias = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FirmadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaFirma = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rondas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rondas_Edificios_EdificioId",
                        column: x => x.EdificioId,
                        principalTable: "Edificios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rondas_Guardias_GuardiaId",
                        column: x => x.GuardiaId,
                        principalTable: "Guardias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tareas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HoraProgramada = table.Column<TimeOnly>(type: "time", nullable: true),
                    Turno = table.Column<int>(type: "int", nullable: false),
                    GuardiaId = table.Column<int>(type: "int", nullable: true),
                    EdificioId = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tareas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tareas_Edificios_EdificioId",
                        column: x => x.EdificioId,
                        principalTable: "Edificios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tareas_Guardias_GuardiaId",
                        column: x => x.GuardiaId,
                        principalTable: "Guardias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AreaRondas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RondaId = table.Column<int>(type: "int", nullable: false),
                    AreaId = table.Column<int>(type: "int", nullable: false),
                    Completada = table.Column<bool>(type: "bit", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCompletada = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaRondas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AreaRondas_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AreaRondas_Rondas_RondaId",
                        column: x => x.RondaId,
                        principalTable: "Rondas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Incidencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RondaId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Severidad = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotasCierre = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidencias_Rondas_RondaId",
                        column: x => x.RondaId,
                        principalTable: "Rondas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FotosRonda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AreaRondaId = table.Column<int>(type: "int", nullable: false),
                    RutaFoto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCaptura = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotosRonda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FotosRonda_AreaRondas_AreaRondaId",
                        column: x => x.AreaRondaId,
                        principalTable: "AreaRondas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_AreaRondas_AreaId",
                table: "AreaRondas",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaRondas_RondaId",
                table: "AreaRondas",
                column: "RondaId");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_EdificioId",
                table: "Areas",
                column: "EdificioId");

            migrationBuilder.CreateIndex(
                name: "IX_FotosRonda_AreaRondaId",
                table: "FotosRonda",
                column: "AreaRondaId");

            migrationBuilder.CreateIndex(
                name: "IX_Guardias_EdificioId",
                table: "Guardias",
                column: "EdificioId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidencias_RondaId",
                table: "Incidencias",
                column: "RondaId");

            migrationBuilder.CreateIndex(
                name: "IX_Rondas_EdificioId",
                table: "Rondas",
                column: "EdificioId");

            migrationBuilder.CreateIndex(
                name: "IX_Rondas_GuardiaId",
                table: "Rondas",
                column: "GuardiaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_EdificioId",
                table: "Tareas",
                column: "EdificioId");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_GuardiaId",
                table: "Tareas",
                column: "GuardiaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosEdificio_EdificioId",
                table: "UsuariosEdificio",
                column: "EdificioId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosEdificio_NombreUsuario",
                table: "UsuariosEdificio",
                column: "NombreUsuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FotosRonda");

            migrationBuilder.DropTable(
                name: "Incidencias");

            migrationBuilder.DropTable(
                name: "Tareas");

            migrationBuilder.DropTable(
                name: "UsuariosEdificio");

            migrationBuilder.DropTable(
                name: "AreaRondas");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "Rondas");

            migrationBuilder.DropTable(
                name: "Guardias");

            migrationBuilder.DropTable(
                name: "Edificios");
        }
    }
}

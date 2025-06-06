using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace app1.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAsientosDisponiblesFromViajeFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AsientosDisponibles",
                table: "Viajes");

            migrationBuilder.CreateTable(
                name: "FechasSalidaViaje",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ViajeId = table.Column<int>(type: "integer", nullable: false),
                    FechaSalida = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AsientosDisponibles = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FechasSalidaViaje", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FechasSalidaViaje_Viajes_ViajeId",
                        column: x => x.ViajeId,
                        principalTable: "Viajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FechasSalidaViaje_ViajeId",
                table: "FechasSalidaViaje",
                column: "ViajeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FechasSalidaViaje");

            migrationBuilder.AddColumn<int>(
                name: "AsientosDisponibles",
                table: "Viajes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}

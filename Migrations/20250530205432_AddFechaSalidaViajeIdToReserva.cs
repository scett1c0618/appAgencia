using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace app1.Migrations
{
    /// <inheritdoc />
    public partial class AddFechaSalidaViajeIdToReserva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FechaSalidaViajeId",
                table: "Reservas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_FechaSalidaViajeId",
                table: "Reservas",
                column: "FechaSalidaViajeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_FechasSalidaViaje_FechaSalidaViajeId",
                table: "Reservas",
                column: "FechaSalidaViajeId",
                principalTable: "FechasSalidaViaje",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_FechasSalidaViaje_FechaSalidaViajeId",
                table: "Reservas");

            migrationBuilder.DropIndex(
                name: "IX_Reservas_FechaSalidaViajeId",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "FechaSalidaViajeId",
                table: "Reservas");
        }
    }
}

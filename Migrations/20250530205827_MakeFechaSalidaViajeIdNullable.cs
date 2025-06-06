using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace app1.Migrations
{
    /// <inheritdoc />
    public partial class MakeFechaSalidaViajeIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_FechasSalidaViaje_FechaSalidaViajeId",
                table: "Reservas");

            migrationBuilder.AlterColumn<int>(
                name: "FechaSalidaViajeId",
                table: "Reservas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_FechasSalidaViaje_FechaSalidaViajeId",
                table: "Reservas",
                column: "FechaSalidaViajeId",
                principalTable: "FechasSalidaViaje",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_FechasSalidaViaje_FechaSalidaViajeId",
                table: "Reservas");

            migrationBuilder.AlterColumn<int>(
                name: "FechaSalidaViajeId",
                table: "Reservas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_FechasSalidaViaje_FechaSalidaViajeId",
                table: "Reservas",
                column: "FechaSalidaViajeId",
                principalTable: "FechasSalidaViaje",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

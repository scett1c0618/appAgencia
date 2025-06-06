using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace app1.Migrations
{
    /// <inheritdoc />
    public partial class AddViajeAndUserToContacto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Contacto",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViajeId",
                table: "Contacto",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contacto_ViajeId",
                table: "Contacto",
                column: "ViajeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contacto_Viajes_ViajeId",
                table: "Contacto",
                column: "ViajeId",
                principalTable: "Viajes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contacto_Viajes_ViajeId",
                table: "Contacto");

            migrationBuilder.DropIndex(
                name: "IX_Contacto_ViajeId",
                table: "Contacto");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Contacto");

            migrationBuilder.DropColumn(
                name: "ViajeId",
                table: "Contacto");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Galeria.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SembrarParametrosNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Parametros",
                columns: new[] { "Clave", "Valor" },
                values: new object[,]
                {
                    { "IvaPorcentaje", "22" },
                    { "RedondeoDolar", "1" },
                    { "RedondeosPesos", "10" },
                    { "UtilidadDefault", "50" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Parametros",
                keyColumn: "Clave",
                keyValue: "IvaPorcentaje");

            migrationBuilder.DeleteData(
                table: "Parametros",
                keyColumn: "Clave",
                keyValue: "RedondeoDolar");

            migrationBuilder.DeleteData(
                table: "Parametros",
                keyColumn: "Clave",
                keyValue: "RedondeosPesos");

            migrationBuilder.DeleteData(
                table: "Parametros",
                keyColumn: "Clave",
                keyValue: "UtilidadDefault");
        }
    }
}

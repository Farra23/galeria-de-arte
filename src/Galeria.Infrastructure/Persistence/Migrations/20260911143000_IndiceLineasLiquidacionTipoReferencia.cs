using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galeria.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndiceLineasLiquidacionTipoReferencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LineasLiquidacion_Tipo_ReferenciaId",
                table: "LineasLiquidacion",
                columns: new[] { "Tipo", "ReferenciaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LineasLiquidacion_Tipo_ReferenciaId",
                table: "LineasLiquidacion");
        }
    }
}

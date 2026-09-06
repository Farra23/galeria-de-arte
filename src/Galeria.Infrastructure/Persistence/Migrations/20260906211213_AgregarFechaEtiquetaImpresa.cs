using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galeria.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFechaEtiquetaImpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaEtiquetaImpresa",
                table: "Obras",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaEtiquetaImpresa",
                table: "Obras");
        }
    }
}

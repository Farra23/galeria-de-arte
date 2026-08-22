using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galeria.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAgendaPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgendasPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArtistaId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaPactada = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    FechaConfirmada = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Comentarios = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendasPago", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgendasPago_Artistas_ArtistaId",
                        column: x => x.ArtistaId,
                        principalTable: "Artistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendasPago_ArtistaId",
                table: "AgendasPago",
                column: "ArtistaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendasPago");
        }
    }
}

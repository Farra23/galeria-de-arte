using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Galeria.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InicialGaleria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Artistas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<int>(type: "INTEGER", nullable: false),
                    Apellido = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Perfil = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Taller = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Celular = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    TelFijo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Correo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artistas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Auditorias",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    NombreUsuario = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Pantalla = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TipoOperacion = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Tabla = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Columna = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ValorAnterior = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ValorNuevo = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ArtistaId = table.Column<int>(type: "INTEGER", nullable: true),
                    ObraId = table.Column<int>(type: "INTEGER", nullable: true),
                    EntidadId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parametros",
                columns: table => new
                {
                    Clave = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Valor = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parametros", x => x.Clave);
                });

            migrationBuilder.CreateTable(
                name: "Rubros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rubros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tecnicas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tecnicas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Liquidaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArtistaId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroCorrelativo = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PeriodoAnio = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodoMes = table.Column<int>(type: "INTEGER", nullable: false),
                    Moneda = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalBruto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalAdelantos = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalDevoluciones = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalNeto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PdfPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Liquidaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Liquidaciones_Artistas_ArtistaId",
                        column: x => x.ArtistaId,
                        principalTable: "Artistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArtistaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Series_Artistas_ArtistaId",
                        column: x => x.ArtistaId,
                        principalTable: "Artistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Adelantos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArtistaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Importe = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Moneda = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaDescontado = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LiquidacionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adelantos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Adelantos_Artistas_ArtistaId",
                        column: x => x.ArtistaId,
                        principalTable: "Artistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Adelantos_Liquidaciones_LiquidacionId",
                        column: x => x.LiquidacionId,
                        principalTable: "Liquidaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Obras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ArtistaId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroObra = table.Column<int>(type: "INTEGER", nullable: false),
                    SerieId = table.Column<int>(type: "INTEGER", nullable: true),
                    RubroId = table.Column<int>(type: "INTEGER", nullable: true),
                    TecnicaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Moneda = table.Column<int>(type: "INTEGER", nullable: false),
                    Costo = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Utilidad = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TieneIVA = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Existencia = table.Column<int>(type: "INTEGER", nullable: false),
                    PagoContado = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaIngreso = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    AltoCm = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    AnchoCm = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    LargoCm = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ImagenPrincipalPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Obras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Obras_Artistas_ArtistaId",
                        column: x => x.ArtistaId,
                        principalTable: "Artistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Obras_Rubros_RubroId",
                        column: x => x.RubroId,
                        principalTable: "Rubros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Obras_Series_SerieId",
                        column: x => x.SerieId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Obras_Tecnicas_TecnicaId",
                        column: x => x.TecnicaId,
                        principalTable: "Tecnicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Alquileres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObraId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    FechaRecupero = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    FechaDevolucion = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Moneda = table.Column<int>(type: "INTEGER", nullable: false),
                    PorcentajeAlquiler = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MontoAlquiler = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MontoArtista = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MontoGaleria = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Cliente = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alquileres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alquileres_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LineasLiquidacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LiquidacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    ObraId = table.Column<int>(type: "INTEGER", nullable: true),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CodigoObra = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NombreObra = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    MontoUnitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MontoTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReferenciaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasLiquidacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasLiquidacion_Liquidaciones_LiquidacionId",
                        column: x => x.LiquidacionId,
                        principalTable: "Liquidaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LineasLiquidacion_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Movimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObraId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Moneda = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferenciaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movimientos_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Retiros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObraId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FechaEstimadaDevolucion = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    FechaDevolucion = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Retiros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Retiros_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObraId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Cantidad = table.Column<int>(type: "INTEGER", nullable: false),
                    Moneda = table.Column<int>(type: "INTEGER", nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PrecioCalculado = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ExentaIVA = table.Column<bool>(type: "INTEGER", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ventas_Obras_ObraId",
                        column: x => x.ObraId,
                        principalTable: "Obras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Certificados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NumeroCertificado = table.Column<int>(type: "INTEGER", nullable: false),
                    VentaId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaEmision = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EmailDestinatario = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PdfPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificados_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Devoluciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VentaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ArtistaYaCobro = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devoluciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devoluciones_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adelantos_ArtistaId",
                table: "Adelantos",
                column: "ArtistaId");

            migrationBuilder.CreateIndex(
                name: "IX_Adelantos_LiquidacionId",
                table: "Adelantos",
                column: "LiquidacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Alquileres_ObraId",
                table: "Alquileres",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Artistas_Codigo",
                table: "Artistas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_ArtistaId",
                table: "Auditorias",
                column: "ArtistaId");

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_ObraId",
                table: "Auditorias",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Tabla_EntidadId",
                table: "Auditorias",
                columns: new[] { "Tabla", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_Timestamp",
                table: "Auditorias",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Certificados_VentaId",
                table: "Certificados",
                column: "VentaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_VentaId",
                table: "Devoluciones",
                column: "VentaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineasLiquidacion_LiquidacionId",
                table: "LineasLiquidacion",
                column: "LiquidacionId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasLiquidacion_ObraId",
                table: "LineasLiquidacion",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Liquidaciones_ArtistaId",
                table: "Liquidaciones",
                column: "ArtistaId");

            migrationBuilder.CreateIndex(
                name: "IX_Liquidaciones_PeriodoAnio_PeriodoMes",
                table: "Liquidaciones",
                columns: new[] { "PeriodoAnio", "PeriodoMes" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_ObraId_Fecha",
                table: "Movimientos",
                columns: new[] { "ObraId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Obras_ArtistaId_NumeroObra",
                table: "Obras",
                columns: new[] { "ArtistaId", "NumeroObra" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Obras_ArtistaId_Titulo",
                table: "Obras",
                columns: new[] { "ArtistaId", "Titulo" });

            migrationBuilder.CreateIndex(
                name: "IX_Obras_RubroId",
                table: "Obras",
                column: "RubroId");

            migrationBuilder.CreateIndex(
                name: "IX_Obras_SerieId",
                table: "Obras",
                column: "SerieId");

            migrationBuilder.CreateIndex(
                name: "IX_Obras_TecnicaId",
                table: "Obras",
                column: "TecnicaId");

            migrationBuilder.CreateIndex(
                name: "IX_Retiros_ObraId",
                table: "Retiros",
                column: "ObraId");

            migrationBuilder.CreateIndex(
                name: "IX_Rubros_Nombre",
                table: "Rubros",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Series_ArtistaId",
                table: "Series",
                column: "ArtistaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tecnicas_Nombre",
                table: "Tecnicas",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Fecha",
                table: "Ventas",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_ObraId",
                table: "Ventas",
                column: "ObraId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adelantos");

            migrationBuilder.DropTable(
                name: "Alquileres");

            migrationBuilder.DropTable(
                name: "Auditorias");

            migrationBuilder.DropTable(
                name: "Certificados");

            migrationBuilder.DropTable(
                name: "Devoluciones");

            migrationBuilder.DropTable(
                name: "LineasLiquidacion");

            migrationBuilder.DropTable(
                name: "Movimientos");

            migrationBuilder.DropTable(
                name: "Parametros");

            migrationBuilder.DropTable(
                name: "Retiros");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropTable(
                name: "Liquidaciones");

            migrationBuilder.DropTable(
                name: "Obras");

            migrationBuilder.DropTable(
                name: "Rubros");

            migrationBuilder.DropTable(
                name: "Series");

            migrationBuilder.DropTable(
                name: "Tecnicas");

            migrationBuilder.DropTable(
                name: "Artistas");
        }
    }
}

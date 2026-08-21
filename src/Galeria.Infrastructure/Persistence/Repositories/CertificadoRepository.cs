using Galeria.Application.Certificados;
using Galeria.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Galeria.Infrastructure.Persistence.Repositories;

public class CertificadoRepository(GaleriaDbContext db) : ICertificadoRepository
{
    public Task<Certificado?> ObtenerPorVentaAsync(int ventaId, CancellationToken ct = default) =>
        db.Certificados.FirstOrDefaultAsync(c => c.VentaId == ventaId, ct);

    public async Task<int> ProximoNumeroAsync(CancellationToken ct = default)
    {
        var maximo = await db.Certificados.MaxAsync(c => (int?)c.NumeroCertificado, ct);
        return (maximo ?? 0) + 1;
    }

    public Task AgregarAsync(Certificado certificado, CancellationToken ct = default)
    {
        db.Certificados.Add(certificado);
        return Task.CompletedTask;
    }

    public async Task<CertificadoDatos?> ObtenerDatosAsync(int certificadoId, CancellationToken ct = default) =>
        await db.Certificados.AsNoTracking()
            .Include(c => c.Venta).ThenInclude(v => v.Obra).ThenInclude(o => o.Artista)
            .Include(c => c.Venta).ThenInclude(v => v.Obra).ThenInclude(o => o.Rubro)
            .Include(c => c.Venta).ThenInclude(v => v.Obra).ThenInclude(o => o.Tecnica)
            .Where(c => c.Id == certificadoId)
            .Select(c => new CertificadoDatos(
                c.NumeroCertificado,
                c.FechaEmision,
                c.Venta.Obra.Artista.Codigo.ToString("D3") + c.Venta.Obra.NumeroObra.ToString("D3"),
                c.Venta.Obra.Titulo,
                c.Venta.Obra.Artista.Apellido + ", " + c.Venta.Obra.Artista.Nombre,
                c.Venta.Obra.Rubro != null ? c.Venta.Obra.Rubro.Nombre : null,
                c.Venta.Obra.Tecnica != null ? c.Venta.Obra.Tecnica.Nombre : null,
                c.Venta.Obra.AltoCm,
                c.Venta.Obra.AnchoCm,
                c.Venta.Obra.LargoCm,
                c.Venta.Obra.FechaIngreso.Year))
            .FirstOrDefaultAsync(ct);

    public Task GuardarCambiosAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

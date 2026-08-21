using Galeria.Application.Auditorias;
using Galeria.Domain.Entities;

namespace Galeria.Application.Certificados;

public class CertificadoService(ICertificadoRepository certificados, AuditoriaService auditoria)
{
    public Task<Certificado?> ObtenerPorVentaAsync(int ventaId, CancellationToken ct = default) =>
        certificados.ObtenerPorVentaAsync(ventaId, ct);

    public Task<CertificadoDatos?> ObtenerDatosAsync(int certificadoId, CancellationToken ct = default) =>
        certificados.ObtenerDatosAsync(certificadoId, ct);

    // Idempotente a propósito: "emitir" dos veces la misma venta devuelve el certificado que ya
    // existía en vez de duplicar numeración correlativa (requerimiento 5.3: "numeración correlativa").
    public async Task<int> EmitirAsync(int ventaId, CancellationToken ct = default)
    {
        var existente = await certificados.ObtenerPorVentaAsync(ventaId, ct);
        if (existente is not null)
        {
            return existente.Id;
        }

        var certificado = new Certificado
        {
            VentaId = ventaId,
            NumeroCertificado = await certificados.ProximoNumeroAsync(ct),
            FechaEmision = DateOnly.FromDateTime(DateTime.Now)
        };

        await certificados.AgregarAsync(certificado, ct);

        await auditoria.RegistrarAsync(new RegistrarAuditoriaRequest(
            Pantalla: "Ventas",
            TipoOperacion: "Alta",
            Tabla: "Certificado",
            EntidadId: ventaId.ToString()), ct);

        await certificados.GuardarCambiosAsync(ct);

        return certificado.Id;
    }
}

namespace Galeria.Application.Certificados;

public interface ICertificadoRepository
{
    Task<Domain.Entities.Certificado?> ObtenerPorVentaAsync(int ventaId, CancellationToken ct = default);

    Task<int> ProximoNumeroAsync(CancellationToken ct = default);

    Task AgregarAsync(Domain.Entities.Certificado certificado, CancellationToken ct = default);

    Task<CertificadoDatos?> ObtenerDatosAsync(int certificadoId, CancellationToken ct = default);

    Task GuardarCambiosAsync(CancellationToken ct = default);
}

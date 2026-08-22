namespace Galeria.Application.Agenda;

public record AgendaFilaItem(
    int ArtistaId,
    string ArtistaNombre,
    int PiezasDelMes,
    decimal PesosDelMes,
    decimal DolaresDelMes,
    decimal PesosMesesAnteriores,
    decimal DolaresMesesAnteriores,
    decimal TotalPesos,
    decimal TotalDolares,
    string? Comentarios,
    DateOnly? FechaPactada,
    DateOnly? FechaConfirmada,
    string? ArtistaCorreo,
    string? ArtistaCelular);

public record GuardarAgendaRequest(int ArtistaId, DateOnly? FechaPactada, DateOnly? FechaConfirmada, string? Comentarios);

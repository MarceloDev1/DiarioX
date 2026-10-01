namespace DiarioX.Server.Application.DTOs.Turmas;

/// <summary>Vagas da turma para uma enturmação iniciada em <see cref="Data"/>.</summary>
public sealed record VagasTurmaResponse(int TurmaId, DateOnly Data, int VagasOfertadas, int Ocupadas, int Disponiveis);

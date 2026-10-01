using System.Text;
using DiarioX.Server.Application.Transferencias;
using DiarioX.Server.Infrastructure.Documentos;

namespace DiarioX.Server.Tests.Infrastructure.Documentos;

public class DeclaracaoTransferenciaPdfTests
{
    [Fact]
    public void Gerar_ComOsDadosMinimos_ProduzPdf()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var dados = new DeclaracaoTransferenciaDados(
            Instituicao: null, EscolaNome: "EM Monteiro Lobato", EscolaInep: null, EscolaMunicipio: null,
            EscolaEndereco: null, EscolaTelefone: null, AlunoNome: "Carla Mendes", Matricula: "20260003",
            DataNascimento: new DateTime(2019, 5, 2), Responsavel: null, Turma: null, AnoReferencia: 2026,
            DataTransferencia: new DateOnly(2026, 9, 30), TipoDescricao: "Transferência para Outra Rede",
            EscolaDestino: "Colégio Y", Motivo: "Mudança de endereço", EmitidaEm: new DateTime(2026, 10, 1, 10, 0, 0));

        var pdf = new DeclaracaoTransferenciaPdf().Gerar(dados);

        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }
}

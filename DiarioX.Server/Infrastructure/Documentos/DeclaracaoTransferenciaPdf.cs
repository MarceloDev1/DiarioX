using DiarioX.Server.Application.Relatorios;
using DiarioX.Server.Application.Transferencias;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DiarioX.Server.Infrastructure.Documentos;

/// <summary>
/// Declaração de Transferência em A4: identificação da escola, texto da declaração, local e data,
/// e campos de assinatura da secretaria e da direção.
/// </summary>
public class DeclaracaoTransferenciaPdf : IDeclaracaoTransferenciaPdf
{
    private const string CorDestaque = "#6A1B9A";

    public byte[] Gerar(DeclaracaoTransferenciaDados dados)
    {
        return Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(2.5f, Unit.Centimetre);
            pagina.DefaultTextStyle(texto => texto.FontSize(11.5f));

            pagina.Header().Element(c => Cabecalho(c, dados));
            pagina.Content().PaddingTop(40).Column(conteudo =>
            {
                conteudo.Spacing(18);
                conteudo.Item().AlignCenter().Text("DECLARAÇÃO DE TRANSFERÊNCIA").Bold().FontSize(16).LetterSpacing(0.05f);
                conteudo.Item().PaddingTop(16).Element(c => Corpo(c, dados));
                conteudo.Item().PaddingTop(16).AlignRight().Text(LocalEData(dados));
                conteudo.Item().PaddingTop(70).Element(Assinaturas);
            });
            pagina.Footer().Text($"Emitida em {dados.EmitidaEm.ToString("dd/MM/yyyy 'às' HH:mm", FormatacaoRelatorio.PtBr)} · Diário X")
                .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
        })).GeneratePdf();
    }

    private static void Cabecalho(IContainer container, DeclaracaoTransferenciaDados dados)
    {
        container.Column(coluna =>
        {
            if (dados.Instituicao is not null)
                coluna.Item().Text(dados.Instituicao).SemiBold().FontSize(9.5f).FontColor(Colors.Grey.Darken2);
            coluna.Item().Text(dados.EscolaNome).Bold().FontSize(14).FontColor(CorDestaque);

            var identificacao = new[]
            {
                dados.EscolaInep is null ? null : $"INEP {dados.EscolaInep}",
                dados.EscolaMunicipio,
                dados.EscolaTelefone,
            }.OfType<string>().ToList();
            if (identificacao.Count > 0)
                coluna.Item().Text(string.Join("   ·   ", identificacao)).FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            if (dados.EscolaEndereco is not null)
                coluna.Item().Text(dados.EscolaEndereco).FontSize(8.5f).FontColor(Colors.Grey.Darken2);

            coluna.Item().PaddingTop(6).LineHorizontal(0.75f).LineColor(CorDestaque);
        });
    }

    private static void Corpo(IContainer container, DeclaracaoTransferenciaDados dados)
    {
        container.Column(coluna =>
        {
            coluna.Spacing(10);
            coluna.Item().Text(texto =>
            {
                texto.Justify();
                texto.ParagraphSpacing(6);
                texto.DefaultTextStyle(estilo => estilo.LineHeight(1.6f));

                texto.Span("Declaramos, para os devidos fins, que ");
                texto.Span(dados.AlunoNome.ToUpper(FormatacaoRelatorio.PtBr)).Bold();
                texto.Span($", matrícula nº {dados.Matricula}, nascido(a) em {dados.DataNascimento.ToString("dd/MM/yyyy", FormatacaoRelatorio.PtBr)}");
                if (dados.Responsavel is not null)
                    texto.Span($", sob a responsabilidade de {dados.Responsavel}");
                texto.Span($", esteve regularmente matriculado(a) nesta unidade escolar no ano letivo de {dados.AnoReferencia}");
                if (dados.Turma is not null)
                    texto.Span($", na turma {dados.Turma}");
                texto.Span(", e dela foi transferido(a) em ");
                texto.Span(dados.DataTransferencia.ToString("dd/MM/yyyy", FormatacaoRelatorio.PtBr)).Bold();
                texto.Span(", com destino a ");
                texto.Span(dados.EscolaDestino).Bold();
                texto.Span($" ({dados.TipoDescricao}).");
            });

            if (dados.Motivo is not null)
                coluna.Item().Text($"Motivo da saída: {dados.Motivo}").LineHeight(1.6f);

            coluna.Item().Text(texto =>
            {
                texto.Justify();
                texto.DefaultTextStyle(estilo => estilo.LineHeight(1.6f));
                texto.Span("As frequências e avaliações registradas até a data da transferência permanecem arquivadas " +
                           "nesta unidade escolar para a emissão do histórico escolar.");
            });
        });
    }

    private static string LocalEData(DeclaracaoTransferenciaDados dados)
    {
        var data = dados.EmitidaEm.ToString("d 'de' MMMM 'de' yyyy", FormatacaoRelatorio.PtBr);
        return dados.EscolaMunicipio is null ? $"{data}." : $"{dados.EscolaMunicipio}, {data}.";
    }

    private static void Assinaturas(IContainer container)
    {
        container.Row(linha =>
        {
            linha.Spacing(40);
            foreach (var cargo in new[] { "Secretário(a) Escolar", "Diretor(a)" })
            {
                linha.RelativeItem().Column(coluna =>
                {
                    coluna.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Darken2);
                    coluna.Item().PaddingTop(4).AlignCenter().Text(cargo).FontSize(10);
                });
            }
        });
    }
}

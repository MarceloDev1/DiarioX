using DiarioX.Server.Application.Relatorios;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DiarioX.Server.Infrastructure.Relatorios;

/// <summary>
/// PDF A4 para impressão: cabeçalho com instituição, título e filtros; resumo; tabela com o cabeçalho
/// repetido em cada página; rodapé com a data de emissão e a paginação.
/// </summary>
public class PdfExportador : IExportadorRelatorio
{
    private const string CorDestaque = "#6A1B9A";

    public string Formato => "pdf";
    public string ContentType => "application/pdf";

    public byte[] Exportar(RelatorioGerado relatorio)
    {
        return Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(relatorio.Paisagem ? PageSizes.A4.Landscape() : PageSizes.A4);
            pagina.Margin(1.5f, Unit.Centimetre);
            pagina.DefaultTextStyle(texto => texto.FontSize(9));

            pagina.Header().Element(c => Cabecalho(c, relatorio));
            pagina.Content().PaddingTop(10).Column(conteudo =>
            {
                conteudo.Spacing(10);
                if (relatorio.Indicadores.Count > 0)
                    conteudo.Item().Element(c => Indicadores(c, relatorio.Indicadores));

                if (relatorio.Linhas.Count == 0)
                    conteudo.Item().PaddingTop(20).AlignCenter().Text("Nenhum registro encontrado para os filtros informados.").Italic();
                else
                    conteudo.Item().Element(c => Tabela(c, relatorio));
            });
            pagina.Footer().Row(rodape =>
            {
                rodape.RelativeItem().Text($"Emitido em {relatorio.GeradoEm.ToString("dd/MM/yyyy 'às' HH:mm", FormatacaoRelatorio.PtBr)} · Diário X")
                    .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                rodape.RelativeItem().AlignRight().Text(texto =>
                {
                    texto.DefaultTextStyle(estilo => estilo.FontSize(7.5f).FontColor(Colors.Grey.Darken1));
                    texto.Span("Página ");
                    texto.CurrentPageNumber();
                    texto.Span(" de ");
                    texto.TotalPages();
                });
            });
        })).GeneratePdf();
    }

    private static void Cabecalho(IContainer container, RelatorioGerado relatorio)
    {
        container.Column(coluna =>
        {
            if (!string.IsNullOrEmpty(relatorio.Instituicao))
                coluna.Item().Text(relatorio.Instituicao).SemiBold().FontSize(9.5f).FontColor(Colors.Grey.Darken2);
            coluna.Item().Text(relatorio.Titulo).Bold().FontSize(15).FontColor(CorDestaque);
            if (relatorio.Filtros.Count > 0)
            {
                coluna.Item().PaddingTop(2).Text(string.Join("   ·   ", relatorio.Filtros.Select(f => $"{f.Rotulo}: {f.Valor}")))
                    .FontSize(8.5f).FontColor(Colors.Grey.Darken2);
            }
            coluna.Item().PaddingTop(6).LineHorizontal(0.75f).LineColor(CorDestaque);
        });
    }

    private static void Indicadores(IContainer container, IReadOnlyList<IndicadorRelatorio> indicadores)
    {
        container.Row(linha =>
        {
            linha.Spacing(6);
            foreach (var indicador in indicadores)
            {
                linha.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(5).Column(c =>
                {
                    c.Item().Text(indicador.Rotulo).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    c.Item().Text(indicador.Valor).Bold().FontSize(11);
                });
            }
        });
    }

    private static void Tabela(IContainer container, RelatorioGerado relatorio)
    {
        container.Table(tabela =>
        {
            tabela.ColumnsDefinition(colunas =>
            {
                // Números e datas têm largura fixa (cabem o título e o valor sem quebrar a linha);
                // o texto divide o espaço que sobra.
                foreach (var coluna in relatorio.Colunas)
                {
                    if (coluna.Tipo == TiposColuna.Texto)
                        colunas.RelativeColumn();
                    else
                        colunas.ConstantColumn(LarguraFixa(coluna));
                }
            });

            tabela.Header(cabecalho =>
            {
                foreach (var coluna in relatorio.Colunas)
                {
                    var celula = cabecalho.Cell().Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3);
                    (Numerica(coluna) ? celula.AlignRight() : celula).Text(coluna.Titulo).SemiBold();
                }
            });

            for (var i = 0; i < relatorio.Linhas.Count; i++)
            {
                var fundo = i % 2 == 1 ? Colors.Grey.Lighten5 : Colors.White;
                for (var c = 0; c < relatorio.Colunas.Count; c++)
                {
                    var coluna = relatorio.Colunas[c];
                    var celula = tabela.Cell().Background(fundo).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(3).PaddingHorizontal(3);
                    (Numerica(coluna) ? celula.AlignRight() : celula)
                        .Text(FormatacaoRelatorio.Formatar(relatorio.Linhas[i][c], coluna.Tipo));
                }
            }
        });
    }

    // Aproximação para fonte 9pt semibold: ~5,6pt por caractere, mais o padding da célula.
    private static float LarguraFixa(ColunaRelatorio coluna)
    {
        var conteudo = coluna.Tipo switch
        {
            TiposColuna.Data => 10,       // dd/MM/yyyy
            TiposColuna.Percentual => 6,  // 100,0%
            _ => 5,
        };
        return Math.Max(coluna.Titulo.Length, conteudo) * 5.6f + 10;
    }

    private static bool Numerica(ColunaRelatorio coluna)
        => coluna.Tipo is TiposColuna.Inteiro or TiposColuna.Decimal or TiposColuna.Percentual;
}

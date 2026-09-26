using ClosedXML.Excel;
using DiarioX.Server.Application.Relatorios;

namespace DiarioX.Server.Infrastructure.Relatorios;

/// <summary>Planilha com cabeçalho do relatório, tabela com filtro automático e o resumo ao final.</summary>
public class ExcelExportador : IExportadorRelatorio
{
    private static readonly XLColor CorCabecalho = XLColor.FromHtml("#EDE7F6");

    public string Formato => "xlsx";
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public byte[] Exportar(RelatorioGerado relatorio)
    {
        using var workbook = new XLWorkbook();
        var planilha = workbook.Worksheets.Add(NomeDaPlanilha(relatorio.Titulo));

        var linha = 1;
        planilha.Cell(linha, 1).Value = relatorio.Titulo;
        planilha.Cell(linha++, 1).Style.Font.SetBold().Font.SetFontSize(14);
        if (!string.IsNullOrEmpty(relatorio.Instituicao))
            planilha.Cell(linha++, 1).Value = relatorio.Instituicao;
        foreach (var filtro in relatorio.Filtros)
            planilha.Cell(linha++, 1).Value = $"{filtro.Rotulo}: {filtro.Valor}";
        planilha.Cell(linha, 1).Value = $"Emitido em {relatorio.GeradoEm.ToString("dd/MM/yyyy HH:mm", FormatacaoRelatorio.PtBr)}";
        planilha.Cell(linha++, 1).Style.Font.SetFontColor(XLColor.Gray);
        linha++;

        var linhaCabecalho = linha;
        for (var c = 0; c < relatorio.Colunas.Count; c++)
            planilha.Cell(linhaCabecalho, c + 1).Value = relatorio.Colunas[c].Titulo;

        var cabecalho = planilha.Range(linhaCabecalho, 1, linhaCabecalho, relatorio.Colunas.Count);
        cabecalho.Style.Font.SetBold().Fill.SetBackgroundColor(CorCabecalho);

        foreach (var valores in relatorio.Linhas)
        {
            linha++;
            for (var c = 0; c < relatorio.Colunas.Count; c++)
                PreencherCelula(planilha.Cell(linha, c + 1), valores[c], relatorio.Colunas[c].Tipo);
        }

        if (relatorio.Linhas.Count > 0)
        {
            planilha.Range(linhaCabecalho, 1, linha, relatorio.Colunas.Count).SetAutoFilter();
            planilha.SheetView.FreezeRows(linhaCabecalho);
        }

        if (relatorio.Indicadores.Count > 0)
        {
            linha += 2;
            planilha.Cell(linha++, 1).SetValue("Resumo").Style.Font.SetBold();
            foreach (var indicador in relatorio.Indicadores)
            {
                planilha.Cell(linha, 1).Value = indicador.Rotulo;
                planilha.Cell(linha++, 2).Value = indicador.Valor;
            }
        }

        planilha.Columns(1, relatorio.Colunas.Count).AdjustToContents(linhaCabecalho, linha);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // Números e datas vão como valores de verdade (somáveis e ordenáveis no Excel), não como texto.
    private static void PreencherCelula(IXLCell celula, object? valor, string tipo)
    {
        switch (valor)
        {
            case null:
                break;
            case DateOnly data:
                celula.Value = data.ToDateTime(TimeOnly.MinValue);
                celula.Style.DateFormat.Format = "dd/mm/yyyy";
                break;
            case DateTime dataHora:
                celula.Value = dataHora;
                celula.Style.DateFormat.Format = "dd/mm/yyyy";
                break;
            case decimal numero when tipo == TiposColuna.Percentual:
                celula.Value = numero / 100m;
                celula.Style.NumberFormat.Format = "0.0%";
                break;
            case decimal numero:
                celula.Value = numero;
                celula.Style.NumberFormat.Format = "#,##0.00";
                break;
            case int numero:
                celula.Value = numero;
                break;
            default:
                celula.Value = Convert.ToString(valor, FormatacaoRelatorio.PtBr);
                break;
        }
    }

    // O Excel limita o nome da aba a 31 caracteres e proíbe alguns símbolos.
    private static string NomeDaPlanilha(string titulo)
    {
        var nome = new string(titulo.Where(c => !"[]:*?/\\".Contains(c)).ToArray()).Trim();
        return nome.Length > 31 ? nome[..31] : nome;
    }
}

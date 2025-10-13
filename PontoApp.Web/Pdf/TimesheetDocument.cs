using System.Globalization;
using PontoApp.Web.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PontoApp.Web.Printing;

public sealed class TimesheetDocument : IDocument
{
    private readonly TimesheetPdfModel _m;
    private static readonly CultureInfo PtBr = new("pt-BR");

    public TimesheetDocument(TimesheetPdfModel model) => _m = model;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;

        container.Page(page =>
        {
            page.Margin(24);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(t => t.FontSize(9));

            page.Header().Element(Header);
            page.Content().Element(Body);
            page.Footer().Element(Footer);
        });
    }

    // ========= HEADER =========
    private void Header(IContainer c)
    {
        c.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(_m.Empresa ?? "EMPRESA").SemiBold().FontSize(12);
                    if (!string.IsNullOrWhiteSpace(_m.Cnpj))
                        left.Item().Text($"CNPJ: {_m.Cnpj}");
                });

                if (_m.LogoPng is not null && _m.LogoPng.Length > 0)
                {
                    row.ConstantItem(90).Image(_m.LogoPng);
                }
            });

            col.Item().PaddingTop(6).Row(r =>
            {
                r.RelativeItem().Text($"DEMONSTRATIVO DE PONTO  •  Competência: {_m.Inicio:MMMM/yyyy}")
                    .SemiBold().FontSize(11);
                r.ConstantItem(190).AlignRight().Text(
                    $"Período: {_m.Inicio:dd/MM/yyyy} a {_m.Fim:dd/MM/yyyy}"
                );
            });

            col.Item().PaddingTop(8).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Grid(grid =>
            {
                grid.Columns(6);
                grid.Item(3).Text($"Nome: {_m.Funcionario}");
                grid.Item(2).Text($"PIN: {_m.Pin ?? "-"}");
                grid.Item(1).Text($"CPF: {_m.Cpf ?? "-"}");
            });
        });
    }

    // ========= CONTENT =========
    private void Body(IContainer c)
    {
        c.Row(row =>
        {
            // Tabela principal (à esquerda)
            row.RelativeItem(2).Element(MainTable);

            // Coluna lateral (banco/ocorrências)
            row.RelativeItem(1).PaddingLeft(8).Element(SidePanel);
        });
    }

    private void MainTable(IContainer c)
    {
        c.Table(table =>
        {
            // colunas
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(32);   // Data
                cols.ConstantColumn(24);   // Dia
                cols.ConstantColumn(28);   // E
                cols.ConstantColumn(28);   // SA
                cols.ConstantColumn(28);   // VA
                cols.ConstantColumn(28);   // S
                cols.ConstantColumn(40);   // Normal
                cols.ConstantColumn(40);   // Extra
            });

            // cabeçalho
            table.Header(h =>
            {
                void Th(string t, float? pad = null) =>
                    h.Cell().Background(Colors.Grey.Lighten3)
                           .Padding(pad ?? 4).Text(t).SemiBold().AlignCenter();

                Th("Data"); Th("Dia"); Th("E"); Th("SA"); Th("VA"); Th("S"); Th("Normal"); Th("Extra");
            });

            foreach (var d in _m.Dias.OrderBy(x => x.Data))
            {
                var dia = d.Data;
                var dow = PtBr.DateTimeFormat.GetAbbreviatedDayName(dia.ToDateTime(TimeOnly.MinValue).DayOfWeek);

                string F(DateTimeOffset? x) => x?.ToLocalTime().ToString("HH:mm") ?? "";
                string Ft(TimeSpan? ts) => ts.HasValue ? $"{(int)ts.Value.TotalHours:00}:{ts.Value.Minutes:00}" : "";

                void Td(string t) => table.Cell().PaddingVertical(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten4).AlignCenter().Text(t);
                void TdL(string t) => table.Cell().PaddingVertical(2).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten4).AlignLeft().Text(t);

                Td(dia.ToString("dd/MM"));
                Td(dow);

                Td(F(d.Entrada));
                Td(F(d.SaidaAlmoco));
                Td(F(d.VoltaAlmoco));
                Td(F(d.Saida));

                Td(Ft(d.Total));
                Td(Ft(d.Extras));
            }

            // Totais
            table.Footer(f =>
            {
                for (int i = 0; i < 6; i++)
                    f.Cell();

                f.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignCenter()
                    .Text(FtOr(_m.TotalPeriodo));
                f.Cell().Background(Colors.Grey.Lighten3).Padding(3).AlignCenter()
                    .Text(FtOr(_m.TotalExtras));
            });
        });
    }

    private void SidePanel(IContainer c)
    {
        c.Column(col =>
        {
            col.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(b =>
            {
                b.Item().Text("Banco de Horas").SemiBold();
                b.Item().PaddingTop(4).Row(r =>
                {
                    r.RelativeItem().Text("Saldo:");
                    r.RelativeItem().AlignRight().Text(FtOr(_m.BancoDeHoras));
                });

                b.Item().PaddingTop(8).Text("Ocorrências").SemiBold();

                // Puxa ocorrências do dia (se preencher o campo Ocorrencia)
                foreach (var occ in _m.Dias.Where(x => !string.IsNullOrWhiteSpace(x.Ocorrencia)))
                {
                    b.Item().PaddingTop(2).Text($"{occ.Data:dd/MM}  •  {occ.Ocorrencia}");
                }
            });

            col.Item().PaddingTop(8).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(b =>
            {
                b.Item().Text("Resumo").SemiBold();
                b.Item().PaddingTop(4).Row(r => { r.RelativeItem().Text("Horas no período:"); r.RelativeItem().AlignRight().Text(FtOr(_m.TotalPeriodo)); });
                b.Item().Row(r => { r.RelativeItem().Text("Horas extras:"); r.RelativeItem().AlignRight().Text(FtOr(_m.TotalExtras)); });
                b.Item().Row(r => { r.RelativeItem().Text("Atrasos/Descontos:"); r.RelativeItem().AlignRight().Text(FtOr(_m.TotalAtrasos)); });
                b.Item().Row(r => { r.RelativeItem().Text("Banco de horas:"); r.RelativeItem().AlignRight().Text(FtOr(_m.BancoDeHoras)); });
            });
        });
    }

    // ========= FOOTER =========
    private void Footer(IContainer c)
    {
        c.Column(col =>
        {
            col.Item().PaddingTop(10).Row(r =>
            {
                r.RelativeItem().AlignCenter().Column(b =>
                {
                    b.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    b.Item().Text("Assinatura do Funcionário").FontSize(8);
                });
                r.RelativeItem().AlignCenter().Column(b =>
                {
                    b.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    b.Item().Text("Assinatura do Responsável").FontSize(8);
                });
            });

            col.Item().AlignRight().Text(txt =>
            {
                txt.Span("Emitido em ").FontSize(8).FontColor(Colors.Grey.Darken1);
                txt.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                   .SemiBold()
                   .FontSize(8)
                   .FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static string Ft(TimeSpan? ts) =>
        ts.HasValue ? $"{(int)ts.Value.TotalHours:00}:{ts.Value.Minutes:00}" : "";

    private static string FtOr(TimeSpan ts) =>
        $"{(int)ts.TotalHours:00}:{ts.Minutes:00}";
}

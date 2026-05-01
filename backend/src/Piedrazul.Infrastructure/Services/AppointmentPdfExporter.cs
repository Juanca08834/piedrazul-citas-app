using Piedrazul.Application;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Piedrazul.Infrastructure.Services;

public sealed class AppointmentPdfExporter : IAppointmentPdfExporter
{
    public AppointmentPdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Export(string centerName, string providerName, string specialty, DateOnly date, IReadOnlyList<AppointmentResponse> appointments)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Spacing(4);
                    column.Item().Text(centerName).FontSize(18).SemiBold();
                    column.Item().Text($"Listado de citas - {providerName}").FontSize(14).SemiBold();
                    column.Item().Text($"Especialidad: {specialty}");
                    column.Item().Text($"Fecha: {date:yyyy-MM-dd} | Total de citas: {appointments.Count}");
                });

                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                    });

                    static IContainer HeaderCell(IContainer container) => container
                        .BorderBottom(1)
                        .PaddingVertical(6)
                        .PaddingRight(4)
                        .Background(Colors.Grey.Lighten3);

                    static IContainer BodyCell(IContainer container) => container
                        .BorderBottom(0.5f)
                        .BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(6)
                        .PaddingRight(4);

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Paciente").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Documento").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Hora").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Canal").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Celular").SemiBold();
                    });

                    if (appointments.Count == 0)
                    {
                        table.Cell().ColumnSpan(5).Element(BodyCell).Text("No hay citas agendadas para esta fecha.");
                    }
                    else
                    {
                        foreach (var appointment in appointments)
                        {
                            table.Cell().Element(BodyCell).Text(appointment.PatientFullName);
                            table.Cell().Element(BodyCell).Text(appointment.DocumentNumber);
                            table.Cell().Element(BodyCell).Text($"{appointment.StartTime} - {appointment.EndTime}");
                            table.Cell().Element(BodyCell).Text(appointment.Channel);
                            table.Cell().Element(BodyCell).Text(appointment.Phone);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generado por Piedrazul • ");
                    text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                });
            });
        }).GeneratePdf();
    }
}

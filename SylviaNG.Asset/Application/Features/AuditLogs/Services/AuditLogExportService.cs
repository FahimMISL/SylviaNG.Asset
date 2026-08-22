using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using RMS.Application.Features.AuditLogs.DTOs;

namespace RMS.Application.Features.AuditLogs.Services;

/// <summary>
/// Feature 8: turns an already-filtered, already-authorized list of entries into a downloadable
/// file. Deliberately takes the same List&lt;AuditLogEntryDto&gt; the view endpoint returns, so the
/// exported rows are always exactly what's on screen - no separate query path that could drift.
/// </summary>
public static class AuditLogExportService
{
    private static readonly string[] Headers =
    [
        "Timestamp (UTC)", "Actor", "Actor Role", "System-Generated", "Action Type",
        "Entity", "Entity Id", "Details", "IP Address"
    ];

    public static byte[] ToCsv(List<AuditLogEntryDto> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", Headers.Select(Escape)));
        foreach (var e in entries)
        {
            sb.AppendLine(string.Join(",", Row(e).Select(Escape)));
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString());
    }

    public static byte[] ToXlsx(List<AuditLogEntryDto> entries)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Audit Log");

        for (var col = 0; col < Headers.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = Headers[col];
        }
        sheet.Row(1).Style.Font.Bold = true;

        var rowIndex = 2;
        foreach (var e in entries)
        {
            var values = Row(e);
            for (var col = 0; col < values.Length; col++)
            {
                sheet.Cell(rowIndex, col + 1).Value = values[col];
            }
            rowIndex++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string[] Row(AuditLogEntryDto e) =>
    [
        e.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        e.ActorName,
        e.ActorRole ?? "",
        e.IsSystemGenerated ? "Yes" : "No",
        e.ActionType,
        e.EntityName,
        e.EntityId.ToString(),
        e.Details ?? "",
        e.IpAddress ?? "",
    ];

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}

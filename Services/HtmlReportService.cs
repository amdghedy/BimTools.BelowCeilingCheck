using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using BimTools.BelowCeilingCheck.Infrastructure;
using BimTools.BelowCeilingCheck.Models;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>Creates a self-contained, print-ready HTML coordination report.</summary>
    public sealed class HtmlReportService
    {
        private const string Style =
            "body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:#f3f5f8;color:#1f2933;}" +
            ".page{max-width:1250px;margin:0 auto;padding:28px;}" +
            "h1{margin:0 0 4px 0;font-size:24px;color:#1f3a5f;}h2{font-size:16px;margin:28px 0 8px 0;color:#1f3a5f;}" +
            ".sub{color:#52606d;margin-bottom:18px;}" +
            ".meta{border-collapse:collapse;margin-bottom:12px;}.meta td{padding:3px 18px 3px 0;font-size:13px;}.meta td:first-child{color:#52606d;}" +
            ".kpis{display:flex;gap:12px;flex-wrap:wrap;margin:16px 0;}" +
            ".kpi{background:#fff;border-radius:8px;padding:12px 18px;min-width:130px;box-shadow:0 1px 3px rgba(0,0,0,.12);}" +
            ".kpi b{display:block;font-size:26px;}.kpi span{font-size:12px;color:#52606d;}" +
            ".crit b{color:#c62828;}.warn b{color:#ef6c00;}" +
            ".cols{display:flex;gap:16px;flex-wrap:wrap;}.cols table{flex:1;min-width:260px;}" +
            "table.grid{border-collapse:collapse;width:100%;background:#fff;font-size:12px;box-shadow:0 1px 3px rgba(0,0,0,.12);}" +
            "table.grid th{background:#1f3a5f;color:#fff;text-align:left;padding:6px 8px;}" +
            "table.grid td{padding:5px 8px;border-bottom:1px solid #e4e7eb;vertical-align:top;}" +
            "tr.c td:nth-child(2){color:#c62828;font-weight:600;}tr.w td:nth-child(2){color:#ef6c00;font-weight:600;}" +
            ".foot{margin-top:24px;font-size:11px;color:#7b8794;}" +
            "@media print{body{background:#fff;}.page{padding:0;}}";

        public void Save(string path, CheckSummary summary, IList<CheckResultItem> items)
        {
            CheckSettings settings = summary.Settings ?? new CheckSettings();
            LengthUnit unit = settings.DisplayUnit;
            StringBuilder b = new StringBuilder();

            b.Append("<!DOCTYPE html><html lang='en'><head><meta charset='utf-8'>");
            b.Append("<title>Below Ceiling Coordination Check</title><style>").Append(Style).Append("</style></head><body><div class='page'>");
            b.Append("<h1>Below Ceiling Coordination Check</h1>");
            b.Append("<div class='sub'>").Append(Enc(settings.CompanyName)).Append("</div>");

            b.Append("<table class='meta'>");
            Meta(b, "Model", summary.ModelTitle);
            Meta(b, "Run date", summary.RunDate.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            Meta(b, "Prepared by", settings.PreparedBy);
            Meta(b, "Reference", settings.Mode == ReferenceMode.CeilingElements
                ? "Ceiling elements (" + summary.CeilingFaceCount + " underside faces)"
                : "Fixed height " + UnitConverter.Format(UnitConverter.MmToFeet(settings.FixedCeilingHeightMm), unit) + " above level");
            Meta(b, "Scope", ScopeText(settings.Scope));
            Meta(b, "Tolerance", UnitConverter.Format(UnitConverter.MmToFeet(settings.ToleranceMm), unit));
            Meta(b, "Headroom check", settings.CheckHeadroom
                ? "Minimum " + UnitConverter.Format(UnitConverter.MmToFeet(settings.MinHeadroomMm), unit)
                : "Off");
            Meta(b, "Run time", summary.Duration.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
            b.Append("</table>");

            b.Append("<div class='kpis'>");
            Kpi(b, "", summary.ElementsChecked, "Elements checked");
            Kpi(b, "crit", summary.CriticalCount, "Critical (fully below)");
            Kpi(b, "warn", summary.WarningCount, "Warning (crossing / headroom)");
            Kpi(b, "", summary.HeadroomCount, "Headroom issues");
            b.Append("</div>");

            b.Append("<h2>Breakdown</h2><div class='cols'>");
            Breakdown(b, "By category", summary.ByCategory);
            Breakdown(b, "By level", summary.ByLevel);
            Breakdown(b, "By system", summary.BySystem);
            b.Append("</div>");

            string u = UnitConverter.Abbreviation(unit);
            b.Append("<h2>Findings (").Append(items.Count).Append(")</h2>");
            b.Append("<table class='grid'><tr><th>No</th><th>Severity</th><th>Source</th><th>Id</th><th>Category</th><th>Family / Type</th>");
            b.Append("<th>System</th><th>Level</th><th>Ceiling</th><th>Extent</th><th>Depth (").Append(u).Append(")</th><th>Bottom AFF</th><th>Ceiling AFF</th>");
            b.Append("<th>Headroom</th><th>Reviewed</th><th>Notes</th></tr>");

            foreach (CheckResultItem item in items)
            {
                b.Append(item.Severity == Severity.Critical ? "<tr class='c'>" : "<tr class='w'>");
                Cell(b, item.Number.ToString(CultureInfo.InvariantCulture));
                Cell(b, item.SeverityText);
                Cell(b, item.SourceText);
                Cell(b, item.ElementIdValue.ToString(CultureInfo.InvariantCulture));
                Cell(b, item.Category);
                Cell(b, string.IsNullOrEmpty(item.Family) ? item.TypeName : item.Family + " : " + item.TypeName);
                Cell(b, item.SystemName);
                Cell(b, item.LevelName);
                Cell(b, item.CeilingHits.Count > 1 ? item.CeilingName + " (+" + (item.CeilingHits.Count - 1) + " more)" : item.CeilingName);
                Cell(b, item.ExtentText);
                Cell(b, item.DepthText);
                Cell(b, item.BottomAffText);
                Cell(b, item.CeilingAffText);
                Cell(b, item.HeadroomText);
                Cell(b, item.IsReviewed ? "Yes" : string.Empty);
                Cell(b, item.Notes);
                b.Append("</tr>");
            }

            b.Append("</table>");
            b.Append("<div class='foot'>Generated by BIM TOOLS - Below Ceiling Check. Element bottoms are taken from model bounding boxes; ");
            b.Append("verify flagged items visually in the model.</div>");
            b.Append("</div></body></html>");

            File.WriteAllText(path, b.ToString(), new UTF8Encoding(false));
        }

        private static string ScopeText(ScopeMode scope)
        {
            switch (scope)
            {
                case ScopeMode.ActiveView: return "Active view";
                case ScopeMode.CurrentSelection: return "Current selection";
                default: return "Entire model";
            }
        }

        private static void Meta(StringBuilder b, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            b.Append("<tr><td>").Append(Enc(label)).Append("</td><td>").Append(Enc(value)).Append("</td></tr>");
        }

        private static void Kpi(StringBuilder b, string css, int value, string label)
        {
            b.Append("<div class='kpi ").Append(css).Append("'><b>").Append(value).Append("</b><span>").Append(Enc(label)).Append("</span></div>");
        }

        private static void Breakdown(StringBuilder b, string title, List<SummaryLine> lines)
        {
            b.Append("<table class='grid'><tr><th>").Append(Enc(title)).Append("</th><th>Count</th></tr>");
            foreach (SummaryLine line in lines)
            {
                b.Append("<tr><td>").Append(Enc(line.Label)).Append("</td><td>").Append(line.Count).Append("</td></tr>");
            }

            b.Append("</table>");
        }

        private static void Cell(StringBuilder b, string value)
        {
            b.Append("<td>").Append(Enc(value)).Append("</td>");
        }

        private static string Enc(string value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }
    }
}

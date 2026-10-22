using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BimTools.BelowCeilingCheck.Infrastructure;
using BimTools.BelowCeilingCheck.Models;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>Writes the results as a UTF-8 CSV that opens directly in Excel.</summary>
    public sealed class CsvExportService
    {
        public void Save(string path, IList<CheckResultItem> items, LengthUnit unit)
        {
            string u = UnitConverter.Abbreviation(unit);
            StringBuilder builder = new StringBuilder();

            AppendRow(builder, new[]
            {
                "No", "Severity", "Source", "Element Id", "Category", "Family", "Type", "System", "Level",
                "Ceiling", "Ceiling Id", "Ceiling Model", "Ceilings Below", "Extent", "Ceiling Level", "Ceiling Elevation (" + u + ")", "Element Bottom Elevation (" + u + ")",
                "Element Top Elevation (" + u + ")", "Depth Below Ceiling (" + u + ")", "Bottom AFF (" + u + ")",
                "Ceiling AFF (" + u + ")", "Headroom Issue", "Reviewed", "Notes"
            });

            foreach (CheckResultItem item in items)
            {
                AppendRow(builder, new[]
                {
                    item.Number.ToString(CultureInfo.InvariantCulture),
                    Text(item.SeverityText),
                    Text(item.SourceText),
                    item.ElementIdValue.ToString(CultureInfo.InvariantCulture),
                    Text(item.Category),
                    Text(item.Family),
                    Text(item.TypeName),
                    Text(item.SystemName),
                    Text(item.LevelName),
                    Text(item.CeilingName),
                    item.CeilingIdText,
                    Text(item.CeilingModelText),
                    item.CeilingCountText,
                    Text(item.ExtentText),
                    Text(item.CeilingLevelName),
                    Num(item.CeilingElevationFt, unit),
                    Num(item.BottomElevationFt, unit),
                    Num(item.TopElevationFt, unit),
                    Num(item.DepthFt, unit),
                    Num(item.BottomAffFt, unit),
                    Num(item.CeilingAffFt, unit),
                    item.HeadroomViolation ? "Yes" : "No",
                    item.IsReviewed ? "Yes" : "No",
                    Text(item.Notes)
                });
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
        }

        private static string Num(double feet, LengthUnit unit)
        {
            return UnitConverter.FromFeet(feet, unit).ToString(UnitConverter.NumberFormat(unit), CultureInfo.InvariantCulture);
        }

        /// <summary>Neutralises spreadsheet formula injection in text cells.</summary>
        private static string Text(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            char first = value[0];
            if (first == '=' || first == '+' || first == '-' || first == '@')
            {
                return "'" + value;
            }

            return value;
        }

        private static void AppendRow(StringBuilder builder, string[] cells)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(Escape(cells[i]));
            }

            builder.Append("\r\n");
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}

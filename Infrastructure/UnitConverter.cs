using System.Globalization;
using BimTools.BelowCeilingCheck.Models;

namespace BimTools.BelowCeilingCheck.Infrastructure
{
    /// <summary>
    /// Converts between Revit internal feet and the display units offered in the tool.
    /// </summary>
    public static class UnitConverter
    {
        private const double MmPerFoot = 304.8;

        public static double MmToFeet(double millimeters)
        {
            return millimeters / MmPerFoot;
        }

        public static double FeetToMm(double feet)
        {
            return feet * MmPerFoot;
        }

        public static double ToFeet(double value, LengthUnit unit)
        {
            switch (unit)
            {
                case LengthUnit.Millimeters: return value / MmPerFoot;
                case LengthUnit.Centimeters: return value / (MmPerFoot / 10.0);
                case LengthUnit.Meters: return value / (MmPerFoot / 1000.0);
                case LengthUnit.Inches: return value / 12.0;
                default: return value;
            }
        }

        public static double FromFeet(double feet, LengthUnit unit)
        {
            switch (unit)
            {
                case LengthUnit.Millimeters: return feet * MmPerFoot;
                case LengthUnit.Centimeters: return feet * (MmPerFoot / 10.0);
                case LengthUnit.Meters: return feet * (MmPerFoot / 1000.0);
                case LengthUnit.Inches: return feet * 12.0;
                default: return feet;
            }
        }

        public static double ToMm(double value, LengthUnit unit)
        {
            return FeetToMm(ToFeet(value, unit));
        }

        public static double FromMm(double millimeters, LengthUnit unit)
        {
            return FromFeet(MmToFeet(millimeters), unit);
        }

        public static string Abbreviation(LengthUnit unit)
        {
            switch (unit)
            {
                case LengthUnit.Millimeters: return "mm";
                case LengthUnit.Centimeters: return "cm";
                case LengthUnit.Meters: return "m";
                case LengthUnit.Inches: return "in";
                default: return "ft";
            }
        }

        public static string NumberFormat(LengthUnit unit)
        {
            switch (unit)
            {
                case LengthUnit.Millimeters: return "0";
                case LengthUnit.Centimeters: return "0.0";
                case LengthUnit.Meters: return "0.000";
                case LengthUnit.Inches: return "0.0";
                default: return "0.00";
            }
        }

        public static string Format(double feet, LengthUnit unit)
        {
            double value = FromFeet(feet, unit);
            return value.ToString(NumberFormat(unit), CultureInfo.InvariantCulture) + " " + Abbreviation(unit);
        }
    }
}

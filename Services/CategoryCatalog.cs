using System.Collections.Generic;
using Autodesk.Revit.DB;
using BimTools.BelowCeilingCheck.Models;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>The MEP categories the tool can inspect, grouped by discipline.</summary>
    public static class CategoryCatalog
    {
        public static List<CategoryOption> CreateOptions(ICollection<string> savedSelection)
        {
            List<CategoryOption> list = new List<CategoryOption>();

            Add(list, "Ducts", BuiltInCategory.OST_DuctCurves, "HVAC", true);
            Add(list, "Duct Fittings", BuiltInCategory.OST_DuctFitting, "HVAC", true);
            Add(list, "Duct Accessories", BuiltInCategory.OST_DuctAccessory, "HVAC", true);
            Add(list, "Flex Ducts", BuiltInCategory.OST_FlexDuctCurves, "HVAC", true);
            Add(list, "Duct Placeholders", BuiltInCategory.OST_PlaceHolderDucts, "HVAC", true);
            Add(list, "Air Terminals", BuiltInCategory.OST_DuctTerminal, "HVAC", false);
            Add(list, "Mechanical Equipment", BuiltInCategory.OST_MechanicalEquipment, "HVAC", true);

            Add(list, "Pipes", BuiltInCategory.OST_PipeCurves, "Piping & Plumbing", true);
            Add(list, "Pipe Fittings", BuiltInCategory.OST_PipeFitting, "Piping & Plumbing", true);
            Add(list, "Pipe Accessories", BuiltInCategory.OST_PipeAccessory, "Piping & Plumbing", true);
            Add(list, "Flex Pipes", BuiltInCategory.OST_FlexPipeCurves, "Piping & Plumbing", true);
            Add(list, "Pipe Placeholders", BuiltInCategory.OST_PlaceHolderPipes, "Piping & Plumbing", true);
            Add(list, "Plumbing Fixtures", BuiltInCategory.OST_PlumbingFixtures, "Piping & Plumbing", false);
            Add(list, "Sprinklers", BuiltInCategory.OST_Sprinklers, "Piping & Plumbing", false);

            Add(list, "Cable Trays", BuiltInCategory.OST_CableTray, "Electrical", true);
            Add(list, "Cable Tray Fittings", BuiltInCategory.OST_CableTrayFitting, "Electrical", true);
            Add(list, "Conduits", BuiltInCategory.OST_Conduit, "Electrical", true);
            Add(list, "Conduit Fittings", BuiltInCategory.OST_ConduitFitting, "Electrical", true);
            Add(list, "Electrical Equipment", BuiltInCategory.OST_ElectricalEquipment, "Electrical", true);
            Add(list, "Electrical Fixtures", BuiltInCategory.OST_ElectricalFixtures, "Electrical", false);
            Add(list, "Lighting Fixtures", BuiltInCategory.OST_LightingFixtures, "Electrical", false);

            Add(list, "Fire Alarm Devices", BuiltInCategory.OST_FireAlarmDevices, "Fire & Low Current", false);
            Add(list, "Communication Devices", BuiltInCategory.OST_CommunicationDevices, "Fire & Low Current", false);
            Add(list, "Data Devices", BuiltInCategory.OST_DataDevices, "Fire & Low Current", false);
            Add(list, "Security Devices", BuiltInCategory.OST_SecurityDevices, "Fire & Low Current", false);
            Add(list, "Nurse Call Devices", BuiltInCategory.OST_NurseCallDevices, "Fire & Low Current", false);
            Add(list, "Telephone Devices", BuiltInCategory.OST_TelephoneDevices, "Fire & Low Current", false);

            Add(list, "Specialty Equipment", BuiltInCategory.OST_SpecialityEquipment, "Other", false);

            if (savedSelection != null && savedSelection.Count > 0)
            {
                foreach (CategoryOption option in list)
                {
                    option.IsSelected = savedSelection.Contains(option.Category.ToString());
                }
            }

            return list;
        }

        private static void Add(List<CategoryOption> list, string name, BuiltInCategory category, string discipline, bool defaultSelected)
        {
            list.Add(new CategoryOption(name, category, discipline, defaultSelected));
        }
    }
}

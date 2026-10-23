BIM TOOLS - Below Ceiling Check
Revit version: 2022

Files:
- BimTools.BelowCeilingCheck.dll: compiled add-in for Revit 2022
- BelowCeilingCheck.addin: Revit add-in manifest
- Install-BelowCeilingCheck.ps1: copies files to the current user's Revit Addins folder

Installation:
1. Close Revit 2022.
2. Keep the DLL and manifest together in this package folder.
3. Run Install-BelowCeilingCheck.ps1 from this folder in PowerShell.
4. Start Revit 2022.

The installer writes only to the current user's AppData\Roaming\Autodesk\Revit\Addins\2022 folder.

$ErrorActionPreference = "Stop"
$RevitVersion = "2025"
$SourceRoot = $PSScriptRoot
$AddinsRoot = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$RevitVersion"
$AssemblyFolder = Join-Path $AddinsRoot "BimTools.BelowCeilingCheck"
if (-not (Test-Path (Join-Path (Join-Path $SourceRoot "BimTools.BelowCeilingCheck") "BimTools.BelowCeilingCheck.dll"))) {
  throw "DLL is missing from the package folder."
}
New-Item -ItemType Directory -Path $AssemblyFolder -Force | Out-Null
Copy-Item (Join-Path (Join-Path $SourceRoot "BimTools.BelowCeilingCheck") "BimTools.BelowCeilingCheck.dll") (Join-Path $AssemblyFolder "BimTools.BelowCeilingCheck.dll") -Force
Copy-Item (Join-Path $PSScriptRoot "BelowCeilingCheck.addin") (Join-Path $AddinsRoot "BelowCeilingCheck.addin") -Force
Write-Host "Below Ceiling Check installed for Revit $RevitVersion." -ForegroundColor Green
Write-Host "Restart Revit to load the add-in."

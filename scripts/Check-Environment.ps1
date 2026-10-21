# SportsBooking checks for JetBrains Rider/PowerShell. Does not install anything.
Write-Host "=== SportsBooking .NET 9 environment check ==="
Write-Host "Default local setup: SQLite (created automatically). Open http://localhost:5179."
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Warning ".NET SDK is missing or not in PATH. Install .NET 9 SDK."
} else {
    Write-Host "dotnet.exe: $($dotnet.Source)"
    $sdks = @(& dotnet --list-sdks)
    if (@($sdks | Where-Object { $_ -match '^9\.' }).Count -eq 0) {
        Write-Warning ".NET 9 SDK is not listed."
    } else {
        Write-Host "Detected .NET 9 SDK:"
        $sdks | Where-Object { $_ -match '^9\.' } | ForEach-Object { Write-Host "  $_" }
    }
}
$localDb = Get-Command SqlLocalDB.exe -ErrorAction SilentlyContinue
if (-not $localDb) {
    $paths = @(Get-ChildItem 'C:\Program Files\Microsoft SQL Server' -Recurse -Filter SqlLocalDB.exe -File -ErrorAction SilentlyContinue)
    if ($paths.Count -gt 0) { $localDb = $paths[0] }
}
if ($localDb) {
    $exe = $localDb.Source
    if (-not $exe) { $exe = $localDb.FullName }
    Write-Host "LocalDB executable: $exe"
    Write-Host "Available LocalDB instances:"
    & $exe info
    Write-Host "To start: & `"$exe`" start MSSQLLocalDB"
} else {
    Write-Host "Optional SQL Server LocalDB is not installed. The default SQLite development setup does not need it."
    Write-Host 'Docs: https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb'
}
$project = Join-Path $PSScriptRoot '..\SportsBooking.Web\SportsBooking.Web.csproj'
if (Test-Path $project) { Write-Host "Project: $project" } else { Write-Warning "Missing .csproj: $project" }
Write-Host 'Next: see docs/SETUP_RIDER_BG.md'

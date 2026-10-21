# Start from anywhere in PowerShell. SQLite development databases initialize automatically.
$project = Join-Path $PSScriptRoot '..\SportsBooking.Web\SportsBooking.Web.csproj'
if (-not (Test-Path $project)) { throw "Cannot find project: $project" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'dotnet not found. Install .NET 9 SDK.' }
& dotnet run --project $project --launch-profile SportsBooking.Web
exit $LASTEXITCODE

[CmdletBinding()]
param(
    [string]$ServerInstance = '(localdb)\MSSQLLocalDB',
    [string]$BusinessDatabaseName = 'SportsBookingDB_Simple',
    [string]$IdentityDatabaseName = 'SportsBookingIdentityDB'
)

$ErrorActionPreference = 'Stop'

if ($BusinessDatabaseName -eq $IdentityDatabaseName) {
    throw 'The business and Identity databases must have different names.'
}

foreach ($requiredTool in @('dotnet', 'sqlpackage')) {
    if (-not (Get-Command $requiredTool -ErrorAction SilentlyContinue)) {
        throw "Missing $requiredTool. See database/README.md for installation instructions."
    }
}

& dotnet build (Join-Path $PSScriptRoot 'SportsBooking.Database.sln') --configuration Release
if ($LASTEXITCODE -ne 0) {
    throw 'Database build failed. No databases were published.'
}

$databases = @(
    @{ Project = 'SportsBooking.Database'; Database = $BusinessDatabaseName },
    @{ Project = 'SportsBooking.Identity'; Database = $IdentityDatabaseName }
)

foreach ($database in $databases) {
    $packagePath = Join-Path $PSScriptRoot "$($database.Project)/bin/Release/$($database.Project).dacpac"
    Write-Host "Publishing $($database.Database) to $ServerInstance..."
    & sqlpackage /Action:Publish "/SourceFile:$packagePath" "/TargetServerName:$ServerInstance" `
        "/TargetDatabaseName:$($database.Database)" /TargetTrustServerCertificate:True `
        /p:BlockOnPossibleDataLoss=True /p:DropObjectsNotInSource=False
    if ($LASTEXITCODE -ne 0) {
        throw "Publishing $($database.Database) failed. See the SqlPackage output above."
    }
}

Write-Host 'Both SQL Server databases are ready. Start SportsBooking.Web to initialize roles and Development demo accounts.'

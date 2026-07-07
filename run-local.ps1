param(
    [int] $ApiPort = 5087,
    [int] $DashboardPort = 5173,
    [switch] $SkipBuild,
    [switch] $SkipDashboardBuild,
    [switch] $SkipNpmInstall,
    [switch] $ApplyMigrations,
    [switch] $NoBrowser
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$artifactsPath = Join-Path $repoRoot "artifacts"
$apiProject = Join-Path $repoRoot "src\Mci.Api\Mci.Api.csproj"
$dashboardPath = Join-Path $repoRoot "mci-dashboard"
$apiUrl = "http://localhost:$ApiPort"
$dashboardUrl = "http://localhost:$DashboardPort"

if ($ApiPort -ne 5087) {
    Write-Warning "The Vite dev proxy currently targets http://localhost:5087. API requests from the dashboard may fail unless mci-dashboard/vite.config.ts is updated."
}

New-Item -ItemType Directory -Path $artifactsPath -Force | Out-Null

function Stop-RecordedProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PidFile
    )

    if (-not (Test-Path $PidFile)) {
        return
    }

    $recordedPid = [int](Get-Content -Path $PidFile)
    Stop-Process -Id $recordedPid -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $PidFile -Force -ErrorAction SilentlyContinue
}

function Wait-ForHttp {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Url,
        [int] $TimeoutSeconds = 45
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3 | Out-Null
            return
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    throw "Timed out waiting for $Url."
}

$apiPidFile = Join-Path $artifactsPath "run-local-api.pid"
$dashboardPidFile = Join-Path $artifactsPath "run-local-dashboard.pid"

Write-Host "Stopping previously recorded local processes..."
Stop-RecordedProcess -PidFile $apiPidFile
Stop-RecordedProcess -PidFile $dashboardPidFile

if (-not $SkipBuild) {
    Write-Host "Building .NET solution..."
    dotnet build (Join-Path $repoRoot "Mci.sln")
}

if ($ApplyMigrations) {
    Write-Host "Applying EF Core migrations to the configured local database..."
    dotnet ef database update --project (Join-Path $repoRoot "src\Mci.Infrastructure") --startup-project (Join-Path $repoRoot "src\Mci.Api")
}

if (-not $SkipNpmInstall -and -not (Test-Path (Join-Path $dashboardPath "node_modules"))) {
    Write-Host "Installing dashboard packages..."
    npm install --prefix $dashboardPath
}

if (-not $SkipDashboardBuild) {
    Write-Host "Building dashboard..."
    npm run build --prefix $dashboardPath
}

$apiOutLog = Join-Path $artifactsPath "run-local-api.log"
$apiErrLog = Join-Path $artifactsPath "run-local-api.err.log"
$dashboardOutLog = Join-Path $artifactsPath "run-local-dashboard.log"
$dashboardErrLog = Join-Path $artifactsPath "run-local-dashboard.err.log"

Remove-Item -Path $apiOutLog, $apiErrLog, $dashboardOutLog, $dashboardErrLog -Force -ErrorAction SilentlyContinue

Write-Host "Starting API on $apiUrl..."
$previousAspNetCoreEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousDotnetEnvironment = $env:DOTNET_ENVIRONMENT
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_ENVIRONMENT = "Development"

$apiArgs = @(
    "run",
    "--project",
    $apiProject,
    "--no-build",
    "--urls",
    $apiUrl,
    "--",
    "--environment",
    "Development"
)
try {
    $apiProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList $apiArgs `
        -WorkingDirectory $repoRoot `
        -PassThru `
        -WindowStyle Hidden `
        -RedirectStandardOutput $apiOutLog `
        -RedirectStandardError $apiErrLog
}
finally {
    $env:ASPNETCORE_ENVIRONMENT = $previousAspNetCoreEnvironment
    $env:DOTNET_ENVIRONMENT = $previousDotnetEnvironment
}
$apiProcess.Id | Set-Content -Path $apiPidFile

Write-Host "Waiting for API health check..."
Wait-ForHttp -Url "$apiUrl/health"

Write-Host "Starting dashboard on $dashboardUrl..."
$npmCommand = if ($env:OS -eq "Windows_NT") { "npm.cmd" } else { "npm" }
$dashboardArgs = @(
    "run",
    "dev",
    "--",
    "--host",
    "localhost",
    "--port",
    "$DashboardPort"
)
$dashboardProcess = Start-Process -FilePath $npmCommand `
    -ArgumentList $dashboardArgs `
    -WorkingDirectory $dashboardPath `
    -PassThru `
    -WindowStyle Hidden `
    -RedirectStandardOutput $dashboardOutLog `
    -RedirectStandardError $dashboardErrLog
$dashboardProcess.Id | Set-Content -Path $dashboardPidFile

Write-Host "Waiting for dashboard..."
Wait-ForHttp -Url $dashboardUrl

if (-not $NoBrowser) {
    Write-Host "Opening dashboard..."
    Start-Process $dashboardUrl
}

Write-Host ""
Write-Host "Michigan County Insights is running locally."
Write-Host "Dashboard: $dashboardUrl"
Write-Host "API:       $apiUrl"
Write-Host "Swagger:   $apiUrl/swagger"
Write-Host ""
Write-Host "Logs:"
Write-Host "API stdout:       $apiOutLog"
Write-Host "API stderr:       $apiErrLog"
Write-Host "Dashboard stdout: $dashboardOutLog"
Write-Host "Dashboard stderr: $dashboardErrLog"
Write-Host ""
Write-Host "To stop the recorded processes:"
Write-Host "Stop-Process -Id (Get-Content $apiPidFile), (Get-Content $dashboardPidFile) -Force"

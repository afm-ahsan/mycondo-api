<#
.SYNOPSIS
    Pre-flight port check for MyCondo local dev. Checks the web port plus both API ports so a
    conflict is caught before `dotnet run` fails with an unhelpful bind error.

.DESCRIPTION
    See docs/local-development-ports.md for the full multi-project port registry this belongs to.
    Run this manually before `dotnet run --project src/MyCondo.Api` (not wired into the build itself,
    so it never affects CI or a non-interactive build).

.EXAMPLE
    pwsh scripts/check-ports.ps1
#>

$ports = @(
    @{ Port = [int]($env:MYCONDO_WEB_PORT ?? 4219); Owner = 'mycondo-web dev server' }
    @{ Port = [int]($env:MYCONDO_API_HTTPS_PORT ?? 7219); Owner = 'mycondo-api (HTTPS)' }
    @{ Port = [int]($env:MYCONDO_API_HTTP_PORT ?? 5219); Owner = 'mycondo-api (HTTP)' }
)

$conflicts = @()

foreach ($entry in $ports) {
    $connections = Get-NetTCPConnection -LocalPort $entry.Port -State Listen -ErrorAction SilentlyContinue
    if ($connections) {
        foreach ($connection in $connections) {
            $processName = $null
            try {
                $processName = (Get-Process -Id $connection.OwningProcess -ErrorAction Stop).ProcessName
            } catch {}

            $conflicts += [pscustomobject]@{
                Port    = $entry.Port
                Owner   = $entry.Owner
                Pid     = $connection.OwningProcess
                Process = $processName
            }
        }
    }
}

if ($conflicts.Count -gt 0) {
    Write-Host ''
    Write-Host 'X MyCondo local port check failed - reserved port(s) already in use:' -ForegroundColor Red
    Write-Host ''
    foreach ($c in $conflicts) {
        $procDesc = if ($c.Process) { "$($c.Process) (PID $($c.Pid))" } else { "PID $($c.Pid)" }
        Write-Host "  - :$($c.Port) ($($c.Owner)) - held by $procDesc"
    }
    Write-Host ''
    Write-Host 'Remediation:'
    Write-Host '  1. Stop whatever is holding the port above, e.g.:'
    Write-Host '       Stop-Process -Id <pid> -Force'
    Write-Host '  2. If it is a different local project, see docs/local-development-ports.md for the'
    Write-Host '     reserved range and free up MyCondo''s ports, or'
    Write-Host '  3. Override the port for this run via ASPNETCORE_URLS, e.g.:'
    Write-Host '       $env:ASPNETCORE_URLS = "https://localhost:7229;http://localhost:5229"'
    Write-Host '     (also update mycondo-web''s VITE_MYCONDO_API_BASE_URL and this API''s'
    Write-Host '     Cors:AllowedOrigins to match if you override the ports).'
    Write-Host ''
    exit 1
}

$portList = ($ports | ForEach-Object { $_.Port }) -join ', '
Write-Host "OK MyCondo ports free: $portList" -ForegroundColor Green

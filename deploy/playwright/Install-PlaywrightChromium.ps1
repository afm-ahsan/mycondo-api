<#
.SYNOPSIS
    Installs (or re-verifies) the Playwright-managed Chromium build used by MyCondo's server-side
    report PDF export (see src/MyCondo.Infrastructure/Reports/PlaywrightPdfRenderer.cs), into a
    deterministic, IIS-application-pool-safe shared location.

.DESCRIPTION
    Production target: Windows Server 2019, IIS (not Docker/containers, not Linux).

    This script does NOT hardcode a Chromium build number. It locates the `playwright.ps1` helper
    that ships in the *published* MyCondo.Api output (copied there transitively by the
    Microsoft.Playwright NuGet package's MSBuild targets, via MyCondo.Infrastructure's project
    reference) and simply runs `install chromium` through it. Playwright's own driver always installs
    the exact Chromium revision matching its own package version, so the correct browser build is
    whatever the currently-published Microsoft.Playwright version says it is — never something this
    script has to track separately. This means the entire upgrade procedure for a future
    Microsoft.Playwright NuGet bump is: bump the package, `dotnet publish` again, then re-run this
    script against the new publish output (see step 6 below) and recycle the IIS app pool.

    On Windows, `--with-deps` is NOT passed (that flag drives an apt-based Linux dependency install
    and is a no-op/inapplicable on Windows) — plain `install chromium` is correct here. CI
    (.github/workflows/ci.yml) still uses `--with-deps` because it runs on ubuntu-latest; that is
    unrelated to this script.

    Idempotent: re-running this script is always safe. Playwright's installer skips the download when
    the target revision is already present under BrowsersPath, so this script doubles as both the
    first-time setup step and the standard upgrade step.

.PARAMETER PublishedApiPath
    Path to a `dotnet publish` (or build) output directory for MyCondo.Api that contains
    `playwright.ps1` — typically the IIS site's physical path, e.g. C:\inetpub\CondoBD\api.

.PARAMETER BrowsersPath
    Deterministic, shared Chromium install location. Defaults to C:\CondoBD\Playwright. Deliberately
    NOT under any user profile — IIS application pool identities (e.g. IIS AppPool\<Name>) frequently
    have no loaded user profile, which makes anything resolved relative to %USERPROFILE% unreliable.

.PARAMETER AppPoolName
    Optional. If supplied, grants the named IIS Application Pool identity NTFS Read & Execute on
    BrowsersPath (icacls "IIS AppPool\<AppPoolName>:(OI)(CI)RX"), recursively.

.PARAMETER SkipMachineEnvironmentVariable
    If set, does not persist PLAYWRIGHT_BROWSERS_PATH as a Machine-level environment variable. The
    authoritative mechanism for the running IIS-hosted app is web.config's <aspNetCore>
    <environmentVariables> element (see mycondo-docs/02-architecture/PLAYWRIGHT_CHROMIUM_DEPLOYMENT.md)
    — the Machine-level variable set by this script is a convenience/defense-in-depth default for any
    other tooling on the box (e.g. re-running this script by hand) and is not relied upon by IIS itself,
    because Windows Process Activation Service (WAS) can cache its environment snapshot across a plain
    app-pool recycle.

.EXAMPLE
    .\Install-PlaywrightChromium.ps1 -PublishedApiPath C:\inetpub\CondoBD\api

.EXAMPLE
    .\Install-PlaywrightChromium.ps1 -PublishedApiPath C:\inetpub\CondoBD\api -AppPoolName CondoBDApiPool

.EXAMPLE
    # Dry run — shows what would happen without downloading/installing anything or touching the
    # filesystem/environment/ACLs.
    .\Install-PlaywrightChromium.ps1 -PublishedApiPath C:\inetpub\CondoBD\api -WhatIf
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [Parameter(Mandatory = $true)]
    [string]$PublishedApiPath,

    [string]$BrowsersPath = 'C:\CondoBD\Playwright',

    [string]$AppPoolName,

    [switch]$SkipMachineEnvironmentVariable
)

$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor Cyan
}

# --- 1. Locate playwright.ps1 in the published output -----------------------------------------

$playwrightScript = Join-Path $PublishedApiPath 'playwright.ps1'
if (-not (Test-Path -LiteralPath $playwrightScript)) {
    throw "playwright.ps1 was not found at '$playwrightScript'. Ensure MyCondo.Api has been " +
        "published first (dotnet publish src/MyCondo.Api -c Release -o `"$PublishedApiPath`"). " +
        "playwright.ps1 is copied into the build/publish output by the Microsoft.Playwright NuGet " +
        "package's MSBuild targets, transitively via MyCondo.Infrastructure's project reference — " +
        "if it is missing, the publish likely targeted the wrong directory or did not complete."
}
Write-Step "Found playwright.ps1 at $playwrightScript"

# --- 2. Report the Microsoft.Playwright version actually present in this publish output ---------
#        (traceability only — never used to pick a Chromium build number; see script header).

$playwrightDll = Join-Path $PublishedApiPath 'Microsoft.Playwright.dll'
if (Test-Path -LiteralPath $playwrightDll) {
    $packageVersion = (Get-Item -LiteralPath $playwrightDll).VersionInfo.ProductVersion
    Write-Step "Microsoft.Playwright package version in this publish output: $packageVersion"
} else {
    Write-Warning ("Microsoft.Playwright.dll not found next to playwright.ps1 — continuing anyway, " +
        "but this publish output looks unusual.")
}

# --- 3. Ensure the deterministic shared browsers path exists ------------------------------------

if (-not (Test-Path -LiteralPath $BrowsersPath)) {
    if ($PSCmdlet.ShouldProcess($BrowsersPath, 'Create directory')) {
        New-Item -ItemType Directory -Path $BrowsersPath -Force | Out-Null
        Write-Step "Created $BrowsersPath"
    }
} else {
    Write-Step "$BrowsersPath already exists"
}

# --- 4. Point Chromium's installer (and this process) at the shared path ------------------------
#        PLAYWRIGHT_BROWSERS_PATH must not depend on %USERPROFILE% — see script header.

$env:PLAYWRIGHT_BROWSERS_PATH = $BrowsersPath

if (-not $SkipMachineEnvironmentVariable) {
    if ($PSCmdlet.ShouldProcess('Machine environment variable PLAYWRIGHT_BROWSERS_PATH', "Set to $BrowsersPath")) {
        [Environment]::SetEnvironmentVariable('PLAYWRIGHT_BROWSERS_PATH', $BrowsersPath, 'Machine')
        Write-Step ("Set machine-level PLAYWRIGHT_BROWSERS_PATH = $BrowsersPath (convenience/defense-in-depth only — " +
            "the IIS-hosted app itself must get this via web.config; see the deployment runbook)")
    }
}

# --- 5. Install Chromium (idempotent — no-ops if the matching revision is already present) ------
#        Windows: no --with-deps (that flag is Linux/apt-specific and is wrong here).

if ($PSCmdlet.ShouldProcess("$BrowsersPath (via $playwrightScript)", 'Install Chromium')) {
    Write-Step "Running: $playwrightScript install chromium (PLAYWRIGHT_BROWSERS_PATH=$BrowsersPath)"
    & $playwrightScript install chromium
    if ($LASTEXITCODE -ne 0) {
        throw "playwright.ps1 install chromium failed with exit code $LASTEXITCODE."
    }

    $installed = Get-ChildItem -LiteralPath $BrowsersPath -Directory -Filter 'chromium*' -ErrorAction SilentlyContinue
    if (-not $installed) {
        throw "Chromium does not appear under $BrowsersPath after running the installer. Check the output above."
    }
    Write-Step "Chromium present under $BrowsersPath : $($installed.Name -join ', ')"
} else {
    Write-Host "WhatIf: would run '$playwrightScript install chromium' with PLAYWRIGHT_BROWSERS_PATH=$BrowsersPath"
}

# --- 6. Optional: grant the IIS Application Pool identity Read & Execute ------------------------

if ($AppPoolName) {
    $identity = "IIS AppPool\$AppPoolName"
    if ($PSCmdlet.ShouldProcess($BrowsersPath, "Grant '$identity' Read & Execute (icacls, recursive)")) {
        Write-Step "Granting '$identity' Read & Execute on $BrowsersPath"
        icacls $BrowsersPath /grant ("${identity}:(OI)(CI)RX") /T | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "icacls failed with exit code $LASTEXITCODE while granting '$identity' access to $BrowsersPath."
        }
    }
} else {
    Write-Warning ("No -AppPoolName supplied — remember to grant the IIS Application Pool identity Read & " +
        "Execute on $BrowsersPath manually. See mycondo-docs/02-architecture/PLAYWRIGHT_CHROMIUM_DEPLOYMENT.md " +
        "for the exact icacls command.")
}

Write-Host ''
Write-Step 'Done.'
Write-Host "Reminder: if PLAYWRIGHT_BROWSERS_PATH was newly set, or Chromium was just upgraded, the IIS " -ForegroundColor Yellow
Write-Host "Application Pool must be RECYCLED (not just app-restarted) for the change to take effect." -ForegroundColor Yellow

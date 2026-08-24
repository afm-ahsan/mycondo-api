<#
.SYNOPSIS
    Post-deployment smoke test for MyCondo's server-side PDF report export (Playwright/Chromium).

.DESCRIPTION
    Run once after each production deployment (and after any Chromium/Playwright upgrade + IIS
    app-pool recycle) as a verification gate: calls a real export endpoint over HTTP and asserts the
    response is actually a rendered PDF. This is the fastest way to catch a broken/missing Chromium
    install, a wrong PLAYWRIGHT_BROWSERS_PATH, or a missing NTFS permission in production — all of
    which fail silently at the API-contract level (the endpoint can return 200 with a broken/empty
    body, or throw only when Playwright first tries to launch Chromium) and are easy to miss until a
    real user hits "Export".

    Uses the Trial Balance PDF export endpoint (the most mature of the report exports):
        GET /api/v1/reports/finance/trial-balance/export?format=pdf

    Auth: the API uses JWT bearer authentication (see src/MyCondo.Api/Authentication/JwtBearerSetup.cs).
    This script takes a pre-obtained bearer token as a parameter rather than performing a login flow —
    obtaining one is an operational/credentials concern outside this script's job.

.PARAMETER BaseUrl
    Base URL of the deployed API, e.g. https://api.condobd.com (no trailing path).

.PARAMETER BearerToken
    A valid JWT bearer token for a tenant user holding the `finance.report.view` permission.

.PARAMETER AsOfDate
    Optional yyyy-MM-dd date to pass through to the report. Omit to use the endpoint's own default.

.EXAMPLE
    .\Test-PdfExportSmoke.ps1 -BaseUrl https://api.condobd.com -BearerToken $token

.OUTPUTS
    Exit code 0 on success, 1 on any assertion failure or request error. Intended for use as a CI/CD
    or manual post-deploy gate — check $LASTEXITCODE (or PowerShell's automatic $? / pipeline exit).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$BearerToken,

    [string]$AsOfDate
)

$ErrorActionPreference = 'Stop'

# Windows PowerShell 5.1 (the default on Windows Server 2019) can default to weaker TLS versions for
# outbound HTTPS unless explicitly raised — do this defensively rather than assuming server config.
try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
} catch {
    # Best-effort; older runtimes that don't expose Tls12 will simply keep their default.
}

function Write-Result {
    param([string]$Message, [ValidateSet('Info', 'Pass', 'Fail')][string]$Kind = 'Info')
    $color = switch ($Kind) { 'Pass' { 'Green' } 'Fail' { 'Red' } default { 'Cyan' } }
    Write-Host $Message -ForegroundColor $color
}

$uri = "$($BaseUrl.TrimEnd('/'))/api/v1/reports/finance/trial-balance/export?format=pdf"
if ($AsOfDate) {
    $uri += "&asOfDate=$AsOfDate"
}

Write-Result "==> GET $uri"

$statusCode = $null
$headers = $null
$bodyBytes = $null
$contentTypeFallback = $null

try {
    $response = Invoke-WebRequest -Uri $uri -Method Get -UseBasicParsing `
        -Headers @{ Authorization = "Bearer $BearerToken" }
    $statusCode = [int]$response.StatusCode
    $headers = $response.Headers
    $bodyBytes = $response.Content
} catch [Microsoft.PowerShell.Commands.HttpResponseException] {
    # PowerShell 7's Invoke-WebRequest throws on non-2xx; recover the response for assertion purposes.
    # System.Net.Http keeps Content-Type on .Content.Headers, not the message .Headers collection, so
    # read it from there as the fallback.
    $statusCode = [int]$_.Exception.Response.StatusCode
    $headers = $_.Exception.Response.Headers
    if ($_.Exception.Response.Content -and $_.Exception.Response.Content.Headers.ContentType) {
        $contentTypeFallback = $_.Exception.Response.Content.Headers.ContentType.ToString()
    }
    $bodyBytes = $null
} catch {
    if ($_.Exception.Response) {
        # Windows PowerShell 5.1's WebException shape — Content-Type is not always populated in
        # .Headers on error responses, so also capture the dedicated .ContentType property as a fallback.
        $webResponse = $_.Exception.Response
        $statusCode = [int]$webResponse.StatusCode
        $headers = $webResponse.Headers
        $contentTypeFallback = $webResponse.ContentType
        $bodyBytes = $null
    } else {
        Write-Result "SMOKE TEST FAILED: request could not be completed — $($_.Exception.Message)" -Kind Fail
        exit 1
    }
}

$failures = New-Object System.Collections.Generic.List[string]

if ($statusCode -ne 200) {
    $failures.Add("Expected HTTP 200, got $statusCode.")
}

$contentType = $null
if ($headers) {
    $contentType = $headers['Content-Type']
    if ($contentType -is [array]) { $contentType = $contentType[0] }
}
if (-not $contentType -and $contentTypeFallback) {
    $contentType = $contentTypeFallback
}
if (-not $contentType -or ($contentType -notlike 'application/pdf*')) {
    $failures.Add("Expected Content-Type 'application/pdf', got '$contentType'.")
}

if ($null -eq $bodyBytes -or $bodyBytes.Length -lt 4) {
    $failures.Add('Response body was empty or too short to be a PDF (request may have failed — see status/content-type above).')
} else {
    if ($bodyBytes -isnot [byte[]]) {
        # Some PowerShell versions/content-types can surface .Content as a string; normalize defensively.
        $bodyBytes = [System.Text.Encoding]::GetEncoding('ISO-8859-1').GetBytes([string]$bodyBytes)
    }
    $magic = [System.Text.Encoding]::ASCII.GetString($bodyBytes[0..3])
    if ($magic -ne '%PDF') {
        $failures.Add("Expected response body to start with the '%PDF' magic bytes, got '$magic'.")
    }
}

Write-Host ''
if ($failures.Count -gt 0) {
    Write-Result 'SMOKE TEST FAILED' -Kind Fail
    foreach ($failure in $failures) {
        Write-Result " - $failure" -Kind Fail
    }
    exit 1
}

$sizeKb = [Math]::Round($bodyBytes.Length / 1KB, 1)
Write-Result "SMOKE TEST PASSED: Trial Balance PDF export returned a valid PDF ($sizeKb KB)." -Kind Pass
exit 0

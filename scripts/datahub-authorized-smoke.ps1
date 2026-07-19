param(
    [string]$BaseUrl = "http://127.0.0.1:5000",
    [string]$SyntheticUserId = ""
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http
$password = [Environment]::GetEnvironmentVariable("IQC_SYNTHETIC_USER_SEED_PASSWORD")
if ([string]::IsNullOrWhiteSpace($password)) {
    throw "IQC_SYNTHETIC_USER_SEED_PASSWORD is required. Set it only in the current Development/Testing process environment; do not save it in the repository."
}

$fixturePath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\fixtures\personnel.synthetic.json"))
if (-not (Test-Path -LiteralPath $fixturePath)) { throw "Synthetic personnel fixture not found: $fixturePath" }
$fixture = Get-Content -LiteralPath $fixturePath -Raw -Encoding UTF8 | ConvertFrom-Json
$candidate = if ($SyntheticUserId) {
    $fixture | Where-Object { $_.employeeId -eq $SyntheticUserId -and $_.isActive -eq $true } | Select-Object -First 1
} else {
    $fixture | Where-Object { $_.employeeId -like "SYN-*" -and $_.isActive -eq $true } | Select-Object -First 1
}
if (-not $candidate) { throw "No active SYN-* fixture user matches the requested smoke identity." }

$client = [Net.Http.HttpClient]::new()
$client.BaseAddress = [Uri]$BaseUrl
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("iqc-datahub-smoke-" + [Guid]::NewGuid().ToString("N"))
$workbookPath = Join-Path $tempRoot "master-plan-smoke.xlsx"
$sku = "SYN-SMOKE-" + [DateTime]::UtcNow.ToString("yyyyMMddHHmmssfff")

function Read-JsonResponse([Net.Http.HttpResponseMessage]$Response, [string]$Action) {
    $text = $Response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    if (-not $Response.IsSuccessStatusCode) {
        $detail = try { ($text | ConvertFrom-Json).detail } catch { $null }
        if (-not $detail) { $detail = $Response.ReasonPhrase }
        throw "$Action failed with HTTP $([int]$Response.StatusCode): $detail"
    }
    if ([string]::IsNullOrWhiteSpace($text)) { return $null }
    return $text | ConvertFrom-Json
}

function Send-Json([string]$Method, [string]$Path, $Body = $null) {
    $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::new($Method), $Path)
    if ($Body -ne $null) {
        $request.Content = [Net.Http.StringContent]::new(($Body | ConvertTo-Json -Depth 8 -Compress), [Text.Encoding]::UTF8, "application/json")
    }
    $response = $client.SendAsync($request).GetAwaiter().GetResult()
    try { return Read-JsonResponse $response "$Method $Path" } finally { $response.Dispose(); $request.Dispose() }
}

function Write-ZipText([IO.Compression.ZipArchive]$Archive, [string]$Path, [string]$Text) {
    $writer = [IO.StreamWriter]::new($Archive.CreateEntry($Path).Open(), [Text.UTF8Encoding]::new($false))
    try { $writer.Write($Text) } finally { $writer.Dispose() }
}

try {
    New-Item -ItemType Directory -Path $tempRoot | Out-Null
    Add-Type -AssemblyName System.IO.Compression
    $fileStream = [IO.File]::Open($workbookPath, [IO.FileMode]::CreateNew)
    try {
        $archive = [IO.Compression.ZipArchive]::new($fileStream, [IO.Compression.ZipArchiveMode]::Create, $false)
        try {
            Write-ZipText $archive "[Content_Types].xml" '<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>'
            Write-ZipText $archive "_rels/.rels" '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>'
            Write-ZipText $archive "xl/workbook.xml" '<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="MasterPlan" sheetId="1" r:id="rId1"/></sheets></workbook>'
            Write-ZipText $archive "xl/_rels/workbook.xml.rels" '<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>'
            $headers = @("Project Name", "SKU", "PVR Target", "Area", "Grade", "HW PIC", "PRA Target", "SRA Target")
            $values = @("Synthetic Smoke Model", $sku, "2026-08-01", "Smoke Area", "A", $candidate.fullName, "2026-08-02", "2026-08-03")
            $headerCells = for ($i = 0; $i -lt $headers.Count; $i++) { '<c r="{0}1" t="inlineStr"><is><t>{1}</t></is></c>' -f [char](65 + $i), [Security.SecurityElement]::Escape($headers[$i]) }
            $valueCells = for ($i = 0; $i -lt $values.Count; $i++) { '<c r="{0}2" t="inlineStr"><is><t>{1}</t></is></c>' -f [char](65 + $i), [Security.SecurityElement]::Escape([string]$values[$i]) }
            Write-ZipText $archive "xl/worksheets/sheet1.xml" ('<?xml version="1.0" encoding="UTF-8"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData><row r="1">{0}</row><row r="2">{1}</row></sheetData></worksheet>' -f ($headerCells -join ""), ($valueCells -join ""))
        } finally { $archive.Dispose() }
    } finally { $fileStream.Dispose() }

    $login = Send-Json "POST" "/api/auth/login" @{ username = $candidate.employeeId; password = $password }
    if ([string]::IsNullOrWhiteSpace($login.token)) { throw "Login succeeded without returning a JWT." }
    $client.DefaultRequestHeaders.Authorization = [Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", [string]$login.token)
    Write-Output "PASS: authenticated active synthetic fixture user $($candidate.employeeId); JWT received (redacted)."

    function Send-Workbook([string]$Path, [string]$MappingJson = "") {
        $content = [Net.Http.MultipartFormDataContent]::new()
        $bytes = [IO.File]::ReadAllBytes($workbookPath)
        $fileContent = [Net.Http.ByteArrayContent]::new($bytes)
        $fileContent.Headers.ContentType = [Net.Http.Headers.MediaTypeHeaderValue]::new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        $content.Add($fileContent, "file", "master-plan-smoke.xlsx")
        if ($MappingJson) { $content.Add([Net.Http.StringContent]::new($MappingJson), "headerMapping") }
        $response = $client.PostAsync($Path, $content).GetAwaiter().GetResult()
        try { return Read-JsonResponse $response "POST $Path" } finally { $response.Dispose(); $content.Dispose() }
    }

    $inspection = Send-Workbook "/api/DataHub/inspect-headers"
    $mappings = @($inspection.columns | Where-Object { $_.suggestedCanonical -and -not $_.ambiguous } | ForEach-Object { @{ columnIndex = $_.columnIndex; canonicalField = $_.suggestedCanonical } })
    $missing = @($inspection.requiredFields | Where-Object { $_ -notin $mappings.canonicalField })
    if ($missing.Count -gt 0) { throw "Header inspection did not map required field(s): $($missing -join ', ')." }
    Write-Output "PASS: workbook headers inspected and required mappings confirmed."

    $batch = Send-Workbook "/api/DataHub/upload" ($mappings | ConvertTo-Json -Compress)
    $review = Send-Json "GET" "/api/DataHub/review/$($batch.batchId)"
    if ($review.errorRows -gt 0) { throw "Smoke batch has blocking validation errors." }
    if ($review.existingSkuConflicts -gt 0) {
        Send-Json "POST" "/api/DataHub/resolve-existing/$($batch.batchId)" @{ resolution = "Cancel" } | Out-Null
        throw "Generated unique SKU unexpectedly conflicted; batch was cancelled without mutation."
    }
    $warningRows = @($review.rows | Where-Object { $_.severity -eq "Warning" } | Select-Object -ExpandProperty rowNumber -Unique)
    foreach ($rowNumber in $warningRows) {
        Send-Json "POST" "/api/DataHub/resolve-warning/$($batch.batchId)/$rowNumber" @{ resolution = "Accept" } | Out-Null
    }
    $review = Send-Json "GET" "/api/DataHub/review/$($batch.batchId)"
    if ($review.errorRows -gt 0 -or $review.warningRows -gt 0 -or $review.existingSkuConflicts -gt 0) { throw "Blocking review items remain after explicit smoke resolutions." }
    Write-Output "PASS: review inspected and explicit warning/conflict handling completed."

    Send-Json "POST" "/api/DataHub/commit/$($batch.batchId)" | Out-Null
    $records = @(Send-Json "GET" "/api/masterplan/records")
    if (-not ($records | Where-Object { $_.sku -eq $sku })) { throw "Committed smoke SKU was not visible through Master Plan records API." }
    Write-Output "PASS: batch committed atomically and the generated SKU is visible through Master Plan API."
} catch {
    if ($_.Exception.Message -match "401|Unauthorized") {
        throw "Synthetic login failed. Confirm the API is running in Development/Testing with the same IQC_SYNTHETIC_USER_SEED_PASSWORD so an active SYN-* user is seeded."
    }
    throw
} finally {
    $client.Dispose()
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
    $password = $null
}

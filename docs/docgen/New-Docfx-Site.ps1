param(
  [switch]$Serve
)

# Configuration
$DocfxDirectory = ".\docs\docgen"
$DocfxJson = "docfx.json"
$SiteRoot = "https://example.org/"

# Build combined path
$configPath = Join-Path -Path $DocfxDirectory -ChildPath $DocfxJson

# Check for docfx JSON file
if (-not (Test-Path -Path $configPath)) {
  Write-Error "Error: Invoke me from Furminal, please."
  exit 1
}

# Check for docfx (must be in PATH)
if (-not (Get-Command "docfx" -ErrorAction SilentlyContinue)) {
  Write-Error "Error: 'docfx' not found in PATH. Install it via 'dotnet tool update -g docfx'."
  exit 1
}

# Check for powershell-yaml
if (-not (Get-Module -ListAvailable "powershell-yaml" -ErrorAction SilentlyContinue)) {
  Write-Error "Error: The 'powershell-yaml' module is not installed. Install it via 'Install-Module powershell-yaml'."
  exit 1
}

Import-Module powershell-yaml

# Pull paths from the docfx JSON file
$config = Get-Content $configPath -Raw | ConvertFrom-Json
$apiOutput = Join-Path -Path $DocfxDirectory -ChildPath $config.metadata.output
$siteOutput = Join-Path -Path $DocfxDirectory -ChildPath $config.build.output

# Delete old output
if (Test-Path -Path $apiOutput) {
  Remove-Item -Recurse -Path $apiOutput
  Write-Host "✗ $apiOutput" -ForegroundColor DarkRed
}

if (Test-Path -Path $siteOutput) {
  Remove-Item -Recurse -Path $siteOutput
  Write-Host "✗ $siteOutput" -ForegroundColor DarkRed
}

# Generate metadata
docfx metadata $configPath

# Check for API output
if (-not (Test-Path -Path $apiOutput)) {
  Write-Error "Error: Expected API output at $apiOutput, but it does not exist."
  exit 1
} else {
  Write-Host "✓ $apiOutput" -ForegroundColor Green
}

# Check for API TOC
$apiTocPath = Join-Path -Path $apiOutput -ChildPath "toc.yml"

if (-not (Test-Path -Path $apiTocPath)) {
  Write-Error "Error: Expected API TOC file at $apiTocPath, but it does not exist."
  exit 1
}

# Keep only namespaces we care about in the API TOC
$inputApiToc = Get-Content -Path $apiTocPath -Raw | ConvertFrom-Yaml
$apiToc = New-Object -TypeName System.Collections.Hashtable

$apiToc.Add("memberLayout", $inputApiToc.memberLayout)
$apiToc.Add("items", (New-Object -TypeName System.Collections.Generic.List[System.Object]))

foreach ($item in $inputApiToc.items) {
  if ($item.uid -match "^Spakov\.") {
    $apiToc.items.Add($item)
  }
}

Set-Content -Path $apiTocPath -Value ($apiToc | ConvertTo-Yaml)
Write-Host "✓ $apiTocPath" -ForegroundColor Green

# Generate the site
docfx build $configPath

# Check for site output
if (-not (Test-Path -Path $siteOutput)) {
  Write-Error "Error: Expected site output at $siteOutput, but it does not exist."
  exit 1
} else {
  Write-Host "✓ $siteOutput" -ForegroundColor Green
}

# Check for the xref map
$xrefMapPath = Join-Path -Path $siteOutput -ChildPath "xrefmap.yml"

if (-not (Test-Path -Path $xrefMapPath)) {
  Write-Error "Error: Expected xrefmap.yml at $xrefMapPath, but it does not exist."
  exit 1
}

# Keep only namespaces we care about in the xref map
$inputXrefMap = Get-Content -Path $xrefMapPath -Raw | ConvertFrom-Yaml
$xrefMap = New-Object -TypeName System.Collections.Hashtable

$xrefMap.Add("sorted", $inputXrefMap.sorted)
$xrefMap.Add("references", (New-Object -TypeName System.Collections.Generic.List[System.Object]))

foreach ($reference in $inputXrefMap.references) {
  if ($reference.uid -match "^Spakov\.") {
    $reference.href = "${SiteRoot}$($reference.href)"
    $xrefMap.references.Add($reference)
  }
}

$outputXrefMap = $xrefMap | ConvertTo-Yaml
$outputXrefMap = "### YamlMime:XRefMap`r`n${outputXrefMap}"

Set-Content -Path $xrefMapPath -Value $outputXrefMap
Write-Host "✓ $xrefMapPath" -ForegroundColor Green

# Optionally serve the site
if ($Serve) {
  docfx serve $siteOutput
}

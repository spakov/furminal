# Configuration
$ConfigFile = "generate-schema-doc-config.json"
$InputPattern = "w6t-*-schema.json"
$OutputFile = "schema_doc.md"
$DocgenDirectory = "docgen"

# Check for generate-schema-doc (must be in PATH)
if (-not (Get-Command "generate-schema-doc" -ErrorAction SilentlyContinue)) {
  Write-Error "Error: 'generate-schema-doc' not found in PATH. Install it via 'pip install json-schema-for-humans'."
  exit 1
}

# Check for config file
if (-not (Test-Path -Path $ConfigFile)) {
  $ConfigFile = Join-Path $DocgenDirectory $ConfigFile
  if (-not (Test-Path -Path $ConfigFile)) {
    Write-Error "Error: Configuration file '$ConfigFile' not found."
    exit 1
  }
}

# Find schema files
$SchemaFiles = Get-ChildItem -Filter $InputPattern | Sort-Object Name
if ($SchemaFiles.Count -eq 0) {
  Write-Warning "No schema files matching '$InputPattern' found."
  exit 0
}

# Process each schema file
foreach ($file in $SchemaFiles) {
  Write-Host "Processing: $($file.Name)"

  # Run generate-schema-doc
  generate-schema-doc --config-file $ConfigFile $file.FullName

  # Confirm output file exists
  if (-not (Test-Path -Path $OutputFile)) {
    Write-Warning "Expected output '$OutputFile' not found after processing $($file.Name). Skipping."
    continue
  }

  # Transform and rename output
  if ($file.BaseName -match 'w6t-(?<version>.+)-schema') {
    $version = $matches.version
    $destination = "w6t-$version-schema.md"
    $doc = Get-Content $OutputFile -Raw

    # Replace Title/Description with Description
    $doc = $doc.Replace("Title/Description", "Description")

    # Replace the initial Title line with the version
    $doc = $doc.Replace("`r`n**Title:** config.json`r`n", "`r`n**Version:** $version`r`n")

    Set-Content -Path $destination -Value $doc
    Write-Host "→ $destination"
  }
  else {
    Write-Warning "Filename format unexpected: $($file.Name). Cannot extract version."
  }

  # Clean up temp output
  Remove-Item -Path $OutputFile
}

Write-Host "`nSchema documentation generation complete." -ForegroundColor Green

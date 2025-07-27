# Configuration
$NGLF = "\opt\NuGetLicenseFramework-3.1.6\NuGetLicenseFramework.exe"
$NGLFGitHubActions = ".\NuGetLicense\artifacts\net472\NuGetLicenseFramework.exe"
$LicenseGenDirectory = "licensegen"
$JsonInput = "$LicenseGenDirectory\json-input.json"
$OutputType = "Json"
$TemporaryOutputFile = "..\THIRD-PARTY-NOTICES.json"
$LicenseOutputDirectory = ".\"
$Ignore = "$LicenseGenDirectory\ignored-packages.json"
$Override = "$LicenseGenDirectory\override-package-information.json"
$OutputFile = "..\THIRD-PARTY-NOTICES.md"

# Check for invocation directory
if (-not (Test-Path -Path $LicenseGenDirectory)) {
  Write-Error "Error: Invoke me from LICENSES, please."
  exit 1
}

# Check for NGLF
if (-not (Test-Path -Path $NGLF)) {
  # Check for GitHub Actions workflow location
  if (Test-Path -Path $NGLFGitHubActions) {
    $NGLF = $NGLFGitHubActions
  } else {
    Write-Error "Error: '$NGLF' not found. Obtain NuGetLicenseFramework.exe from https://github.com/sensslen/nuget-license and point me to it via $$NGLF."
    exit 1
  }
}

# Check for jq
if (-not (Get-Command "jq" -ErrorAction SilentlyContinue)) {
  Write-Error "Error: 'jq' not found in PATH. Install it via 'winget install jqlang.jq'."
  exit 1
}

# Clear output
Get-ChildItem -Path $LicenseOutputDirectory -Exclude $LicenseGenDirectory | Remove-Item

# Run NGLF
& $NGLF -ji $JsonInput -t -o $OutputType -fo $TemporaryOutputFile -d $LicenseOutputDirectory -ignore $Ignore -override $Override

# Confirm temporary output file exists
if (-not (Test-Path -Path $TemporaryOutputFile)) {
  Write-Error "Error: Expected output '$TemporaryOutputFile' not found."
  exit 1
}

# Read and delete temporary output
$out = Get-Content -Path $TemporaryOutputFile -Raw
Remove-Item -Path $TemporaryOutputFile

# Rename RTFs
$wronglyNamedRtfs = "Microsoft.Windows.SDK.BuildTools*", "Microsoft.Windows.SDK.Win32Docs*"
foreach ($wronglyNamedRtf in Get-ChildItem -Path $LicenseOutputDirectory* -Include $wronglyNamedRtfs) {
  $correctlyNamedRtf = [System.IO.Path]::ChangeExtension($wronglyNamedRtf.FullName, ".rtf")
  Rename-Item -Path $wronglyNamedRtf.FullName -NewName $correctlyNamedRtf
}

# Remove errors and only keep properties we care about
$out = ($out | & jq '[ .[] | { PackageId, PackageVersion, PackageProjectUrl, License, LicenseUrl } ]')

# Add Microsoft.Web.WebView2
$id = "Microsoft.Web.WebView2"
$version = "1.0.2903.40"
$projectUrl = "https://aka.ms/webview"
$license = "WebView2"
$licenseUrl = "https://www.nuget.org/packages/Microsoft.Web.WebView2/$version/License"
$rawLicensePath = "$Env:USERPROFILE\.nuget\packages\microsoft.web.webview2\$version\LICENSE.txt"
$out = ($out | & jq ". += [{ `"PackageId`": `"${id}`", `"PackageVersion`": `"${version}`", `"PackageProjectUrl`": `"${projectUrl}`", `"License`": `"${license}`", `"LicenseUrl`": `"${licenseUrl}`" }]")
Copy-Item -Path $rawLicensePath -Destination ${id}__${version}.txt

# Add utf8proc
$id = "utf8proc"
$version = (git -C ..\..\Furminal\utf8proc log -n 1 --pretty=format:"%H")
$projectUrl = "https://github.com/JuliaStrings/utf8proc/"
$license = "MIT"
$licenseUrl = "https://github.com/JuliaStrings/utf8proc/raw/refs/heads/master/LICENSE.md"
$out = ($out | & jq ". += [{ `"PackageId`": `"${id}`", `"PackageVersion`": `"${version}`", `"PackageProjectUrl`": `"${projectUrl}`", `"License`": `"${license}`", `"LicenseUrl`": `"${licenseUrl}`" }]")
Invoke-WebRequest -Uri $licenseUrl -OutFile ${id}__${version}.txt

# Add Nerd Fonts
$id = "Nerd Fonts"
$version = "3.4.0"
$projectUrl = "https://github.com/ryanoasis/nerd-fonts/"
$license = "Nerd Fonts"
$licenseUrl = "https://github.com/ryanoasis/nerd-fonts/raw/refs/heads/master/LICENSE"
$out = ($out | & jq ". += [{ `"PackageId`": `"${id}`", `"PackageVersion`": `"${version}`", `"PackageProjectUrl`": `"${projectUrl}`", `"License`": `"${license}`", `"LicenseUrl`": `"${licenseUrl}`" }]")
Invoke-WebRequest -Uri $licenseUrl -OutFile ${id}__${version}.txt

# Add Unicode
$id = "Unicode"
$version = "16.0.0"
$projectUrl = "https://home.unicode.org/"
$license = "Unicode License v3"
$licenseUrl = "https://www.unicode.org/license.txt"
$out = ($out | & jq ". += [{ `"PackageId`": `"${id}`", `"PackageVersion`": `"${version}`", `"PackageProjectUrl`": `"${projectUrl}`", `"License`": `"${license}`", `"LicenseUrl`": `"${licenseUrl}`" }]")
Invoke-WebRequest -Uri $licenseUrl -OutFile ${id}__${version}.txt

# Convert to Markdown
$markdown = [System.Collections.Generic.List[string]]::new()
$json = ($out | ConvertFrom-Json | Sort-Object PackageId)

foreach ($package in $json) {
  $id = $package.PackageId
  $projectUrl = $package.PackageProjectUrl
  $version = $package.PackageVersion
  $license = $package.License
  $licenseUrl = $package.LicenseUrl

  $markdown.Add("- **[$id]($projectUrl)** ${version}: [$license]($licenseUrl)")
}

$markdown.Insert(0, "# Third Party Notices")
$markdown.Insert(1, "Furminal uses licensed components from third parties. These are summarized below and license text is present in [LICENSES](LICENSES/).")
$markdown.Insert(2, "")

# Write file
Set-Content -Path $OutputFile -Value ($markdown | Join-String -Separator "`r`n")
Write-Host "→ $OutputFile"

Write-Host "`nLicense download and enumeration complete." -ForegroundColor Green

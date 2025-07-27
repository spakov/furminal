param(
  [Parameter(mandatory=$true)]
  [string]$Platform,
  [Parameter(mandatory=$true)]
  [string]$Configuration
)

# Configuration
$Utf8procDirectory = "..\utf8proc"
$CMake = "CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
$VsDevShell = "..\Tools\Microsoft.VisualStudio.DevShell.dll"
$CMakePresets = "CMakePresets.json"
$Out = "out"
$Build = "build"

# Check for vswhere
if (-not (Get-Command "vswhere" -ErrorAction SilentlyContinue)) {
  Write-Error "Error: 'vswhere' not found in PATH. Install it via 'winget install vswhere'."
  exit 1
}

# Check for utf8proc
if (-not (Test-Path -Path $Utf8procDirectory)) {
  Write-Error "Error: '$Utf8procDirectory' not found."
  exit 1
}

# Find the Visual Studio directory
$vsDirectory = $(vswhere -property productPath | Split-Path)
if (-not (Test-Path -Path $vsDirectory)) {
  Write-Error "Error: 'vswhere' did not locate Visual Studio."
  exit 1
}

# Check for cmake
$cmakePath = $(Join-Path -Path $vsDirectory -ChildPath $CMake)
if (-not (Test-Path -Path $cmakePath)) {
  Write-Error "Error: 'cmake' not found."
  exit 1
}

# Check for the Visual Studio dev shell
$vsDevShellPath = $(Join-Path -Path $vsDirectory -ChildPath $VsDevShell)
if (-not (Test-Path -Path $vsDevShellPath)) {
  Write-Error "Error: Visual Studio dev shell not found."
  exit 1
}

# Convert parameters to lowercase
$lowercasePlatform = $Platform.ToLower()
if ($Platform -cne $lowercasePlatform) {
  Write-Host "$Platform → $lowercasePlatform" -ForegroundColor Blue
}

$lowercaseConfiguration = $Configuration.ToLower()
if ($Configuration -cne $lowercaseConfiguration) {
  Write-Host "$Configuration → $lowercaseConfiguration" -ForegroundColor Blue
}

# Activate the Visual Studio dev shell
Import-Module $vsDevShellPath

$vsInstallPath = $(Join-Path -Path $vsDirectory -ChildPath "..\..")
$machineArch = $Env:Processor_Architecture
Enter-VsDevShell -VsInstallPath "$vsInstallPath" -Arch $machineArch -StartInPath $(Get-Location)

# Check for CMakePresets.json
if (-not (Test-Path -Path $CMakePresets)) {
  Write-Error "Error: '$CMakePresets' not found."
  exit 1
}

# Copy CMakePresets.json to the utf8proc directory
$sourceCMakePresets = $(Join-Path -Path $Utf8procDirectory -ChildPath $CMakePresets)
Copy-Item -Path $CMakePresets -Destination $Utf8procDirectory
Write-Host "✓ $sourceCMakePresets" -ForegroundColor Green

# Run cmake configure
$preset = "${lowercasePlatform}-${lowercaseConfiguration}"
$buildOutputDirectory = "$Out/$Build/${preset}"
& "$cmakePath" -S "$Utf8procDirectory" -B "$buildOutputDirectory" --preset "$preset"

if ($LASTEXITCODE -ne 0) {
  Write-Error "Error: cmake configuration failed."
  exit 1
}

# Run cmake build
& "$cmakePath" --build --preset "$preset"

if ($LASTEXITCODE -ne 0) {
  Write-Error "Error: cmake build failed."
  exit 1
}

# Remove CMakePresets.json from the utf8proc directory
Remove-Item $sourceCMakePresets
Write-Host "✗ $sourceCMakePresets" -ForegroundColor DarkRed

# Check for build output
$buildOutput = $(Join-Path -Path "$buildOutputDirectory" -ChildPath "utf8proc.dll")
if (-not (Test-Path -Path $buildOutput)) {
  Write-Error "Error: expected build output '$buildOutput', but not found."
  exit 1
}

Write-Host "✓ $buildOutput" -ForegroundColor Green

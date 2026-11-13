#requires -Version 5.1
<#
  Builds the native macOS .app bundle (minimal Avalonia shell) for one architecture.
  The bundle contains the portable engine and the early native UI. It is unsigned.
#>
param(
    [ValidateSet('osx-arm64', 'osx-x64')]
    [string]$Runtime = 'osx-arm64',
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'src\HumbleBakerStudio.Mac\HumbleBakerStudio.Mac.csproj'
$dist = Join-Path $root (Join-Path 'dist-macos' ("app-" + $Runtime))
$publish = Join-Path $dist 'publish'
$appName = 'Humble Baker Studio.app'
$app = Join-Path $dist $appName
$zip = Join-Path $root (Join-Path 'dist-macos' ("Humble-Baker-Studio-macOS-" + $Runtime + "-v0.1.0.zip"))

Write-Host "Publishing the Avalonia shell for $Runtime..." -ForegroundColor Cyan
dotnet publish $project -c $Configuration -r $Runtime --self-contained true `
    -p:IncludeNativeLibrariesForSelfExtract=true --nologo -o $publish
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

if (Test-Path -LiteralPath $app) { Remove-Item -LiteralPath $app -Recurse -Force }
$macos = Join-Path $app 'Contents\MacOS'
$resources = Join-Path $app 'Contents\Resources'
New-Item -ItemType Directory -Force -Path $macos, $resources | Out-Null
Copy-Item -Path (Join-Path $publish '*') -Destination $macos -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root 'icon.png') -Destination (Join-Path $resources 'icon.png') -Force

$plist = @'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>Humble Baker Studio</string>
  <key>CFBundleDisplayName</key><string>Humble Baker Studio</string>
  <key>CFBundleIdentifier</key><string>com.daiviey.humblebakerstudio</string>
  <key>CFBundleVersion</key><string>0.1.0</string>
  <key>CFBundleShortVersionString</key><string>0.1.0</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleExecutable</key><string>HumbleBakerStudio</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>LSApplicationCategoryType</key><string>public.app-category.music</string>
</dict>
</plist>
'@
Set-Content -LiteralPath (Join-Path $app 'Contents\Info.plist') -Value $plist -Encoding UTF8

# Zip the .app and preserve the executable bit for the main binary. macOS unzip honours the
# Unix mode stored in the ZIP external attributes.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
$archive = [System.IO.Compression.ZipFile]::Open($zip, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $app -Recurse -File) {
        $relative = $file.FullName.Substring($app.Length).TrimStart('\', '/')
        $entryName = $appName + '/' + ($relative -replace '\\', '/')
        $entry = $archive.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::Optimal)
        $mode = if ($file.Name -eq 'HumbleBakerStudio') { 0x81ED } else { 0x81A4 } # 0755 / 0644
        $entry.ExternalAttributes = $mode -shl 16
        $stream = $entry.Open()
        try {
            $bytes = [IO.File]::ReadAllBytes($file.FullName)
            $stream.Write($bytes, 0, $bytes.Length)
        } finally { $stream.Dispose() }
    }
} finally { $archive.Dispose() }

Write-Host ''
Write-Host "Built: $app" -ForegroundColor Green
Write-Host "Zip:   $zip" -ForegroundColor Green

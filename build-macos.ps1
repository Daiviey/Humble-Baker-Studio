#requires -Version 5.1
<#
  Builds the cross-platform renderer for macOS.
  This is the first shipping milestone of the native macOS port: it proves the engine,
  project load/save and render pipeline run on osx-arm64/osx-x64.
#>
param(
    [ValidateSet('osx-arm64', 'osx-x64')]
    [string]$Runtime = 'osx-arm64',
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$cli = Join-Path $root 'src\HumbleBakerStudio.Cli\HumbleBakerStudio.Cli.csproj'
$out = Join-Path $root (Join-Path 'dist-macos' $Runtime)

Write-Host "Publishing the headless renderer for $Runtime..." -ForegroundColor Cyan
dotnet publish $cli -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    --nologo -o $out
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

Write-Host ''
Write-Host "Built: $(Join-Path $out 'hbs-render')" -ForegroundColor Green
Write-Host 'Run it on the Mac with: ./hbs-render demo out.wav'

#requires -Version 5.1
<#
  Builds the C# port.

  Default output is a self-contained portable build in dist\ (no .NET install needed).
  Use -FrameworkDependent for a small build that relies on an installed .NET 8 runtime.
#>
param(
    [switch]$FrameworkDependent,
    [switch]$SkipTests,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$app = Join-Path $root 'src\HumbleBakerStudio.App\HumbleBakerStudio.App.csproj'
$dist = Join-Path $root 'dist'

if (-not $SkipTests) {
    Write-Host 'Running tests...' -ForegroundColor Cyan
    dotnet test (Join-Path $root 'HumbleBakerStudio.sln') --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed; not building.' }
}

$args = @('publish', $app, '-c', $Configuration, '-r', 'win-x64', '--nologo', '-o', $dist)
if ($FrameworkDependent) {
    $args += @('--self-contained', 'false')
} else {
    $args += @(
        '--self-contained', 'true',
        '-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true',
        '-p:EnableCompressionInSingleFile=true'
    )
}

Write-Host 'Publishing...' -ForegroundColor Cyan
dotnet @args
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

Write-Host ''
Write-Host "Built: $(Join-Path $dist 'Humble Baker Studio.exe')" -ForegroundColor Green

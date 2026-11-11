#requires -Version 5.1
# SPDX-License-Identifier: MIT
# Copyright (c) 2026 David Burr (Daiviey)
<#
  Humble Baker Studio - standalone Windows GUI build of the Audio Studio implementation.

  Dot-sources the studio implementation and launches the main window.
  Built into a standalone exe with: Invoke-ps2exe -inputFile '.\Humble Baker Studio.ps1' -outputFile '.\Humble Baker Studio.exe' -noConsole -STA -embedFiles $embed
#>
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$script:StudioRoot = $PSScriptRoot
if (-not $script:StudioRoot) { try { $script:StudioRoot = Split-Path -Parent $MyInvocation.MyCommand.Path } catch { } }
if (-not $script:StudioRoot) { try { $script:StudioRoot = Split-Path -Parent ([Diagnostics.Process]::GetCurrentProcess().MainModule.FileName) } catch { } }
if (-not $script:StudioRoot) { $script:StudioRoot = (Get-Location).Path }
$studioImplementation = Join-Path $script:StudioRoot 'AudioStudioImplementation.ps1'
if (-not (Test-Path -LiteralPath $studioImplementation -PathType Leaf)) {
    $alt = Join-Path ([IO.Path]::GetDirectoryName([Diagnostics.Process]::GetCurrentProcess().MainModule.FileName)) 'AudioStudioImplementation.ps1'
    if (Test-Path -LiteralPath $alt -PathType Leaf) { $studioImplementation = $alt }
}
if (-not (Test-Path -LiteralPath $studioImplementation -PathType Leaf)) { throw "Required studio implementation not found: $studioImplementation" }
. $studioImplementation
try { Show-AudioStudio } catch {
    $crash = "STUDIO CRASH: " + $_.Exception.ToString() + "`r`n" + $_.ScriptStackTrace
    try { [IO.File]::WriteAllText((Join-Path $env:TEMP 'hb-studio-crash.log'), $crash) } catch { }
    Write-Output $crash
    exit 1
}

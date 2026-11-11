#requires -Version 5.1
<#
  Humble Baker Studio - standalone Windows GUI build of the Audio Studio implementation.

  Dot-sources the studio implementation and launches the main window.
  Built into a standalone exe with: Invoke-ps2exe -inputFile '.\Humble Baker Studio.ps1' -outputFile '.\Humble Baker Studio.exe' -noConsole -STA -embedFiles $embed
#>
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$script:StudioRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$studioImplementation = Join-Path $script:StudioRoot 'AudioStudioImplementation.ps1'
if (-not (Test-Path -LiteralPath $studioImplementation -PathType Leaf)) { throw "Required studio implementation not found: $studioImplementation" }
. $studioImplementation
try { Show-AudioStudio } catch { Write-Output ("STUDIO CRASH: " + $_.Exception.ToString()); exit 1 }

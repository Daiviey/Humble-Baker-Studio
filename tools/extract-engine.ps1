#requires -Version 5.1
<#
  Extracts the C# engine that the PowerShell Studio passes to Add-Type and splits it
  into one file per top-level type.

  Source:  Humble Baker Studio.standalone.ps1  (the @' ... '@ block after Add-Type)
  Output:  src/HumbleBakerStudio.Audio/Engine/<Type>.cs

  This is a one-way bridge so the port starts from the proven DSP rather than a rewrite.
#>
param(
    [string]$Source = (Join-Path $PSScriptRoot '..\..\Humble-Baker-Studio\Humble Baker Studio.standalone.ps1'),
    [string]$OutDir = (Join-Path $PSScriptRoot '..\src\HumbleBakerStudio.Audio\Engine'),
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# Engine/ is the imported baseline. The C# port applies a few deliberate UI patches on top
# (see src/HumbleBakerStudio.Audio/README.md); re-extracting would discard them.
if (-not $Force -and (Test-Path -LiteralPath $OutDir) -and (Get-ChildItem -LiteralPath $OutDir -Filter '*.cs' -ErrorAction SilentlyContinue)) {
    Write-Host "Engine sources already exist in $OutDir. Pass -Force to replace them."
    exit 0
}
$lines = Get-Content -LiteralPath $Source

# The embedded engine starts at "using System;" inside the first Add-Type heredoc and
# ends at the namespace's closing brace just before the terminating '@.
$start = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -eq 'using System;' -and ($i + 14) -lt $lines.Count -and $lines[$i + 13] -eq 'namespace HbStudio {') { $start = $i; break }
}
if ($start -lt 0) { throw 'Could not locate the embedded engine block.' }

$end = -1
for ($i = $start; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -eq "'@") { $end = $i - 1; break }
}
if ($end -lt 0) { throw 'Could not locate the end of the embedded engine block.' }

# Drop the namespace wrapper: keep everything between "namespace HbStudio {" and its final "}".
$nsLine = $start + 13
$bodyStart = $nsLine + 1
$bodyEnd = $end - 1
if ($lines[$bodyEnd].Trim() -ne '}' -and $lines[$end - 1].Trim() -ne '}') {
    for ($i = $end; $i -ge $nsLine; $i--) { if ($lines[$i].Trim() -eq '}') { $bodyEnd = $i - 1; break } }
}

Write-Host "Engine block: lines $($start + 1)..$($end + 1); body $($bodyStart + 1)..$($bodyEnd + 1)"

# Character scanner that tracks strings, chars, comments and brace depth.
function Measure-Braces([string[]]$Text, [int]$From, [int]$To) {
    $depth = 0
    $result = New-Object 'System.Collections.Generic.List[int]'
    $inString = $false; $inChar = $false; $inLine = $false; $inBlock = $false; $verbatim = $false
    for ($li = $From; $li -le $To; $li++) {
        $line = $Text[$li]
        $i = 0
        while ($i -lt $line.Length) {
            $c = $line[$i]
            $next = if ($i + 1 -lt $line.Length) { $line[$i + 1] } else { [char]0 }
            if ($inLine) { break }
            if ($inBlock) { if ($c -eq '*' -and $next -eq '/') { $inBlock = $false; $i += 2; continue } ; $i++; continue }
            if ($inString) {
                if ($verbatim) {
                    if ($c -eq '"' -and $next -eq '"') { $i += 2; continue }
                    if ($c -eq '"') { $inString = $false; $verbatim = $false }
                } else {
                    if ($c -eq '\') { $i += 2; continue }
                    if ($c -eq '"') { $inString = $false }
                }
                $i++; continue
            }
            if ($inChar) { if ($c -eq '\') { $i += 2; continue }; if ($c -eq "'") { $inChar = $false }; $i++; continue }
            if ($c -eq '/' -and $next -eq '/') { $inLine = $true; break }
            if ($c -eq '/' -and $next -eq '*') { $inBlock = $true; $i += 2; continue }
            if ($c -eq '@' -and $next -eq '"') { $verbatim = $true; $inString = $true; $i += 2; continue }
            if ($c -eq '"') { $inString = $true; $i++; continue }
            if ($c -eq "'") { $inChar = $true; $i++; continue }
            if ($c -eq '{') { $depth++ }
            elseif ($c -eq '}') { $depth-- }
            $i++
        }
        $inLine = $false
        $result.Add($depth)
    }
    return , $result
}

$depths = Measure-Braces $lines $bodyStart $bodyEnd
$typeRegex = '^\s*(public|internal)\s+(?:(?:static|abstract|sealed|partial)\s+)*(?:class|struct|enum|interface|delegate)\s+([A-Za-z_][A-Za-z0-9_]*)'

# Find the start line of every top-level type (depth 0 relative to the body).
$starts = New-Object 'System.Collections.Generic.List[object]'
for ($li = $bodyStart; $li -le $bodyEnd; $li++) {
    $depthBefore = if ($li -eq $bodyStart) { 0 } else { $depths[$li - $bodyStart - 1] }
    if ($depthBefore -ne 0) { continue }
    $m = [regex]::Match($lines[$li], $typeRegex)
    if (-not $m.Success) { continue }

    # The contiguous comment or attribute block directly above the type belongs with it.
    $commentFrom = $li
    for ($k = $li - 1; $k -ge $bodyStart; $k--) {
        $t = $lines[$k].Trim()
        if ($t.StartsWith('//') -or $t.StartsWith('[') -or $t -eq '') { $commentFrom = $k } else { break }
    }
    $starts.Add([pscustomobject]@{ Line = $li; CommentFrom = $commentFrom; Name = $m.Groups[2].Value })
}
if ($starts.Count -eq 0) { throw 'No top-level types found.' }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Get-ChildItem -LiteralPath $OutDir -Filter '*.cs' -ErrorAction SilentlyContinue | Remove-Item -Force

$written = 0
for ($s = 0; $s -lt $starts.Count; $s++) {
    $from = $starts[$s].Line
    $commentFrom = $starts[$s].CommentFrom
    $to = if ($s + 1 -lt $starts.Count) { $starts[$s + 1].CommentFrom - 1 } else { $bodyEnd }

    # Trim trailing blank lines.
    $last = $to
    while ($last -gt $from -and [string]::IsNullOrWhiteSpace($lines[$last])) { $last-- }

    $file = Join-Path $OutDir ($starts[$s].Name + '.cs')
    $n = 2
    while (Test-Path -LiteralPath $file) { $file = Join-Path $OutDir ($starts[$s].Name + "_$n.cs"); $n++ }
    $body = $lines[$commentFrom..$last]
    Set-Content -LiteralPath $file -Value @('namespace HumbleBakerStudio.Audio;', '') -Encoding UTF8
    Add-Content -LiteralPath $file -Value $body -Encoding UTF8
    $written++
}

Write-Host "Wrote $written type files to $OutDir"

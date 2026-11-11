#requires -Version 5.1
<#
  SuperCollider parity harness for Humble Baker Studio.

  Part A: verifies that all 113 effect types compile to native SynthDefs.
  Part B: renders every effect offline through scsynth (NRT) and checks the audio.
  Part C: compares the shared SC DSP families with the app's FxProc on the same
          test signal and records correlation / RMS error.

  Usage:
      powershell -NoProfile -STA -ExecutionPolicy Bypass -File tests\sc-parity.ps1
      ... -Sc "C:\path\to\sclang.exe" -Out "C:\temp\sc-parity"
#>
param(
    [string]$Sc = '',
    [string]$Out = (Join-Path $env:TEMP 'hb-sc-parity'),
    [switch]$SkipRender
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$impl = Join-Path $root 'AudioStudioImplementation.ps1'
if (-not (Test-Path -LiteralPath $impl)) { throw "AudioStudioImplementation.ps1 not found next to tests folder." }
. $impl

if (-not $Sc) {
    foreach ($candidate in @(
        'Z:\LocalAI\SuperCollider\sclang.exe',
        (Join-Path $env:ProgramFiles 'SuperCollider\sclang.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'SuperCollider\sclang.exe')
    )) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) { $Sc = $candidate; break }
    }
}
if (-not $Sc -or -not (Test-Path -LiteralPath $Sc)) { throw 'sclang.exe not found. Pass -Sc <path>.' }

New-Item -ItemType Directory -Force -Path $Out | Out-Null
$Out = (Resolve-Path -LiteralPath $Out).Path
$renderDir = Join-Path $Out 'render'
New-Item -ItemType Directory -Force -Path $renderDir | Out-Null
Get-ChildItem -LiteralPath $renderDir -Filter '*.wav' -ErrorAction SilentlyContinue | Remove-Item -Force

$types = $script:StudioScEffectTypes
$expected = 113
if ($types.Count -ne $expected) { throw "Expected $expected effect types, found $($types.Count)." }

# ---------------------------------------------------------------- metrics helper
Add-Type -Language CSharp -TypeDefinition @'
using System;
public static class HbScParity {
    public static double Rms(float[] a) {
        double s = 0; for (int i = 0; i < a.Length; i++) s += (double)a[i] * a[i];
        return a.Length == 0 ? 0 : Math.Sqrt(s / a.Length);
    }
    public static double Peak(float[] a) {
        double p = 0; for (int i = 0; i < a.Length; i++) { double v = Math.Abs(a[i]); if (v > p) p = v; }
        return p;
    }
    public static int NonFinite(float[] a) {
        int n = 0; for (int i = 0; i < a.Length; i++) if (float.IsNaN(a[i]) || float.IsInfinity(a[i])) n++;
        return n;
    }
    public static double Correlation(float[] a, float[] b, int maxLag, int step) {
        double best = -1; int bestLag = 0;
        for (int lag = -maxLag; lag <= maxLag; lag += step) {
            int i0 = Math.Max(0, -lag), i1 = Math.Min(a.Length, b.Length - lag);
            if (i1 - i0 < 256) continue;
            double sa = 0, sb = 0, sab = 0, saa = 0, sbb = 0;
            for (int i = i0; i < i1; i++) {
                double x = a[i], y = b[i + lag];
                sa += x; sb += y; sab += x * y; saa += x * x; sbb += y * y;
            }
            int n = i1 - i0;
            double cov = sab - sa * sb / n;
            double va = saa - sa * sa / n, vb = sbb - sb * sb / n;
            double c = (va <= 1e-12 || vb <= 1e-12) ? 0 : cov / Math.Sqrt(va * vb);
            if (c > best) { best = c; bestLag = lag; }
        }
        return best;
    }
    public static int BestLag(float[] a, float[] b, int maxLag, int step) {
        double best = -2; int bestLag = 0;
        for (int lag = -maxLag; lag <= maxLag; lag += step) {
            int i0 = Math.Max(0, -lag), i1 = Math.Min(a.Length, b.Length - lag);
            if (i1 - i0 < 256) continue;
            double sa = 0, sb = 0, sab = 0, saa = 0, sbb = 0;
            for (int i = i0; i < i1; i++) {
                double x = a[i], y = b[i + lag];
                sa += x; sb += y; sab += x * y; saa += x * x; sbb += y * y;
            }
            int n = i1 - i0;
            double cov = sab - sa * sb / n;
            double va = saa - sa * sa / n, vb = sbb - sb * sb / n;
            double c = (va <= 1e-12 || vb <= 1e-12) ? 0 : cov / Math.Sqrt(va * vb);
            if (c > best) { best = c; bestLag = lag; }
        }
        return bestLag;
    }
}
'@ -ErrorAction Stop

function Read-WavStereo([string]$Path) {
    $b = [IO.File]::ReadAllBytes($Path)
    if ($b.Length -lt 44) { throw "Not a WAV file: $Path" }
    $pos = 12; $channels = 2; $bits = 16; $rate = 44100; $dataStart = -1; $dataLen = 0
    while ($pos + 8 -le $b.Length) {
        $id = [Text.Encoding]::ASCII.GetString($b, $pos, 4)
        $len = [BitConverter]::ToInt32($b, $pos + 4)
        if ($id -eq 'fmt ') {
            $channels = [BitConverter]::ToInt16($b, $pos + 10)
            $rate = [BitConverter]::ToInt32($b, $pos + 12)
            $bits = [BitConverter]::ToInt16($b, $pos + 22)
        } elseif ($id -eq 'data') {
            $dataStart = $pos + 8; $dataLen = $len; break
        }
        $pos += 8 + $len + ($len % 2)
    }
    if ($dataStart -lt 0) { throw "No data chunk in $Path" }
    $frames = [int]($dataLen / ($channels * ($bits / 8)))
    $l = New-Object float[] $frames
    $r = New-Object float[] $frames
    $p = $dataStart
    for ($i = 0; $i -lt $frames; $i++) {
        $l[$i] = [BitConverter]::ToInt16($b, $p) / 32768.0; $p += 2
        $r[$i] = if ($channels -gt 1) { [BitConverter]::ToInt16($b, $p) / 32768.0 } else { $l[$i] }
        if ($channels -gt 1) { $p += 2 }
    }
    return @{ L = $l; R = $r; Rate = $rate; Channels = $channels; Frames = $frames }
}

# ---------------------------------------------------------------- Part A: compile defs
$effectText = Get-StudioScEffectText
$effectFile = Join-Path $Out 'hb-effects.scd'
[IO.File]::WriteAllText($effectFile, $effectText, (New-Object Text.UTF8Encoding($false)))

$verifySc = Join-Path $Out 'verify.scd'
$fxPath = $effectFile.Replace('\', '/')
$typeList = ($types | ForEach-Object { '\' + $_ }) -join ', '
$verifyCode = @"
File.readAllString("$fxPath").interpret;
~want = [$typeList];
~have = SynthDescLib.global.synthDescs.keys.asArray;
~missing = ~want.select({ |t| ~have.includes(("hb_" ++ t).asSymbol).not });
("TYPES=" ++ ~want.size).postln;
("DEFS=" ++ ~have.select({ |k| k.asString.beginsWith("hb_") }).size).postln;
("MISSING=" ++ ~missing.size ++ " " ++ ~missing).postln;
0.exit;
"@
[IO.File]::WriteAllText($verifySc, $verifyCode, (New-Object Text.UTF8Encoding($false)))
$verifyOut = Join-Path $Out 'verify.out'
$vp = Start-Process -FilePath $Sc -ArgumentList ('-D "' + $verifySc + '"') -PassThru -WindowStyle Hidden -RedirectStandardOutput $verifyOut -RedirectStandardError ($verifyOut + '.err')
if (-not $vp.WaitForExit(120000)) { try { $vp.Kill() } catch { } }
$verifyText = Get-Content -LiteralPath $verifyOut -Raw
$missingMatch = [regex]::Match($verifyText, 'MISSING=(\d+)')
$defsMatch = [regex]::Match($verifyText, 'DEFS=(\d+)')
$typeMatch = [regex]::Match($verifyText, 'TYPES=(\d+)')
if (-not $defsMatch.Success) { throw "SuperCollider did not compile the effect library.`n$verifyText" }
$defCount = [int]$defsMatch.Groups[1].Value
$missing = if ($missingMatch.Success) { [int]$missingMatch.Groups[1].Value } else { -1 }
Write-Host ("Part A: {0} types, {1} native defs, {2} missing" -f $typeMatch.Groups[1].Value, $defCount, $missing)
if ($defCount -lt ($expected + 2) -or $missing -ne 0) { throw "Effect definition library is incomplete." }

# ---------------------------------------------------------------- Part B: NRT render
$results = New-Object Collections.Generic.List[object]
if (-not $SkipRender) {
    $renderSc = Join-Path $Out 'render-all.scd'
    $renderDirFwd = $renderDir.Replace('\', '/')
    $renderCode = @"
(
File.readAllString("$fxPath").interpret;
SynthDef(\hbsrc, { |out = 0|
    Out.ar(out, [SinOsc.ar(220) * 0.3, SinOsc.ar(330) * 0.3])
}).add;
~srcDef = SynthDescLib.global.at(\hbsrc).def;
~types = [$typeList];
~renderOne = { |i|
    if(i >= ~types.size) {
        "ALLDONE".postln;
        0.exit;
    } {
        var type = ~types[i];
        var sd = SynthDescLib.global.at(("hb_" ++ type).asSymbol);
        var score = Score([
            [0.0, [\d_recv, ~srcDef.asBytes]],
            [0.0, [\d_recv, sd.def.asBytes]],
            [0.01, [\s_new, \hbsrc, 1000, 1, 0, \out, 16]],
            [0.01, [\s_new, ("hb_" ++ type).asSymbol, 1001, 1, 0, \in, 16, \out, 0, \mix, 1]],
            [0.80, [\c_set, 0, 0]]
        ]);
        score.recordNRT(
            outputFilePath: "$renderDirFwd/" ++ type ++ ".wav",
            sampleRate: 44100,
            headerFormat: "WAV",
            sampleFormat: "int16",
            duration: 0.80,
            action: { |res|
                ("RENDER " ++ type ++ " " ++ res).postln;
                ~renderOne.(i + 1);
            });
    };
};
~renderOne.(0);
)
"@
    [IO.File]::WriteAllText($renderSc, $renderCode, (New-Object Text.UTF8Encoding($false)))
    $renderOut = Join-Path $Out 'render.out'
    $rp = Start-Process -FilePath $Sc -ArgumentList ('-D "' + $renderSc + '"') -PassThru -WindowStyle Hidden -RedirectStandardOutput $renderOut -RedirectStandardError ($renderOut + '.err')
    $deadline = (Get-Date).AddMinutes(12)
    $done = $false
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
        if (Test-Path -LiteralPath $renderOut) {
            $t = Get-Content -LiteralPath $renderOut -Raw -ErrorAction SilentlyContinue
            if ($t -match 'ALLDONE') { $done = $true; break }
            if ($t -match 'ERROR') { break }
        }
        if ($rp.HasExited) { break }
    }
    if (-not $rp.HasExited) { try { $rp.Kill() } catch { } }
    if (-not $done) {
        $tail = if (Test-Path $renderOut) { Get-Content $renderOut -Tail 20 | Out-String } else { '' }
        throw "NRT render did not finish.`n$tail"
    }
    Write-Host "Part B: rendered $($types.Count) effects."

    foreach ($t in $types) {
        $wav = Join-Path $renderDir ($t + '.wav')
        if (-not (Test-Path -LiteralPath $wav)) {
            $results.Add([pscustomobject]@{ Type = $t; Status = 'missing'; Rms = 0; Peak = 0; Correlation = 0; Lag = 0 })
            continue
        }
        $w = Read-WavStereo $wav
        $rms = [math]::Sqrt(([HbScParity]::Rms($w.L) * [HbScParity]::Rms($w.L) + [HbScParity]::Rms($w.R) * [HbScParity]::Rms($w.R)) / 2)
        $peak = [math]::Max([HbScParity]::Peak($w.L), [HbScParity]::Peak($w.R))
        $bad = [HbScParity]::NonFinite($w.L) + [HbScParity]::NonFinite($w.R)
        $status = if ($bad -gt 0) { 'non-finite' } elseif ($peak -lt 1e-5) { 'silent' } elseif ($peak -gt 4) { 'hot' } else { 'ok' }
        $results.Add([pscustomobject]@{ Type = $t; Status = $status; Rms = [math]::Round($rms, 5); Peak = [math]::Round($peak, 4); Correlation = 0; Lag = 0 })
    }
}

# ---------------------------------------------------------------- Part C: family parity
# Match the app FxProc's primary stage to the shared SC family and measure the
# waveform correlation. These families are deliberately approximate, so the
# harness records the number rather than asserting sample equality.
function New-ParitySignal([int]$N, [int]$Rate = 44100) {
    $l = New-Object float[] $N; $r = New-Object float[] $N
    $w1 = 2 * [math]::PI * 220 / $Rate; $w2 = 2 * [math]::PI * 330 / $Rate
    for ($i = 0; $i -lt $N; $i++) {
        $l[$i] = [float](0.3 * [math]::Sin($w1 * $i))
        $r[$i] = [float](0.3 * [math]::Sin($w2 * $i))
    }
    return @{ L = $l; R = $r }
}

$familySamples = @(
    @{ Family = 'hb_filter'; Type = 'filter'; Setup = { param($f) $f.Filter = [HbStudio.FilterMode]::lowpass; $f.Cutoff = 1200; $f.Resonance = 0.3 } },
    @{ Family = 'hb_drive'; Type = 'drive'; Setup = { param($f) $f.Drive = 0.4; $f.Cutoff = 6000 } },
    @{ Family = 'hb_crush'; Type = 'crush'; Setup = { param($f) $f.Bits = 8; $f.Downsample = 4 } },
    @{ Family = 'hb_delay'; Type = 'delay'; Setup = { param($f) $f.DelayMix = 1; $f.DelayBeats = 0.75; $f.DelayFeedback = 0.4 } },
    @{ Family = 'hb_reverb'; Type = 'reverb'; Setup = { param($f) $f.ReverbMix = 1; $f.ReverbSize = 0.6 } },
    @{ Family = 'hb_comp'; Type = 'comp'; Setup = { param($f) $f.CompRatio = 3; $f.CompThresh = -18 } },
    @{ Family = 'hb_eq'; Type = 'eq'; Setup = { param($f) $f.LowShelf = 6; $f.HighShelf = 6 } },
    @{ Family = 'hb_width'; Type = 'width'; Setup = { param($f) $f.Width = 0.5 } }
)

$parity = New-Object Collections.Generic.List[object]
if (-not $SkipRender) {
    foreach ($fs in $familySamples) {
        $wav = Join-Path $renderDir ($fs.Type + '.wav')
        if (-not (Test-Path -LiteralPath $wav)) { continue }
        $w = Read-WavStereo $wav
        $sig = New-ParitySignal $w.Frames
        $fx = New-Object HbStudio.Fx
        & $fs.Setup $fx
        $al = New-Object float[] $w.Frames; $ar = New-Object float[] $w.Frames
        [Array]::Copy($sig.L, $al, $w.Frames); [Array]::Copy($sig.R, $ar, $w.Frames)
        [HbStudio.FxProc]::Process($fx, $al, $ar, $false, 0.5)
        $corr = [HbScParity]::Correlation($al, $w.L, 2048, 1)
        $lag = [HbScParity]::BestLag($al, $w.L, 2048, 1)
        $appRms = [math]::Sqrt(([HbScParity]::Rms($al) * [HbScParity]::Rms($al) + [HbScParity]::Rms($ar) * [HbScParity]::Rms($ar)) / 2)
        $scRms = [math]::Sqrt(([HbScParity]::Rms($w.L) * [HbScParity]::Rms($w.L) + [HbScParity]::Rms($w.R) * [HbScParity]::Rms($w.R)) / 2)
        $parity.Add([pscustomobject]@{
            Family = $fs.Family; Type = $fs.Type; Correlation = [math]::Round($corr, 4); Lag = $lag
            AppRms = [math]::Round($appRms, 5); ScRms = [math]::Round($scRms, 5)
        })
    }
}

# ---------------------------------------------------------------- report
$badResults = @($results | Where-Object { $_.Status -ne 'ok' })
$reportCsv = Join-Path $Out 'parity-report.csv'
$parityCsv = Join-Path $Out 'family-parity.csv'
if ($results.Count -gt 0) { $results | Export-Csv -LiteralPath $reportCsv -NoTypeInformation -Encoding UTF8 }
if ($parity.Count -gt 0) { $parity | Export-Csv -LiteralPath $parityCsv -NoTypeInformation -Encoding UTF8 }

Write-Host ""
Write-Host "Effect render results:"
if ($results.Count -gt 0) {
    $results | Format-Table -AutoSize | Out-String | Write-Host
} else {
    Write-Host "  (skipped)"
}
if ($parity.Count -gt 0) {
    Write-Host "Family parity vs app FxProc (approximate families):"
    $parity | Format-Table -AutoSize | Out-String | Write-Host
}
Write-Host ("Report: {0}" -f $reportCsv)
Write-Host ("Family parity: {0}" -f $parityCsv)

if ($badResults.Count -gt 0) {
    Write-Host ("FAILED: {0} effect(s) did not render clean audio." -f $badResults.Count) -ForegroundColor Red
    exit 1
}
Write-Host "PASS: all effects compiled and rendered clean audio." -ForegroundColor Green
exit 0

$ErrorActionPreference = "Stop"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$srcDir = $PSScriptRoot
$outExe = Join-Path $srcDir "LiteClip.exe"
$icoFile = Join-Path $srcDir "LiteClip.ico"

Write-Host "Building LiteClip Studio (x64, unsafe, optimized)..." -ForegroundColor Cyan

Get-Process -Name "LiteClip" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 120

$icoArg = if (Test-Path $icoFile) { "/win32icon:$icoFile" } else { $null }

& $csc /nologo /target:winexe /platform:x64 /optimize+ /unsafe `
    $icoArg `
    /out:$outExe `
    /r:System.dll `
    /r:System.Core.dll `
    /r:System.Drawing.dll `
    /r:System.Windows.Forms.dll `
    (Join-Path $srcDir "Program.cs") `
    (Join-Path $srcDir "ConfigAndPresets.cs") `
    (Join-Path $srcDir "NativeMediaFoundation.cs") `
    (Join-Path $srcDir "WasapiAudioMixer.cs") `
    (Join-Path $srcDir "CaptureEngine.cs") `
    (Join-Path $srcDir "RecorderCore.cs") `
    (Join-Path $srcDir "MainForm.cs")

if ($LASTEXITCODE -eq 0) {
    $sizeKb = [math]::Round((Get-Item $outExe).Length / 1KB, 1)
    Write-Host "Build succeeded: $outExe ($sizeKb KB)" -ForegroundColor Green
} else {
    Write-Error "Build failed with exit code $LASTEXITCODE"
}

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDir = Join-Path $root "src"
$buildDir = Join-Path $root "build"
$output = Join-Path $buildDir "NestsOS.exe"
$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir | Out-Null
}

$sources = Get-ChildItem -Path $sourceDir -Filter *.cs | ForEach-Object { $_.FullName }

& $compiler /nologo /target:winexe /out:$output /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll $sources

Write-Host "Built $output"

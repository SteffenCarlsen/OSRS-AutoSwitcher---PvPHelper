#requires -Version 7
<#
.SYNOPSIS
    Publishes the two release flavours of OSRS PvP Helper into ./dist.

.DESCRIPTION
    portable : self-contained single-file .exe. Runs on any Windows 10/11 x64 machine with
               NO .NET install required. Larger download (single-file, compressed).
    lite     : framework-dependent single-file .exe. Needs the ".NET Desktop Runtime 10"
               installed (https://dotnet.microsoft.com/download/dotnet/10.0). Much smaller.

    Both builds drop debug symbols, ship English resources only and disable ICU
    (InvariantGlobalization) — set in the .csproj — to keep them lean.

.EXAMPLE
    pwsh build/publish.ps1                 # build both
    pwsh build/publish.ps1 -Variant lite   # only the small one
    pwsh build/publish.ps1 -Trim           # experimental: trim the portable build even smaller
#>
[CmdletBinding()]
param(
    [ValidateSet('both', 'portable', 'lite')]
    [string]$Variant = 'both',
    [string]$Runtime = 'win-x64',
    [switch]$Trim
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $repo 'src/PvPHelper.App/PvPHelper.App.csproj'
$dist = Join-Path $repo 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null

function Publish-Variant {
    param(
        [string]$Name,
        [bool]$SelfContained,
        [string[]]$ExtraArgs
    )

    $outDir = Join-Path $repo "artifacts/publish/$Name"
    if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }

    Write-Host "==> Publishing '$Name'  ($Runtime, self-contained=$SelfContained)" -ForegroundColor Cyan

    $publishArgs = @(
        'publish', $proj,
        '-c', 'Release',
        '-r', $Runtime,
        "--self-contained=$($SelfContained.ToString().ToLower())",
        '-p:PublishSingleFile=true',
        '-p:DebugType=none',
        '-p:DebugSymbols=false',
        '-o', $outDir
    ) + $ExtraArgs

    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for '$Name' (exit $LASTEXITCODE)." }

    $exe = Join-Path $outDir 'OSRS-PvPHelper.exe'
    if (-not (Test-Path $exe)) { throw "Expected output not found: $exe" }

    $dest = Join-Path $dist "OSRS-PvPHelper-$Name-$Runtime.exe"
    Copy-Item -Force $exe $dest
    $mb = [math]::Round((Get-Item $dest).Length / 1MB, 1)
    Write-Host "    -> $dest  ($mb MB)`n" -ForegroundColor Green
}

if ($Variant -in @('both', 'portable')) {
    $extra = @('-p:EnableCompressionInSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true')
    if ($Trim) {
        Write-Host "NOTE: trimming is experimental for Avalonia — smoke-test the .exe before shipping.`n" -ForegroundColor Yellow
        $extra += '-p:PublishTrimmed=true'
    }
    Publish-Variant -Name 'portable' -SelfContained $true -ExtraArgs $extra
}

if ($Variant -in @('both', 'lite')) {
    Publish-Variant -Name 'lite' -SelfContained $false -ExtraArgs @()
}

Write-Host "Done. Artifacts are in $dist" -ForegroundColor Cyan

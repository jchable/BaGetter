<#
.SYNOPSIS
    Migre tous les packages d'un feed MyGet vers une instance BaGetter.

.DESCRIPTION
    Ce script liste toutes les versions disponibles sur un feed MyGet (v2),
    télécharge chaque .nupkg, et le pousse vers BaGetter via l'API NuGet.
    Les erreurs sont loguées dans un fichier pour permettre une reprise partielle.

.PARAMETER MyGetFeedUrl
    URL du feed MyGet (format v2). Exemple : https://www.myget.org/F/MON-FEED/api/v2

.PARAMETER MyGetApiKey
    Clé API MyGet (nécessaire pour les feeds privés).

.PARAMETER BaGetterUrl
    URL du service index BaGetter. Exemple : http://localhost:5000/v3/index.json

.PARAMETER BaGetterApiKey
    Clé API BaGetter configurée dans appsettings.json.

.PARAMETER TempDir
    Dossier temporaire pour stocker les .nupkg téléchargés.
    Par défaut : %TEMP%\myget-migration

.PARAMETER ReplayErrors
    Si spécifié, rejoue uniquement les packages listés dans errors.log
    (utile après une migration partielle).

.EXAMPLE
    .\migrate-myget.ps1 `
        -MyGetFeedUrl  "https://www.myget.org/F/MON-FEED/api/v2" `
        -MyGetApiKey   "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" `
        -BaGetterUrl   "http://localhost:5000/v3/index.json" `
        -BaGetterApiKey "MA_CLE_BAGETTER"

.EXAMPLE
    # Rejouer uniquement les échecs
    .\migrate-myget.ps1 `
        -MyGetFeedUrl  "https://www.myget.org/F/MON-FEED/api/v2" `
        -MyGetApiKey   "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" `
        -BaGetterUrl   "http://localhost:5000/v3/index.json" `
        -BaGetterApiKey "MA_CLE_BAGETTER" `
        -ReplayErrors
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$MyGetFeedUrl,

    [Parameter(Mandatory = $true)]
    [string]$MyGetApiKey,

    [Parameter(Mandatory = $true)]
    [string]$BaGetterUrl,

    [Parameter(Mandatory = $true)]
    [string]$BaGetterApiKey,

    [string]$TempDir = "$env:TEMP\myget-migration",

    [switch]$ReplayErrors
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

# ── Helpers ──────────────────────────────────────────────────────────────────

function Write-Step([string]$msg) {
    Write-Host "`n==> $msg" -ForegroundColor Cyan
}

function Write-Ok([string]$msg) {
    Write-Host "    OK  $msg" -ForegroundColor Green
}

function Write-Fail([string]$msg) {
    Write-Host "    ERR $msg" -ForegroundColor Red
}

# ── Init ─────────────────────────────────────────────────────────────────────

New-Item -ItemType Directory -Force -Path $TempDir | Out-Null
$errorLog   = Join-Path $TempDir "errors.log"
$successLog = Join-Path $TempDir "success.log"

# Vérifier que nuget.exe est disponible
if (-not (Get-Command nuget -ErrorAction SilentlyContinue)) {
    Write-Error "nuget.exe introuvable. Téléchargez-le depuis https://www.nuget.org/downloads et ajoutez-le au PATH."
    exit 1
}

# ── Lister les packages ───────────────────────────────────────────────────────

if ($ReplayErrors) {
    if (-not (Test-Path $errorLog)) {
        Write-Error "Aucun fichier errors.log trouvé dans $TempDir"
        exit 1
    }
    Write-Step "Mode reprise — lecture de $errorLog"
    # Format du log : "Id Version: message erreur" — on extrait Id et Version
    $lines = Get-Content $errorLog | ForEach-Object {
        if ($_ -match '^(.+?) (.+?):') { "$($Matches[1]) $($Matches[2])" }
    } | Where-Object { $_ }
}
else {
    Write-Step "Listage des packages depuis MyGet..."
    $lines = & nuget list -AllVersions -PreRelease -Source $MyGetFeedUrl -ApiKey $MyGetApiKey 2>&1 |
             Where-Object { $_ -match '^\S+ \S+$' }  # filtre les lignes "Id Version"

    if (-not $lines) {
        Write-Warning "Aucun package trouvé. Vérifiez MyGetFeedUrl et MyGetApiKey."
        exit 0
    }

    # Réinitialiser les logs si on repart de zéro
    Remove-Item $errorLog   -ErrorAction SilentlyContinue
    Remove-Item $successLog -ErrorAction SilentlyContinue
}

$total = @($lines).Count
Write-Host "  $total version(s) à migrer." -ForegroundColor Yellow

# ── Migration ─────────────────────────────────────────────────────────────────

Write-Step "Démarrage de la migration..."

$success = 0
$failed  = 0
$i       = 0

foreach ($line in $lines) {
    $i++
    $parts   = $line -split ' '
    $id      = $parts[0]
    $version = $parts[1]
    $label   = "[$i/$total] $id $version"

    Write-Host "  $label" -NoNewline

    try {
        # 1. Télécharger depuis MyGet
        $installDir = Join-Path $TempDir "dl\$id.$version"
        New-Item -ItemType Directory -Force -Path $installDir | Out-Null

        $nugetOut = & nuget install $id `
            -Version $version `
            -Source $MyGetFeedUrl `
            -ApiKey $MyGetApiKey `
            -OutputDirectory $installDir `
            -NoCache `
            -NonInteractive `
            -DirectDownload 2>&1

        # 2. Trouver le .nupkg
        $nupkg = Get-ChildItem -Path $installDir -Filter "$id.$version.nupkg" -Recurse |
                 Select-Object -First 1

        if (-not $nupkg) {
            # Fallback : nom avec casse différente
            $nupkg = Get-ChildItem -Path $installDir -Filter "*.nupkg" -Recurse |
                     Where-Object { $_.Name -ieq "$id.$version.nupkg" } |
                     Select-Object -First 1
        }

        if (-not $nupkg) {
            throw "Fichier .nupkg introuvable après téléchargement dans $installDir"
        }

        # 3. Pousser vers BaGetter
        $pushOut = & nuget push $nupkg.FullName `
            -Source $BaGetterUrl `
            -ApiKey $BaGetterApiKey `
            -SkipDuplicate 2>&1

        Write-Ok ""
        Add-Content -Path $successLog -Value "$id $version"
        $success++

        # Nettoyer le dossier de téléchargement pour économiser l'espace
        Remove-Item $installDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    catch {
        Write-Fail ""
        Write-Fail "    $_"
        Add-Content -Path $errorLog -Value "$id ${version}: $_"
        $failed++
    }
}

# ── Résumé ────────────────────────────────────────────────────────────────────

Write-Step "Migration terminée"
Write-Host "  Succès  : $success" -ForegroundColor Green
Write-Host "  Échecs  : $failed"  -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
Write-Host "  Logs    : $TempDir"

if ($failed -gt 0) {
    Write-Host "`nPour rejouer les échecs :" -ForegroundColor Yellow
    Write-Host "  .\migrate-myget.ps1 -MyGetFeedUrl `"$MyGetFeedUrl`" -MyGetApiKey `"...`" -BaGetterUrl `"$BaGetterUrl`" -BaGetterApiKey `"...`" -ReplayErrors"
}

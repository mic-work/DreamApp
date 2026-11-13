<#
.SYNOPSIS
    Downloads a consistent backup of the DreamApp database and photos from the server.

.DESCRIPTION
    On the server it makes a safe copy of the SQLite database (sqlite3 .backup, checked with
    integrity_check) and packs the uploaded photos, then downloads both to your PC:
      <OutDir>\dreamapp-backup-YYYYMMDD-HHMMSS\dreamapp.db
      <OutDir>\dreamapp-backup-YYYYMMDD-HHMMSS\uploads.tgz
    The backup contains real user data: it is kept OUTSIDE the repository on purpose.

.PARAMETER Server
    Public IP (or DuckDNS name) of the server.

.PARAMETER Key
    Path of the private SSH key downloaded when the VM was created.

.PARAMETER User
    SSH user of the VM. Default: ubuntu.

.PARAMETER OutDir
    Where backups are stored. Default: DreamAppBackups in your user folder.

.PARAMETER Keep
    How many of the most recent backups to keep on this PC (older ones are removed). Default: 10.

.EXAMPLE
    .\backup.ps1 -Server 129.152.10.20 -Key C:\Users\Michele\.ssh\oracle.key
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Server,

    [Parameter(Mandatory)]
    [string]$Key,

    [string]$User = 'ubuntu',

    [string]$OutDir = (Join-Path $env:USERPROFILE 'DreamAppBackups'),

    [ValidateRange(1, 1000)]
    [int]$Keep = 10
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Step([string]$Message) {
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Assert-Ok([string]$What) {
    if ($LASTEXITCODE -ne 0) { throw "$What non riuscito (codice $LASTEXITCODE)." }
}

foreach ($tool in 'ssh', 'scp') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "$tool non trovato: attiva 'Client OpenSSH' nelle Funzionalita facoltative di Windows."
    }
}
if (-not (Test-Path $Key)) { throw "Chiave SSH non trovata: $Key" }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$target = "${User}@${Server}"
$sshOpts = @('-i', $Key, '-o', 'StrictHostKeyChecking=accept-new')
$remoteDir = "/tmp/dreamapp-backup-$stamp"

# Script run on the server (LF line endings, written to a temp file and copied over).
$remoteScript = @'
set -euo pipefail
STAMP="$1"
DB=/var/lib/dreamapp/dreamapp.db
OUT="/tmp/dreamapp-backup-$STAMP"

command -v sqlite3 >/dev/null 2>&1 || sudo apt-get install -y sqlite3 >/dev/null

mkdir -p "$OUT"
sudo sqlite3 "$DB" ".backup '$OUT/dreamapp.db'"
RESULT="$(sudo sqlite3 "$OUT/dreamapp.db" 'PRAGMA integrity_check;')"
if [ "$RESULT" != "ok" ]; then
  echo "Controllo di integrita del database fallito: $RESULT" >&2
  exit 1
fi

if [ -d /var/lib/dreamapp/uploads ]; then
  sudo tar czf "$OUT/uploads.tgz" -C /var/lib/dreamapp uploads
else
  tar czf "$OUT/uploads.tgz" --files-from /dev/null
fi
sudo chown -R "$(id -un):$(id -gn)" "$OUT"
echo "Backup pronto sul server: $OUT"
'@
$localScript = Join-Path ([System.IO.Path]::GetTempPath()) "dreamapp-backup-$stamp.sh"
[System.IO.File]::WriteAllText($localScript, ($remoteScript -replace "`r`n", "`n"), (New-Object System.Text.UTF8Encoding($false)))

try {
    Write-Step "Creazione del backup sul server ($target)"
    scp @sshOpts $localScript "${target}:/tmp/dreamapp-backup.sh"
    Assert-Ok 'Copia dello script sul server'
    ssh @sshOpts $target "bash /tmp/dreamapp-backup.sh $stamp"
    Assert-Ok 'Creazione del backup sul server'

    Write-Step "Download in $OutDir"
    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
    scp @sshOpts -r "${target}:${remoteDir}" $OutDir
    Assert-Ok 'Download del backup'

    ssh @sshOpts $target "rm -rf $remoteDir /tmp/dreamapp-backup.sh"
}
finally {
    Remove-Item -LiteralPath $localScript -ErrorAction SilentlyContinue
}

$backupDir = Join-Path $OutDir "dreamapp-backup-$stamp"
$dbFile = Join-Path $backupDir 'dreamapp.db'
$photos = Join-Path $backupDir 'uploads.tgz'
if (-not (Test-Path $dbFile) -or -not (Test-Path $photos)) {
    throw "Il backup scaricato e incompleto: controlla $backupDir"
}
if ((Get-Item $dbFile).Length -eq 0) { throw "Il database scaricato e vuoto: $dbFile" }

# Keep only the most recent backups (only folders created by this script are touched).
Get-ChildItem -Path $OutDir -Directory |
    Where-Object { $_.Name -match '^dreamapp-backup-\d{8}-\d{6}$' } |
    Sort-Object Name -Descending |
    Select-Object -Skip $Keep |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }

Write-Host ''
Write-Host 'Backup completato.' -ForegroundColor Green
Write-Host "  Database : $dbFile ($([math]::Round((Get-Item $dbFile).Length / 1KB)) KB)"
Write-Host "  Foto     : $photos ($([math]::Round((Get-Item $photos).Length / 1KB)) KB)"

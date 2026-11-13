<#
.SYNOPSIS
    Publishes the DreamApp backend for Linux and collects everything needed
    to put it online in bin\publish-release.

.DESCRIPTION
    Result:
      bin\publish-release\app\      the published, self-contained backend (nothing to install on the server)
      bin\publish-release\deploy\   ready-to-edit service file, env file, Caddyfile and a short README

.PARAMETER Runtime
    linux-arm64 (Oracle Ampere A1 VM, default) or linux-x64 (Oracle AMD Micro VM).

.PARAMETER SkipTests
    By default the backend tests run first and a failure stops the build. Use this to skip them.

.PARAMETER Domain
    Public host name of the backend. Default: taken from apiUrl in
    frontend\dreamapp-web\src\environments\environment.prod.ts.

.PARAMETER FrontendOrigin
    Origin of the frontend (GitHub Pages): scheme + host only. Default: derived
    from the git remote (https://github.com/<user>/<repo> -> https://<user>.github.io).

.EXAMPLE
    .\buildscript.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('linux-arm64', 'linux-x64')]
    [string]$Runtime = 'linux-arm64',

    [switch]$SkipTests,

    [string]$Domain,

    [string]$FrontendOrigin
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Step([string]$Message) {
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

# Files for the Linux server must use LF line endings (a CRLF would end up inside the values of the env file).
function Write-LinuxFile([string]$Path, [string]$Text) {
    $lf = $Text -replace "`r`n", "`n"
    [System.IO.File]::WriteAllText($Path, $lf, (New-Object System.Text.UTF8Encoding($false)))
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet non trovato: installa il .NET 8 SDK (https://dotnet.microsoft.com/download/dotnet/8.0) e riprova.'
}

$project = Join-Path $PSScriptRoot 'DreamApp.Api.csproj'
if (-not (Test-Path $project)) {
    throw "Non trovo DreamApp.Api.csproj accanto a questo script ($PSScriptRoot)."
}

# ---------------------------------------------------------------- automatic settings
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')

if (-not $Domain) {
    $prodEnv = Join-Path $repoRoot 'frontend\dreamapp-web\src\environments\environment.prod.ts'
    if (Test-Path $prodEnv) {
        $match = [regex]::Match((Get-Content $prodEnv -Raw), "apiUrl:\s*'https?://([^/']+)")
        if ($match.Success -and $match.Groups[1].Value -notmatch 'REPLACE_WITH') {
            $Domain = $match.Groups[1].Value
        }
    }
    if (-not $Domain) {
        $Domain = 'IL-TUO-NOME.duckdns.org'
        Write-Warning 'Dominio del backend non impostato in environment.prod.ts: nei file generati trovi un segnaposto (IL-TUO-NOME.duckdns.org).'
    }
}

if (-not $FrontendOrigin) {
    $remote = ''
    if (Get-Command git -ErrorAction SilentlyContinue) {
        $remote = (git -C $repoRoot remote get-url origin 2>$null | Out-String).Trim()
    }
    $match = [regex]::Match($remote, 'github\.com[:/]([^/]+)/')
    if ($match.Success) {
        $FrontendOrigin = "https://$($match.Groups[1].Value.ToLower()).github.io"
    } else {
        $FrontendOrigin = 'https://TUO-USERNAME.github.io'
        Write-Warning 'Non riesco a ricavare lo username GitHub dal remote git: nei file generati trovi un segnaposto per l''origine CORS.'
    }
}

Write-Host "Backend   : $Domain"
Write-Host "Frontend  : $FrontendOrigin"
Write-Host "Runtime   : $Runtime"

$outRoot = Join-Path $PSScriptRoot 'bin\publish-release'
$appDir = Join-Path $outRoot 'app'
$deployDir = Join-Path $outRoot 'deploy'

# ---------------------------------------------------------------- tests
if (-not $SkipTests) {
    Write-Step 'Test del backend'
    $testProject = Join-Path $PSScriptRoot '..\DreamApp.Api.Tests\DreamApp.Api.Tests.csproj'
    dotnet test $testProject -c Release
    if ($LASTEXITCODE -ne 0) {
        throw 'I test non sono passati: pubblicazione annullata.'
    }
}

# ---------------------------------------------------------------- clean
Write-Step "Pulizia di $outRoot"
if (Test-Path $outRoot) {
    Remove-Item -Path $outRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $appDir -Force | Out-Null
New-Item -ItemType Directory -Path $deployDir -Force | Out-Null

# ---------------------------------------------------------------- publish
Write-Step "Pubblicazione ($Runtime, self-contained)"
dotnet publish $project -c Release -r $Runtime --self-contained true -p:DebugType=None -p:DebugSymbols=false -o $appDir
if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish e fallito.'
}

# Never ship development settings or local data.
Remove-Item -Path (Join-Path $appDir 'appsettings.Development.json') -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $appDir 'App_Data') -Recurse -Force -ErrorAction SilentlyContinue

if (-not (Test-Path (Join-Path $appDir 'DreamApp.Api'))) {
    throw 'Pubblicazione incompleta: manca il file eseguibile DreamApp.Api.'
}

# ---------------------------------------------------------------- deploy helpers
Write-Step 'Creazione dei file di configurazione per il server'

$service = @'
[Unit]
Description=DreamApp API
After=network.target

[Service]
WorkingDirectory=/opt/dreamapp
ExecStart=/opt/dreamapp/DreamApp.Api
EnvironmentFile=/etc/dreamapp.env
User=www-data
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
'@
Write-LinuxFile (Join-Path $deployDir 'dreamapp.service') $service

$envFile = @"
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5000
DATABASE_PATH=/var/lib/dreamapp/dreamapp.db
IMAGES_PATH=/var/lib/dreamapp/uploads
Jwt__Key=INCOLLA_QUI_LA_CHIAVE_GENERATA_CON_openssl_rand_-base64_48
Cors__AllowedOrigins__0=$FrontendOrigin
"@
Write-LinuxFile (Join-Path $deployDir 'dreamapp.env.example') $envFile

$caddy = @"
$Domain {
    reverse_proxy 127.0.0.1:5000
}
"@
Write-LinuxFile (Join-Path $deployDir 'Caddyfile.example') $caddy

$readme = @"
DREAMAPP - PUBBLICAZIONE SUL SERVER ($Runtime)
================================================

Contenuto di questa cartella
  app\      il backend pubblicato (self-contained: sul server non serve installare .NET)
  deploy\   dreamapp.service, dreamapp.env.example, Caddyfile.example

PRIMA VOLTA
-----------
1) Dal tuo PC (PowerShell), carica i file sulla VM:
     ssh -i CHIAVE.key ubuntu@IP_DELLA_VM "mkdir -p ~/publish ~/deploy"
     scp -i CHIAVE.key -r app\* ubuntu@IP_DELLA_VM:~/publish/
     scp -i CHIAVE.key deploy\* ubuntu@IP_DELLA_VM:~/deploy/

2) Sulla VM:
     sudo mkdir -p /opt/dreamapp /var/lib/dreamapp/uploads
     sudo cp -r ~/publish/* /opt/dreamapp/
     sudo chmod +x /opt/dreamapp/DreamApp.Api
     sudo chown -R www-data:www-data /var/lib/dreamapp

     openssl rand -base64 48                      # genera la chiave segreta dei login
     sudo cp ~/deploy/dreamapp.env.example /etc/dreamapp.env
     sudo nano /etc/dreamapp.env                  # incolla la chiave in Jwt__Key
     sudo chmod 600 /etc/dreamapp.env

     sudo cp ~/deploy/dreamapp.service /etc/systemd/system/dreamapp.service
     sudo systemctl daemon-reload
     sudo systemctl enable --now dreamapp
     sudo systemctl status dreamapp

     sudo cp ~/deploy/Caddyfile.example /etc/caddy/Caddyfile
     sudo systemctl reload caddy

3) Prova: apri https://$Domain/api/auth/me  (deve rispondere 401).

AGGIORNAMENTI SUCCESSIVI
------------------------
Rilancia .\buildscript.ps1, poi:
     scp -i CHIAVE.key -r app\* ubuntu@IP_DELLA_VM:~/publish/
     (sulla VM)
     sudo systemctl stop dreamapp
     sudo cp -r ~/publish/* /opt/dreamapp/
     sudo chmod +x /opt/dreamapp/DreamApp.Api
     sudo systemctl start dreamapp

Database e foto restano in /var/lib/dreamapp (non vengono toccati dagli aggiornamenti).

BACKUP (fallo prima di ogni aggiornamento e ogni tanto)
-----------------------------------------------------
Dal tuo PC, nella cartella backend\DreamApp.Api:
     .\backup.ps1 -Server IP_DELLA_VM -Key CHIAVE.key
Scarica database e foto nella cartella DreamAppBackups (nella tua cartella utente).
Il file /etc/dreamapp.env resta com'e: non va ricaricato.
"@
Write-LinuxFile (Join-Path $deployDir 'LEGGIMI.txt') $readme

# ---------------------------------------------------------------- summary
$sizeMb = [math]::Round((Get-ChildItem -Path $appDir -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB, 1)

Write-Host ''
Write-Host 'Fatto.' -ForegroundColor Green
Write-Host "  App pubblicata : $appDir ($sizeMb MB)"
Write-Host "  File per server: $deployDir"
Write-Host "  Istruzioni     : $(Join-Path $deployDir 'LEGGIMI.txt')"

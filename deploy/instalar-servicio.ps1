<#
.SINOPSIS
    Instala/registra el ERP como Servicio de Windows: arranca solo con la PC y se reinicia si se cuelga.

.DESCRIPCION
    Requiere PowerShell como Administrador (crear un servicio de Windows no se puede sin eso).
    Si el servicio ya existe, lo borra primero y lo vuelve a crear (útil para reinstalar tras
    publicar una versión nueva con .\publicar.ps1).

.PARAMETRO RutaApp
    Carpeta donde está publicado Galeria.Web.exe (la que generó .\publicar.ps1).

.PARAMETRO NombreServicio
    Nombre interno del servicio en Windows (el que aparece en services.msc).

.EJEMPLO
    .\deploy\instalar-servicio.ps1
#>
param(
    [string]$RutaApp = "C:\GaleriaACATRAS\app",
    [string]$NombreServicio = "GaleriaACATRAS"
)

$ErrorActionPreference = "Stop"

$esAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
if (-not $esAdmin) {
    Write-Host "Este script necesita PowerShell como Administrador." -ForegroundColor Red
    Write-Host "Click derecho sobre PowerShell -> 'Ejecutar como administrador', y volvé a correr este script desde ahí." -ForegroundColor Yellow
    exit 1
}

$exe = Join-Path $RutaApp "Galeria.Web.exe"
if (-not (Test-Path $exe)) {
    throw "No se encontró $exe. Corré .\deploy\publicar.ps1 primero (o revisá -RutaApp)."
}

$existente = Get-Service -Name $NombreServicio -ErrorAction SilentlyContinue
if ($existente) {
    Write-Host "El servicio '$NombreServicio' ya existe, lo reinstalo (para tomar la versión publicada nueva)..." -ForegroundColor Yellow
    if ($existente.Status -eq "Running") {
        Stop-Service -Name $NombreServicio -Force
    }
    sc.exe delete $NombreServicio | Out-Null
    Start-Sleep -Seconds 2
}

Write-Host "Creando el servicio '$NombreServicio' -> $exe" -ForegroundColor Cyan
& sc.exe create $NombreServicio binPath= "`"$exe`"" start= auto DisplayName= "Galeria ACATRAS (ERP)" | Out-Null
& sc.exe description $NombreServicio "ERP de la galeria (Blazor Server). Corre localmente en esta PC, sin exposicion a internet." | Out-Null

# Recuperación ante fallas: si el proceso se cuelga, Windows lo reinicia solo (hasta 3 veces,
# esperando 5s cada vez) en vez de dejar el sistema caído hasta que alguien lo note.
& sc.exe failure $NombreServicio reset= 86400 actions= restart/5000/restart/5000/restart/5000 | Out-Null

Write-Host "Iniciando el servicio..." -ForegroundColor Cyan
Start-Service -Name $NombreServicio
Start-Sleep -Seconds 2

$estado = Get-Service -Name $NombreServicio
Write-Host "Servicio '$NombreServicio': $($estado.Status)" -ForegroundColor Green
Write-Host "Probá abrir el navegador en la URL configurada (por defecto http://localhost:5121)." -ForegroundColor Cyan

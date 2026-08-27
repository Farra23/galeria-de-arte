<#
.SINOPSIS
    Para y borra el Servicio de Windows del ERP (para reinstalar una versión nueva o desarmar todo).

.DESCRIPCION
    Requiere PowerShell como Administrador. No toca la base de datos ni los archivos publicados
    en disco — solo el registro del servicio en Windows.

.EJEMPLO
    .\deploy\desinstalar-servicio.ps1
#>
param(
    [string]$NombreServicio = "GaleriaACATRAS"
)

$ErrorActionPreference = "Stop"

$esAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
if (-not $esAdmin) {
    Write-Host "Este script necesita PowerShell como Administrador." -ForegroundColor Red
    exit 1
}

$existente = Get-Service -Name $NombreServicio -ErrorAction SilentlyContinue
if (-not $existente) {
    Write-Host "El servicio '$NombreServicio' no existe, no hay nada que desinstalar." -ForegroundColor Yellow
    exit 0
}

if ($existente.Status -eq "Running") {
    Write-Host "Deteniendo el servicio '$NombreServicio'..." -ForegroundColor Cyan
    Stop-Service -Name $NombreServicio -Force
    Start-Sleep -Seconds 2
}

& sc.exe delete $NombreServicio | Out-Null
Write-Host "Servicio '$NombreServicio' desinstalado." -ForegroundColor Green

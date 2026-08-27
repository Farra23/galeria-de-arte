<#
.SINOPSIS
    Programa una Tarea de Windows que corre .\hacer-backup.ps1 una vez al mes, de madrugada.

.DESCRIPCION
    Requiere PowerShell como Administrador. Usa schtasks.exe (disponible en cualquier Windows,
    sin depender de una versión particular de PowerShell) para crear un disparador mensual.

    Un backup mensual es lo que pidió el cliente para arrancar simple, pero en un sistema donde
    entran ventas y adelantos todos los días, si el disco falla justo antes del backup del mes
    se puede perder hasta un mes de movimientos. Si más adelante quieren más frecuencia, cambiar
    "/SC MONTHLY /D 1" más abajo por "/SC WEEKLY /D SUN" (o correr .\deploy\hacer-backup.ps1 a
    mano cuando quieran, no hace falta esperar a la tarea programada).

.PARAMETRO RutaApp
    Carpeta de la app publicada (se la pasa a hacer-backup.ps1).

.PARAMETRO DestinoBackups
    Carpeta donde se guardan los .zip (se la pasa a hacer-backup.ps1).

.EJEMPLO
    .\deploy\instalar-tarea-backup.ps1
#>
param(
    [string]$RutaApp = "C:\GaleriaACATRAS\app",
    [string]$DestinoBackups = "C:\GaleriaACATRAS\backups",
    [string]$NombreTarea = "GaleriaACATRAS-Backup"
)

$ErrorActionPreference = "Stop"

$esAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
if (-not $esAdmin) {
    Write-Host "Este script necesita PowerShell como Administrador." -ForegroundColor Red
    exit 1
}

$scriptBackup = Join-Path $PSScriptRoot "hacer-backup.ps1"
if (-not (Test-Path $scriptBackup)) {
    throw "No se encontró $scriptBackup."
}

$existente = Get-ScheduledTask -TaskName $NombreTarea -ErrorAction SilentlyContinue
if ($existente) {
    Write-Host "Ya existe la tarea '$NombreTarea', la borro y la vuelvo a crear..." -ForegroundColor Yellow
    schtasks.exe /Delete /TN $NombreTarea /F | Out-Null
}

$argumentos = "-NoProfile -ExecutionPolicy Bypass -File `"$scriptBackup`" -RutaApp `"$RutaApp`" -DestinoBackups `"$DestinoBackups`""
$comando = "powershell.exe $argumentos"

Write-Host "Programando '$NombreTarea': el 1 de cada mes a las 03:00..." -ForegroundColor Cyan
schtasks.exe /Create /SC MONTHLY /D 1 /ST 03:00 /TN $NombreTarea /TR $comando /RU SYSTEM /RL HIGHEST /F | Out-Null

$tarea = Get-ScheduledTask -TaskName $NombreTarea -ErrorAction SilentlyContinue
if ($tarea) {
    Write-Host "Tarea '$NombreTarea' creada. Estado: $($tarea.State)" -ForegroundColor Green
    Write-Host "Podés probarla ahora mismo con: Start-ScheduledTask -TaskName '$NombreTarea'" -ForegroundColor Cyan
} else {
    throw "La tarea no quedó registrada — revisá el mensaje de schtasks.exe arriba."
}

<#
.SINOPSIS
    Programa el backup automatico DIARIO de la base y las imagenes.

.DESCRIPCION
    Reemplaza la tarea mensual anterior. Tres cambios, los tres importantes:

    1. DIARIA en vez de mensual. Antes, la ventana de perdida era de hasta 31 dias: un mes entero
       de ventas, altas de obra y liquidaciones. Ahora es de un dia como maximo.

    2. -StartWhenAvailable: si la PC estaba apagada a la hora programada, la tarea corre apenas
       se prende. La version anterior usaba schtasks.exe sin esta opcion, asi que en una PC de
       mostrador que se apaga a la noche el backup simplemente no corria NUNCA - y nadie se
       enteraba, porque una tarea que no se ejecuta no da error.

    3. Se registra con el modulo ScheduledTasks (Register-ScheduledTask) en vez de schtasks.exe,
       que es lo que permite configurar el punto 2 y un limite de duracion.

    La tarea corre como SYSTEM, asi que anda con la sesion cerrada y sin nadie logueado.

.PARAMETRO Hora
    Hora de la corrida diaria, formato HH:mm. Por defecto 03:00 (de madrugada, con la galeria
    cerrada: el backup detiene el servicio unos segundos).

.EJEMPLO
    .\deploy\instalar-tarea-backup.ps1
    .\deploy\instalar-tarea-backup.ps1 -DestinoBackups "D:\BackupsGaleria" -Hora "02:30"
#>
param(
    [string]$RutaApp = "C:\GaleriaACATRAS\app",
    [string]$DestinoBackups = "C:\GaleriaACATRAS\backups",
    [string]$NombreTarea = "GaleriaACATRAS-Backup",
    [string]$Hora = "03:00"
)

$ErrorActionPreference = "Stop"

$esAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
if (-not $esAdmin) {
    Write-Host "Este script necesita PowerShell como Administrador." -ForegroundColor Red
    Write-Host "Click derecho sobre PowerShell -> 'Ejecutar como administrador'." -ForegroundColor Yellow
    exit 1
}

$scriptBackup = Join-Path $PSScriptRoot "hacer-backup.ps1"
if (-not (Test-Path $scriptBackup)) {
    throw "No se encontro $scriptBackup (tiene que estar en la misma carpeta que este script)."
}

try {
    $horaParseada = [datetime]::ParseExact($Hora, "HH:mm", $null)
} catch {
    throw "-Hora tiene que tener formato HH:mm (ej. 03:00). Recibi: '$Hora'."
}

$existente = Get-ScheduledTask -TaskName $NombreTarea -ErrorAction SilentlyContinue
if ($existente) {
    Write-Host "Ya existe la tarea '$NombreTarea' - la borro y la vuelvo a crear con la configuracion nueva..." -ForegroundColor Yellow
    Unregister-ScheduledTask -TaskName $NombreTarea -Confirm:$false
}

$argumentos = '-NoProfile -ExecutionPolicy Bypass -File "{0}" -RutaApp "{1}" -DestinoBackups "{2}"' -f $scriptBackup, $RutaApp, $DestinoBackups

$accion = New-ScheduledTaskAction -Execute "powershell.exe" -Argument $argumentos
$disparador = New-ScheduledTaskTrigger -Daily -At $horaParseada
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

$config = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -DontStopIfGoingOnBatteries `
    -AllowStartIfOnBatteries `
    -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit (New-TimeSpan -Hours 2)

Write-Host "Programando '$NombreTarea': todos los dias a las $Hora..." -ForegroundColor Cyan
Register-ScheduledTask -TaskName $NombreTarea -Action $accion -Trigger $disparador `
    -Principal $principal -Settings $config `
    -Description "Backup diario de la base y las imagenes del ERP de la galeria." | Out-Null

$tarea = Get-ScheduledTask -TaskName $NombreTarea -ErrorAction SilentlyContinue
if (-not $tarea) {
    throw "La tarea no quedo registrada."
}

Write-Host "Tarea '$NombreTarea' creada. Estado: $($tarea.State)" -ForegroundColor Green
Write-Host ""
Write-Host "IMPORTANTE: probala AHORA, no te vayas sin ver un .zip generado:" -ForegroundColor Yellow
Write-Host "    Start-ScheduledTask -TaskName '$NombreTarea'" -ForegroundColor White
Write-Host "    Get-ChildItem '$DestinoBackups'" -ForegroundColor White

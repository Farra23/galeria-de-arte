<#
.SINOPSIS
    Restaura un backup .zip sobre la instalacion: base de datos + imagenes de obras.

.DESCRIPCION
    Es la otra mitad de hacer-backup.ps1. Sin este script, "tenemos backup" es una suposicion:
    la unica forma de saber que el backup sirve es restaurarlo una vez.

    Que hace, en orden:
      1. Verifica que el .zip tenga adentro un app.db con cabecera SQLite valida ANTES de tocar
         nada de lo que esta andando.
      2. Guarda la base actual en un .zip de seguridad (antes-de-restaurar-*.zip). Si te
         equivocaste de backup, tenes como volver.
      3. Para el servicio, reemplaza los archivos, arranca el servicio.

    IMPORTANTE (y por esto no alcanza con descomprimir el zip a mano): al restaurar hay que
    BORRAR los archivos app.db-wal y app.db-shm que estuvieran en la carpeta. Son el diario de
    transacciones de la base ANTERIOR; si quedan ahi, SQLite los aplica encima de la base
    restaurada y el resultado es una mezcla de las dos. Se pierde justo lo que se queria
    recuperar, y sin ningun mensaje de error.

.PARAMETRO Ultimo
    Toma automaticamente el backup mas reciente de -DestinoBackups, sin tener que tipear el nombre.

.PARAMETRO Force
    No pide confirmacion. Usalo solo en scripts, nunca a mano.

.EJEMPLO
    .\deploy\restaurar-backup.ps1 -Ultimo
    .\deploy\restaurar-backup.ps1 -ArchivoZip "C:\GaleriaACATRAS\backups\backup-2026-09-11_0300.zip"
#>
param(
    [string]$ArchivoZip,
    [switch]$Ultimo,
    [string]$RutaApp = "C:\GaleriaACATRAS\app",
    [string]$DestinoBackups = "C:\GaleriaACATRAS\backups",
    [string]$NombreServicio = "GaleriaACATRAS",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$esAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
if (-not $esAdmin) {
    Write-Host "Este script necesita PowerShell como Administrador (tiene que parar el servicio)." -ForegroundColor Red
    exit 1
}

function Test-CabeceraSqlite {
    param([string]$Ruta)
    try {
        $fs = [System.IO.File]::OpenRead($Ruta)
        try {
            $buffer = New-Object byte[] 16
            if ($fs.Read($buffer, 0, 16) -ne 16) { return $false }
            return ([System.Text.Encoding]::ASCII.GetString($buffer, 0, 15) -eq "SQLite format 3")
        } finally { $fs.Dispose() }
    } catch { return $false }
}

if ($Ultimo) {
    $masNuevo = Get-ChildItem -Path $DestinoBackups -Filter "backup-*.zip" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $masNuevo) { throw "No hay ningun backup-*.zip en $DestinoBackups." }
    $ArchivoZip = $masNuevo.FullName
}

if (-not $ArchivoZip) { throw "Indica -ArchivoZip <ruta> o -Ultimo." }
if (-not (Test-Path $ArchivoZip)) { throw "No se encontro $ArchivoZip." }

$carpetaDatos = Join-Path $RutaApp "Datos"
if (-not (Test-Path $carpetaDatos)) { throw "No se encontro $carpetaDatos. Revisa -RutaApp." }

$temporal = Join-Path $env:TEMP ("galeria-restore-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
New-Item -ItemType Directory -Path $temporal -Force | Out-Null

try {
    Write-Host "Abriendo $ArchivoZip ..." -ForegroundColor Cyan
    Expand-Archive -Path $ArchivoZip -DestinationPath $temporal -Force

    $baseEnZip = Join-Path $temporal "Datos\app.db"
    if (-not (Test-Path $baseEnZip)) { throw "El .zip no contiene Datos\app.db - no parece un backup de esta app." }
    if (-not (Test-CabeceraSqlite $baseEnZip)) { throw "El app.db del .zip esta corrupto (cabecera SQLite invalida). NO se restauro nada." }

    $fechaBackup = (Get-Item $ArchivoZip).LastWriteTime
    Write-Host ""
    Write-Host "  Backup a restaurar : $ArchivoZip" -ForegroundColor Yellow
    Write-Host "  Fecha              : $fechaBackup" -ForegroundColor Yellow
    Write-Host ("  Tamanio de la base : {0:N1} MB" -f ((Get-Item $baseEnZip).Length / 1MB)) -ForegroundColor Yellow
    Write-Host "  Se va a pisar      : $carpetaDatos" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Todo lo cargado DESPUES de esa fecha se pierde." -ForegroundColor Red
    Write-Host ""

    if (-not $Force) {
        $respuesta = Read-Host "Escribi RESTAURAR (en mayusculas) para continuar"
        if ($respuesta -cne "RESTAURAR") {
            Write-Host "Cancelado. No se toco nada." -ForegroundColor Green
            exit 0
        }
    }

    $servicio = Get-Service -Name $NombreServicio -ErrorAction SilentlyContinue
    if ($servicio -and $servicio.Status -eq "Running") {
        Write-Host "Deteniendo el servicio '$NombreServicio'..." -ForegroundColor Cyan
        Stop-Service -Name $NombreServicio -Force
        Start-Sleep -Seconds 3
    }

    # Red de seguridad: si el backup elegido resulta ser el equivocado, esto permite volver.
    $respaldo = Join-Path $DestinoBackups ("antes-de-restaurar-" + (Get-Date -Format "yyyy-MM-dd_HHmm") + ".zip")
    $tempRespaldo = Join-Path $env:TEMP ("galeria-prev-" + (Get-Date -Format "yyyyMMddHHmmss"))
    New-Item -ItemType Directory -Path (Join-Path $tempRespaldo "Datos") -Force | Out-Null
    foreach ($sufijo in @("app.db", "app.db-wal", "app.db-shm")) {
        $origen = Join-Path $carpetaDatos $sufijo
        if (Test-Path $origen) { Copy-Item $origen -Destination (Join-Path $tempRespaldo "Datos") }
    }
    Compress-Archive -Path (Join-Path $tempRespaldo "*") -DestinationPath $respaldo -Force
    Remove-Item $tempRespaldo -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "Base actual guardada en $respaldo" -ForegroundColor Green

    # Ver la nota de la cabecera: los -wal/-shm viejos tienen que desaparecer SI O SI.
    foreach ($sufijo in @("app.db-wal", "app.db-shm")) {
        $viejo = Join-Path $carpetaDatos $sufijo
        if (Test-Path $viejo) { Remove-Item $viejo -Force }
    }

    Copy-Item $baseEnZip -Destination (Join-Path $carpetaDatos "app.db") -Force
    Write-Host "Base restaurada." -ForegroundColor Green

    $imagenesEnZip = Join-Path $temporal "uploads\obras"
    if (Test-Path $imagenesEnZip) {
        $destinoImagenes = Join-Path $RutaApp "wwwroot\uploads\obras"
        New-Item -ItemType Directory -Path $destinoImagenes -Force | Out-Null
        Copy-Item (Join-Path $imagenesEnZip "*") -Destination $destinoImagenes -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "Imagenes restauradas." -ForegroundColor Green
    }

    if ($servicio) {
        Start-Service -Name $NombreServicio
        Start-Sleep -Seconds 3
        Write-Host "Servicio '$NombreServicio': $((Get-Service -Name $NombreServicio).Status)" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "Listo. Abri la app y verifica que los datos sean los esperados." -ForegroundColor Green
}
finally {
    Remove-Item $temporal -Recurse -Force -ErrorAction SilentlyContinue
}

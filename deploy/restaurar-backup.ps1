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

function Esperar-BaseLiberada {
    param([string]$Ruta, [int]$SegundosMaximo = 60)
    if (-not (Test-Path $Ruta)) { return $true }
    $limite = (Get-Date).AddSeconds($SegundosMaximo)
    while ((Get-Date) -lt $limite) {
        try {
            $fs = [System.IO.File]::Open($Ruta, 'Open', 'ReadWrite', 'None')
            $fs.Dispose()
            return $true
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }
    return $false
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
        try {
            (Get-Service -Name $NombreServicio).WaitForStatus('Stopped', (New-TimeSpan -Seconds 60))
        } catch { }

        # Pisar el .db mientras el proceso todavia lo tiene abierto deja la base a medio escribir.
        if (-not (Esperar-BaseLiberada (Join-Path $carpetaDatos "app.db") 60)) {
            throw "El servicio se detuvo pero la base sigue tomada por otro proceso. NO se restauro nada: cerra la app / reinicia la PC y proba de nuevo."
        }
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

    # Las imagenes pueden venir de dos lados:
    #   - del .zip, si es un backup del formato viejo (las incluia adentro);
    #   - del espejo <DestinoBackups>\imagenes, que es donde van ahora (ver hacer-backup.ps1).
    # Se prueban en ese orden: lo que traiga el .zip es de la misma fecha que la base restaurada,
    # asi que tiene prioridad sobre el espejo, que siempre refleja el estado mas reciente.
    $destinoImagenes = Join-Path $RutaApp "wwwroot\uploads\obras"
    $origenImagenes = $null
    $imagenesEnZip = Join-Path $temporal "uploads\obras"
    $espejoImagenes = Join-Path $DestinoBackups "imagenes"

    if (Test-Path $imagenesEnZip) { $origenImagenes = $imagenesEnZip }
    elseif (Test-Path $espejoImagenes) { $origenImagenes = $espejoImagenes }

    if ($origenImagenes) {
        New-Item -ItemType Directory -Path $destinoImagenes -Force | Out-Null
        robocopy $origenImagenes $destinoImagenes /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) {
            Write-Host "AVISO: fallo la copia de imagenes (robocopy $LASTEXITCODE). La base SI se restauro." -ForegroundColor Yellow
        } else {
            $cantidad = @(Get-ChildItem -Path $destinoImagenes -File -Recurse -ErrorAction SilentlyContinue).Count
            Write-Host "Imagenes restauradas desde $origenImagenes ($cantidad archivo(s))." -ForegroundColor Green
        }
        $global:LASTEXITCODE = 0
    } else {
        Write-Host "No se encontraron imagenes para restaurar (ni en el .zip ni en $espejoImagenes)." -ForegroundColor Yellow
    }

    if ($servicio) {
        Start-Service -Name $NombreServicio
        try {
            (Get-Service -Name $NombreServicio).WaitForStatus('Running', (New-TimeSpan -Seconds 120))
        } catch { }
        $estadoFinal = (Get-Service -Name $NombreServicio).Status
        Write-Host "Servicio '$NombreServicio': $estadoFinal" -ForegroundColor Green

        if ($estadoFinal -ne "Running") {
            Write-Host ""
            Write-Host "El servicio no arranco. La causa mas probable al restaurar un backup VIEJO:" -ForegroundColor Yellow
            Write-Host "esa base es anterior a una actualizacion del programa, asi que le faltan" -ForegroundColor Yellow
            Write-Host "migraciones y la app se niega a arrancar a proposito. Se arregla corriendo" -ForegroundColor Yellow
            Write-Host "deploy\publicar.ps1 apuntando a esta instalacion (no se pierden datos)." -ForegroundColor Yellow
            Write-Host "Para ver el error exacto:" -ForegroundColor Yellow
            Write-Host "    Get-EventLog -LogName Application -Source '$NombreServicio' -Newest 5 | Format-List" -ForegroundColor White
        }
    }

    Write-Host ""
    Write-Host "Listo. Abri la app y verifica que los datos sean los esperados." -ForegroundColor Green
}
finally {
    Remove-Item $temporal -Recurse -Force -ErrorAction SilentlyContinue
}

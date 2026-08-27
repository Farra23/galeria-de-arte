<#
.SINOPSIS
    Hace un backup de la base de datos y las imágenes de obras a un .zip con fecha.

.DESCRIPCION
    Copia el archivo SQLite (Datos\app.db, más -wal/-shm si existen — son parte de la misma
    base mientras el proceso está escribiendo) y la carpeta de imágenes subidas
    (wwwroot\uploads\obras) a un único .zip fácil de guardar o restaurar a mano.

    RECOMENDACIÓN: apuntá -DestinoBackups a un disco externo o a una carpeta sincronizada con
    OneDrive/Google Drive, no solo al disco interno de esta misma PC — si el disco de la PC de
    la galería se rompe, un backup guardado ahí mismo no sirve de nada.

    Este script está pensado para correr con la app tranquila (de noche, sin nadie usándola) —
    ver .\instalar-tarea-backup.ps1, que programa la tarea de madrugada por eso mismo.

.PARAMETRO RutaApp
    Carpeta donde está publicada la app (con la carpeta Datos adentro).

.PARAMETRO DestinoBackups
    Carpeta donde se guardan los .zip de backup.

.EJEMPLO
    .\deploy\hacer-backup.ps1
    .\deploy\hacer-backup.ps1 -DestinoBackups "D:\BackupsGaleria"
#>
param(
    [string]$RutaApp = "C:\GaleriaACATRAS\app",
    [string]$DestinoBackups = "C:\GaleriaACATRAS\backups"
)

$ErrorActionPreference = "Stop"

$carpetaDatos = Join-Path $RutaApp "Datos"
$baseDatos = Join-Path $carpetaDatos "app.db"
if (-not (Test-Path $baseDatos)) {
    throw "No se encontró $baseDatos. Revisá -RutaApp, o si la base todavía no se creó (ver docs/DESPLIEGUE.md)."
}

if (-not (Test-Path $DestinoBackups)) {
    New-Item -ItemType Directory -Path $DestinoBackups | Out-Null
}

$carpetaTemporal = Join-Path $env:TEMP "galeria-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Path $carpetaTemporal | Out-Null

try {
    $carpetaDatosTemp = Join-Path $carpetaTemporal "Datos"
    New-Item -ItemType Directory -Path $carpetaDatosTemp | Out-Null
    foreach ($sufijo in @("app.db", "app.db-wal", "app.db-shm")) {
        $origen = Join-Path $carpetaDatos $sufijo
        if (Test-Path $origen) {
            Copy-Item $origen -Destination $carpetaDatosTemp
        }
    }

    $carpetaImagenes = Join-Path $RutaApp "wwwroot\uploads\obras"
    if (Test-Path $carpetaImagenes) {
        $destinoImagenes = Join-Path $carpetaTemporal "uploads\obras"
        New-Item -ItemType Directory -Path $destinoImagenes -Force | Out-Null
        Copy-Item (Join-Path $carpetaImagenes "*") -Destination $destinoImagenes -Recurse -ErrorAction SilentlyContinue
    }

    $timestamp = Get-Date -Format "yyyy-MM-dd_HHmm"
    $zip = Join-Path $DestinoBackups "backup-$timestamp.zip"
    Compress-Archive -Path (Join-Path $carpetaTemporal "*") -DestinationPath $zip -Force

    Write-Host "Backup creado en $zip" -ForegroundColor Green
}
finally {
    Remove-Item $carpetaTemporal -Recurse -Force -ErrorAction SilentlyContinue
}

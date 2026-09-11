<#
.SINOPSIS
    Backup consistente de la base y las imagenes a un .zip con fecha. Pensado para correr solo,
    todos los dias, sin que nadie lo mire.

.DESCRIPCION
    Mejoras sobre la version anterior (que copiaba en caliente una vez por mes):

    1. CONSISTENCIA: para el servicio unos segundos antes de copiar y lo vuelve a arrancar. Al
       cerrar limpio, SQLite hace checkpoint del WAL y app.db queda integro. Copiar el archivo
       con el servicio escribiendo podia dar un backup roto que solo se descubre el dia que hace
       falta restaurarlo. El arranque del servicio esta en un finally: si falla la copia, el
       servicio vuelve a levantar igual.
    2. VERIFICACION: chequea la cabecera del archivo copiado ("SQLite format 3") y que el .zip
       haya quedado con contenido. Un backup que nadie verifica no es un backup.
    3. RETENCION: borra los .zip mas viejos que -RetenerDias, pero NUNCA baja de -RetenerMinimo
       copias. Sin esto la carpeta crece para siempre y termina llenando el disco.
    4. BITACORA: deja una linea por corrida en backups.log, con resultado y tamanio. Es lo que
       se mira para saber si el backup viene corriendo de verdad.

    RECOMENDACION: apunta -DestinoBackups a un disco externo o a una carpeta de OneDrive/Google
    Drive. Un backup en el mismo disco que la base no sirve si el disco se rompe.

.PARAMETRO SinParar
    Copia sin detener el servicio (backup en caliente, puede quedar inconsistente). Solo para
    sacar una copia rapida en medio del dia sin interrumpir a nadie.

.EJEMPLO
    .\deploy\hacer-backup.ps1
    .\deploy\hacer-backup.ps1 -DestinoBackups "D:\BackupsGaleria"
    .\deploy\hacer-backup.ps1 -SinParar
#>
param(
    [string]$RutaApp = "C:\GaleriaACATRAS\app",
    [string]$DestinoBackups = "C:\GaleriaACATRAS\backups",
    [string]$NombreServicio = "GaleriaACATRAS",
    [int]$RetenerDias = 60,
    [int]$RetenerMinimo = 10,
    [switch]$SinParar
)

$ErrorActionPreference = "Stop"

$carpetaDatos = Join-Path $RutaApp "Datos"
$baseDatos = Join-Path $carpetaDatos "app.db"
$archivoLog = Join-Path $DestinoBackups "backups.log"

function Escribir-Bitacora {
    param([string]$Nivel, [string]$Mensaje)
    $linea = "{0}  {1,-5}  {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Nivel, $Mensaje
    try {
        if (-not (Test-Path $DestinoBackups)) { New-Item -ItemType Directory -Path $DestinoBackups -Force | Out-Null }
        Add-Content -Path $archivoLog -Value $linea -Encoding UTF8
    } catch { }
    if ($Nivel -eq "ERROR") { Write-Host $Mensaje -ForegroundColor Red }
    else { Write-Host $Mensaje -ForegroundColor Cyan }
}

# La cabecera de todo archivo SQLite valido son los bytes "SQLite format 3" + 0x00. Si la copia
# quedo truncada o es basura, esto lo detecta ahora y no el dia de la restauracion.
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

if (-not (Test-Path $baseDatos)) {
    Escribir-Bitacora "ERROR" "No se encontro $baseDatos. Revisa -RutaApp."
    exit 1
}

if (-not (Test-Path $DestinoBackups)) {
    New-Item -ItemType Directory -Path $DestinoBackups -Force | Out-Null
}

# Espacio libre: necesito al menos 3x el tamanio de la base (copia temporal + zip + margen).
$tamanioBase = (Get-Item $baseDatos).Length
try {
    $unidad = (Get-Item $DestinoBackups).PSDrive.Name
    $libre = (Get-PSDrive -Name $unidad).Free
    if ($libre -lt ($tamanioBase * 3)) {
        Escribir-Bitacora "ERROR" ("Espacio insuficiente en {0}: quedan {1:N0} MB y la base pesa {2:N0} MB." -f $unidad, ($libre / 1MB), ($tamanioBase / 1MB))
        exit 1
    }
} catch { }

$servicio = Get-Service -Name $NombreServicio -ErrorAction SilentlyContinue
$habiaQueArrancar = $false
$carpetaTemporal = Join-Path $env:TEMP ("galeria-backup-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
$zip = $null
$errorDeCopia = $null

try {
    if ($servicio -and $servicio.Status -eq "Running" -and -not $SinParar) {
        Escribir-Bitacora "INFO" "Deteniendo el servicio '$NombreServicio' para copiar la base integra..."
        # El flag se marca ANTES de parar: si Stop-Service falla a mitad de camino, el servicio
        # puede quedar detenido igual, y el finally tiene que intentar levantarlo lo mismo.
        $habiaQueArrancar = $true
        Stop-Service -Name $NombreServicio -Force
        Start-Sleep -Seconds 3
    } elseif ($SinParar) {
        Escribir-Bitacora "INFO" "Modo -SinParar: copia en caliente (puede quedar inconsistente)."
    }

    New-Item -ItemType Directory -Path $carpetaTemporal -Force | Out-Null
    $carpetaDatosTemp = Join-Path $carpetaTemporal "Datos"
    New-Item -ItemType Directory -Path $carpetaDatosTemp -Force | Out-Null

    foreach ($sufijo in @("app.db", "app.db-wal", "app.db-shm")) {
        $origen = Join-Path $carpetaDatos $sufijo
        if (Test-Path $origen) { Copy-Item $origen -Destination $carpetaDatosTemp }
    }

    $copiaBase = Join-Path $carpetaDatosTemp "app.db"
    if (-not (Test-CabeceraSqlite $copiaBase)) {
        throw "La copia de app.db no tiene cabecera SQLite valida - backup abortado."
    }

    $carpetaImagenes = Join-Path $RutaApp "wwwroot\uploads\obras"
    if (Test-Path $carpetaImagenes) {
        $destinoImagenes = Join-Path $carpetaTemporal "uploads\obras"
        New-Item -ItemType Directory -Path $destinoImagenes -Force | Out-Null
        Copy-Item (Join-Path $carpetaImagenes "*") -Destination $destinoImagenes -Recurse -ErrorAction SilentlyContinue
    }

    $zip = Join-Path $DestinoBackups ("backup-" + (Get-Date -Format "yyyy-MM-dd_HHmm") + ".zip")
    Compress-Archive -Path (Join-Path $carpetaTemporal "*") -DestinationPath $zip -Force
}
catch {
    # Sin este catch, un fallo en la copia mataba el script por $ErrorActionPreference sin
    # escribir nada en backups.log — que es justo donde se mira para saber si el backup anda.
    $errorDeCopia = $_.Exception.Message
}
finally {
    # Pase lo que pase con la copia, el servicio tiene que volver a levantar.
    if ($habiaQueArrancar) {
        try {
            Start-Service -Name $NombreServicio
            Start-Sleep -Seconds 2
            $estado = (Get-Service -Name $NombreServicio).Status
            Escribir-Bitacora "INFO" "Servicio '$NombreServicio' reiniciado: $estado"
            if ($estado -ne "Running") {
                Escribir-Bitacora "ERROR" "ATENCION: el servicio NO volvio a arrancar. Arrancalo a mano: Start-Service $NombreServicio"
            }
        } catch {
            Escribir-Bitacora "ERROR" "ATENCION: fallo al reiniciar el servicio: $($_.Exception.Message)"
        }
    }
    if (Test-Path $carpetaTemporal) { Remove-Item $carpetaTemporal -Recurse -Force -ErrorAction SilentlyContinue }
}

if ($errorDeCopia) {
    Escribir-Bitacora "ERROR" "Fallo el backup: $errorDeCopia"
    exit 1
}

if (-not $zip -or -not (Test-Path $zip)) {
    Escribir-Bitacora "ERROR" "El .zip no se genero."
    exit 1
}

$tamanioZip = (Get-Item $zip).Length
if ($tamanioZip -lt 1024) {
    Escribir-Bitacora "ERROR" "El .zip quedo vacio o corrupto ($tamanioZip bytes): $zip"
    exit 1
}

Escribir-Bitacora "OK" ("Backup creado: {0} ({1:N1} MB)" -f $zip, ($tamanioZip / 1MB))

# Retencion: borra lo viejo, pero nunca deja menos de $RetenerMinimo copias. El minimo manda
# sobre los dias — si el backup estuvo caido un mes, no quiero quedarme sin nada por antiguedad.
$todos = @(Get-ChildItem -Path $DestinoBackups -Filter "backup-*.zip" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending)
if ($todos.Count -gt $RetenerMinimo) {
    $limite = (Get-Date).AddDays(-$RetenerDias)
    $candidatos = $todos[$RetenerMinimo..($todos.Count - 1)] | Where-Object { $_.LastWriteTime -lt $limite }
    foreach ($viejo in $candidatos) {
        Remove-Item $viejo.FullName -Force -ErrorAction SilentlyContinue
        Escribir-Bitacora "INFO" "Backup viejo borrado: $($viejo.Name)"
    }
}

Escribir-Bitacora "INFO" ("Copias guardadas: {0}" -f @(Get-ChildItem -Path $DestinoBackups -Filter "backup-*.zip").Count)
exit 0

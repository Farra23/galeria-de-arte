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
    3b. IMAGENES APARTE: el .zip lleva solo la base. Las imagenes de obras se mantienen en una
       copia espejo incremental (<DestinoBackups>\imagenes) que robocopy actualiza con lo que
       cambio. Meterlas en el .zip significaba recomprimir varios GB todas las noches y guardar
       ese volumen multiplicado por la cantidad de copias retenidas.
    4. BITACORA: deja una linea por corrida en backups.log, con resultado y tamanio, y reescribe
       ESTADO-BACKUP.txt con un resumen de una pantalla. Si -DestinoBackups apunta a una carpeta
       sincronizada (OneDrive/Drive), ese archivo se puede mirar desde otra PC: es la forma de
       enterarse de que el backup dejo de correr sin depender de que alguien avise.

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

# La bitacora y el estado se escriben SIEMPRE tambien al lado de este script, en el disco interno.
# Si -DestinoBackups apunta a un disco externo o a una carpeta de red y ese destino no esta
# disponible (alguien desenchufo el disco, se cayo la sincronizacion), el aviso de que el backup
# fallo no se puede dejar en el destino: justamente no se puede escribir ahi. Sin esta copia local,
# ese fallo seria completamente invisible y el backup podria quedar caido durante meses.
$carpetaLocal = $PSScriptRoot
$archivoLogLocal = Join-Path $carpetaLocal "backups.log"

# Test-Path falla con error cuando lo que no existe es la UNIDAD entera, no la carpeta: "No se
# encuentra la unidad". Con $ErrorActionPreference = "Stop" ese error corta el script. Y que
# desaparezca la letra de unidad es exactamente lo que pasa cuando alguien desenchufa el disco
# externo — el caso que la copia local de la bitacora existe para hacer visible. Sin esta
# envoltura el backup moria antes de poder avisar nada, que es el peor de los dos mundos: caido
# y en silencio.
function Test-RutaDisponible {
    param([string]$Ruta)
    try { return [bool](Test-Path -Path $Ruta -ErrorAction SilentlyContinue) } catch { return $false }
}

function Escribir-Bitacora {
    param([string]$Nivel, [string]$Mensaje)
    $linea = "{0}  {1,-5}  {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Nivel, $Mensaje

    try { Add-Content -Path $archivoLogLocal -Value $linea -Encoding UTF8 } catch { }

    try {
        if (-not (Test-RutaDisponible $DestinoBackups)) { New-Item -ItemType Directory -Path $DestinoBackups -Force | Out-Null }
        Add-Content -Path $archivoLog -Value $linea -Encoding UTF8
    } catch { }

    if ($Nivel -eq "ERROR") { Write-Host $Mensaje -ForegroundColor Red }
    else { Write-Host $Mensaje -ForegroundColor Cyan }
}

# Resumen en texto plano, reescrito en cada corrida. La bitacora (backups.log) crece y hay que
# leerla; esto es una sola pantalla que dice si el backup esta sano. Si -DestinoBackups apunta a
# una carpeta de OneDrive/Drive, se puede mirar desde otra PC sin molestar al cliente ni esperar
# a que alguien avise que algo dejo de andar.
function Escribir-Estado {
    param([string]$Resultado, [string]$Detalle)
    try {
        $copias = @(Get-ChildItem -Path $DestinoBackups -Filter "backup-*.zip" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending)
        $lineas = @(
            "ESTADO DEL BACKUP - ERP Galeria ACATRAS",
            "=======================================",
            "",
            ("Ultima corrida : {0}" -f (Get-Date -Format "dd/MM/yyyy HH:mm")),
            ("Resultado      : {0}" -f $Resultado),
            ("Detalle        : {0}" -f $Detalle),
            ("PC             : {0}" -f $env:COMPUTERNAME),
            ""
        )
        if ($copias.Count -gt 0) {
            $lineas += ("Copias guardadas : {0}" -f $copias.Count)
            $lineas += ("Mas reciente     : {0}  ({1:N1} MB)" -f $copias[0].Name, ($copias[0].Length / 1MB))
            $lineas += ("Mas antigua      : {0}" -f $copias[$copias.Count - 1].Name)
        } else {
            $lineas += "Copias guardadas : NINGUNA"
        }
        $lineas += ""
        $lineas += ("Destino configurado : {0}" -f $DestinoBackups)
        $lineas += ("Destino accesible   : {0}" -f $(if (Test-RutaDisponible $DestinoBackups) { "SI" } else { "NO - revisar que el disco este conectado" }))
        $lineas += ""
        $lineas += "Si 'Resultado' no dice OK, o si 'Ultima corrida' tiene mas de 2 dias,"
        $lineas += "el backup automatico dejo de funcionar. Ver backups.log."

        # Local primero: es el que sigue existiendo aunque el destino se haya vuelto inalcanzable.
        try { Set-Content -Path (Join-Path $carpetaLocal "ESTADO-BACKUP.txt") -Value $lineas -Encoding UTF8 } catch { }
        try { Set-Content -Path (Join-Path $DestinoBackups "ESTADO-BACKUP.txt") -Value $lineas -Encoding UTF8 } catch { }
    } catch { }
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

# Que el SCM diga "Stopped" no garantiza que el proceso ya haya soltado el archivo. Lo que
# importa no es el estado del servicio sino poder abrir la base en exclusiva: eso se prueba
# directamente, en vez de dormir una cantidad fija de segundos y cruzar los dedos.
function Esperar-BaseLiberada {
    param([string]$Ruta, [int]$SegundosMaximo = 60)
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

if (-not (Test-Path $baseDatos)) {
    Escribir-Bitacora "ERROR" "No se encontro $baseDatos. Revisa -RutaApp."
    exit 1
}

if (-not (Test-RutaDisponible $DestinoBackups)) {
    try {
        New-Item -ItemType Directory -Path $DestinoBackups -Force -ErrorAction Stop | Out-Null
    } catch {
        # Caso tipico: -DestinoBackups es un disco externo que alguien desenchufo, o una unidad de
        # red caida. Se corta aca, pero queda constancia en la copia local de la bitacora y del
        # estado; sin eso el backup podia estar caido meses sin que nadie se enterara.
        Escribir-Bitacora "ERROR" "No se pudo acceder a $DestinoBackups. Si es un disco externo, revisa que este conectado."
        Escribir-Estado "FALLO" "Destino de backups inaccesible: $DestinoBackups"
        exit 1
    }
}

# Espacio libre. Se calcula fuera de cualquier try/catch que pudiera tragarse el exit, y se mide
# contra la base (lo unico que entra al .zip): las imagenes van por separado, ver mas abajo.
$tamanioBase = (Get-Item $baseDatos).Length
$libre = $null
try {
    $unidad = (Get-Item $DestinoBackups).PSDrive.Name
    $libre = (Get-PSDrive -Name $unidad).Free
} catch { }

if ($null -ne $libre -and $libre -lt ($tamanioBase * 3)) {
    Escribir-Bitacora "ERROR" ("Espacio insuficiente en {0}: quedan {1:N0} MB y la base pesa {2:N0} MB (hacen falta 3x para la copia temporal y el .zip)." -f $unidad, ($libre / 1MB), ($tamanioBase / 1MB))
    Escribir-Estado "FALLO" "Espacio insuficiente en disco."
    exit 1
}

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
        try {
            (Get-Service -Name $NombreServicio).WaitForStatus('Stopped', (New-TimeSpan -Seconds 60))
        } catch { }

        if (-not (Esperar-BaseLiberada $baseDatos 60)) {
            throw "El servicio se detuvo pero la base sigue tomada por otro proceso. No se copia: un backup en ese estado puede salir inconsistente."
        }
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
            # Esperar a Running de verdad: el arranque verifica migraciones y abre la base, asi que
            # tarda mas que un par de segundos. Con un Start-Sleep fijo, la bitacora reportaba
            # "no arranco" casi todas las noches aunque hubiera arrancado bien un momento despues.
            try {
                (Get-Service -Name $NombreServicio).WaitForStatus('Running', (New-TimeSpan -Seconds 120))
            } catch { }
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
    Escribir-Estado "FALLO" $errorDeCopia
    exit 1
}

if (-not $zip -or -not (Test-Path $zip)) {
    Escribir-Bitacora "ERROR" "El .zip no se genero."
    Escribir-Estado "FALLO" "El .zip no se genero."
    exit 1
}

$tamanioZip = (Get-Item $zip).Length
if ($tamanioZip -lt 1024) {
    Escribir-Bitacora "ERROR" "El .zip quedo vacio o corrupto ($tamanioZip bytes): $zip"
    Escribir-Estado "FALLO" "El .zip quedo vacio o corrupto ($tamanioZip bytes)."
    exit 1
}

Escribir-Bitacora "OK" ("Backup creado: {0} ({1:N1} MB)" -f $zip, ($tamanioZip / 1MB))

# Las imagenes NO van adentro del .zip, a proposito. Son la parte que crece sin techo (una foto
# por obra, y hay miles): comprimirlas enteras todas las noches y guardar 10 copias multiplica
# por diez una carpeta que sola ya puede llegar a varios GB, y termina llenando el disco - con el
# agravante de que el que se llena es el disco donde estan los backups.
# Se mantiene UNA copia espejo, actualizada de forma incremental: robocopy solo transfiere lo que
# cambio. /E copia subcarpetas pero NO borra en el destino, asi que un borrado accidental del lado
# de la app no se propaga al respaldo.
$carpetaImagenes = Join-Path $RutaApp "wwwroot\uploads\obras"
if (Test-Path $carpetaImagenes) {
    $espejoImagenes = Join-Path $DestinoBackups "imagenes"
    robocopy $carpetaImagenes $espejoImagenes /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null

    # robocopy usa 0-7 para "todo bien" (0 = sin cambios, 1 = copio archivos, etc.) y 8 o mas
    # para error real. No es un exit code comun y conviene no confundirlo con un fallo.
    if ($LASTEXITCODE -ge 8) {
        Escribir-Bitacora "ERROR" "Fallo la copia de imagenes (robocopy devolvio $LASTEXITCODE). La base SI quedo respaldada."
    } else {
        $cantidad = @(Get-ChildItem -Path $espejoImagenes -File -Recurse -ErrorAction SilentlyContinue).Count
        Escribir-Bitacora "INFO" "Imagenes sincronizadas: $cantidad archivo(s) en $espejoImagenes"
    }
    $global:LASTEXITCODE = 0
}

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
Escribir-Estado "OK" ("{0} ({1:N1} MB)" -f (Split-Path $zip -Leaf), ($tamanioZip / 1MB))
exit 0

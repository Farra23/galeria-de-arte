<#
.SINOPSIS
    Revision rapida de la instalacion: servicio, base, backups y espacio en disco.

.DESCRIPCION
    Un solo comando que contesta "esta todo bien?" sin tener que revisar cinco lugares a mano.
    Pensado para dos momentos:

      - Al terminar una instalacion o una actualizacion, antes de irte de la galeria.
      - Cada tanto, o cuando el cliente dice "algo raro pasa": que lo corra y te mande la captura.

    Lo mas util que chequea es la ANTIGUEDAD DEL ULTIMO BACKUP. La forma tipica en que un backup
    automatico falla no es con un error, es en silencio: la tarea se desactiva, cambia una ruta,
    se llena el disco. Nadie lo nota hasta que hace falta restaurar. Esto lo hace visible.

.EJEMPLO
    .\deploy\verificar-instalacion.ps1
#>
param(
    [string]$RutaApp = "C:\GaleriaACATRAS\app",
    [string]$DestinoBackups = "C:\GaleriaACATRAS\backups",
    [string]$NombreServicio = "GaleriaACATRAS",
    [string]$NombreTarea = "GaleriaACATRAS-Backup",
    [int]$DiasMaximoSinBackup = 2
)

$problemas = 0
$avisos = 0

function Resultado {
    param([string]$Estado, [string]$Titulo, [string]$Detalle)
    $color = "Green"; $marca = "[ OK ]"
    if ($Estado -eq "WARN") { $color = "Yellow"; $marca = "[AVISO]"; $script:avisos++ }
    if ($Estado -eq "ERROR") { $color = "Red"; $marca = "[ERROR]"; $script:problemas++ }
    Write-Host ("{0}  {1}" -f $marca, $Titulo) -ForegroundColor $color
    if ($Detalle) { Write-Host ("         {0}" -f $Detalle) -ForegroundColor Gray }
}

Write-Host ""
Write-Host "=== ERP Galeria ACATRAS - verificacion ===" -ForegroundColor Cyan
Write-Host ("Fecha: {0}    PC: {1}" -f (Get-Date -Format "dd/MM/yyyy HH:mm"), $env:COMPUTERNAME) -ForegroundColor Gray
Write-Host ""

# --- Servicio ---
# Cuando el servicio no levanta, el motivo esta en el Visor de eventos y nadie lo va a ir a
# buscar. Se muestra aca mismo: casi siempre alcanza para saber si es la base, un permiso o un
# puerto ocupado, sin tener que entrar a la PC.
function Mostrar-UltimosErrores {
    try {
        $errores = Get-EventLog -LogName Application -EntryType Error -Newest 300 -ErrorAction SilentlyContinue |
            Where-Object { $_.Source -like "*Galeria*" -or $_.Message -like "*Galeria*" } |
            Select-Object -First 2
        if ($errores) {
            Write-Host "         Ultimos errores registrados por Windows:" -ForegroundColor Gray
            foreach ($e in $errores) {
                $texto = ($e.Message -split "`n")[0]
                if ($texto.Length -gt 160) { $texto = $texto.Substring(0, 160) + "..." }
                Write-Host ("           {0:dd/MM HH:mm}  {1}" -f $e.TimeGenerated, $texto) -ForegroundColor Gray
            }
        }
    } catch { }
}

$servicio = Get-Service -Name $NombreServicio -ErrorAction SilentlyContinue
if (-not $servicio) {
    Resultado "ERROR" "El servicio '$NombreServicio' no existe." "Corre .\deploy\instalar-servicio.ps1 como Administrador."
} elseif ($servicio.Status -ne "Running") {
    Resultado "ERROR" "El servicio existe pero esta $($servicio.Status)." "Arrancalo con: Start-Service $NombreServicio"
    Mostrar-UltimosErrores
} else {
    $inicio = (Get-CimInstance Win32_Service -Filter "Name='$NombreServicio'" -ErrorAction SilentlyContinue).StartMode
    if ($inicio -ne "Auto") {
        Resultado "WARN" "El servicio corre, pero su arranque es '$inicio'." "Si la PC se reinicia, la app no levanta sola."
    } else {
        Resultado "OK" "Servicio corriendo, con arranque automatico."
    }
}

# --- Ejecutable ---
$exe = Join-Path $RutaApp "Galeria.Web.exe"
if (-not (Test-Path $exe)) {
    Resultado "ERROR" "No se encontro $exe." "Revisa -RutaApp."
} else {
    $fecha = (Get-Item $exe).LastWriteTime
    Resultado "OK" "Aplicacion instalada." ("Version del {0:dd/MM/yyyy HH:mm}" -f $fecha)
}

# --- Base de datos ---
$baseDatos = Join-Path $RutaApp "Datos\app.db"
if (-not (Test-Path $baseDatos)) {
    Resultado "ERROR" "No se encontro la base $baseDatos."
} else {
    # Se abre compartiendo lectura Y escritura: con el servicio andando, la app tiene la base
    # abierta para escribir, y un OpenRead (que pide share=Read, o sea niega la escritura ajena)
    # falla. Ese fallo no dice nada sobre el contenido del archivo, asi que no puede tratarse como
    # "corrupta" — llegar a decirle a alguien que restaure un backup sobre una base sana es peor
    # que no chequear nada.
    $cabeceraOk = $false
    $noSePudoLeer = $null
    try {
        $fs = [System.IO.File]::Open($baseDatos, [System.IO.FileMode]::Open,
                                     [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        try {
            $buffer = New-Object byte[] 16
            if ($fs.Read($buffer, 0, 16) -eq 16) {
                $cabeceraOk = ([System.Text.Encoding]::ASCII.GetString($buffer, 0, 15) -eq "SQLite format 3")
            } else {
                $noSePudoLeer = "el archivo tiene menos de 16 bytes"
            }
        } finally { $fs.Dispose() }
    } catch {
        $noSePudoLeer = $_.Exception.Message
    }

    $mb = (Get-Item $baseDatos).Length / 1MB
    if ($noSePudoLeer) {
        Resultado "WARN" "No se pudo leer la cabecera de la base." "$noSePudoLeer - No significa que este corrupta: puede estar tomada por otro proceso."
    } elseif (-not $cabeceraOk) {
        Resultado "ERROR" "La base existe pero no tiene cabecera SQLite valida." "Puede estar corrupta. NO sigas cargando datos: restaura un backup."
    } else {
        Resultado "OK" "Base de datos correcta." ("{0:N1} MB" -f $mb)
    }

    # Un -wal enorme significa que hace mucho no se cierra limpio (checkpoint pendiente).
    $wal = "$baseDatos-wal"
    if (Test-Path $wal) {
        $walMb = (Get-Item $wal).Length / 1MB
        if ($walMb -gt 64) {
            Resultado "WARN" ("El diario de la base (-wal) pesa {0:N1} MB." -f $walMb) "Reinicia el servicio para que haga checkpoint."
        }
    }
}

# --- Backups ---
# Se mira primero la copia local del estado: si el destino es un disco externo desconectado, es la
# unica que sobrevive, y es justo el caso que hay que detectar.
$estadoLocal = Join-Path $PSScriptRoot "ESTADO-BACKUP.txt"
if (Test-Path $estadoLocal) {
    $lineaResultado = (Select-String -Path $estadoLocal -Pattern "^Resultado" -ErrorAction SilentlyContinue | Select-Object -First 1).Line
    if ($lineaResultado -and $lineaResultado -notmatch "OK") {
        Resultado "ERROR" "La ultima corrida del backup no termino bien." "$lineaResultado  (detalle en $estadoLocal)"
    }
}

# -ErrorAction SilentlyContinue porque si lo que falta es la unidad entera (disco externo
# desenchufado), Test-Path ademas de devolver false escupe un error crudo de PowerShell al lado
# del mensaje. El caso ya esta contemplado abajo y explicado en castellano; el error suelto solo
# confunde a quien esta leyendo el chequeo.
if (-not (Test-Path $DestinoBackups -ErrorAction SilentlyContinue)) {
    Resultado "ERROR" "No existe o no se puede acceder a la carpeta de backups $DestinoBackups." "Si es un disco externo, revisa que este conectado. Si nunca se configuro: .\deploy\instalar-tarea-backup.ps1"
} else {
    $copias = @(Get-ChildItem -Path $DestinoBackups -Filter "backup-*.zip" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending)
    if ($copias.Count -eq 0) {
        Resultado "ERROR" "No hay NINGUN backup en $DestinoBackups." "Corre ahora: .\deploy\hacer-backup.ps1"
    } else {
        $ultimo = $copias[0]
        $dias = [math]::Round(((Get-Date) - $ultimo.LastWriteTime).TotalDays, 1)
        $detalle = "{0} copias. La ultima: {1} ({2:N1} MB, hace {3} dias)" -f $copias.Count, $ultimo.Name, ($ultimo.Length / 1MB), $dias
        if ($dias -gt $DiasMaximoSinBackup) {
            Resultado "ERROR" "El ultimo backup tiene $dias dias de antiguedad." $detalle
        } else {
            Resultado "OK" "Backups al dia." $detalle
        }

        # Una copia sospechosamente chica suele ser un backup que fallo a mitad de camino.
        if ($ultimo.Length -lt 10240) {
            Resultado "WARN" "El ultimo backup pesa muy poco." "Revisa $DestinoBackups\backups.log"
        }
    }
}

# --- Tarea programada ---
$tarea = Get-ScheduledTask -TaskName $NombreTarea -ErrorAction SilentlyContinue
if (-not $tarea) {
    Resultado "ERROR" "La tarea de backup automatico '$NombreTarea' no existe." "Corre .\deploy\instalar-tarea-backup.ps1 como Administrador."
} elseif ($tarea.State -eq "Disabled") {
    Resultado "ERROR" "La tarea de backup existe pero esta DESACTIVADA." "Activala: Enable-ScheduledTask -TaskName '$NombreTarea'"
} else {
    $info = Get-ScheduledTaskInfo -TaskName $NombreTarea -ErrorAction SilentlyContinue
    if ($info -and $info.LastTaskResult -ne 0 -and $null -ne $info.LastRunTime -and $info.LastRunTime.Year -gt 1999) {
        Resultado "WARN" "La ultima corrida del backup termino con codigo $($info.LastTaskResult)." ("Ultima corrida: {0}" -f $info.LastRunTime)
    } else {
        $proxima = if ($info) { $info.NextRunTime } else { "?" }
        Resultado "OK" "Backup automatico programado." ("Proxima corrida: {0}" -f $proxima)
    }
}

# --- Espacio en disco ---
try {
    $unidad = (Get-Item $RutaApp).PSDrive.Name
    $libreGb = (Get-PSDrive -Name $unidad).Free / 1GB
    if ($libreGb -lt 1) {
        Resultado "ERROR" ("Quedan {0:N1} GB libres en {1}:." -f $libreGb, $unidad) "Con el disco lleno la app no puede escribir y el backup falla."
    } elseif ($libreGb -lt 5) {
        Resultado "WARN" ("Quedan {0:N1} GB libres en {1}:." -f $libreGb, $unidad) "Conviene liberar espacio o mover los backups a otro disco."
    } else {
        Resultado "OK" "Espacio en disco suficiente." ("{0:N1} GB libres en {1}:" -f $libreGb, $unidad)
    }
} catch { }

# --- La app responde? ---
$url = "http://localhost:5121"
try {
    $config = Get-Content (Join-Path $RutaApp "appsettings.json") -Raw -ErrorAction Stop | ConvertFrom-Json
    if ($config.Urls) { $url = ($config.Urls -split ';')[0] }
} catch { }

try {
    $respuesta = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 15 -ErrorAction Stop
    Resultado "OK" "La aplicacion responde en $url" ("HTTP {0}" -f $respuesta.StatusCode)
} catch {
    # Identity redirige el anonimo al login: una redireccion tambien es "esta viva".
    $codigo = $null
    if ($_.Exception.Response) { $codigo = [int]$_.Exception.Response.StatusCode }
    if ($codigo -and $codigo -lt 500) {
        Resultado "OK" "La aplicacion responde en $url" ("HTTP {0} (redirige al login, es lo esperado)" -f $codigo)
    } else {
        Resultado "ERROR" "La aplicacion NO responde en $url" $_.Exception.Message
        Mostrar-UltimosErrores
    }
}

Write-Host ""
if ($problemas -gt 0) {
    Write-Host ("RESULTADO: {0} problema(s) y {1} aviso(s). Revisa los [ERROR] de arriba." -f $problemas, $avisos) -ForegroundColor Red
    exit 1
} elseif ($avisos -gt 0) {
    Write-Host ("RESULTADO: todo funciona, con {0} aviso(s) para mirar con calma." -f $avisos) -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "RESULTADO: todo en orden." -ForegroundColor Green
    exit 0
}

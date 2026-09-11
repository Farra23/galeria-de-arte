# Operación del ERP en la PC de la galería

Guía de los scripts de `deploy/`. Complementa a `docs/INSTALACION_PASO_A_PASO.md`
(instalación desde cero); acá está lo que se hace **después**: backup, restauración,
verificación y actualizaciones.

> **Sobre las rutas.** Todos los scripts asumen la instalación por defecto:
> app en `C:\GaleriaACATRAS\app` y backups en `C:\GaleriaACATRAS\backups`.
> Si la instalación está en otro lado, pasá `-RutaApp` y `-DestinoBackups`.

---

## 1. Backup automático

### Instalarlo (una sola vez, como Administrador)

```powershell
.\deploy\instalar-tarea-backup.ps1 -DestinoBackups "D:\BackupsGaleria"
```

Deja programada una tarea de Windows que corre **todos los días a las 03:00**.

**Poné `-DestinoBackups` en un disco externo o en una carpeta de OneDrive / Google Drive.**
Un backup guardado en el mismo disco que la base no sirve para nada el día que ese disco
se rompe — que es el día para el que existe el backup.

Antes de irte, comprobá que genera un `.zip` de verdad:

```powershell
Start-ScheduledTask -TaskName 'GaleriaACATRAS-Backup'
Start-Sleep -Seconds 60
Get-ChildItem 'D:\BackupsGaleria'
```

### Qué hace cada noche

1. **Detiene el servicio unos segundos.** Al cerrar limpio, SQLite consolida el diario de
   transacciones y `app.db` queda íntegro. Copiar el archivo con el servicio escribiendo
   puede producir un backup roto que solo se descubre el día que hace falta restaurarlo.
   El arranque del servicio está en un `finally`: si la copia falla, el servicio vuelve a
   levantar igual.
2. Copia `app.db` (+ `-wal` / `-shm` si existen).
3. **Verifica** que la copia tenga cabecera SQLite válida antes de comprimir.
4. Comprime a `backup-AAAA-MM-DD_HHmm.zip`.
5. **Retención:** borra los `.zip` de más de 60 días, pero nunca deja menos de 10 copias.
6. **Sincroniza las imágenes aparte**, a `<DestinoBackups>\imagenes`, de forma incremental
   (solo lo que cambió). No van adentro del `.zip`: son la parte que crece sin techo — una foto
   por obra, y hay miles — y recomprimirlas enteras cada noche, multiplicado por 10 copias
   retenidas, terminaría llenando justamente el disco donde están los backups.
7. Escribe una línea en `backups.log` y reescribe `ESTADO-BACKUP.txt`.

> **`ESTADO-BACKUP.txt` es la pieza que evita la llamada dentro de seis meses.** Es una sola
> pantalla de texto que dice si el último backup salió bien y de cuándo es. Si apuntaste
> `-DestinoBackups` a una carpeta de OneDrive o Drive, lo podés abrir **desde tu casa**, sin
> molestar al cliente y sin depender de que alguien te avise que algo dejó de andar.

Backup manual, en cualquier momento:

```powershell
.\deploy\hacer-backup.ps1 -DestinoBackups "D:\BackupsGaleria"
.\deploy\hacer-backup.ps1 -SinParar     # sin interrumpir a nadie (copia en caliente, menos segura)
```

---

## 2. Verificar que todo esté bien

```powershell
.\deploy\verificar-instalacion.ps1
```

Revisa de una sola vez: servicio corriendo y con arranque automático, base íntegra,
**antigüedad del último backup**, tarea programada activa, espacio en disco y si la app
responde. Termina en `todo en orden` o listando los problemas.

Corrélo **al terminar cualquier instalación o actualización, antes de irte**. Y cuando el
cliente diga "algo raro pasa": que lo corra y te mande la captura.

> El chequeo más valioso es el de la antigüedad del último backup. Un backup automático
> rara vez falla con un error: falla en silencio (la tarea se desactiva, cambia una ruta,
> se llena el disco) y nadie se entera hasta que hace falta restaurar.

---

## 3. Restaurar un backup

```powershell
.\deploy\restaurar-backup.ps1 -Ultimo -DestinoBackups "D:\BackupsGaleria"
```

Como Administrador. Verifica el `.zip` **antes** de tocar nada, guarda la base actual en
`antes-de-restaurar-*.zip` por si elegiste el backup equivocado, y pide que escribas
`RESTAURAR` para confirmar. Para elegir una copia puntual, `-ArchivoZip "...\backup-2026-09-11_0300.zip"`.

> **No restaures descomprimiendo el `.zip` a mano.** Hay que borrar los archivos
> `app.db-wal` y `app.db-shm` que estén en la carpeta: son el diario de la base *anterior*,
> y si quedan ahí SQLite los aplica encima de la base restaurada y mezcla las dos, sin dar
> ningún error. El script se encarga de eso.

**Probá una restauración completa al menos una vez**, en tu PC, con un backup real de la
galería. Un backup que nunca se restauró es una suposición, no un respaldo.

---

## 4. Actualizar a una versión nueva

Desde tu PC de desarrollo:

```powershell
.\deploy\publicar.ps1 -Destino "C:\ParaLlevar\app"
```

Publica en Release, aplica las migraciones pendientes y deja la carpeta lista para copiar.
Por defecto publica **autocontenido**: el runtime de .NET va adentro de la carpeta, así que
la PC de la galería no necesita tener .NET instalado ni actualizarlo nunca.

En la PC de la galería, como Administrador y **en este orden**:

```powershell
# 1. Backup ANTES de tocar nada, y verificá que el .zip se haya creado
.\deploy\hacer-backup.ps1 -DestinoBackups "D:\BackupsGaleria"

# 2. Guardar la configuración actual (ver el aviso de abajo)
Copy-Item C:\GaleriaACATRAS\app\appsettings.json $env:USERPROFILE\Desktop\appsettings.anterior.json

# 3. Parar el servicio
Stop-Service GaleriaACATRAS

# 4. Copiar la carpeta nueva sobre C:\GaleriaACATRAS\app
#    (Datos\ y wwwroot\uploads\ NO se tocan: son los datos del cliente)

# 5. Comparar appsettings.json con el guardado y reponer lo que sea propio de esta instalación

# 6. Reinstalar el servicio para que tome los binarios nuevos
.\deploy\instalar-servicio.ps1

# 7. Verificar
.\deploy\verificar-instalacion.ps1
```

> ⚠️ **`appsettings.json` se sobrescribe al copiar la carpeta nueva.** Todo lo que se haya
> ajustado a mano en la PC de la galería (el puerto en `Urls`, el usuario admin inicial en
> `AdminSeed`, cualquier ruta) vuelve al valor del repositorio. Por eso los pasos 2 y 5.

Si la app no arranca y el log dice **"La base de datos no está actualizada"**, faltó aplicar
las migraciones: corré `publicar.ps1` apuntando a la instalación. La app se niega a arrancar
con el esquema desactualizado a propósito — es preferible a que falle más tarde, en medio del
trabajo, con un error incomprensible.

---

## 5. Cuándo conviene llamar

Señales de que algo necesita atención, en orden de urgencia:

| Señal | Qué significa |
|---|---|
| `verificar-instalacion.ps1` marca `[ERROR]` en la base | Posible corrupción. **No seguir cargando datos**: restaurar. |
| `ESTADO-BACKUP.txt` no dice `OK`, o su fecha tiene más de 2 días | El backup automático dejó de correr. Revisar `backups.log`. |
| Quedan menos de 1 GB libres en disco | La app no puede escribir y el backup falla. |
| El servicio no arranca solo al prender la PC | Revisar que su tipo de inicio sea automático. |

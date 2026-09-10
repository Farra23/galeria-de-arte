# Manual de uso — ERP Galería ACATRAS

Guía completa del sistema de gestión de la galería. Explica, pantalla por pantalla, qué se
puede hacer y cómo. Está pensada para leerse una vez de corrido y después usarse como consulta.

---

## 1. Qué es este sistema

Reemplaza a las planillas de Excel por una única aplicación web. Maneja todo el circuito de la
galería en consignación:

- El **catálogo** de obras y la ficha de cada **artista**.
- Las **ventas**, con su certificado de autenticidad.
- Los **retiros** (el artista se lleva una pieza) y los **alquileres** de obra.
- Las **devoluciones** de piezas ya vendidas.
- Los **adelantos** de dinero a los artistas.
- Las **liquidaciones**: el documento que se le entrega al artista cuando cobra.
- La **agenda de pagos**: a quién hay que llamar para que pase a cobrar.
- La **auditoría**: quién cambió qué y cuándo.

### Ideas base que conviene tener claras desde el principio

| Concepto | Cómo funciona |
|---|---|
| **Pesos y dólares nunca se mezclan** | Son dos contabilidades paralelas. Un artista con obras en las dos monedas tiene dos saldos y dos liquidaciones. Los totales siempre se muestran como par: `$ 10.100 · U$S 0`. |
| **Al artista se le paga el costo** | El costo es el precio que el artista puso al entregar la obra. La diferencia entre el precio de venta y el costo es la utilidad de la galería (por defecto 50 %, ajustable por obra). |
| **Nada se borra, todo se marca** | Un retiro definitivo, una obra sin stock, un usuario dado de baja: quedan en el sistema marcados, nunca desaparecen. Así el historial siempre cierra. |
| **Código de obra = 3 dígitos de artista + 3 de obra** | Cada artista tiene su propio contador. La obra nueva de un artista toma el siguiente número **de ese artista**. El código puede pasar de 6 dígitos si un artista supera las 999 obras. |
| **El precio de venta se calcula solo, pero se puede editar** | Al registrar una venta el precio viene precargado con el cálculo y redondeado, y se puede cambiar porque se negocia con el cliente. Si se vende a otro precio, el sistema guarda el desvío. |

### Fórmula del precio de venta

```
Sin IVA:   Precio = Costo × (1 + Utilidad/100)
Con IVA:   Precio = Costo × 1,22 × (1 + Utilidad/100)
```

Después se redondea según los parámetros del negocio (por defecto: a $10 en pesos, a U$S 1 en
dólares). El IVA (22 %) y los redondeos se configuran en **Datos de la galería**.

---

## 2. Entrar al sistema

1. Abrir el navegador (Chrome, Edge o Firefox) en la dirección del sistema.
   - En la PC de la galería: **http://localhost:5121**
   - Desde otra PC de la misma red (si se habilitó): la dirección que indicó quien lo instaló.
2. Escribir **usuario** y **contraseña** y presionar **Iniciar sesión**.
3. Si se equivoca la contraseña 5 veces seguidas, la cuenta queda **bloqueada 15 minutos**. Es
   una protección; hay que esperar o pedirle a otro usuario que la desbloquee desde
   *Configuración → Usuarios*.

**Cierre por inactividad.** Si la pantalla queda abierta sin usarse **30 minutos**, el sistema
pide iniciar sesión de nuevo. Es a propósito, para que no quede abierto en el mostrador.

**Cerrar sesión.** Botón con el nombre de usuario, arriba a la derecha → *Cerrar sesión*.

---

## 3. La pantalla de inicio (Resumen)

Es lo primero que aparece al entrar. Da la foto del día:

- **Ventas del mes** y **últimas ventas** registradas.
- **Adeudado a artistas**, separado en pesos y dólares.
- **Artistas con saldo pendiente que no vinieron a cobrar**, ordenados por antigüedad (el que
  hace más tiempo que no cobra, arriba). Es la lista de "a este llamalo ya". Al hacer clic en
  un artista se va directo a generarle la liquidación.
- **Panel de pendientes** (se despliega): obras sin técnica o sin rubro cargado, retiros
  temporales vencidos, alquileres sin fecha de recupero, artistas con deuda.

Cada número importante es un enlace: lleva a la lista correspondiente ya filtrada.

**Cómo volver al Resumen desde cualquier lugar:** el menú (ícono de tres rayas, arriba a la
izquierda) → *Resumen*. La flecha ← de la barra superior vuelve a la pantalla anterior.

---

## 4. El menú

Se abre con el ícono de **tres rayas** arriba a la izquierda. Opciones:

| Opción | Para qué |
|---|---|
| **Obras** | Catálogo completo, alta y ficha de cada obra. |
| **Artistas** | Listado y ficha de cada artista, con su estado de cuenta. |
| **Ventas** | Listado de ventas y registro de una venta nueva. |
| **Venta rápida** | Registrar una venta escaneando el código de barras de la etiqueta. |
| **Retiros** | El artista se lleva una pieza (temporal o definitivo). |
| **Alquileres** | Obra alquilada a un cliente, con reparto artista / galería. |
| **Devoluciones** | Un cliente devuelve una pieza que había comprado. |
| **Adelantos** | Dinero adelantado a un artista, a descontar de su liquidación. |
| **Liquidaciones** | Generar y consultar las liquidaciones a los artistas. |
| **Agenda de pagos** | Quién tiene plata para cobrar y cuándo se lo citó. |
| **Cambio de precios** | Cambiar el precio de varias obras de un artista a la vez. |
| **Auditoría** | Registro de todos los cambios hechos en el sistema. |
| **Rubros y técnicas** | Administrar las listas de rubros y técnicas. |
| **Datos de la galería** | Nombre, dirección, IVA, redondeos, plantillas de mensajes. |
| **Usuarios** | Alta, baja y contraseñas de las personas que usan el sistema. |

---

## 5. Cómo funcionan todas las listas

Todas las listas (Obras, Ventas, Artistas, etc.) se comportan igual:

- **Buscador de texto** arriba: busca por nombre, código o artista según la lista. No distingue
  mayúsculas ni tildes (buscar "garcia" encuentra "García").
- **Filtros** por los campos de esa lista (moneda, estado, rango de fechas, rango de precio…).
- **Orden**: clic en el título de una columna ordena por esa columna; otro clic invierte el orden.
- **Chips**: cada filtro aplicado aparece como una etiqueta con una **X** para quitarlo.
- **La vista queda en la dirección web**: se puede guardar como favorito o mandar el enlace a
  otra persona y verá la misma lista filtrada.
- **Exportar CSV**: botón que baja la lista tal como está filtrada, para abrir en Excel. El
  archivo respeta las tildes.
- **Listas muy largas**: Obras y Ventas muestran hasta 500 filas por vez. Si hay más, aparece un
  aviso "mostrando las primeras 500" — usá el buscador o los filtros para acotar. La exportación
  a CSV sí incluye todo.

---

## 6. Obras

### 6.1 Ver el catálogo

*Menú → Obras.* Columnas: miniatura, código, nombre, artista, rubro, técnica, moneda, costo,
precio de venta, stock, estado y fecha de ingreso.

Filtros disponibles: artista, rubro, técnica, moneda, con/sin IVA, con stock, estado, rango de
precio y rango de fecha de ingreso. También se puede filtrar/agrupar por **serie**.

Las obras que no están disponibles (retiradas, alquiladas, sin stock) aparecen atenuadas con un
**badge** que indica su estado.

### 6.2 Ficha de una obra

Clic en cualquier fila. La ficha tiene pestañas:

- **Datos**: todos los campos de la obra, editables. Acá se cambia el precio unitario, se sube o
  reemplaza la imagen, se corrigen medidas u observaciones.
- **Historial de precios**: cada cambio de precio, con fecha y quién lo hizo (viene de Auditoría).
- **Historial de movimientos**: ventas, retiros, alquileres y devoluciones de esa pieza.

Botón **Descargar etiqueta**: genera el PDF de la etiqueta adhesiva con el código de barras para
pegarle a la obra. Al generarla, la obra queda marcada como "etiqueta impresa".

### 6.3 Agregar una obra

*Menú → Obras → Nueva obra* (o el botón desde la lista).

1. **Artista**: elegir del desplegable. Al lado aparece su código. Si el artista todavía no
   existe, el enlace *"Crealo acá"* abre el alta de artista.
2. **Nombre de la obra**: a medida que se escribe, el sistema sugiere obras del mismo artista con
   nombre parecido. Si la obra ya existe, conviene usar **Agregar existencia** en vez de crear
   una nueva (evita duplicados).
3. **Resto de los campos**: rubro, técnica, medidas (largo/alto/ancho), existencia, observaciones,
   moneda, costo, utilidad %, IVA. El **precio de venta se calcula solo** mientras se escribe, y
   se puede ajustar a mano.
4. **Serie**: si son varias piezas.
   - *Serie 1 + existencia 10*: una obra, un código, stock 10 (ej.: 10 caravanas iguales).
   - *Serie 10 + existencia 1*: el sistema crea **10 obras** con códigos consecutivos, marcadas
     como la misma serie, cada una con su numeral romano en el nombre (I, II, III…).
5. **Pago contado / A liquidar**:
   - *A liquidar* (lo normal): al artista se le paga cuando la obra se vende.
   - *Pago contado*: la galería ya le pagó al artista al recibir la pieza. Cuando se venda,
     aparecerá en la liquidación con importe 0 y la nota "Pago contado", y en la pestaña
     *Piezas pagas* de la ficha del artista.
6. **Imagen**: se puede subir ahora o después desde la ficha. JPG, PNG o WEBP, hasta 5 MB.
7. **Guardar**. El sistema se queda en el formulario listo para cargar otra obra del mismo
   artista (no vuelve a la lista).

### 6.4 Agregar stock a una obra que ya existe

En vez de crear una obra repetida: buscarla (por el autocompletado del nombre o desde la lista) y
usar **Agregar existencia**. Si el costo que se ingresa difiere del que tenía la obra, el sistema
**avisa antes de sumar** (podría ser un cambio de precio encubierto).

---

## 7. Artistas

### 7.1 Listado

*Menú → Artistas.* Muestra código, nombre, taller, celular, correo, cantidad de obras, obras en
stock y **saldo a pagar en las dos monedas**. El saldo se calcula en el momento.

### 7.2 Ficha del artista

Clic en una fila. El encabezado muestra siempre el **saldo actual** en pesos y dólares, con los
botones **Editar** y **Liquidar**. Pestañas:

| Pestaña | Contenido |
|---|---|
| **Datos** | Ficha completa (apellido, nombre, perfil, taller, contacto, dirección). |
| **Obras** | Todas las obras que tuvo, históricas, no solo las que están en stock. |
| **Ventas** | Todas sus ventas. |
| **Liquidaciones** | Historial, con acceso al PDF de cada una. |
| **Adelantos** | Fecha y monto de cada adelanto. |
| **Piezas pagas** | Las marcadas como *pago contado* al ingresar. |
| **Retiros** | Temporales y definitivos. |
| **Alquileres** | Alquileres de sus obras. |

> Para que aparezcan los botones de enviar por mail / WhatsApp en Agenda, Liquidaciones y
> Retiros, el artista tiene que tener **correo y/o celular** cargados en esta ficha.

### 7.3 Nuevo artista

*Menú → Artistas → Nuevo artista.* Apellido y nombre son obligatorios; el resto es opcional. El
**código se asigna automáticamente** (siguiente libre).

---

## 8. Ventas

### 8.1 Listado

*Menú → Ventas.* Fecha, código de obra, nombre, artista, cantidad, moneda y precio final. Se
filtra por texto, artista, moneda y fecha.

### 8.2 Registrar una venta

*Menú → Ventas → Nueva venta.*

1. **Buscar la obra** por código. Al encontrarla muestra el nombre, la foto (si tiene), el
   artista y cuántas unidades quedan.
2. Si es **la última pieza**, aparece un aviso destacado **¡ÚLTIMA PIEZA!**.
3. **Cantidad** y **moneda**.
4. **Costo de ingreso**: oculto por defecto, se muestra con el ícono del ojo.
5. **Precio de venta**: en grande, precargado con el calculado y **editable** (se negocia).
6. **Exento de IVA** (por defecto) o **con IVA**: viene precargado desde la obra.
7. **Fecha**: con calendario, se pueden registrar ventas de días anteriores.
8. **Observaciones**.
9. **Confirmar**. Aparece un cartel de confirmación. Al aceptar: la obra baja 1 de stock, si
   llega a 0 pasa a estado *Sin stock*, y la venta queda registrada.

Si se vendió a un precio distinto del calculado, el sistema guarda esa diferencia (desvío) para
la auditoría.

### 8.3 Venta rápida (por código de barras)

*Menú → Venta rápida.* Pensada para el mostrador: se escanea la etiqueta con el lector de código
de barras y el sistema trae la obra directamente. Se completa cantidad, precio y moneda y se
confirma. Hace lo mismo que la venta normal, más rápido.

### 8.4 Certificado de autenticidad

Después de registrar la venta, botón **Emitir certificado**. Genera un PDF con los datos de la
pieza (código, nombre, artista, técnica, rubro, medidas, foto y la leyenda de autenticidad de la
galería) — **nunca** el costo ni la utilidad. Lleva **numeración correlativa** y queda registrado
cuándo se emitió. Se puede descargar e imprimir.

---

## 9. Retiros

Cuando un artista se lleva una o varias piezas.

### 9.1 Nuevo retiro

*Menú → Retiros → Nuevo retiro.*

1. Buscar la obra por código. Se pueden **seleccionar varias** (retiro por lote).
2. Cantidad a retirar, **tipo** (temporal o definitivo), motivo y, si es temporal, **fecha
   estimada de devolución**.
3. Confirmar.

- **Temporal**: la pieza sale del stock y no se puede vender. Se devuelve con un botón (ver
  abajo). Si se pasa la fecha estimada, aparece como vencido en el Resumen.
- **Definitivo**: la pieza queda en el historial con existencia 0 y estado *Retirada definitiva*.
  No se puede deshacer con un botón.

### 9.2 Devolver una pieza retirada

En la lista de Retiros, en la fila del retiro temporal: botón **Devolver al stock**. La pieza
vuelve a estar disponible para la venta. Sin formularios.

### 9.3 Comprobante

Cada retiro tiene un **PDF** descargable y un botón para enviarlo por **mail o WhatsApp** al
artista (requiere sus datos de contacto en la ficha).

---

## 10. Alquileres

Obra que se alquila a un cliente en vez de venderse.

### 10.1 Nuevo alquiler

*Menú → Alquileres → Nuevo alquiler.*

1. Buscar la/las obra/s por código (también por lote).
2. **Moneda** y **porcentaje de alquiler** sobre el precio de venta de la obra.
   Ej.: obra de $1.000, alquiler al 10 % → $100.
3. **Reparto**: un segundo porcentaje divide ese monto entre **galería y artista**. El sistema
   muestra cuánto le toca a cada parte antes de confirmar.
4. **Cliente**, **fecha de inicio** y **fecha de recupero prevista**.
5. Confirmar.

La pieza queda en estado *Alquilada*: no se puede vender.

### 10.2 Devolver la obra alquilada

Botón **Devolver al stock** en la fila, igual que el retiro temporal. La pieza vuelve a estar
disponible.

### 10.3 En la liquidación

Lo que le toca al artista por el alquiler aparece en su liquidación y en la agenda, con el
detalle "Alquiler", nombre y código de la obra.

---

## 11. Devoluciones

Cuando un cliente devuelve una pieza que **ya había comprado**.

*Menú → Devoluciones → Nueva devolución.*

1. Buscar por código, nombre de obra o artista (entre las ventas registradas).
2. **Motivo** y **fecha**.
3. Indicar si **el artista ya cobró** esa pieza.
4. Confirmar.

Efecto:

- La pieza **vuelve al stock** y deja de figurar como vendida.
- **Si el artista ya cobró**: se genera automáticamente un **adelanto** por ese importe, que se
  descuenta de su próxima liquidación.
- **Si el artista todavía no cobró**: en la próxima liquidación aparece una línea *Devolución*
  que no suma dinero.

---

## 12. Adelantos

Dinero que la galería le adelanta a un artista, a descontar cuando se le liquide.

### 12.1 Listado

*Menú → Adelantos.* Fecha, artista, tipo, moneda, monto, observaciones y una columna que indica
**si ya fue descontado y en qué liquidación**. Se filtra por artista y por fecha.

### 12.2 Nuevo adelanto

*Menú → Adelantos → Nuevo adelanto.* Artista, moneda, monto, fecha y observaciones. Hay **dos
tipos**, para no equivocar el signo:

- **Adelanto**: plata que se le dio al artista. **Resta** en la liquidación.
- **Ajuste a favor**: una corrección a favor del artista. **Suma** en la liquidación.

---

## 13. Liquidaciones

El corazón del sistema: el documento que se le entrega al artista cuando pasa a cobrar.

### 13.1 Generar una liquidación

*Menú → Liquidaciones → Generar* (o el botón **Liquidar** desde la ficha del artista o desde el
Resumen).

1. Elegir el **artista** (desplegable o por código).
2. Elegir la **moneda** (pesos y dólares se liquidan por separado).
3. Elegir el **período / fecha**. El sistema marca cuál fue la última fecha que se le liquidó.
4. Aparece la **vista previa** con todas las líneas:

| Tipo de línea | Qué muestra |
|---|---|
| **Venta de pieza** | Fecha, código, nombre, cantidad y el **costo** (lo que cobra el artista). |
| **Alquiler** | El porcentaje que le toca al artista. |
| **Pago contado** | Importe 0 y la nota "Pago contado". |
| **Adelanto** | Con su fecha; **resta** del total. |
| **Devolución** | Marcada como tal, sin sumar dinero. |

5. Al pie: **total bruto**, menos adelantos, menos devoluciones, igual **total neto a pagar**.

### 13.2 Confirmar

- La vista previa es **obligatoria**: se ve exactamente cómo va a quedar.
- Botón **Confirmar**. Aparece el mensaje *"Liquidación confirmada y registrada"*.
- **Una liquidación confirmada no se puede anular.** Revisar bien antes.
- Queda con **número correlativo**, disponible en PDF (con los datos de la galería en el
  encabezado y la fecha de emisión bien visible) y en la pestaña *Liquidaciones* de la ficha del
  artista.
- Botón para **enviar por mail o WhatsApp** al artista.

### 13.3 Consultar liquidaciones anteriores

*Menú → Liquidaciones.* Listado filtrable por artista y fecha, con el PDF de cada una.

---

## 14. Agenda de pagos

La versión mensual y anticipada de las liquidaciones: sirve para avisarles a los artistas que
tienen plata para cobrar y coordinar cuándo vienen.

*Menú → Agenda de pagos.* Se genera sola. Muestra el mes actual y, por artista:

- Piezas vendidas / alquiladas / plata adelantada en el mes.
- Total en pesos y en dólares.
- **Plata de meses anteriores** (solo si todavía no se liquidó — es el arrastre).
- **Plata total**.
- **Fecha pactada** y **fecha confirmada** (se marcan con un calendario haciendo clic en el campo).
- **Comentarios**.

**El arrastre.** Si un artista vendió en mayo y no vino a cobrar, ese saldo se suma a "meses
anteriores" en junio. Ejemplo: mayo $100 sin cobrar + junio $10.000 → *meses anteriores $100 ·
del mes $10.000 · total $10.100*.

**Clic en la fila**: desglose de las obras (fecha de venta, nombre, código, valor, costo).

**Vista "próximos 7 días"**: filtro para ver a quién se citó esta semana.

**Aviso por mail / WhatsApp.** Botón a la derecha de cada fila con un mensaje predeterminado:

> Estimado *[nombre]*, usted tiene *[$ X / U$S X]* para retirar. La fecha pactada sería el
> *[fecha]*. Recuerde confirmar al *[teléfono]*.

Se pueden **seleccionar varias filas** y preparar todos los avisos juntos. El texto de la
plantilla se edita en *Datos de la galería*.

**Botón Liquidar** directo en la fila: el paso natural cuando el artista aparece a cobrar.

---

## 15. Cambio de precios

*Menú → Cambio de precios.*

- **Unitario**: se ingresa el código de una obra y se abre su ficha para cambiar el precio (lo
  mismo que entrar desde Obras y editar).
- **Global**: se elige un artista y se seleccionan varias de sus piezas. El cambio se aplica
  como **porcentaje** (ej. +15 % a todo) o **precio a precio**.
- **Vista previa obligatoria**: muestra *precio actual → precio nuevo* de cada obra antes de
  aplicar.
- Todo cambio queda en el **historial de precios** de cada obra y en **Auditoría**.

---

## 16. Auditoría

*Menú → Auditoría.* Registro automático de **todos** los cambios del sistema: fecha y hora,
usuario, pantalla, tipo de operación (alta / modificación / baja), tabla, campo, valor anterior y
valor nuevo, y el artista u obra afectada.

Se filtra por fecha, usuario, pantalla y tipo de operación. Se exporta a CSV.

Es la fuente para reconstruir cómo se llegó al stock actual y para saber quién hizo cada cambio.

---

## 17. Configuración

### 17.1 Rubros y técnicas

*Menú → Rubros y técnicas.* Alta, edición y baja de las dos listas que se usan al cargar una
obra. Una lista dada de baja deja de ofrecerse pero no rompe las obras que ya la usan.

### 17.2 Datos de la galería

*Menú → Datos de la galería.* Acá se define:

- **Nombre, dirección y teléfono** de la galería (aparecen en todos los PDF).
- **IVA %** (por defecto 22), **redondeo en pesos** (10), **redondeo en dólares** (1),
  **utilidad por defecto** (50).
- **Plantillas de los mensajes**: aviso de agenda, envío de liquidación, certificado.

> Cambiar el IVA o los redondeos afecta el cálculo de precios de **las obras nuevas y los
> recálculos**, no reescribe los precios ya guardados.

### 17.3 Usuarios

*Menú → Usuarios.* Alta de usuarios, restablecer contraseña, **bloquear / desbloquear**.

- **Nunca se borra un usuario**: se bloquea. Un usuario bloqueado no puede iniciar sesión.
- No se puede bloquear al **usuario actual** ni al **último usuario activo** que queda.
- Contraseña: mínimo 8 caracteres.

---

## 18. Impresión, PDF y envíos

- **Toda lista** se exporta a **CSV** (se abre en Excel) respetando el filtro activo.
- **Los comprobantes** (liquidación, retiro, certificado, etiqueta) tienen **PDF** con diseño de
  documento.
- **Envío por mail**: abre el programa de correo con el mensaje ya escrito.
- **Envío por WhatsApp**: abre WhatsApp Web / la app con el mensaje ya escrito; solo hay que
  presionar enviar. Requiere el celular del artista en su ficha.

---

## 19. Copias de seguridad

Toda la información vive en **una sola carpeta** (`Datos`, con el archivo `app.db` y las imágenes).
Quien instaló el sistema dejó configurada una **copia automática mensual** a un `.zip`.

**Recomendación:** que ese respaldo se guarde en un **disco externo o en la nube** (OneDrive,
Google Drive), no solo en la misma PC. Si el disco de la PC falla, un respaldo guardado ahí mismo
no sirve.

Se puede pedir una copia manual en cualquier momento a quien administra el sistema.

---

## 20. Problemas frecuentes

| Situación | Qué hacer |
|---|---|
| "La cuenta está bloqueada" | Se erró la contraseña 5 veces. Esperar 15 minutos o pedir a otro usuario que la desbloquee en *Usuarios*. |
| Pide iniciar sesión de golpe | Pasaron 30 minutos sin actividad. Es normal. Volver a entrar. |
| No aparece el botón de mail / WhatsApp | El artista no tiene correo o celular en su ficha. Cargarlo en *Artistas → ficha → Datos*. |
| No encuentro una obra para vender | Puede estar retirada, alquilada o sin stock. Buscarla en *Obras* sin filtro de estado y revisar su badge. |
| El precio de venta no es el que esperaba | Revisar costo, utilidad, IVA y los redondeos en *Datos de la galería*. La fórmula está en la sección 1. |
| Cambié algo por error | Buscar el cambio en *Auditoría* para ver el valor anterior y corregirlo a mano. |
| No carga la página | Avisar a quien administra el sistema: puede que la PC donde corre esté apagada o el servicio detenido. |

---

*Fin del manual.*

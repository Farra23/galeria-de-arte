# Análisis del sistema "ACATRAS" — Galería de arte

*Análisis técnico de los 6 archivos Excel — 27/07/2026*

---

## 1. Qué es esto, en una frase

No son seis planillas sueltas: es **una aplicación de gestión de galería de arte en consignación**, programada en VBA sobre Excel. Un archivo es el programa y la base de datos (`Acatràs`), y los otros cinco son **módulos satélite** que ese programa abre, escribe e imprime automáticamente.

El negocio que modela es: la galería recibe obras de artistas en consignación → las vende → le liquida al artista su parte → registra todo el historial.

**Regla clave:** `Acatràs sin los for next.xlsm` abre automáticamente los otros 5 archivos al arrancar (evento `Workbook_Open`). Si abrís cualquier satélite por separado, no funciona: las macros escriben con `Workbooks("Liquidaciones")...`, y si el libro no está abierto, falla.

> ⚠️ **Problema detectado:** la ruta de apertura está fija en el código:
> `C:\Users\YAMA\Desktop\PROGRAMA GALERIA viejo\`
> Si los archivos no están en esa carpeta exacta de esa PC, el arranque falla. Es lo primero a corregir si se quiere mover el sistema.

---

## 2. Mapa de conexiones

```
                    ┌─────────────────────────────────┐
                    │  ACATRÀS sin los for next.xlsm  │  ← EL PROGRAMA + LA BASE
                    │  (menú, formularios, ~100 mód.  │
                    │   VBA, maestros y transacciones)│
                    └───────────────┬─────────────────┘
                                    │ abre y escribe al arrancar
        ┌────────────────┬──────────┼──────────────┬─────────────────┐
        ▼                ▼          ▼              ▼                 ▼
┌───────────────┐ ┌────────────┐ ┌──────────┐ ┌──────────────┐ ┌──────────────┐
│ Liquidaciones │ │ Control    │ │ Entregas │ │  Historico   │ │ CorreoLiqui  │
│    .xlsm      │ │  .xlsx     │ │  .xlsx   │ │Entregas.xlsx │ │   .xlsx      │
│ CUENTA        │ │ AUDITORÍA  │ │ REMITO   │ │ ARCHIVO de   │ │ ADJUNTO para │
│ CORRIENTE del │ │ (log de    │ │ del día  │ │ remitos por  │ │ mandar por   │
│ artista       │ │  cambios)  │ │          │ │ artista      │ │ mail         │
└───────────────┘ └────────────┘ └──────────┘ └──────────────┘ └──────────────┘
```

Los 5 satélites además tienen un **vínculo externo de Excel** apuntando a `Acatràs sin los for next.xlsm` (fórmulas que leen datos del maestro), no solo la conexión por macro.

### Clave que une todo: el código de obra de 6 dígitos

```
   998027
   └┬┘└┬┘
    │  └── Código de Obra (3 díg.)  → identifica la pieza dentro de ese artista
    └───── Código de Artista (3 díg.) → apunta a la hoja "Artistas"
```

En la hoja `Obras`, la columna A lo arma con `=TRIM(B)&TRIM(C)`. Ese código de 6 dígitos es lo que viaja a `Ventas`, `Movimientos`, `Liquidaciones!Pagos`, `Control!CambioPrecios`, `Entregas`, etc. **Es la llave primaria de todo el sistema.**

El otro nexo es el **nombre del artista en formato `"Apellido, Nombre"`**, que se usa para nombrar hojas enteras en `Liquidaciones` e `Historico Entregas`.

---

## 3. Archivo por archivo, hoja por hoja

---

### 📘 `Acatràs sin los for next.xlsm` — 17 hojas · 5,8 MB · ~100 módulos VBA

Es el corazón. Contiene la base de datos y toda la lógica.

#### Hojas de datos maestros

| Hoja | Contenido | Volumen |
|---|---|---|
| **Artistas** | Ficha del artista: Código, Apellido, Nombre, Perfil, Taller, Celular, Tel. Fijo, Dirección, Correo, "Próximo Código" (contador del siguiente nº de obra libre para ese artista) | **262 artistas**, códigos 102–910 |
| **Clientes** | Apellido, País, Ciudad, Teléfono, Correo, Perfil de Compra | Prácticamente **vacía** — la ficha existe pero no se usa |
| **Rubro** | Lista de validación: Aguafuerte, Cerámica, Dibujo, Ensamblajes… | 32 rubros |
| **Tècnica** | Lista de validación: ACRILICO, ACRILICO S/LIENZO, ACRILICO S/CARTÓN… | 63 técnicas |
| **Param** | 3 parámetros globales del negocio: **Redondeo Pesos = 10**, **IVA = 22 %**, **Redondeo Dólar = 1** | 3 filas |
| **Dolar** | Cotización DLS. USA BILLETE (fecha, venta, compra, arbitraje). Se bajaba del BCU/DGI | **301 cotizaciones, 03/12/2018 → 13/03/2020 — desactualizada hace 6 años** |

#### Hojas transaccionales (el libro mayor)

| Hoja | Contenido | Volumen |
|---|---|---|
| **Obras** | El **catálogo/inventario**. Cód.Artista, Cód.Obra, Artista, Obra, Iva (V/F), Uti.(% utilidad), Moneda, Costo, Precio Venta, Perfil, Técnica, **Existencia**, Largo/Alto/Ancho, Observaciones, Pago Contado, Fecha Ingreso | **14.696 obras** · 277 artistas · **9.056 unidades en stock** · ingresos 2009→2026 |
| **Movimientos** | El **libro diario de stock**. Fecha, E/S, Tipo Movimiento, Cantidad, Cód.Artista, Cód.Obra, Fecha Pago, Saldo Pago contado, Tarjeta, Efectivo, Moneda | **26.020 movimientos** (2002→2026): *Venta 13.840 · Inicial 10.224 · Ingreso 1.257 · Ajuste 649 · Devolución 50* |
| **Ventas** | Detalle de cada venta. Fecha, **Boleta** (nº factura tipo `10079-1`), Cód.Artista, Cód.Obra, Artista, Nombre Obra, Cantidad, Iva, Per% (comisión), Moneda, Costo, Venta, Observaciones, Cliente, Total Venta, Cotización del día, Conversión U$S→$ | **13.840 ventas**, 2018→2026. 10.651 en Pesos / 3.163 en Dólares |

*Verificación cruzada: 13.840 ventas en `Ventas` = 13.840 movimientos tipo "Venta" en `Movimientos`. Las dos tablas están perfectamente sincronizadas.*

#### Hojas de interfaz y trabajo

| Hoja | Para qué sirve |
|---|---|
| **MenuInicio** | Pantalla de arranque (vacía; sobre ella se despliega el formulario `UserForm13`, el menú real) |
| **Menu** | La **matriz del menú**: filas = módulos (Artistas, Clientes, Obras, Movimientos, Ventas, Dólar…), columnas = acciones (Ver Información, Ingresar, Liquidación, Cambio de Precios, Consulta Filtrada, Facturación, Listado Ventas, Cotizador DGI-BCU…). El formulario lee esta grilla para armar las opciones |
| **MenuInicioAnte** | Menú viejo, **oculto**. Residuo de una versión anterior |
| **Hist.Ventas** | Mini-informe fiscal mensual: Venta Total, Venta Tasa Básica, Venta Exenta, IVA de ventas, separado Pesos/Dólares + coeficiente a aplicar a las compras. **Congelado en Julio 2020** |
| **ConsultaObras** | Hoja-formulario en blanco donde se vuelcan los resultados de la consulta filtrada de obras |
| **ImpObrasFil** | Salida **imprimible** del filtro de obras (Código, Obra, Costo, Precio Venta, Existencia, Fecha Ingreso). Tiene área de impresión definida |
| **Stock 0** | Extracción puntual de obras con existencia en 0 (ejemplo con Lozoya, Graciela). Hoja de trabajo suelta |
| **Chart1** | Hoja de gráfico |

---

### 📗 `Liquidaciones.xlsm` — 186 hojas · 4,4 MB

**La cuenta corriente de cada artista.** Es el satélite más importante. 7 hojas de sistema + **179 hojas, una por artista**.

#### Hojas de sistema

| Hoja | Qué hace |
|---|---|
| **Actual** | **La liquidación que se está generando ahora mismo** (plantilla de trabajo). Encabezado: Artista, Moneda. Detalle: Año, Mes, Fecha, Código, Cantidad, Obra, Pre.Uni., Total. Cierra con **"Total a Pagar"**, y si corresponde: *Adel. $* (adelantos), *DevoCli* (devoluciones) y *Nuevo Tot.* Se borra y se rearma en cada liquidación |
| **Indice** | Registro de todas las liquidaciones emitidas: Fecha, Artista y un **hipervínculo** a la hoja del artista. Es el índice navegable del archivo |
| **Listado Ventas** | Resumen del período por artista: Artista, Piezas, Pesos, Dólares, **Fecha Pactada**, **Estado**, Comentarios. Es la **agenda de pagos**: quién cobra, cuánto y cuándo. Tiene ordenamiento por doble clic en el encabezado y una columna auxiliar con los días de la semana. *86 artistas listados actualmente* |
| **Pagos** | **Histórico definitivo de lo pagado**. Fecha Pago, Artista, Año/Mes/Fecha de Venta, Código, Cantidad, Obra, Moneda, Pre.Uni., Total. **2.820 líneas pagadas** |
| **Adelantos** | Adelantos de dinero al artista, a descontar de la próxima liquidación: Fecha, Cód.Artista, Artista, Importe, Moneda, **Fecha Descontado**, Cód.Obra. *13 registros vigentes* |
| **Retiros** | Obras que el artista se lleva de vuelta: Cód.Obra, Nombre, Cant. Devuelta, Moneda, Costo, Total. Se llena de a un artista por vez |
| **Alquiler** | Obras alquiladas (no vendidas): Fecha, Cód.Obra, Nombre, Fecha Retiro, **Fecha Recupero**, Importe Alquiler, Moneda, Factura. *Uso muy bajo: 2 registros* |

#### Las 179 hojas de artista

Una por artista, nombradas `"Apellido, Nombre"` (`López, Diego`, `Perotti, Marcelo`…). Cada una es el **archivo histórico apilado**: cada vez que se liquida, la macro `Respaldar` copia el bloque `A1:I66` de la hoja `Actual` y lo pega **debajo del anterior**, en saltos de 66 filas, guardando el contador de bloques en la celda `CV1` (columna 100). Por eso `López, Diego` tiene 2.309 filas: son ~35 liquidaciones apiladas desde 2018.

---

### 📙 `Control.xlsx` — 5 hojas · 5,6 MB

**El log de auditoría.** Nadie lo mira a diario, pero registra todo cambio de dato sensible.

| Hoja | Qué contiene |
|---|---|
| **Sheet1** | **Log de modificaciones**: valor nuevo, Fecha, Hora, Formulario, Campo modificado, Cód.Artista, Cód.Obra. **130.352 registros** (2009→2026), todos del formulario `IngObras`. Campos auditados: **Costo (51.355) · Precio Venta (40.345) · Moneda (23.075) · Existencia (15.577)**. La celda `Q1` guarda el contador de la próxima fila libre |
| **CambioPrecios** | Historial de repricing: Código, Fecha, Fecha Vigencia, **Precio Anterior**, **Precio Actual**, Método (**Global** 878 / **Individual** 447). **1.325 cambios**, 2019→2025 |
| **Sheet2** | Hoja de depuración: obras detectadas "Sin Moneda en ventas" — inconsistencias a corregir |
| **Sheet3** | Vacía |
| **Chart1** | Hoja de gráfico |

---

### 📕 `Entregas.xlsx` — 4 hojas · 239 KB

**El remito del día.** Cuando un artista trae obra nueva, el sistema imprime este comprobante.

| Hoja | Qué contiene |
|---|---|
| **Actual** | El remito en curso: Artista, Fecha, y grilla de Cód.Obra, Nombre de la obra, Cantidad, Pesos, Dólares. La macro limpia los bloques (filas 5-14, 21-30, 38-47, 54-63 → 4 copias en la misma página) y los rellena desde el formulario de ingreso de obras. Tiene **área de impresión definida (`A2:F63`)** y puede enviarse por mail al artista con `SendMail` usando el correo de la hoja `Artistas` |
| Sheet2 / Sheet3 / Chart1 | Vacías / gráfico |

---

### 📒 `Historico Entregas.xlsx` — 6 hojas · 35 KB

**El archivo de los remitos.** Misma lógica de apilado que Liquidaciones: la macro `RespaldarEntrega` copia el remito de `Entregas!Actual` a la hoja del artista y agrega una línea al índice.

| Hoja | Qué contiene |
|---|---|
| **Indice** | Fecha + Artista de cada entrega archivada |
| **Aquel Ciprés Taller** · **Apfelbaum Helene** · **Guias** | Histórico apilado de entregas por artista (mismo formato: Cód.Obra, Nombre, Cantidad, Pesos, Dólares) |
| Sheet1 / Sheet3 | Vacías |

> Solo hay **3 artistas archivados** acá contra 179 en Liquidaciones. El circuito de entregas se usa mucho menos que el de liquidaciones, o se dejó de usar.

---

### 📓 `CorreoLiqui.xlsx` — 3 hojas · 19 KB

**El adjunto que se manda por mail.** Es la copia "limpia" de la liquidación, sin macros ni el resto del sistema, para que el artista la reciba sin riesgo.

| Hoja | Qué contiene |
|---|---|
| **Sheet1** | Copia exacta del rango `B2:I500` de `Liquidaciones!Actual`. Ahora tiene la liquidación de *Oyhantcabal, Marcela* (feb-mar 2026, en Pesos) |
| Sheet2 / Sheet3 | Vacías |

---

## 4. Los dos circuitos del negocio

### 🔵 Circuito A — Ingreso de obra
```
Formulario IngObra
      │
      ├──▶ Acatràs!Obras           (alta de la pieza, código 6 dígitos)
      ├──▶ Acatràs!Movimientos     (tipo "Ingreso"/"Inicial")
      ├──▶ Control!Sheet1          (log: qué campo se cargó, cuándo, quién)
      ├──▶ Entregas!Actual         (remito imprimible)
      │         └──▶ mail al artista (SendMail, correo de hoja Artistas)
      └──▶ Historico Entregas!<Artista> + !Indice   (archivo)
```

### 🟢 Circuito B — Venta y liquidación
```
Venta registrada
      │
      ├──▶ Acatràs!Ventas + !Movimientos (tipo "Venta")  [descuenta Existencia]
      │
      ▼   [se ejecuta la liquidación del artista, macro `liqui`]
Liquidaciones!Actual   ← recorre Movimientos filtrando por artista, período y moneda
      │                  (corre por separado Pesos y Dólares)
      │
      ├──▶ resta Adelantos     (Liquidaciones!Adelantos)
      ├──▶ resta DevoCliente   (devoluciones de cliente)
      ├──▶ suma Alquiler       (si hubo alquiler de obra)
      │
      ├──▶ CorreoLiqui!Sheet1                  (copia para enviar por mail)
      ├──▶ Liquidaciones!Pagos                 (histórico de lo pagado)
      ├──▶ Liquidaciones!<Artista> + !Indice   (respaldo apilado + hipervínculo)
      └──▶ Liquidaciones!Listado Ventas        (agenda de a quién pagar)
```

### Cómo se forma el precio

`Obras` guarda **Costo** (lo que cobra el artista) y **Precio Venta** (lo que paga el cliente). La utilidad de la galería está en la columna **Uti.** (típicamente 50 %), y el flag **Iva** decide si se aplica el 22 % de `Param`:

- Sin IVA: `Precio Venta = Costo × (1 + Uti/100)` → 520 × 1,5 = **780** ✓
- Con IVA: `Precio Venta = Costo × 1,22 × (1 + Uti/100)` → 250 × 1,22 × 1,5 = **457,50** ✓

Al liquidar, al artista se le paga el **Costo**, no el Precio Venta.

---

## 5. Problemas y riesgos detectados

| # | Hallazgo | Impacto |
|---|---|---|
| 1 | **Ruta fija** `C:\Users\YAMA\Desktop\PROGRAMA GALERIA viejo\` en `Workbook_Open` | 🔴 Alto — el sistema no arranca fuera de esa PC/carpeta |
| 2 | **Hoja `Dolar` congelada en 13/03/2020** (301 cotizaciones) | 🔴 Alto — toda conversión U$S→$ desde 2020 usa datos de hace 6 años |
| 3 | **Nombres de artista duplicados/mal formateados**: 32 de las 179 hojas de Liquidaciones no coinciden con el maestro `Artistas`. Ej.: `Dario Vairoletti, ` vs `Dario, Vairoletti`; `Cynthia Godoy, ` vs `Cynthia, Godoy`; `Batitu - Alejandro Maya, Del` vs `Batitu Alejandro Maya, Del`; `Dean , ` / `Dean, Williams` / `Williams, Dean` (3 hojas para la misma persona) | 🔴 Alto — un artista puede tener su historial partido en 2-3 hojas |
| 4 | **1.129 filas de `Obras`** tienen el nombre de artista desalineado respecto al maestro (mismo problema de formato) | 🟡 Medio — rompe búsquedas y filtros por nombre |
| 5 | **Fechas imposibles**: un movimiento tipo "Venta" fechado **12/10/2102**; una venta fechada 14/12/2026 sin nº de boleta | 🟡 Medio — error de tipeo que distorsiona rangos y reportes |
| 6 | **Hoja `Clientes` vacía** y campo `Cliente` de `Ventas` sin usar | 🟡 Medio — no hay trazabilidad de a quién se vendió; imposible hacer CRM o repetición de compra |
| 7 | **`Hist.Ventas` congelada en Julio 2020** | 🟡 Medio — el informe de IVA quedó abandonado |
| 8 | **`Historico Entregas` con solo 3 artistas** vs 179 en Liquidaciones | 🟡 Medio — el circuito de remitos se abandonó |
| 9 | **26 ventas sin Moneda** (detectadas también en `Control!Sheet2`) | 🟡 Medio — no se puede saber si son pesos o dólares |
| 10 | `Liquidaciones` con **186 hojas** creciendo por artista, y bloques apilados de 66 filas | 🟠 Estructural — Excel no está pensado para esto; el archivo pesa 4,4 MB y se degrada |
| 11 | Todo depende de **5 libros abiertos simultáneamente** y de rangos con posiciones fijas (`B2:I66`, `Cells(1,20)` como contador) | 🟠 Estructural — muy frágil, cualquier fila insertada a mano rompe el sistema |
| 12 | El nombre del archivo (`sin los for next`) y hojas como `MenuInicioAnte`, `Stock 0`, `Sheet2/Sheet3` indican versiones en paralelo | 🟢 Bajo — deuda técnica / desorden |

---

## 6. Resumen ejecutivo

**Lo que hay:** un ERP artesanal de galería, funcionando desde 2009, con 14.696 obras, 26.020 movimientos, 13.840 ventas y 262 artistas. Está bien pensado — tiene log de auditoría, control de adelantos, respaldo histórico por artista e hipervínculos de navegación. No es una planilla improvisada.

**Dónde duele:** la arquitectura (5 libros abiertos + rutas fijas + rangos hardcodeados) lo hace frágil e imposible de mover de máquina, y la **falta de una tabla normalizada de artistas** ya generó historiales partidos. El dólar sin actualizar desde 2020 es el problema con impacto contable más inmediato.

**Qué haría primero (sin rehacer nada):**

1. Cambiar la ruta fija por `ThisWorkbook.Path` — 5 minutos, resuelve la portabilidad.
2. Actualizar la hoja `Dolar` (o conectarla al BCU).
3. Unificar los nombres de artista: una lista canónica en `Artistas` y consolidar las 32 hojas duplicadas de `Liquidaciones`.
4. Corregir las fechas imposibles (2102) y las 26 ventas sin moneda.

**A mediano plazo:** los datos ya tienen forma relacional (Artistas → Obras → Movimientos/Ventas → Pagos, todo unido por el código de 6 dígitos). Migrar a una base real es directo; el trabajo estaría en portar la lógica VBA, no en modelar los datos.

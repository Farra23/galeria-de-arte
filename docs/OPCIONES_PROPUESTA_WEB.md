# Propuesta web — Galería ACATRAS
## Las 4 opciones, pantallas, y guion para la reunión

*Documento de trabajo — todo derivado del análisis de los 6 archivos Excel · Julio 2026*

---

## Punto de partida: qué hay hoy

| Dato | Valor |
|---|---|
| Obras en catálogo | 14.696 (6.222 con stock, 9.145 unidades) |
| Artistas | 262 en el maestro · 256 con stock |
| Movimientos (libro diario) | 26.020 (desde 2002) |
| Ventas registradas | 13.840 (2018–2026) |
| Pagos a artistas | 2.820 |
| Registros de auditoría | 130.352 |
| Ticket mediano | $825 UYU (~USD 20) |
| Facturación 2025 | ~$2,2M UYU (~USD 55k) |
| **Fotos de obras** | **0 en los archivos** |

Todo el sistema pesa ~16 MB, de los cuales la mayoría es sobrecarga de Excel. **Los datos reales son unas 190.000 filas: en una base de datos son decenas de megas.** El volumen no es el problema. La arquitectura sí.

---

# PARTE 1 — Las 4 opciones

## Comparativa rápida

| | **1. Escritorio** | **2. Nube (solo ERP)** | **3. Doble app** | **4. App única con login** |
|---|---|---|---|---|
| **Quién entra** | 1 admin, 1 PC | Admins, desde cualquier lado | Público + admins | Público + admins |
| **Hosting** | No | Sí | Sí (×2) | Sí |
| **Dominio** | No | Opcional | Sí | Sí |
| **Base de datos** | Local (archivo) | En la nube | En la nube, compartida | En la nube, única |
| **Se hace con Blazor** | No (WPF/MAUI) | Sí | Sí | Sí |
| **Complejidad** | Baja | Media | Media-alta | Media-alta |
| **Backups** | Manuales (riesgo) | Automáticos | Automáticos | Automáticos |
| **Resuelve el Excel** | Parcial | Sí | Sí | Sí |
| **Da presencia web** | No | No | Sí | Sí |
| **Recomendación** | ❌ | ✅ si el alcance es solo interno | ⚠️ | ✅✅ **la mejor** |

---

## Opción 1 — App de escritorio en una sola PC

**Cómo funciona.** Un programa que se instala en la PC de la galería. La base de datos es un archivo en el disco de esa misma máquina.

**Qué necesitás:** nada más que la PC. Sin hosting, sin dominio, sin costo mensual.

**Stack:** WPF, WinForms o .NET MAUI + SQLite o SQL Server LocalDB. **No es Blazor.** Se podría hacer con Blazor Hybrid dentro de MAUI, pero no te aporta nada frente a WPF y te complica.

**Complejidad:** la más baja de las cuatro en infraestructura.

### El problema

Esta opción **reproduce exactamente la limitación que tiene hoy**: una sola máquina, una sola persona a la vez, sin acceso remoto, backups a mano o inexistentes. Si se rompe el disco, se perdió la contabilidad de 17 años.

Es cambiar Excel por otro programa igual de atado. Resuelve la fragilidad del VBA, pero no el problema de fondo. **Y para que dos personas compartan datos, no sirve.**

> Solo tiene sentido si la galería tiene internet malo, o si el dueño se niega a que los datos salgan del local. Es una decisión de él, no técnica.

---

## Opción 2 — App en la nube, solo gestión

**Cómo funciona.** La app y la base viven en un servidor alquilado. Cada administrador abre el navegador, entra a una URL, se loguea y trabaja. Desde la galería, desde su casa o desde el celular. No instala nada.

**Qué necesitás:**

- **Hosting:** sí, pero chico. Unos pocos dólares al mes.
- **Dominio:** **no es obligatorio.** El hosting te da un subdominio gratis (tipo `acatras-gestion.hosting.net`). El dominio propio recién lo comprás si se hace el sitio público.
- **Base de datos:** PostgreSQL o SQL Server administrado (el proveedor hace los backups).

**Stack:** Blazor Web App (.NET 9) con render mode `InteractiveServer` + EF Core + ASP.NET Core Identity para login y roles. SEO no importa: es privado.

**Complejidad:** media. Es la opción con **mejor relación esfuerzo/valor operativo**: mata el Excel, permite varios usuarios, backups automáticos y acceso remoto.

**Limitación:** no le da al dueño nada visible. Si lo que quiere es "tener una página", esto no se la da.

---

## Opción 3 — Dos apps separadas contra la misma base

**Cómo funciona.** Dos proyectos independientes que apuntan a la misma base de datos. El sitio público lee; el panel de admin lee y escribe. **No hay sincronización entre ellos: comparten el dato, no lo copian.** Si el admin carga una obra, el público la ve en la siguiente carga de página.

**Qué necesitás:**

- **Hosting:** sí, y **dos deploys** (o dos servicios en el mismo servidor).
- **Dominio:** sí, para el público. El admin puede ir en un subdominio (`admin.galeria.uy`).
- **Base de datos:** una sola, compartida.

**Stack:** dos proyectos Blazor + una biblioteca de clases común con el modelo y el acceso a datos (para no duplicar código).

**Complejidad:** media-alta. Más alta que la 4, **no por programar sino por mantener**: dos configuraciones, dos deploys, dos certificados, dos lugares donde equivocarse.

**Ventaja real:** aislamiento. El proceso que atiende a internet no es el mismo que maneja la gestión. Es un plus de seguridad genuino, pero para una galería de este tamaño está sobredimensionado.

---

## Opción 4 — Una sola app con login por rol ⭐

**Cómo funciona.** Un solo proyecto, un solo deploy. Sin loguearte ves el catálogo público; al entrar con tu cuenta, según tu rol, se te habilitan las pantallas de gestión.

**Qué necesitás:**

- **Hosting:** sí, uno solo.
- **Dominio:** sí.
- **Base de datos:** una.

**Stack:** Blazor Web App con **render modes mixtos**, que es la clave técnica:

- **Público → `Static SSR`.** El servidor manda HTML ya armado. Google indexa cada obra y cada artista. Rápido y barato: no consume recursos por visitante.
- **Admin → `InteractiveServer`.** Conexión viva, formularios reactivos de verdad. Solo para los 2 o 3 usuarios logueados.

Los dos modos conviven en la misma app, se elige página por página.

> ⚠️ **No usar Blazor WebAssembly en la parte pública.** El navegador arma la página después de descargar el código, y Google llega antes: no indexa nada. Para una galería, la vitrina indexable *es* el producto.

**Complejidad:** media-alta para programar, pero **la más simple de operar**: un deploy, una config, un certificado.

**Por qué es la mejor:** hace todo lo que hace la 3 con menos infraestructura, y si más adelante querés cuentas de usuario final o pasarela de pago, ya tenés Identity montado — es agregar, no reescribir.

---

# PARTE 2 — Las pantallas, derivadas de las planillas

De **6 archivos y ~215 hojas quedan unas 20 pantallas.** No porque se recorte funcionalidad, sino porque muchas hojas eran parches:

- Las **179 hojas de artista** de `Liquidaciones` → **una sola pantalla** con filtro por artista
- `ConsultaObras`, `ImpObrasFil`, `Stock 0` → eran filtros manuales → **filtros en la grilla de Obras**
- `Menu`, `MenuInicio`, `MenuInicioAnte` → **navegación nativa de la web**
- `CorreoLiqui` (archivo entero) → **botón "Generar PDF y enviar"**
- `Control!Sheet2` (obras sin moneda) → **validación en el formulario**, el error no llega a existir

## Panel de administración (existe en las 4 opciones)

### Módulo Catálogo

| Pantalla | Viene de | Qué tiene |
|---|---|---|
| **Obras** | `Acatràs!Obras` (14.696) | El CRUD central. Cód. Artista, Cód. Obra, Artista, Nombre, IVA (sí/no), Utilidad %, Moneda, Costo, Precio Venta, Rubro, Técnica, **Existencia**, Largo/Alto/Ancho, Observaciones, Pago Contado, Fecha Ingreso, **+ fotos**. Filtros por artista, rubro, técnica, stock, rango de precio. Precio calculado automático |
| **Artistas** | `Acatràs!Artistas` (262) | Código, Apellido, Nombre, Perfil, Taller, Celular, Tel. Fijo, Dirección, Correo. **Además:** sus obras, su estado de cuenta y su historial de liquidaciones en la misma ficha |
| **Clientes** | `Acatràs!Clientes` (vacía) | Apellido, País, Ciudad, Teléfono, Correo, Perfil de Compra. **Hoy no se usa** — decisión a tomar con el cliente |

### Módulo Operación

| Pantalla | Viene de | Qué tiene |
|---|---|---|
| **Movimientos** | `Acatràs!Movimientos` (26.020) | Libro diario de stock. Fecha, E/S, Tipo, Cantidad, Cód. Artista, Cód. Obra, Moneda. Tipos: **Venta, Inicial, Ingreso, Ajuste, Devolución** |
| **Ventas** | `Acatràs!Ventas` (13.840) | Registrar venta: Fecha, Boleta, Obra, Cantidad, Cliente, Moneda, Precio, Cotización del día. **Descuenta stock y genera el movimiento automáticamente** |
| **Remitos de ingreso** | `Entregas` + `Historico Entregas` | Cuando el artista trae obra: se genera el comprobante, se imprime o se manda por mail, y queda archivado. Hoy solo hay 3 artistas archivados — el circuito se abandonó |
| **Retiros / Devoluciones** | `Liquidaciones!Retiros` | Obra que el artista se lleva. Cód. Obra, Cantidad, Moneda, Costo. Repone stock |

### Módulo Liquidaciones (el corazón del negocio)

| Pantalla | Viene de | Qué tiene |
|---|---|---|
| **Generar liquidación** | `Liquidaciones!Actual` | Elegís artista y período → arma el detalle (Año, Mes, Fecha, Código, Cantidad, Obra, Pre.Uni., Total), calcula **Total a Pagar**, resta **adelantos** y **devoluciones**, muestra **Nuevo Total**. Corre Pesos y Dólares por separado. Modo **borrador** y **definitivo** |
| **Historial de liquidaciones** | `Liquidaciones!Indice` + las 179 hojas | Todas las liquidaciones emitidas, filtrables por artista y fecha. **Reemplaza las 179 hojas** |
| **Agenda de pagos** | `Liquidaciones!Listado Ventas` | Artista, Piezas, Pesos, Dólares, **Fecha Pactada**, **Estado**, Comentarios. Quién cobra, cuánto y cuándo |
| **Pagos** | `Liquidaciones!Pagos` (2.820) | Histórico de lo efectivamente pagado |
| **Adelantos** | `Liquidaciones!Adelantos` | Plata adelantada al artista, a descontar. Fecha, Importe, Moneda, **Fecha Descontado** |
| **Alquileres** | `Liquidaciones!Alquiler` | Obra alquilada, no vendida. Fecha Retiro, **Fecha Recupero**, Importe, Factura. *Solo 2 registros: preguntar si se usa* |

### Módulo Control

| Pantalla | Viene de | Qué tiene |
|---|---|---|
| **Auditoría** | `Control!Sheet1` (130.352) | Quién cambió qué, cuándo. Campo, valor anterior, valor nuevo, usuario, fecha/hora. **Mejora sobre el Excel: hoy no guarda quién lo hizo** |
| **Cambios de precio** | `Control!CambioPrecios` (1.325) | Historial de repricing + herramienta de **cambio global** (hoy: 878 globales / 447 individuales) |

### Módulo Configuración

| Pantalla | Viene de | Qué tiene |
|---|---|---|
| **Rubros y Técnicas** | `Rubro` (32) + `Tècnica` (63) | ABM de las listas. 3.303 obras hoy no tienen técnica cargada |
| **Parámetros** | `Param` | Redondeo Pesos (10), **IVA (22%)**, Redondeo Dólar (1) |
| **Cotización del dólar** | `Dolar` | **Congelada desde 13/03/2020.** En la web: carga automática desde el BCU |
| **Usuarios y roles** | *no existe hoy* | Admin / Operador. Necesario para que la auditoría diga quién hizo qué |

### Reportes

| Pantalla | Viene de | Qué tiene |
|---|---|---|
| **Reporte fiscal** | `Hist.Ventas` | Venta Total, Tasa Básica, Exenta, IVA de ventas, por mes y moneda. **Congelado en julio 2020** |
| **Dashboard** | *nuevo* | Ventas del mes, stock por artista, obras sin movimiento, artistas pendientes de pago |

## Sitio público (solo opciones 3 y 4)

| Pantalla | Qué tiene |
|---|---|
| **Inicio** | Obras destacadas, novedades |
| **Catálogo** | Las 6.222 obras con stock. Filtros por rubro, técnica, artista, precio, medidas |
| **Ficha de obra** | Foto, nombre, artista, técnica, medidas, precio, disponibilidad, botón de consulta |
| **Artistas** | Los 256 con stock, con su ficha y su obra |
| **Noticias / Exposiciones** | *No existe en el Excel — contenido nuevo a cargar* |
| **Contacto** | Consulta por pieza (mail o WhatsApp) |
| **Carrito y pago** | *Solo si el cliente lo pide. Ver advertencias abajo* |

---

# PARTE 3 — Qué decirle al cliente

## Los 4 temas que hay que poner sobre la mesa sí o sí

### 1. Las fotos son el mayor riesgo del proyecto

**No hay una sola foto en los 6 archivos.** El código VBA las buscaba en carpetas locales (`C:\Users\user\Documents\Acatras\programa stock\imagenes\`), con el nombre = código de 6 dígitos + `.jpg`.

- **Si esa carpeta existe y está completa:** la migración es un script de 20 líneas. Sin costo.
- **Si no existe:** hay que fotografiar **6.222 obras**. Eso cuesta más que el software y no lo hacés vos.

**Sin fotos no hay sitio público.** Un catálogo de arte sin imágenes no sirve. Esto define si las opciones 3 y 4 son viables.

### 2. El sistema actual está colgado de una PC ajena

El Excel abre los otros 5 archivos desde una ruta fija: `C:\Users\YAMA\Desktop\PROGRAMA GALERIA viejo\`. Si esa carpeta no existe exactamente así, no arranca. **Vale la pena preguntar quién es "YAMA" y si esa máquina sigue en la galería.**

### 3. Hay errores de datos que le están costando plata hoy

- **Dólar congelado desde marzo 2020.** Todas las conversiones U$S→$ de los últimos 6 años usan datos viejos.
- **32 artistas con el historial partido** por diferencias de tipeo en el nombre. *"Dean Williams" tiene tres hojas distintas.* Eso significa liquidaciones incompletas.
- **26 ventas sin moneda cargada** — no se sabe si son pesos o dólares.
- **Fechas imposibles**: una venta fechada en el año **2102**.
- **La hoja Clientes está vacía.** Hoy la galería no sabe a quién le vendió. No puede hacer seguimiento, ni avisar de una muestra, ni saber quién es cliente frecuente.

### 4. No pueden convivir dos sistemas

Si el sistema web maneja el catálogo pero el Excel sigue liquidando, hay **doble carga y dos fuentes de verdad**. Es el peor escenario posible, peor que dejar todo como está. Hay que decidir: o la web reemplaza al Excel, o la web es un espejo de solo lectura. **Definirlo antes de firmar.**

## Sobre plata e infraestructura

- **El hosting lo paga la galería, no el desarrollador.** Es un gasto operativo como la luz. Va en la cotización como línea aparte, con la tarjeta del dueño. Además así la infraestructura no queda atada a vos si algún día dejás el proyecto.
- **Existen planes gratuitos** (MonsterASP.NET para .NET, Neon o Supabase para la base) y sirven perfecto para desarrollar y para mostrarle la demo. **Pero no para producción:** no tienen backups ni garantía de servicio. Esto es la contabilidad de un negocio de USD 55.000 al año.
- **Los datos entran holgados en cualquier plan; las fotos no.** 6.222 imágenes optimizadas son 2–4 GB y se pasan de todos los planes gratuitos de almacenamiento.

## Sobre vender online

Con **ticket mediano de USD 20** y piezas mayormente **únicas (existencia 1)**, una pasarela de pago obliga a resolver reserva de stock en tiempo real, envíos y devoluciones, y las comisiones se comen el margen. **Recomendación: no en la primera etapa.** Arrancar con "consultar por esta pieza" por WhatsApp o mail, medir cuántas consultas llegan, y recién ahí decidir.

## Cómo presentar el precio

No cotizar "una página web". Cotizar **fases**, para que el dueño elija cuánto compra:

| Fase | Qué incluye | Peso |
|---|---|---|
| **F0** | Migración de datos y modelo | Chico |
| **F1** | Catálogo público + admin de obras/artistas/fotos | Medio |
| **F2** | Núcleo operativo: movimientos, ventas, liquidaciones, adelantos, retiros, auditoría | **El más grande — acá está el valor real** |
| **F3** | Cuentas de usuario, pasarela, noticias | Medio |

La línea de **las fotos va separada y aclarando que no es desarrollo.**

---

# PARTE 4 — Preguntas para la entrevista

## La pregunta que ordena todo

> **"¿Qué querés lograr: vender más, mostrar el catálogo, o dejar de sufrir el Excel?"**

Si dice *vender* → opciones 3 o 4. Si dice *mostrar* → opción 4 mínima. Si dice *el Excel* → opción 2 y te ahorrás la mitad del proyecto.

## Sobre las fotos (crítico)

1. ¿Existen fotos de las obras? ¿Dónde están y cómo se llaman los archivos?
2. ¿Todas las piezas tienen foto, o solo algunas?
3. ¿Quién las saca hoy? ¿Con qué calidad?
4. Si hay que fotografiar 6.222 obras, ¿quién lo hace y en cuánto tiempo?

## Sobre el uso actual

5. ¿Cuántas personas usan el sistema? ¿Al mismo tiempo?
6. ¿Quién es "YAMA"? ¿Esa PC sigue en la galería?
7. ¿Qué es lo que más tiempo les lleva hoy?
8. ¿Qué es lo que más se rompe o más bronca les da?
9. ¿Alguien necesita entrar desde afuera de la galería?
10. ¿Hacen backup de los Excel? ¿Cada cuánto?

## Sobre la operación

11. ¿Cada cuánto se le liquida a los artistas? ¿Fecha fija o cuando se puede?
12. ¿Cómo se le paga: transferencia, efectivo? ¿Queda comprobante?
13. **¿El % de utilidad es siempre 50, o varía por artista?** (en las planillas es casi siempre 50)
14. ¿Cómo se emite la factura hoy? ¿El número de boleta lo genera el Excel o un sistema fiscal aparte?
15. **¿Usan facturación electrónica ante DGI?** Si hay un sistema fiscal, el nuevo no debería duplicarlo — se integra o se deja afuera.
16. ¿Alquilan obras? *(solo 2 registros en 4 años — ¿se dejó de usar?)*
17. ¿Usan los remitos de entrega? *(solo 3 artistas archivados de 179)*
18. ¿Qué pasa cuando una obra no se vende en mucho tiempo?
19. ¿Manejan descuentos o promociones?

## Sobre clientes

20. **La hoja Clientes está vacía. ¿Guardan datos de compradores en algún otro lado?**
21. ¿Les interesaría saber quién compra qué, para avisarle de nuevas piezas?

## Sobre la migración

22. ¿Migramos los 17 años de historia, o desde cierta fecha?
23. ¿Los 130.000 registros de auditoría hay que conservarlos?
24. Hay artistas cargados con el nombre de varias formas distintas. **¿Alguien puede revisar y confirmar cuáles son la misma persona?** (son 32 casos)

## Sobre el sitio público

25. ¿Querés vender online de verdad, o mostrar y que te contacten?
26. ¿Hacen envíos? ¿Solo Montevideo, todo el país, exterior?
27. ¿Tienen dominio o redes sociales ya andando?
28. ¿Quién va a cargar las noticias y novedades?
29. ¿Hay alguna galería o sitio que te guste como referencia?

## Sobre plazos y plata

30. ¿Hay fecha límite? ¿Alguna muestra o evento?
31. ¿Quién decide y quién firma?
32. ¿Está dispuesto a pagar un costo mensual de infraestructura?
33. **Si tuvieras que elegir UNA sola cosa para tener andando en 3 meses, ¿cuál sería?**

---

## Recomendación en una línea

**Opción 4, construida por fases, arrancando por el núcleo de gestión.** Es la que resuelve el Excel y le da presencia web con una sola infraestructura, y deja la puerta abierta a cuentas y pagos sin reescribir nada.

**Pero antes de cotizar, averiguá si existen las fotos.** Esa respuesta define si el proyecto es la opción 4 o la opción 2.

# Plan de seguimiento — Motor de MacroX

| Campo | Valor |
|---|---|
| Producto | MacroX |
| Documento | Orden de construcción del motor de ejecución |
| Estado | Vigente |
| Abierto | 2026-11-25 |
| Última revisión | 2026-11-25 |
| Relación | Complementa `PROYECTO_MACROX.md`. El alcance, el catálogo y la arquitectura viven allí. Este archivo solo dice **en qué orden se construye** y **cuándo se da por cerrada cada oleada**. |

Leyenda de estado: `Pendiente` · `En curso` · `Hecho`.

| Oleada | Nombre | Estado |
|---|---|---|
| 0 | Acuerdo de orden | Hecho |
| 1 | Contratos de dominio y macro de referencia | Hecho |
| 2 | Caso de uso del motor | En curso |
| 3 | Excel real (ClosedXML) | Hecho |
| 4 | Composición en la aplicación | En curso |
| 5 | Historial local (SQLite) | Pendiente |
| 6 | Conexión con la interfaz | En curso |
| 7 | Resto del catálogo y presets | Pendiente |

## 1. Qué es el motor

El motor es el recorrido de una ejecución, ya descrito en `PROYECTO_MACROX.md` §4.5:

```
Lote (BatchJob)
 └── por cada archivo de entrada
      ├── Copia de trabajo temporal
      ├── Contexto de libro (IWorkbookContext)
      ├── por cada paso del pipeline, en orden
      │     └── IMacroOperation.ExecuteAsync(...) → MacroResult
      ├── Escritura del archivo de salida
      └── MacroResult agregado + entrada de historial
```

Cada macro es una estrategia detrás de `IMacroOperation`. El dominio habla con `IWorkbookContext` y no conoce ClosedXML, WPF ni Entity Framework. La infraestructura abre **una copia** del libro y escribe el resultado en **una ruta nueva**. El archivo de entrada queda intacto.

SQLite no es el motor. SQLite guarda el historial de ejecuciones que el motor ya produjo. Por eso entra en la oleada 5, cuando existe un resultado que registrar.

## 2. Decisiones de este plan

1. **Rebanada vertical.** Cada oleada deja algo ejecutable y probado. No se rellena el catálogo entero antes de tocar un `.xlsx`.
2. **Macro de referencia: Trim.** Quita espacios al inicio y al final de las columnas indicadas. Es la prueba más pequeña del contrato (`IMacroOperation` + parámetros + `IWorkbookContext` + `MacroResult`). El resto del catálogo de `PROYECTO_MACROX.md` §3 espera a la oleada 7, una macro por cambio, con sus pruebas.
3. **Paquetes, en el proyecto que indica `AGENTS.md`, y solo en la oleada que los usa.**

| Paquete | Versión de referencia | Proyecto | Oleada |
|---|---|---|---|
| ClosedXML | 0.105.1 | `MacroX.Infrastructure` | 3 |
| Microsoft.Extensions.DependencyInjection / Hosting | 10.x | `macrox` | 4 |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.15 | `MacroX.Infrastructure` | 5 |

Confirmar la versión en NuGet en el cambio que instale el paquete.

4. **`MacroX.Shared` sigue vacío** hasta que aparezca un tipo de verdad transversal. Las entidades del motor viven en Domain. Las abstracciones de repositorio viven en Application.
5. **Una oleada a la vez.** La siguiente empieza cuando la anterior cumple su criterio de cierre: `dotnet build` y `dotnet test` con 0 warnings y 0 errores.
6. **Si la macro de referencia debe cambiar** (por ejemplo, a «Eliminar filas vacías»), se cambia en este documento antes de abrir la oleada 1. No se cambian a la vez la macro de referencia y el diseño del motor.

## 3. Punto de partida (2026-11-25)

Ya existe:

- Shell WPF: navegación Inicio, Macros, Excel, Configuración. Cada sección muestra un placeholder.
- MVVM propio en `macrox/Mvvm` (`ObservableObject`, `RelayCommand`, `AsyncRelayCommand`) con pruebas en `tests/MacroX.Tests`.
- `MainViewModel` con `IsBusy`, `BusyMessage` y `StatusMessage`, listos para una ejecución larga.
- Proyectos de capa creados y con la dirección de dependencias correcta.
- Contrato de `IMacroOperation` escrito en `PROYECTO_MACROX.md` §4.3. Aún no está en código.

Aún no existe:

- Código de dominio, aplicación, infraestructura o shared (solo `Class1.cs` de plantilla).
- ClosedXML, SQLite, Entity Framework y el Generic Host.
- Inyección de dependencias: `App.xaml.cs` está vacío y `MainWindow` crea `MainViewModel` a mano.
- Proyectos `tests/MacroX.Domain.Tests` y `tests/MacroX.Application.Tests`.

## 4. Oleadas

### Oleada 1 — Contratos de dominio y macro de referencia

**Objetivo.** El corazón del motor, puro y testeable, sin paquetes NuGet.

**Carpetas.** `src/MacroX.Domain/**`. Pruebas nuevas en `tests/MacroX.Domain.Tests` (`net10.0`). Alta del proyecto de pruebas en `MacroX.slnx` en el mismo cambio. Retirar `Class1.cs`.

**Entregables.**

- `MacroParameterDefinition`.
- `MacroResult` inmutable: éxito o fallo, filas afectadas, advertencias, mensaje.
- `IWorkbookContext`: hojas, filas y celdas como valores. Suficiente para Trim. Se amplía cuando una macro posterior lo exija.
- `IMacroOperation`, con la firma ya fijada en `PROYECTO_MACROX.md` §4.3.
- `Pipeline` y `PipelineStep` (orden explícito).
- Validación de pipeline: un pipeline inválido no se ejecuta; el orden forma parte del resultado.
- Macro `Trim` como primera `IMacroOperation`.
- Doble de `IWorkbookContext` en memoria, solo en pruebas.
- Pruebas `MetodoEscenario_ResultadoEsperado`: caso normal, celda vacía, solo espacios, columna ausente, parámetro faltante, cancelación.

**Cierre.** Domain compila sin referencias a otros proyectos. Las pruebas de dominio pasan. Trim no abre un archivo real.

**Fuera de esta oleada.** ClosedXML, lotes, copias de archivo, SQLite, pantallas.

### Oleada 2 — Caso de uso del motor

**Objetivo.** Orquestar un pipeline sobre un libro ya abierto, sin saber cómo se leyó el Excel.

**Carpetas.** `src/MacroX.Application/**`. Pruebas nuevas en `tests/MacroX.Application.Tests` (`net10.0`), con dobles. Alta en `MacroX.slnx`. Retirar `Class1.cs`.

**Entregables.**

- `IMacroCatalog`: resuelve una macro por su identificador.
- Caso de uso de ejecución de un archivo: validar, recorrer los pasos en orden, acumular `MacroResult`.
- Caso de uso de lote: un fallo en un archivo se registra y el lote sigue con el siguiente.
- Progreso y `CancellationToken` en toda la operación.
- Abstracciones, sin implementación concreta: fábrica de sesión de libro (abre una copia), almacén de archivos de trabajo (ruta de salida nueva, sin colisión), repositorio de historial.
- Pruebas del caso normal, pipeline vacío, paso desconocido, fallo a mitad de lote y cancelación.

**Cierre.** Application solo referencia Domain. Las pruebas usan dobles; no hay `.xlsx` todavía.

**Fuera de esta oleada.** ClosedXML, SQLite, cambios de interfaz.

### Oleada 3 — Excel real (ClosedXML)

**Objetivo.** La primera ejecución de punta a punta sobre un libro de verdad. Aquí el motor deja de ser un contrato y pasa a procesar un archivo.

**Carpetas.** `src/MacroX.Infrastructure/**`. Pruebas de integración en `tests/MacroX.Infrastructure.Tests` (`net10.0`), dadas de alta en `MacroX.slnx`. Muestras `.xlsx` versionadas bajo `tests/`, con rutas relativas. Retirar `Class1.cs`.

**Entregables.**

- Paquete `ClosedXML` en Infrastructure.
- `IWorkbookContext` implementado con ClosedXML.
- Copia de trabajo temporal y escritura en una ruta de salida nueva. El original no se modifica (la prueba lo comprueba comparando el archivo de entrada antes y después).
- Nombres de salida sin colisión y limpieza de temporales.
- Catálogo con Trim registrado.
- Una prueba de integración: libro de muestra → pipeline de un solo paso (Trim) → libro nuevo con el texto recortado.
- Bordes del archivo: libro sin la hoja pedida, archivo que no se puede leer, cancelación a mitad de la copia.

**Cierre.** Un `.xlsx` de muestra sale transformado en otro archivo. El de entrada queda igual. Domain y Application siguen sin referenciar ClosedXML.

**Fuera de esta oleada.** Base de datos, presets, pantallas, el resto de macros.

### Oleada 4 — Composición en la aplicación

**Objetivo.** Arrancar el proceso con el motor ya resuelto por inyección de dependencias. La ventana deja de construir el grafo a mano.

**Carpetas.** `macrox/App.xaml.cs` y el registro de servicios en `macrox`. Este archivo de arranque es el único punto que puede ver Infrastructure.

**Entregables.**

- Paquetes de Generic Host / Dependency Injection en `macrox`.
- Registro de catálogo, caso de uso, contexto ClosedXML y almacén de archivos.
- `MainWindow` recibe su ViewModel desde el contenedor.
- El arranque sigue abriendo el shell actual. Las secciones siguen en placeholder: esta oleada no diseña pantallas.

**Cierre.** `dotnet run --project macrox/macrox.csproj` abre la ventana que ya existe, con el motor compuesto en el arranque. 0 warnings.

**Fuera de esta oleada.** SQLite y el reemplazo del placeholder de Macros / Excel.

### Oleada 5 — Historial local (SQLite)

**Objetivo.** Cada ejecución del motor deja una entrada consultable en el equipo del usuario.

**Carpetas.** Modelo de ejecución en Domain (si no quedó cerrado en la oleada 1), interfaz de repositorio en Application, implementación en Infrastructure.

**Entregables.**

- Paquete `Microsoft.EntityFrameworkCore.Sqlite` en Infrastructure.
- Entidad de ejecución: estado, duración, errores, resultados por archivo y resultado del lote.
- Base en una ruta de entorno (`Environment.GetFolderPath`), nunca una ruta fija ni una cadena secreta en código.
- El caso de uso de la oleada 2 guarda la entrada al terminar cada archivo y al cerrar el lote, tanto en éxito como en fallo.
- Prueba de integración con una base temporal del test, no con la base del usuario.
- Presets quedan fuera. La configuración liviana que no requiera consultas sigue aplazada.

**Cierre.** Una ejecución de Trim sobre el `.xlsx` de muestra inserta una fila de historial y la prueba la vuelve a leer.

**Fuera de esta oleada.** Pantallas de historial, presets, sincronización.

### Oleada 6 — Conexión con la interfaz

**Objetivo.** Sustituir el placeholder de las secciones que el motor ya puede cumplir. El XAML actual dice que la vista se conecta cuando Application exponga sus casos de uso: a partir de la oleada 4, eso ya es cierto.

**Carpetas.** `macrox/**` (vistas y ViewModels). Las pruebas de ViewModel siguen en `tests/MacroX.Tests`.

**Entregables, en este orden.**

1. **Excel:** elegir uno o varios archivos, lanzar el pipeline de Trim, mostrar progreso con `IsBusy` y el mensaje en `StatusMessage`. El procesamiento no bloquea el hilo de UI (`AsyncRelayCommand`).
2. **Macros:** ver Trim en el catálogo (identificador, nombre, categoría, parámetros) y armar un pipeline de un paso.
3. **Inicio o la misma sección Excel:** lista del historial guardado en la oleada 5.
4. **Configuración:** se queda en placeholder hasta que exista una preferencia real que guardar.

**Cierre.** Desde la ventana se importa un `.xlsx`, se ejecuta Trim, aparece un archivo nuevo y el historial muestra esa ejecución. El original no cambia.

**Fuera de esta oleada.** El resto de macros, presets, temas nuevos.

### Oleada 7 — Resto del catálogo y presets

**Objetivo.** Crecer sin reabrir el motor. Cada macro nueva es otra `IMacroOperation`, su registro en el catálogo y sus pruebas (caso normal y bordes) en el mismo cambio.

Orden sugerido, el de `PROYECTO_MACROX.md` §3:

1. Eliminar filas vacías y eliminar columnas vacías.
2. Eliminar duplicados y filtrar registros.
3. MAYÚSCULAS, minúsculas, formato nombre propio, espacios dobles.
4. Buscar y reemplazar, y limpiar y organizar datos.
5. Presets: guardar un pipeline con nombre y volver a aplicarlo. Repositorio definido en Application e implementado en Infrastructure, sobre la misma base local.

El motor, la copia de trabajo y la UI de ejecución no se modifican para aceptar una macro nueva. Si una macro obliga a cambiar `IWorkbookContext`, el cambio se documenta en este archivo y en `PROYECTO_MACROX.md` en el mismo cambio.

## 5. Definición de cierre de cualquier oleada

- Archivos tocados limitados a las carpetas de esa oleada, más `MacroX.slnx` cuando nazca un proyecto de pruebas.
- `Class1.cs` fuera del proyecto en el que se escribió código real.
- Métodos de I/O en `async`/`await`, con `CancellationToken`. Sin `.Result` ni `.Wait()`.
- APIs públicas en `IReadOnlyList<T>` / `IEnumerable<T>` cuando no haga falta mutar.
- `dotnet build MacroX.slnx` y `dotnet test` en verde, con 0 warnings.
- Este documento: la fila de la oleada pasa a `Hecho` y la fecha de revisión se actualiza.

## 6. Registro de avances

| Fecha | Oleada | Nota |
|---|---|---|
| 2026-11-25 | 0 | Plan abierto. Decisiones confirmadas: rebanada vertical y macro de referencia Trim. SQLite queda en la oleada 5. |
| 2026-11-25 | 1 | Contratos de dominio, validación de pipeline, Trim y pruebas en memoria. Build y tests en verde, 0 warnings. |
| 2026-11-25 | 3 y 6 | Primer uso real: biblioteca de macros, apertura de Excel, aplicar Trim y guardar otro archivo. `ModTrim.bas` se vincula y no se ejecuta. El lote de varios archivos, el Generic Host y SQLite siguen pendientes. |
| 2026-11-25 | 6 | Importar el `.bas` y el `.xlsx` los copia a la plataforma. La tabla y el archivo nuevo se ven dentro de MacroX. |

## 7. Siguiente paso

Probar en la ventana el libro `practica_trim.xlsx`. Después: lote de varios archivos, Generic Host en el arranque, o SQLite para que la biblioteca y el historial sobrevivan al cerrar la aplicación.

# MacroX — Documento Maestro de Visión, Alcance y Arquitectura

| Campo | Valor |
|---|---|
| Producto | MacroX |
| Tipo | Aplicación de escritorio para Windows (local, un solo usuario) |
| Solución | `MacroX.slnx` (formato XML de solución, .NET 10) |
| Stack base | C# · .NET 10 LTS · WPF + XAML · MVVM |
| Estado | En construcción — shell de UI y base de MVVM ya creados |
| Rama de trabajo | `sdev-main` |
| Documento | Visión, alcance funcional y arquitectura técnica |

---

## 1. Visión General y Propósito

**MacroX** es una aplicación de escritorio para Windows que permite **automatizar tareas repetitivas sobre archivos Excel** mediante una biblioteca de *macros* reutilizables, sin que el usuario necesite saber programar.

El objetivo central es **transformar tareas manuales tediosas en flujos automatizados, seguros, reutilizables y fáciles de ejecutar**. MacroX no es solo una interfaz visual sobre Excel: su valor está en un **motor de procesamiento sólido**, determinista y auditable, que aplica operaciones encadenadas sobre los libros y deja constancia de lo ocurrido.

### Principios de producto

- **Seguridad del dato original:** los archivos de entrada nunca se modifican; todo el procesamiento ocurre sobre copias de trabajo y el resultado se escribe en archivos nuevos.
- **Sin programación:** el usuario compone flujos a partir de un catálogo de operaciones parametrizables.
- **Reutilización:** una macro se define una vez y se aplica a uno o a cientos de archivos.
- **Trazabilidad:** cada ejecución queda registrada con su estado, duración, errores y resultados.
- **Extensibilidad:** agregar una nueva macro no debe obligar a modificar el motor de ejecución.
- **Local por diseño:** sin servidores, sin cuentas, sin dependencia de Microsoft Excel instalado.

### Alcance y no alcance

| En alcance | Fuera de alcance (por ahora) |
|---|---|
| Procesamiento por lotes de `.xlsx` / `.xls` | Edición colaborativa o multiusuario |
| Catálogo extensible de macros | Ejecución de código VBA arbitrario |
| Pipelines ordenados y parametrizados | Fórmulas dinámicas o modelos financieros |
| Historial local de ejecuciones | Sincronización en la nube |
| Presets y plantillas de flujos | Automatización programada por servicio |

---

## 2. Flujo Principal de Usuario (Pipeline de 6 pasos)

El recorrido del usuario es lineal y siempre el mismo, independientemente de la macro elegida:

```
┌────────────────┐   ┌────────────────┐   ┌────────────────┐
│ 1. Importar    │──▶│ 2. Seleccionar │──▶│ 3. Configurar  │
│    Excel       │   │    macros      │   │  operaciones   │
└────────────────┘   └────────────────┘   └────────────────┘
                                                   │
┌────────────────┐   ┌────────────────┐   ┌───────▼────────┐
│ 6. Consultar   │◀──│ 5. Exportar    │◀──│ 4. Procesar    │
│    historial   │   │   resultados   │   │   archivos     │
└────────────────┘   └────────────────┘   └────────────────┘
```

| Paso | Nombre | Descripción |
|---|---|---|
| 1 | **Importar Excel** | Selección de uno o varios archivos `.xlsx` / `.xls`, para procesamiento individual o por lotes. |
| 2 | **Seleccionar macros** | Exploración de la biblioteca categorizada de operaciones disponibles. |
| 3 | **Configurar operaciones** | Selección, ordenamiento y parametrización de las acciones que se aplicarán al libro. |
| 4 | **Procesar archivos** | Ejecución secuencial y automática del motor sobre los documentos, siempre en memoria o sobre una copia de trabajo. |
| 5 | **Exportar resultados** | Generación de nuevos archivos Excel procesados; los originales permanecen **100 % intactos**. |
| 6 | **Consultar historial** | Registro y visualización de ejecuciones previas: estados, errores y resultados obtenidos. |

### Reglas del pipeline

1. La configuración (paso 3) se valida **antes** de iniciar el procesamiento; un pipeline inválido no se ejecuta.
2. El orden de las operaciones es explícito y determinante: dos pipelines con distinto orden producen resultados distintos.
3. Un fallo en un archivo no debe abortar el lote completo: se registra el error y se continúa con el siguiente (comportamiento configurable a futuro).
4. Todo el procesamiento de I/O y Excel es **asíncrono** y no bloquea el hilo de UI.
5. Cada ejecución produce un resultado por archivo y un resultado agregado del lote.

---

## 3. Catálogo Inicial de Macros (Extensible)

El catálogo es la pieza funcional central. Cada macro es una operación atómica, con metadatos (identificador, nombre, categoría, descripción) y un conjunto de parámetros tipados.

### 3.1 Limpieza estructural

| Macro | Descripción | Parámetros principales |
|---|---|---|
| Eliminar filas vacías | Quita filas sin contenido útil | Hoja / rango, criterio de "vacía" |
| Eliminar columnas vacías | Quita columnas sin contenido útil | Hoja / rango, criterio de "vacía" |

### 3.2 Integridad de datos

| Macro | Descripción | Parámetros principales |
|---|---|---|
| Eliminar duplicados | Elimina registros repetidos | Modo: fila completa o columnas clave; conservar primero/último |
| Filtrar registros | Descarta filas según condición | Columna, operador, valor |

### 3.3 Normalización de texto

| Macro | Descripción | Parámetros principales |
|---|---|---|
| MAYÚSCULAS | Convierte el texto a mayúsculas | Columnas objetivo |
| minúsculas | Convierte el texto a minúsculas | Columnas objetivo |
| Formato Nombre Propio | Capitaliza cada palabra | Columnas objetivo |
| Trim | Elimina espacios al inicio y al final | Columnas objetivo |
| Espacios dobles | Colapsa espacios repetidos internos | Columnas objetivo |

### 3.4 Transformación

| Macro | Descripción | Parámetros principales |
|---|---|---|
| Buscar y reemplazar | Sustituye valores o patrones | Columna, texto/patrón, reemplazo, distinguir mayúsculas |
| Limpiar y organizar datos | Normalización combinada de celdas | Conjunto de reglas habilitadas |

### 3.5 Encadenamiento (Pipelines)

| Capacidad | Descripción |
|---|---|
| Encadenar operaciones | Ejecutar varias macros de forma consecutiva sobre un mismo archivo |
| Reordenar pasos | Cambiar la secuencia antes de ejecutar |
| Guardar presets | Reutilizar un pipeline con nombre para futuros lotes |
| Ejecución por lotes | Aplicar el mismo pipeline a múltiples archivos |

> **Extensibilidad:** cada nueva macro se incorpora como una implementación adicional de `IMacroOperation`, registrada en el catálogo. El motor de ejecución y la UI no requieren cambios para soportarla.

---

## 4. Arquitectura Técnica y Principios de Diseño

### 4.1 Stack tecnológico

| Área | Tecnología | Estado |
|---|---|---|
| Lenguaje | C# | En uso |
| Framework | .NET 10 LTS | En uso |
| Interfaz | WPF + XAML | En uso (shell inicial) |
| Patrón de UI | MVVM estricto | En uso (`macrox/Mvvm`) |
| Inyección de dependencias | .NET Generic Host | Previsto |
| Motor Excel | ClosedXML | Previsto |
| Persistencia | SQLite + Entity Framework Core | Previsto |
| Pruebas | xUnit | En uso (`tests/MacroX.Tests`) |

> Las dependencias marcadas como *previstas* aún no están instaladas. Al incorporarlas se hará en el proyecto que corresponda y se justificará el paquete en el cambio.

### 4.2 Estructura real de la solución

```
MacroX.slnx
├── macrox/                        ← Presentación (WPF, WinExe, net10.0-windows)
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml(.cs)
│   ├── Mvvm/                      ← ObservableObject, RelayCommand, AsyncRelayCommand
│   ├── ViewModels/                ← MainViewModel, NavigationSection
│   ├── Themes/                    ← Colors, Controls, Typography
│   ├── Interop/                   ← DarkTitleBar (integración Win32)
│   └── Assets/
├── src/
│   ├── MacroX.Domain/             ← Core (net10.0, sin dependencias)
│   ├── MacroX.Application/        ← Casos de uso (→ Domain)
│   ├── MacroX.Infrastructure/     ← Excel + persistencia (→ Domain, Application)
│   └── MacroX.Shared/             ← Contratos y utilidades transversales
├── tests/
│   └── MacroX.Tests/              ← xUnit (net10.0-windows, referencia a macrox)
└── docs/
```

### 4.3 Capas y dirección de dependencias

La regla es única e inquebrantable: **las dependencias siempre apuntan hacia adentro**, hacia el dominio.

```
┌──────────────────────────────────────────────────────────────┐
│  PRESENTACIÓN  ·  macrox                                     │
│  Views · ViewModels · Commands · Converters · Validaciones UI│
└───────────────────────────┬──────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────┐
│  APLICACIÓN  ·  MacroX.Application                           │
│  Casos de uso · Orquestación de pipelines · Lotes            │
│  DTOs · Coordinación con persistencia                        │
└───────────────────────────┬──────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────┐
│  DOMINIO  ·  MacroX.Domain                                   │
│  Entidades · IMacroOperation · Reglas puras · Resultados     │
│  (no referencia a ningún otro proyecto)                      │
└───────────────────────────▲──────────────────────────────────┘
                            │
┌───────────────────────────┴──────────────────────────────────┐
│  INFRAESTRUCTURA  ·  MacroX.Infrastructure                   │
│  Motor Excel (ClosedXML) · Persistencia (SQLite/EF Core)     │
│  Sistema de archivos · Implementaciones de contratos         │
└──────────────────────────────────────────────────────────────┘

Transversal: MacroX.Shared  ·  DTOs, constantes, helpers sin dependencias de capas
```

#### Capa de Presentación — `macrox`

- **Responsabilidad:** mostrar estado y capturar intención del usuario.
- Contiene `Views` (XAML), `ViewModels`, `Commands`, `Converters` y validaciones de interfaz.
- **Sin lógica de negocio en code-behind.** El code-behind solo inicializa la vista.
- Se comunica exclusivamente con la capa de Aplicación.
- Ya implementa una base MVVM propia en `macrox/Mvvm` (`ObservableObject`, `RelayCommand`, `AsyncRelayCommand`) con pruebas en `tests/MacroX.Tests/Mvvm`.

#### Capa de Aplicación — `MacroX.Application`

- **Responsabilidad:** orquestar casos de uso.
- Construcción y validación de pipelines, gestión de lotes, progreso, cancelación y resultados agregados.
- Define **abstracciones** (interfaces) que Infraestructura implementa: motor Excel, repositorio de historial, repositorio de presets.
- No conoce WPF ni depende de ClosedXML/EF Core directamente.

#### Capa de Dominio — `MacroX.Domain`

- **Responsabilidad:** el corazón del negocio, puro y testeable.
- Entidades: ejecución, paso de pipeline, parámetro de macro, resultado de operación, error.
- Contrato central del catálogo:

```csharp
public interface IMacroOperation
{
    string Id { get; }
    string DisplayName { get; }
    string Category { get; }
    IReadOnlyList<MacroParameterDefinition> Parameters { get; }

    Task<MacroResult> ExecuteAsync(
        IWorkbookContext workbook,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken);
}
```

- `IWorkbookContext` abstrae el libro de trabajo para que el dominio no dependa de ClosedXML.
- Modelos de resultado inmutables: éxito/fallo, filas afectadas, advertencias y mensaje.
- **Sin dependencias de otros proyectos de la solución.**

#### Capa de Infraestructura — `MacroX.Infrastructure`

- **Responsabilidad:** implementar los detalles técnicos.
- **Motor Excel:** lectura/escritura con ClosedXML; siempre abre una **copia de trabajo** y escribe en una ruta de salida nueva.
- **Persistencia local:** SQLite + EF Core para historial y presets; configuración liviana en JSON cuando no requiera consultas.
- **Sistema de archivos:** resolución de rutas, nombres de salida sin colisión, respaldo y limpieza de temporales.
- Implementa las interfaces definidas en Aplicación/Dominio. **Nunca es referenciada por Dominio ni Aplicación.**

#### Capa de Pruebas — `tests/MacroX.Tests`

- xUnit, ya configurado en la solución.
- **Pruebas unitarias:** una por cada macro (casos borde incluidos), validación de pipelines, reglas de dominio.
- **Pruebas de integración:** motor Excel contra archivos `.xlsx` de muestra; persistencia del historial.
- Las pruebas de macros deben usar archivos de muestra versionados en `tests/`, no rutas absolutas.

### 4.4 Patrones de diseño previstos

| Patrón | Uso en MacroX |
|---|---|
| **Strategy** | Cada macro es una estrategia intercambiable detrás de `IMacroOperation`. |
| **Chain of Responsibility / Pipeline** | El motor aplica los pasos en orden, pasando el contexto del libro de un eslabón al siguiente. |
| **Repository** | Abstracción del historial y de los presets, implementada en Infraestructura. |
| **Factory / Registry** | Catálogo que resuelve una macro por su identificador. |
| **MVVM + Command** | Enlace entre Views y ViewModels sin lógica en code-behind. |
| **Dependency Injection** | Composición de la aplicación mediante el Generic Host. |

### 4.5 Modelo de ejecución (conceptual)

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

### 4.6 Escalabilidad futura

- **Open/Closed:** nuevas macros sin tocar el motor ni la UI.
- **Categorización** del catálogo y búsqueda por nombre/categoría.
- **Plantillas y presets** guardados por el usuario.
- **Flujos complejos reutilizables** que combinen pipelines completos.
- **Ejecución por lotes de gran volumen** con progreso y cancelación.
- **Exportación de resultados** a otros formatos e informes de ejecución.

---

## 5. Reglas de Desarrollo para el Agente (Contexto Técnico)

Estas reglas son obligatorias en cualquier cambio dentro del repositorio.

### Arquitectura y responsabilidades

1. Respetar la dirección de dependencias: **Presentación → Aplicación → Dominio**, con Infraestructura implementando contratos hacia adentro. Nunca `Domain → Infrastructure`.
2. **Cero lógica de negocio en code-behind.** La vista solo enlaza con su ViewModel.
3. No filtrar tipos de WPF, ClosedXML o EF Core hacia Dominio ni Aplicación.
4. Aplicar **SOLID**, en particular Open/Closed para el catálogo de macros y Dependency Inversion para el motor y la persistencia.
5. Mantener las APIs públicas basadas en `IReadOnlyList<T>` / `IEnumerable<T>` cuando no se requiera mutación.

### Calidad e integridad

6. **Inmutabilidad de los archivos originales:** toda operación trabaja sobre copia de trabajo y escribe en una ruta nueva. Nunca sobrescribir la entrada.
7. **Toda macro nueva requiere pruebas unitarias** (casos normales y borde) antes de considerarse terminada.
8. Operaciones de I/O siempre `async`/`await`, con `CancellationToken`; prohibido `.Result` y `.Wait()`.
9. No bloquear el hilo de UI en procesos largos.
10. `Nullable` habilitado; no introducir warnings nuevos. Criterio de aceptación: `dotnet build` con **0 warnings y 0 errores**.

### Convenciones

11. PascalCase para tipos y miembros públicos; `_camelCase` para campos privados; métodos asíncronos terminados en `Async`.
12. Nunca hardcodear rutas, cadenas de conexión ni secretos: usar configuración o rutas del entorno (`Environment.GetFolderPath`, `AppContext.BaseDirectory`).
13. No agregar paquetes NuGet sin justificación en el cambio.
14. No modificar archivos fuera del alcance de la tarea ni hacer renombrados o reformateos masivos no solicitados.
15. Commits pequeños y descriptivos sobre `sdev-main`; sin reescritura de historia ni `force push`.

### Comandos de referencia

Ejecutar desde la raíz del repositorio:

```bash
dotnet build MacroX.slnx
dotnet test
dotnet run --project macrox/macrox.csproj
dotnet format MacroX.slnx
```

---

*Documento maestro vivo. Debe actualizarse cuando cambien el alcance, el catálogo de macros o la arquitectura de capas.*

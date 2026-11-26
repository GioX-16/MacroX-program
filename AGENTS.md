# Proyecto: MacroX

Aplicación de escritorio Windows en C# con WPF y .NET 10, patrón MVVM.
Aplicación **local de un solo usuario**, sin servidores ni servicios externos.

## Precedencia de documentos

| Documento | Contenido | Manda en |
|---|---|---|
| `PROYECTO_MACROX.md` | Visión, alcance, catálogo de macros, modelo de ejecución | Alcance funcional |
| `AGENTS.md` (este) | Reglas operativas, convenciones, comandos, propiedad de archivos | Cómo se trabaja |

- Leer `PROYECTO_MACROX.md` antes de tocar alcance, catálogo o arquitectura.
- **No duplicar información entre ambos.** Si un dato vive en una tabla, el otro archivo remite.
- Cambiar el alcance obliga a revisar los dos archivos en el mismo commit.

## Estado actual del repositorio

> Última verificación: **2026-11-19**. Actualizar esta fecha al revisar la tabla.

| Elemento | Valor real |
|---|---|
| Solución | `MacroX.slnx` (formato XML de solución) |
| Proyecto principal | `macrox/macrox.csproj` → `net10.0-windows`, `UseWPF=true` |
| Bibliotecas | `src/MacroX.Domain`, `src/MacroX.Application`, `src/MacroX.Infrastructure`, `src/MacroX.Shared` → `net10.0` |
| Pruebas | `tests/MacroX.Tests` (xUnit, `net10.0-windows`, referencia a `macrox`) |
| Documentación | `docs/` (vacía; los ADR van en `docs/adr/`) |
| Rama por defecto | `sdev-main` (rema `origin` → `GioX-16/MacroX-program`) |
| SDK instalado | 11.0.100; los proyectos apuntan a `net10.0` — no cambiar `TargetFramework` sin aprobación |

**Implementado:** shell WPF, MVVM propio (`macrox/Mvvm`), temas, navegación y sus pruebas.

**Sin implementar:** las cuatro bibliotecas contienen solo el `Class1.cs` de plantilla.
Dominio, motor Excel, persistencia e inyección de dependencias **no existen todavía**.
Reemplazar las clases de ejemplo por código real al implementar cada capa; no dejar
clases de plantilla en commits definitivos.

## Arquitectura

Capas y dependencias (siempre hacia adentro):

```
macrox (Presentation: WPF / Views / ViewModels)
 ├──> MacroX.Application (casos de uso / servicios)
 │     └──> MacroX.Domain
 ├──> MacroX.Infrastructure (persistencia / servicios externos)
 │     ├──> MacroX.Application
 │     └──> MacroX.Domain
 └──> MacroX.Shared (utilidades y contratos transversales)
```

- `MacroX.Domain`: entidades y reglas de negocio. **No referencia a ningún otro proyecto.**
- `MacroX.Application`: casos de uso, abstracciones (interfaces de repositorios/servicios).
- `MacroX.Infrastructure`: implementaciones concretas (EF Core/SQLite, ClosedXML, I/O).
- `MacroX.Shared`: DTOs, constantes y helpers sin dependencias de capas.
- Flujo permitido: Views → ViewModels → Services → Repositories.
- **No** agregar referencias que rompan esta dirección (p. ej. Domain → Infrastructure).
- La composición de dependencias vive solo en `macrox`: es la única capa que puede
  referenciar `MacroX.Infrastructure`.

### MVVM

- MVVM propio en `macrox/Mvvm` (`ObservableObject`, `RelayCommand`, `AsyncRelayCommand`),
  con pruebas en `tests/MacroX.Tests/Mvvm`.
- **Decisión tomada:** no se introduce `CommunityToolkit.Mvvm`, Prism ni otro toolkit
  sin aprobación explícita.

### Dependencias externas

Todavía no están instaladas. Al agregarlas, hacerlo en el proyecto que corresponde
y justificar el paquete en el commit:

| Paquete | Versión de referencia | Proyecto |
|---|---|---|
| `ClosedXML` | 0.105.1 | `MacroX.Infrastructure` |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.15 | `MacroX.Infrastructure` |
| `Microsoft.Extensions.DependencyInjection` / Hosting | 10.x | `macrox` |

> Las versiones son referencia verificada en NuGet; confirmarlas en el commit que las instale.

## Propiedad de archivos (trabajo multi-agente)

Cada agente escribe **solo** en su carpeta. Fuera de ella, únicamente lectura.

| Agente | Puede editar |
|---|---|
| `domain` | `src/MacroX.Domain/**`, `docs/adr/**` |
| `infra` | `src/MacroX.Infrastructure/**` |
| `app` | `src/MacroX.Application/**` |
| `ui` | `macrox/**` |
| `qa` | `tests/**` |
| `reviewer` | nada (solo lectura, `dotnet build` y `dotnet test`) |

**Solo el orquestador edita:** `MacroX.slnx`, `AGENTS.md`, `PROYECTO_MACROX.md`,
`macrox/App.xaml.cs` (arranque y DI), `.editorconfig`, `Directory.Build.props`
y el registro del catálogo de macros.

Dos agentes nunca editan el mismo archivo. Si una tarea lo requiere, es una tarea del
orquestador, no un trabajo paralelo.

## Convenciones C#

- `Nullable` habilitado en todos los proyectos. No introducir warnings nuevos.
- `ImplicitUsings` habilitado.
- PascalCase para tipos y miembros públicos; `_camelCase` para campos privados.
- `async`/`await` en toda operación de I/O; nunca `.Result` ni `.Wait()`.
- Los métodos asíncronos terminan en `Async` y reciben `CancellationToken` cuando aplique.
- Sin lógica de negocio en code-behind: la vista solo enlaza con el ViewModel.
- No bloquear el hilo de UI en operaciones largas.
- Preferir `IReadOnlyList<T>`/`IEnumerable<T>` en APIs públicas cuando no se requiera mutación.
- El dominio no conoce tipos de WPF, ClosedXML ni EF Core.

## Pruebas

| Proyecto | Target | Qué prueba |
|---|---|---|
| `tests/MacroX.Domain.Tests` | `net10.0` | contratos, reglas, validación de pipelines |
| `tests/MacroX.Application.Tests` | `net10.0` | casos de uso con dobles de prueba |
| `tests/MacroX.Tests` | `net10.0-windows` | MVVM y ViewModels (existente) |

- Los dos primeros aún no existen; al crearlos, agregarlos a `MacroX.slnx`.
- Toda macro nueva requiere pruebas unitarias (caso normal y bordes) en el mismo cambio.
- Los `.xlsx` de muestra se versionan en `tests/`; **prohibido** usar rutas absolutas en pruebas.
- Convención de nombre: `MetodoEscenario_ResultadoEsperado`.

## Restricciones

- No agregar paquetes NuGet sin justificarlo en el commit.
- No modificar archivos fuera del alcance de la tarea.
- No hacer cambios masivos de formato ni renombrados no solicitados.
- Nunca hardcodear rutas, cadenas de conexión ni secretos. Usar configuración o rutas
  del entorno (`Environment.GetFolderPath`, `AppContext.BaseDirectory`).
- No registrar archivos generados (`bin/`, `obj/`, `.vs/`, `*.user`): ya están en `.gitignore`.
- Los archivos de entrada del usuario nunca se sobrescriben: se procesa sobre una copia de
  trabajo y se escribe en una ruta nueva.

## Comandos

Desde la raíz del repositorio:

- `dotnet build MacroX.slnx` — compilar la solución
- `dotnet build` — equivalente (solución única en el directorio)
- `dotnet test` — ejecutar las suites de prueba
- `dotnet run --project macrox/macrox.csproj` — ejecutar la aplicación
- `dotnet format MacroX.slnx` — aplicar formato

Criterio de aceptación de cualquier cambio: **0 warnings y 0 errores**, con las pruebas en verde.

## Flujo de trabajo Git

- Rama de trabajo y por defecto: `sdev-main`.
- Antes de empezar: `git pull --ff-only origin sdev-main`.
- Commits pequeños y descriptivos, en español o inglés consistente con el historial.
- No reescribir historia publicada ni hacer `force push` sobre `sdev-main`.
- Los agentes no hacen commits: commitea el orquestador al cerrar cada oleada.

## Reglas para agentes

1. Escribir solo en la carpeta asignada; el resto del repositorio es de solo lectura.
2. Leer `PROYECTO_MACROX.md` y los contratos del dominio antes de implementar.
3. Dejar `dotnet build` y `dotnet test` en verde antes de reportar terminado.
4. Reporte de cierre: archivos tocados, pendientes y resultado de build/test. Máximo 10 líneas.

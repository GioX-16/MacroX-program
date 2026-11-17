# Proyecto: MacroX

Aplicación de escritorio Windows en C# con WPF y .NET 10, patrón MVVM.
Aplicación **local de un solo usuario**, sin servidores ni servicios externos.

## Estado actual del repositorio

| Elemento | Valor real |
|---|---|
| Solución | `MacroX.slnx` (formato XML de solución, .NET 10) |
| Proyecto principal | `macrox/macrox.csproj` → `net10.0-windows`, `UseWPF=true` |
| Bibliotecas | `src/MacroX.Domain`, `src/MacroX.Application`, `src/MacroX.Infrastructure`, `src/MacroX.Shared` → `net10.0` |
| Carpeta de pruebas | `tests/` (aún vacía, sin proyectos) |
| Carpeta de documentación | `docs/` |
| Rama por defecto | `sdev-main` (rema `origin` → `GioX-16/MacroX-program`) |
| SDK | .NET 10 LTS |

> Las bibliotecas todavía contienen el `Class1.cs` de plantilla. Reemplazar por código real
> al implementar cada capa; no dejar clases de ejemplo en commits definitivos.

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

### Tecnologías previstas (según README)

- Interfaz: WPF + XAML, patrón MVVM.
- Persistencia: SQLite + Entity Framework Core.
- Excel: ClosedXML.
- Inyección de dependencias: .NET Generic Host (`Microsoft.Extensions.DependencyInjection`).
- Pruebas: xUnit.

> Estas dependencias aún **no están instaladas**. Al agregarlas, hacerlo en el proyecto
> que corresponda (EF Core y ClosedXML en `MacroX.Infrastructure`, DI/Host en `macrox`)
> y justificar el paquete en el cambio.

### Pendiente de decisión

- Toolkit MVVM: aún no se ha elegido (`CommunityToolkit.Mvvm`, Prism u otro). No asumir uno
  sin confirmarlo con el equipo antes de introducir su dependencia.
- No existen proyectos de prueba todavía; al crear el primero, ubicarlo en `tests/` y
  agregarlo a `MacroX.slnx`.

## Convenciones C#

- `Nullable` habilitado en todos los proyectos. No introducir warnings nuevos.
- `ImplicitUsings` habilitado.
- PascalCase para tipos y miembros públicos; `_camelCase` para campos privados.
- `async`/`await` en toda operación de I/O; nunca `.Result` ni `.Wait()`.
- Los métodos asíncronos terminan en `Async` y reciben `CancellationToken` cuando aplique.
- Sin lógica de negocio en code-behind: la vista solo enlaza con el ViewModel.
- No bloquear el hilo de UI en operaciones largas.
- Preferir `IReadOnlyList<T>`/`IEnumerable<T>` en APIs públicas cuando no se requiera mutación.

## Restricciones

- No agregar paquetes NuGet sin justificarlo.
- No modificar archivos fuera del alcance de la tarea.
- No hacer cambios masivos de formato ni renombrados no solicitados.
- Nunca hardcodear rutas, cadenas de conexión ni secretos. Usar configuración/`appsettings`
  o rutas del entorno (`Environment.GetFolderPath`, `AppContext.BaseDirectory`).
- No registrar archivos generados (`bin/`, `obj/`, `.vs/`, `*.user`): ya están en `.gitignore`.

## Comandos

Ejecutar desde la raíz del repositorio (`C:\Users\dansa\Desktop\SANDINO\Projects\MacroX`):

- Compilar solución: `dotnet build MacroX.slnx`
- Compilar (solución única en el directorio): `dotnet build`
- Pruebas: `dotnet test` *(sin proyectos de prueba por ahora; no falla la compilación)*
- Ejecutar la app: `dotnet run --project macrox/macrox.csproj`
- Formato: `dotnet format MacroX.slnx`

Criterio de aceptación de cualquier cambio: `dotnet build` con **0 warnings y 0 errores**.

## Flujo de trabajo Git

- Rama de trabajo y por defecto: `sdev-main`.
- Antes de empezar: `git pull --ff-only origin sdev-main`.
- Commits pequeños y descriptivos, en español o inglés consistente con el historial.
- No reescribir historia publicada ni hacer `force push` sobre `sdev-main`.

# MacroX

<img width="1901" height="560" alt="image_35e42128" src="https://github.com/user-attachments/assets/95dd7803-1f60-44b4-b41b-8cb79554ce01" />


**Aplicación de escritorio para Windows desarrollada con C# y .NET 10.**

MacroX es un proyecto de aplicación local diseñado para ejecutarse inicialmente en un entorno Windows de un solo usuario, sin depender de servidores ni servicios externos.

El proyecto busca aprovechar tecnologías modernas del ecosistema .NET para ofrecer una arquitectura organizada, almacenamiento local y herramientas para la manipulación de archivos Excel.

---

## 🛠️ Tecnologías utilizadas

| Área | Tecnología |
|---|---|
| Lenguaje | C# |
| Framework | .NET 10 LTS |
| Interfaz gráfica | WPF + XAML |
| Arquitectura | MVVM |
| Base de datos | SQLite |
| ORM | Entity Framework Core |
| Manipulación de Excel | ClosedXML |
| Inyección de dependencias | .NET Generic Host |
| Pruebas automatizadas | xUnit |
| Control de versiones | Git |

## 🏗️ Arquitectura del proyecto

MacroX utiliza el patrón **MVVM (Model-View-ViewModel)** para separar las responsabilidades de la aplicación.

Esta arquitectura permite mantener una estructura organizada y facilitar el mantenimiento, las pruebas y la evolución del software.

- **Model:** representa los datos y las entidades del sistema.
- **View:** contiene las interfaces gráficas desarrolladas con WPF y XAML.
- **ViewModel:** administra la lógica de presentación y la comunicación entre la interfaz y los datos.

## 💾 Almacenamiento de datos

El proyecto utiliza **SQLite** como motor de base de datos local, permitiendo almacenar información directamente en el equipo del usuario.

La gestión de datos se realiza mediante **Entity Framework Core**, facilitando las operaciones de consulta, creación, actualización y eliminación de registros.

## 📊 Gestión de archivos Excel

Para trabajar con libros de Excel se utiliza **ClosedXML**, una biblioteca que permite manipular archivos `.xlsx` mediante C#.

Esto permite trabajar con documentos de Excel sin depender de Microsoft Excel instalado en el equipo.

## ⚙️ Características de la solución tecnológica

- Aplicación de escritorio nativa para Windows.
- Funcionamiento local, sin necesidad de servidores externos.
- Base de datos SQLite integrada.
- Arquitectura MVVM.
- Separación de responsabilidades mediante inyección de dependencias.
- Manipulación de archivos Excel.
- Preparación para pruebas automatizadas con xUnit.
- Control de versiones mediante Git.

## 🚀 Requisitos de desarrollo

Para trabajar con el proyecto se contempla el siguiente entorno:

- Windows 10 o Windows 11.
- .NET 10 SDK.
- Visual Studio 2026 o un entorno compatible con desarrollo .NET.
- Git.

## 📁 Configuración inicial

1. Clonar el repositorio:

   ```bash
   git clone URL_DEL_REPOSITORIO
   ```

2. Acceder al directorio:

   ```bash
   cd MacroX
   ```

3. Restaurar las dependencias:

   ```bash
   dotnet restore
   ```

4. Compilar el proyecto:

   ```bash
   dotnet build
   ```

5. Ejecutar las pruebas automatizadas:

   ```bash
   dotnet test
   ```

*Nota: Los comandos deberán ejecutarse desde la solución o el proyecto correspondiente.*

## 🔄 Control de versiones

El proyecto utiliza Git para gestionar los cambios en el código fuente, mantener un historial de desarrollo y facilitar la colaboración.

## 📌 Estado del proyecto

**En desarrollo.**

MacroX se encuentra en su etapa de construcción y organización tecnológica. La arquitectura y las herramientas principales han sido seleccionadas como base para el desarrollo de la aplicación.

---

## 👨‍💻 Autor

**Geovanny Sandino**

Proyecto de desarrollo de software utilizando tecnologías del ecosistema Microsoft .NET.

---

*MacroX — Desarrollo local, arquitectura organizada y tecnología .NET.*

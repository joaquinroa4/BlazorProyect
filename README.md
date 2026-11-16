# StudyFlow

StudyFlow es una aplicación web para organizar el estudio: permite administrar **materias**, **tareas** y **usuarios**, con autenticación real, roles, modo claro/oscuro y una interfaz responsive.

Está construida como **Blazor Web App de .NET 8** con renderizado interactivo en servidor (`InteractiveServer`), **Entity Framework Core 8** sobre **SQL Server**, **ASP.NET Core Identity** para autenticación y autorización, **Bootstrap 5.3.3** para la base visual y **Radzen.Blazor** para la grilla de administración de usuarios.

> Este documento explica el proyecto de principio a fin: cómo se ejecuta, cómo está estructurado, qué hace cada archivo, por qué se tomaron ciertas decisiones, cómo se arma la base de datos, cómo funciona el enrutamiento y cómo se integra cada librería.

---

## Índice

- [Qué es StudyFlow](#qué-es-studyflow)
- [Stack tecnológico](#stack-tecnológico)
- [Puesta en marcha](#puesta-en-marcha)
- [Arquitectura general](#arquitectura-general)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Punto de entrada: Program.cs](#punto-de-entrada-programcs)
- [Modelo de datos](#modelo-de-datos)
- [EF Core y base de datos](#ef-core-y-base-de-datos)
- [Autenticación y autorización](#autenticación-y-autorización)
- [Enrutamiento](#enrutamiento)
- [Componentes](#componentes)
- [Servicios](#servicios)
- [Interfaz y sistema de diseño](#interfaz-y-sistema-de-diseño)
- [Flujos principales](#flujos-principales)
- [Validaciones](#validaciones)
- [Decisiones de diseño](#decisiones-de-diseño)
- [Limitaciones y notas](#limitaciones-y-notas)
- [Comandos útiles](#comandos-útiles)
- [Créditos](#créditos)

---

## Qué es StudyFlow

La aplicación resuelve tres necesidades concretas:

1. **Organizar materias**: cada usuario crea sus materias con nombre, descripción y un color propio.
2. **Gestionar tareas**: cada tarea pertenece a una materia y tiene estado, prioridad, dificultad, fecha de entrega y tiempo estimado.
3. **Administrar usuarios**: un administrador puede listar, buscar, filtrar, editar y eliminar usuarios; también puede crear cuentas nuevas.

Alrededor de eso hay tres piezas transversales:

- **Autenticación con Identity** (cookie, contraseñas hasheadas, roles).
- **Autorización por rol**: `Admin` para la sección de administración, usuario autenticado para el resto.
- **Tema claro/oscuro** persistente, aplicado sin parpadeo y sin perderlo al navegar.

### Credenciales de demostración

| Rol | Email | Contraseña |
|---|---|---|
| Administrador | `admin@studyflow.com` | `Admin_1234!` |
| Usuarios migrados del sistema viejo | el email que tenían | `StudyFlow_1234!` |

Estas credenciales las crea/repara [DbInitializer.cs](Data/DbInitializer.cs) al arrancar la aplicación.

---

## Stack tecnológico

| Pieza | Versión | Para qué se usa |
|---|---|---|
| .NET | 8.0 | Framework base |
| Blazor Web App | .NET 8 | Componentes, SSR estático e islas interactivas |
| Entity Framework Core | 8.0.0 | Acceso a datos y migraciones |
| EF Core SQL Server | 8.0.0 | Proveedor de base de datos |
| ASP.NET Core Identity | 8.0.0 | Usuarios, contraseñas, roles, cookies |
| Bootstrap | 5.3.3 | Grilla, utilidades, componentes y `data-bs-theme` |
| Radzen.Blazor | 11.2.8 | `RadzenDataGrid` de la página de usuarios |
| Blazored.LocalStorage | 4.5.0 | Registrado en DI (hoy el tema se maneja por JS) |
| BCrypt.Net-Next | 4.2.0 | Legado: se conserva la referencia, ya no se usa para hashear |
| SQL Server | 2022 | Base de datos (`docker-compose.yml`) |

Todo está declarado en [ProyectoBlazor.csproj](ProyectoBlazor.csproj).

---

## Puesta en marcha

### Requisitos

- .NET SDK 8.
- Docker (recomendado) o una instancia de SQL Server accesible.

### 1. Levantar SQL Server

El archivo [docker-compose.yml](docker-compose.yml) define un contenedor con SQL Server 2022, usuario `sa`, contraseña `StudyFlow_1234!` y el puerto `1433` publicado.

```bash
docker compose up -d
```

### 2. Cadena de conexión

Está en [appsettings.json](appsettings.json):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=StudyFlowDB;User Id=sa;Password=StudyFlow_1234!;TrustServerCertificate=True;"
}
```

La aplicación usa esa cadena desde [Program.cs](Program.cs) al registrar el `DbContext`.

### 3. Crear/actualizar la base de datos

La aplicación **no aplica migraciones automáticamente**. Hay que ejecutar:

```bash
dotnet ef database update
```

> Importante: [DbInitializer.cs](Data/DbInitializer.cs) detecta migraciones pendientes y, si las hay, **no inicializa nada** (ni rol ni admin). Primero se actualiza la base, después se arranca la app.

### 4. Ejecutar

```bash
dotnet run
```

Los perfiles están en [launchSettings.json](Properties/launchSettings.json):

- `http` → `http://localhost:5046`
- `https` → `https://localhost:7299` y `http://localhost:5046`

### 5. Iniciar sesión

Entrar a `/login` con `admin@studyflow.com` / `Admin_1234!`.

### 6. (Opcional) Compilar y publicar

```bash
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

El flujo de CI está en [deploy.yml](.github/workflows/deploy.yml): en cada push o PR a `main` hace restore, build Release y publish de verificación.

---

## Arquitectura general

StudyFlow sigue una arquitectura en capas simple, sin capas ceremoniales de más:

```
┌──────────────────────────────────────────────────────────────┐
│  Navegador                                                   │
│  HTML + Bootstrap + CSS aislado + theme.js                   │
└───────────────────────────┬──────────────────────────────────┘
                            │ HTTP / SignalR
┌───────────────────────────▼──────────────────────────────────┐
│  Components/  (capa de presentación)                         │
│  App.razor → Routes.razor → Layouts → Pages → Shared         │
│  · SSR estático para login/home/errores                      │
│  · InteractiveServer para materias, tareas, usuarios,        │
│    registro                                                  │
└───────────────────────────┬──────────────────────────────────┘
                            │ inyección de dependencias
┌───────────────────────────▼──────────────────────────────────┐
│  Services/  (capa de aplicación)                             │
│  UsuarioService · MateriaService · TareaService              │
│  UsuarioActualService                                        │
└───────────────────────────┬──────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────┐
│  Data/  (capa de persistencia)                               │
│  ApplicationDbContext (IdentityDbContext)                    │
│  DbInitializer + DbInitializerHostedService                  │
└───────────────────────────┬──────────────────────────────────┘
                            │ EF Core 8
┌───────────────────────────▼──────────────────────────────────┐
│  SQL Server · base StudyFlowDB                               │
│  Usuarios, Materias, Tareas, SesionesEstudio, Etiquetas,     │
│  TareasEtiquetas + tablas AspNet* de Identity                │
└──────────────────────────────────────────────────────────────┘
```

### Por qué está separado así

- **Componentes**: solo UI y estado de pantalla. No hablan con EF directamente.
- **Servicios**: concentran las reglas y las consultas. Se registran como `Scoped` y se inyectan en las páginas.
- **Datos**: un único `DbContext` de Identity que además mapea las entidades del dominio.
- **Identity** se integra en la capa de datos y en la capa de servicios: `UsuarioService` usa `UserManager<Usuario>` para todo lo sensible a credenciales y el `IDbContextFactory` para consultas masivas.

---

## Estructura del proyecto

```
ProyectoBlazor/
├── Components/
│   ├── App.razor                  ← documento HTML raíz
│   ├── Routes.razor               ← router + autorización
│   ├── RedirectToLogin.razor      ← redirección a /login
│   ├── _Imports.razor             ← usings globales de Razor
│   ├── Layout/
│   │   ├── MainLayout.razor       ← shell con sidebar y topbar
│   │   ├── MainLayout.razor.css
│   │   ├── NavMenu.razor          ← navegación lateral
│   │   ├── NavMenu.razor.css
│   │   ├── AuthLayout.razor       ← layout de login
│   │   └── AuthLayout.razor.css
│   └── Pages/
│       ├── Home.razor             ← panel de inicio
│       ├── Login.razor            ← inicio de sesión
│       ├── Registro.razor         ← alta de usuarios (admin)
│       ├── Materias.razor         ← CRUD de materias
│       ├── Tareas.razor           ← CRUD de tareas
│       ├── Usuarios.razor         ← administración de usuarios
│       ├── AccesoDenegado.razor
│       └── Error.razor
├── Shared/
│   ├── MateriaCard.razor
│   ├── TareaCard.razor
│   └── UsuarioCard.razor
├── Models/
│   ├── Usuario.cs
│   ├── Materia.cs
│   ├── Tarea.cs
│   ├── SesionEstudio.cs
│   ├── Etiqueta.cs
│   ├── TareaEtiqueta.cs
│   ├── EstadoTarea.cs
│   ├── PrioridadTarea.cs
│   ├── DificultadTarea.cs
│   ├── LoginModel.cs
│   ├── RegistroModel.cs
│   └── TareaModel.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DbInitializer.cs
│   └── DbInitializerHostedService.cs
├── Services/
│   ├── UsuarioServices.cs
│   ├── MateriaService.cs
│   ├── TareaService.cs
│   └── UsuarioActualService.cs
├── Migrations/
├── wwwroot/
│   ├── app.css
│   ├── favicon.png
│   ├── bootstrap/
│   └── js/theme.js
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── docker-compose.yml
└── ProyectoBlazor.csproj
```

### Mapa rápido de archivos

| Archivo | Rol |
|---|---|
| [Program.cs](Program.cs) | Configuración de servicios, pipeline y endpoint de logout |
| [App.razor](Components/App.razor) | Documento HTML raíz, tema del servidor y scripts |
| [Routes.razor](Components/Routes.razor) | Router con `AuthorizeRouteView` |
| [RedirectToLogin.razor](Components/RedirectToLogin.razor) | Redirección con `returnUrl` |
| [_Imports.razor](Components/_Imports.razor) | `@using` globales de Razor |
| [MainLayout.razor](Components/Layout/MainLayout.razor) | Layout principal autenticado |
| [NavMenu.razor](Components/Layout/NavMenu.razor) | Menú lateral con roles |
| [AuthLayout.razor](Components/Layout/AuthLayout.razor) | Layout de pantalla de login |
| [Home.razor](Components/Pages/Home.razor) | Panel de inicio |
| [Login.razor](Components/Pages/Login.razor) | Inicio de sesión |
| [Registro.razor](Components/Pages/Registro.razor) | Alta de usuario + resumen |
| [Materias.razor](Components/Pages/Materias.razor) | Materias del usuario |
| [Tareas.razor](Components/Pages/Tareas.razor) | Tareas del usuario |
| [Usuarios.razor](Components/Pages/Usuarios.razor) | Administración de usuarios |
| [AccesoDenegado.razor](Components/Pages/AccesoDenegado.razor) | Página 403 |
| [Error.razor](Components/Pages/Error.razor) | Página de error del pipeline |
| [MateriaCard.razor](Shared/MateriaCard.razor) | Tarjeta ver/editar/eliminar materia |
| [TareaCard.razor](Shared/TareaCard.razor) | Tarjeta ver/editar/eliminar tarea |
| [UsuarioCard.razor](Shared/UsuarioCard.razor) | Tarjeta ver/editar/eliminar usuario |
| [ApplicationDbContext.cs](Data/ApplicationDbContext.cs) | Mapeo EF Core + Identity |
| [DbInitializer.cs](Data/DbInitializer.cs) | Rol Admin, admin inicial, reseteo de hashes |
| [DbInitializerHostedService.cs](Data/DbInitializerHostedService.cs) | Ejecuta el inicializador al arrancar |
| [UsuarioServices.cs](Services/UsuarioServices.cs) | Registro/edición/borrado de usuarios |
| [MateriaService.cs](Services/MateriaService.cs) | CRUD de materias |
| [TareaService.cs](Services/TareaService.cs) | CRUD de tareas |
| [UsuarioActualService.cs](Services/UsuarioActualService.cs) | Usuario autenticado desde claims |
| [app.css](wwwroot/app.css) | Sistema de diseño global |
| [theme.js](wwwroot/js/theme.js) | Tema claro/oscuro y sidebar móvil |

---

## Punto de entrada: Program.cs

[Program.cs](Program.cs) hace cinco cosas: registra servicios, configura Identity, registra el inicializador, arma el pipeline y expone `/logout`.

### 1. Blazor y Radzen

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();
builder.Services.AddBlazoredLocalStorage();
```

- `AddRazorComponents()` habilita el modelo de **Blazor Web App**.
- `AddInteractiveServerComponents()` permite que las páginas pidan `@rendermode InteractiveServer`.
- `AddRadzenComponents()` es necesario para `RadzenDataGrid`, `RadzenIcon`, etc.
- `AddBlazoredLocalStorage()` queda registrado; hoy el tema se resuelve con JavaScript.

### 2. EF Core: factory + contexto scoped

```csharp
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ApplicationDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>()
      .CreateDbContext());
```

Esta combinación es clave en Blazor Server:

- **`IDbContextFactory`** permite que cada operación cree y destruya su propio `DbContext`. Es la forma segura de trabajar con EF en componentes interactivos, donde un contexto scoped puede quedar vivo durante todo el circuito y acumular estado.
- **`AddScoped<ApplicationDbContext>`** existe porque **Identity** (`UserManager`, `RoleManager`, stores) espera un `DbContext` scoped. Se construye a partir del factory para no duplicar configuración.

### 3. Identity

```csharp
builder.Services.AddIdentity<Usuario, IdentityRole<int>>(options =>
{
    options.User.RequireUniqueEmail = true;

    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;

    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
```

- La clave primaria es `int` (`IdentityRole<int>`), igual que la tabla existente.
- Email único, contraseña de 8 caracteres con dígito y minúscula.
- No exige mayúscula, símbolo ni confirmación de cuenta (es una app de demo/TP).

### 4. Cookie de autenticación

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/acceso-denegado";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
```

La sesión dura 8 horas y se renueva con actividad. Las rutas coinciden con las páginas y el endpoint reales.

### 5. Servicios de aplicación

```csharp
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<MateriaService>();
builder.Services.AddScoped<UsuarioActualService>();
builder.Services.AddScoped<TareaService>();
```

`Scoped` en Blazor Server equivale a **por circuito**: cada pestaña conectada tiene su instancia.

### 6. Inicialización al arrancar

```csharp
builder.Services.AddHostedService<DbInitializerHostedService>();
```

El hosted service ejecuta [DbInitializer.cs](Data/DbInitializer.cs) una vez al iniciar.

### 7. Pipeline

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
```

Orden importante: autenticación antes de autorización, antiforgery antes de los componentes.

### 8. Logout por POST

```csharp
app.MapPost("/logout", async (SignInManager<Usuario> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
}).RequireAuthorization();
```

**Por qué un endpoint y no un método interactivo**: borrar la cookie de autenticación requiere una respuesta HTTP real. Desde el circuito de SignalR no se puede modificar la cookie del navegador. Por eso el botón de cerrar sesión es un `<form method="post" action="/logout">` con `<AntiforgeryToken />` en [MainLayout.razor](Components/Layout/MainLayout.razor).

---

## Modelo de datos

Las entidades viven en `Models/`. Hay dos grupos: **entidades persistidas** y **modelos de formulario**.

### Entidades persistidas

| Entidad | Archivo | Descripción |
|---|---|---|
| `Usuario` | [Usuario.cs](Models/Usuario.cs) | Extiende `IdentityUser<int>` con nombre, fecha de nacimiento y fecha de registro |
| `Materia` | [Materia.cs](Models/Materia.cs) | Nombre, descripción, color y dueño |
| `Tarea` | [Tarea.cs](Models/Tarea.cs) | Título, descripción, fechas, estado, prioridad, dificultad y tiempo estimado |
| `SesionEstudio` | [SesionEstudio.cs](Models/SesionEstudio.cs) | Registro de una sesión de estudio asociada a una tarea |
| `Etiqueta` | [Etiqueta.cs](Models/Etiqueta.cs) | Etiqueta reutilizable |
| `TareaEtiqueta` | [TareaEtiqueta.cs](Models/TareaEtiqueta.cs) | Tabla intermedia N:M entre tareas y etiquetas |

### Enumeraciones

| Enum | Archivo | Valores |
|---|---|---|
| `EstadoTarea` | [EstadoTarea.cs](Models/EstadoTarea.cs) | `Pendiente`, `EnProgreso`, `Completada` |
| `PrioridadTarea` | [PrioridadTarea.cs](Models/PrioridadTarea.cs) | `Baja`, `Media`, `Alta`, `Critica` |
| `DificultadTarea` | [DificultadTarea.cs](Models/DificultadTarea.cs) | `Facil`, `Media`, `Dificil` |

### Modelos de formulario (no se persisten)

| Modelo | Archivo | Para qué |
|---|---|---|
| `LoginModel` | [LoginModel.cs](Models/LoginModel.cs) | Email, contraseña y “recordarme” |
| `RegistroModel` | [RegistroModel.cs](Models/RegistroModel.cs) | Alta de usuario + validación de edad |
| `TareaModel` | [TareaModel.cs](Models/TareaModel.cs) | Formulario de tarea + validación de fecha de entrega |

Separar estos modelos de las entidades evita “ensuciar” la base con campos de UI y permite validaciones específicas de formulario.

### Diagrama de relaciones

```
Usuario (IdentityUser<int>)
  Id  ── columna física: IdUsuario  (PK)
  Nombre
  FechaNacimiento (date)
  FechaRegistro
  Email, PasswordHash, SecurityStamp, ...  (Identity)
   │
   ├──< Materia            (1:N, cascade)
   │        │
   │        └──< Tarea     (1:N, restrict)
   │
   ├──< Tarea              (1:N, cascade)
   │        │
   │        ├──< SesionEstudio   (1:N, cascade)
   │        └──< TareaEtiqueta   (1:N, cascade)  >── Etiqueta
   │
   └──< SesionEstudio      (1:N, restrict)
```

### Decisiones del modelo

- **`Usuario : IdentityUser<int>`**: se reutiliza toda la infraestructura de Identity (hash, roles, claims, logins) en lugar de mantener una tabla de usuarios propia.
- **`IdUsuario` como alias `[NotMapped]`**: el código viejo usaba `IdUsuario`; Identity usa `Id`. El alias mantiene compatibilidad de C# sin duplicar columnas. **Ojo**: no se puede usar dentro de consultas LINQ de EF; ahí va siempre `u.Id` (ver [MateriaService.cs](Services/MateriaService.cs)).
- **`DateOnly` para fecha de nacimiento**: evita horas y zonas horarias innecesarias.
- **`DateTime` para fechas de registro/entrega**: se usan con formato local.
- **`decimal`/enums**: estado, prioridad y dificultad se guardan como enteros (valor por defecto de EF para enums).
- **N:M con entidad intermedia**: `TareaEtiqueta` permite agregar datos a la relación en el futuro (por ejemplo, quién la asignó).

---

## EF Core y base de datos

### El contexto

[ApplicationDbContext.cs](Data/ApplicationDbContext.cs) hereda de:

```csharp
public class ApplicationDbContext
    : IdentityDbContext<Usuario, IdentityRole<int>, int>
```

Eso trae automáticamente las tablas de Identity:

- `AspNetUsers`
- `AspNetRoles`
- `AspNetUserRoles`
- `AspNetUserClaims`
- `AspNetUserLogins`
- `AspNetUserTokens`
- `AspNetRoleClaims`

Y además declara las tablas del dominio:

```csharp
public DbSet<Usuario> Usuarios { get; set; }
public DbSet<Materia> Materias { get; set; }
public DbSet<Tarea> Tareas { get; set; }
public DbSet<SesionEstudio> SesionesEstudio { get; set; }
public DbSet<Etiqueta> Etiquetas { get; set; }
public DbSet<TareaEtiqueta> TareasEtiquetas { get; set; }
```

### Configuración en `OnModelCreating`

1. **Tabla `Usuarios`** (en lugar de `AspNetUsers`):

   ```csharp
   modelBuilder.Entity<Usuario>().ToTable("Usuarios");
   ```

2. **Columna `IdUsuario`** para la PK de Identity:

   ```csharp
   modelBuilder.Entity<Usuario>()
       .Property(u => u.Id)
       .HasColumnName("IdUsuario");
   ```

3. **Email de 450 caracteres** y **índice único**:

   ```csharp
   modelBuilder.Entity<Usuario>()
       .Property(u => u.Email)
       .HasMaxLength(450);

   modelBuilder.Entity<Usuario>()
       .HasIndex(u => u.Email)
       .IsUnique();
   ```

4. **Claves primarias** de cada entidad y **clave compuesta** de `TareaEtiqueta`.

5. **Relaciones y borrado**:

   | Relación | FK | Borrado |
   |---|---|---|
   | Usuario → Materia | `Materia.UsuarioId` | `Cascade` |
   | Usuario → Tarea | `Tarea.UsuarioId` | `Cascade` |
   | Materia → Tarea | `Tarea.MateriaId` | `Restrict` |
   | Usuario → SesionEstudio | `SesionEstudio.UsuarioId` | `Restrict` |
   | Tarea → SesionEstudio | `SesionEstudio.TareaId` | `Cascade` |
   | Tarea ↔ Etiqueta | `TareaEtiqueta.TareaId` / `EtiquetaId` | `Cascade` |

   **Por qué `Restrict` en Materia → Tarea**: evita borrar una materia con tareas asociadas “por accidente”. El borrado se hace explícito en el servicio, primero las tareas y después la materia.

   **Por qué `Restrict` en Usuario → SesionEstudio**: obliga a decidir qué hacer con el historial de estudio antes de borrar un usuario.

### Migraciones

Hay tres migraciones en `Migrations/`:

| Migración | Qué hace |
|---|---|
| [20260824153205_InitialCreate.cs](Migrations/20260824153205_InitialCreate.cs) | Crea `Usuarios`, `Materias`, `Tareas`, `SesionesEstudio`, `Etiquetas`, `TareasEtiquetas` e índices |
| [20260824183620_UniqueEmailUsuario.cs](Migrations/20260824183620_UniqueEmailUsuario.cs) | Email a `nvarchar(450)` + índice único |
| [20261114180638_IntegracionIdentity.cs](Migrations/20261114180638_IntegracionIdentity.cs) | Agrega columnas y tablas de Identity sobre la base existente |

El snapshot del modelo está en [ApplicationDbContextModelSnapshot.cs](Migrations/ApplicationDbContextModelSnapshot.cs).

**Cómo se aplican**: manualmente con `dotnet ef database update`. No hay `Database.Migrate()` en el arranque; [DbInitializer.cs](Data/DbInitializer.cs) solo continúa si **no** hay migraciones pendientes.

### Inicialización de datos

[DbInitializer.cs](Data/DbInitializer.cs) se ejecuta desde [DbInitializerHostedService.cs](Data/DbInitializerHostedService.cs) al iniciar. Hace cuatro tareas:

1. **Crea el rol `Admin`** si no existe.
2. **Completa datos de Identity** en usuarios viejos:
   - `UserName` = `Email` si falta.
   - `SecurityStamp` y `ConcurrencyStamp` si faltan (sin ellos, `UpdateAsync` falla).
   - `EmailConfirmed = true`.
3. **Resetea contraseñas viejas**: si el hash empieza con `$2` (BCrypt del sistema anterior), le quita la contraseña y le asigna `StudyFlow_1234!` con el hasher de Identity.
4. **Crea el administrador** `admin@studyflow.com` / `Admin_1234!` y le asigna el rol `Admin`.

Es idempotente: se puede ejecutar en cada arranque sin duplicar datos.

### Por qué `IDbContextFactory` y no un `DbContext` inyectado

En Blazor Server un componente interactivo vive en un **circuito** de larga duración. Un `DbContext` scoped quedaría vivo durante minutos u horas, acumulando entidades trackeadas y pudiendo fallar con accesos concurrentes.

La estrategia es:

```csharp
await using var context = await _contextFactory.CreateDbContextAsync();
```

Cada operación abre, consulta/guarda y cierra. Identity, en cambio, sigue usando el contexto scoped porque sus stores están diseñados así.

---

## Autenticación y autorización

### Piezas

| Pieza | Dónde | Función |
|---|---|---|
| `AddIdentity` | [Program.cs](Program.cs) | Usuarios, contraseñas, roles |
| `AddCascadingAuthenticationState` | [Program.cs](Program.cs) | `AuthorizeView` y `[Authorize]` en componentes |
| `AuthorizeRouteView` | [Routes.razor](Components/Routes.razor) | Protege páginas y decide qué mostrar |
| `RedirectToLogin` | [RedirectToLogin.razor](Components/RedirectToLogin.razor) | Manda a `/login?returnUrl=...` |
| `[Authorize]` / `[Authorize(Roles = "Admin")]` | Páginas | Reglas por página |
| `AuthorizeView` | Layouts y páginas | Mostrar/ocultar según sesión y rol |
| `UsuarioActualService` | [UsuarioActualService.cs](Services/UsuarioActualService.cs) | Lee el usuario desde los claims |

### Cómo se autentica

El login es **SSR estático** (no interactivo). [Login.razor](Components/Pages/Login.razor) usa:

```razor
<EditForm Model="Input" method="post" OnValidSubmit="LoginUser" FormName="login">
```

y recibe el modelo con:

```csharp
[SupplyParameterFromForm]
private LoginModel Input { get; set; } = new();
```

Al enviar:

1. Busca el usuario con `UserManager.FindByEmailAsync`.
2. Valida la contraseña con `SignInManager.PasswordSignInAsync`.
3. Si todo está bien, `Navigation.NavigateTo(destino, forceLoad: true)`.

**Por qué SSR estático**: `SignInManager` necesita el `HttpContext` de la petición para escribir la cookie. Eso solo ocurre en una petición HTTP real, no en el circuito de SignalR.

**Protección de redirección**: `DestinoSeguro()` solo acepta rutas que empiecen con `/` y rechaza `//` y `/\`, para evitar *open redirects*.

### Cómo se cierra sesión

En [MainLayout.razor](Components/Layout/MainLayout.razor):

```razor
<form method="post" action="/logout" class="m-0">
    <AntiforgeryToken />
    <button type="submit" class="dropdown-item sf-logout">Cerrar sesión</button>
</form>
```

El POST lo atiende el endpoint de [Program.cs](Program.cs), que llama a `SignOutAsync()` y redirige a `/login`.

### Autorización por rol

- [Home.razor](Components/Pages/Home.razor): `[Authorize]`.
- [Materias.razor](Components/Pages/Materias.razor): `[Authorize]`.
- [Tareas.razor](Components/Pages/Tareas.razor): `[Authorize]`.
- [Usuarios.razor](Components/Pages/Usuarios.razor): `[Authorize(Roles = "Admin")]`.
- [Registro.razor](Components/Pages/Registro.razor): `[Authorize(Roles = "Admin")]`.

La UI acompaña: [NavMenu.razor](Components/Layout/NavMenu.razor) y [Home.razor](Components/Pages/Home.razor) muestran la sección de administración solo dentro de `<AuthorizeView Roles="Admin">`.

### Cuando no hay permiso

[Routes.razor](Components/Routes.razor) distingue dos casos:

- **No autenticado** → `<RedirectToLogin />`, que navega a `/login?returnUrl=...` con `forceLoad: true`.
- **Autenticado sin rol** → bloque “Sin permisos” con enlace al inicio.

Además existe la página [AccesoDenegado.razor](Components/Pages/AccesoDenegado.razor) como `AccessDeniedPath` de la cookie.

### Cómo se obtiene el usuario actual

[UsuarioActualService.cs](Services/UsuarioActualService.cs) lee `AuthenticationStateProvider`:

```csharp
var id = estado.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
UsuarioId = int.TryParse(id, out var valor) ? valor : 0;
NombreUsuario = estado.User.Identity?.Name ?? string.Empty;
```

Las páginas interactivas lo llaman en `OnAfterRenderAsync(firstRender)` porque la autenticación puede no estar disponible en el primer render estático.

---

## Enrutamiento

### Cómo se arma

1. [App.razor](Components/App.razor) contiene `<Routes />` dentro del `<body>`.
2. [Routes.razor](Components/Routes.razor) define:

   ```razor
   <Router AppAssembly="typeof(Program).Assembly">
       <Found Context="routeData">
           <AuthorizeRouteView RouteData="routeData"
                               DefaultLayout="typeof(Layout.MainLayout)">
               <NotAuthorized>...</NotAuthorized>
           </AuthorizeRouteView>
           <FocusOnNavigate RouteData="routeData" Selector="h1" />
       </Found>
   </Router>
   ```

3. Cada página declara su ruta con `@page` y, si corresponde, su layout con `@layout` y su modo de render con `@rendermode`.
4. `MapRazorComponents<App>()` en [Program.cs](Program.cs) conecta el árbol de componentes con el pipeline HTTP.

`FocusOnNavigate` mueve el foco al `h1` al cambiar de página, lo que mejora accesibilidad y navegación con teclado.

### Tabla de rutas

| Ruta | Archivo | Layout | Render | Acceso |
|---|---|---|---|---|
| `/` | [Home.razor](Components/Pages/Home.razor) | `MainLayout` | SSR estático | Autenticado |
| `/login` | [Login.razor](Components/Pages/Login.razor) | `AuthLayout` | SSR estático | Anónimo |
| `/registro` | [Registro.razor](Components/Pages/Registro.razor) | `MainLayout` | `InteractiveServer` | Admin |
| `/materias` | [Materias.razor](Components/Pages/Materias.razor) | `MainLayout` | `InteractiveServer` | Autenticado |
| `/tareas` | [Tareas.razor](Components/Pages/Tareas.razor) | `MainLayout` | `InteractiveServer` | Autenticado |
| `/usuarios` | [Usuarios.razor](Components/Pages/Usuarios.razor) | `MainLayout` | `InteractiveServer` | Admin |
| `/acceso-denegado` | [AccesoDenegado.razor](Components/Pages/AccesoDenegado.razor) | `MainLayout` | SSR estático | — |
| `/Error` | [Error.razor](Components/Pages/Error.razor) | `MainLayout` | SSR estático | — |
| `POST /logout` | [Program.cs](Program.cs) | — | Endpoint HTTP | Autenticado |

### Por qué cada página tiene el modo que tiene

- **Estático** (`Home`, `Login`, `AccesoDenegado`, `Error`): no necesitan estado interactivo. Se renderizan en el servidor y se envían como HTML.
- **`InteractiveServer`** (`Materias`, `Tareas`, `Usuarios`, `Registro`): necesitan botones, formularios, grilla, filtros y actualizaciones sin recargar.
- **`Login` estático** además por una razón técnica: la cookie solo se puede escribir en una respuesta HTTP.

### Navegación mejorada de Blazor

Blazor intercepta los clics en enlaces internos y reemplaza el contenido sin recargar la página completa (*enhanced navigation*). Eso mejora la velocidad, pero tiene una consecuencia: **reemplaza los atributos de `<html>`** con los del documento nuevo. Por eso el tema se resuelve en el servidor y con un respaldo en JavaScript (ver [Interfaz y sistema de diseño](#interfaz-y-sistema-de-diseño)).

---

## Componentes

### Layouts

#### MainLayout

**Archivo**: [MainLayout.razor](Components/Layout/MainLayout.razor) · **Estilos**: [MainLayout.razor.css](Components/Layout/MainLayout.razor.css)

Es el layout por defecto de `AuthorizeRouteView`. Arma el esqueleto visual:

```
sf-shell
├── aside.sf-sidebar        → <NavMenu />
├── div.sf-backdrop         → cierra el menú en móvil
└── div.sf-main
    ├── header.sf-topbar
    │   ├── botón menú (móvil)
    │   ├── marca StudyFlow
    │   ├── botón tema (sfTheme.toggle)
    │   └── dropdown de usuario + form de logout
    ├── main.sf-content     → @Body
    └── footer.sf-footer
```

**Lógica**:

- `AuthorizeView` decide si muestra el menú de usuario o el botón “Iniciar sesión”.
- Calcula la inicial del nombre y el rol visible (`Admin` → “Administrador”, resto → “Estudiante”).
- El botón de tema llama a `sfTheme.toggle()` (JavaScript).
- El botón de menú llama a `sfLayout.toggleSidebar()`.
- El logout es un POST real con antiforgery.
- Al final incluye `#blazor-error-ui`, el panel de error de la plantilla.

**Por qué así**: el layout no conoce la lógica de negocio; solo compone navegación, sesión y contenedor. El estado del tema y del menú móvil vive en JavaScript para que funcione incluso antes de que Blazor hidrate.

#### NavMenu

**Archivo**: [NavMenu.razor](Components/Layout/NavMenu.razor) · **Estilos**: [NavMenu.razor.css](Components/Layout/NavMenu.razor.css)

Menú lateral con tres estados:

| Estado | Qué muestra |
|---|---|
| Autenticado | Inicio, Materias, Tareas |
| Autenticado + Admin | Además: Usuarios, Registrar usuario |
| No autenticado | Iniciar sesión |

Usa `NavLink` con `Match="NavLinkMatch.All"` en Inicio para que solo se marque activo en `/`. Los iconos son SVG inline (sin dependencia de fuentes de iconos).

**Por qué así**: la navegación se adapta al rol sin lógica imperativa; `AuthorizeView` y `NavLink` ya resuelven el caso.

#### AuthLayout

**Archivo**: [AuthLayout.razor](Components/Layout/AuthLayout.razor) · **Estilos**: [AuthLayout.razor.css](Components/Layout/AuthLayout.razor.css)

Layout de dos paneles para `/login`:

- **Izquierda**: marca, propuesta de valor y lista de features.
- **Derecha**: tarjeta del formulario (`@Body`) y botón de tema.

En pantallas chicas el panel de marca se oculta y aparece un logo compacto.

**Por qué así**: separa la experiencia de autenticación del shell de la aplicación; el login no necesita sidebar.

### Páginas

#### Home

**Archivo**: [Home.razor](Components/Pages/Home.razor) · **Estilos**: [Home.razor.css](Components/Pages/Home.razor.css)

Panel de inicio autenticado. Muestra:

- Saludo según la hora del servidor (“Buenos días/tardes/noches”).
- Nombre del usuario y chip “Sesión activa”.
- Tarjetas de acción a Materias, Tareas y (si es Admin) Usuarios.

**Lógica**: todo se resuelve dentro de `AuthorizeView`; no hay estado propio ni llamadas a servicios. Es SSR estático: se renderiza por petición.

#### Login

**Archivo**: [Login.razor](Components/Pages/Login.razor) · **Estilos**: [Login.razor.css](Components/Pages/Login.razor.css)

Formulario de inicio de sesión. Puntos clave:

- `@layout AuthLayout`.
- `EditForm` con `method="post"` y `FormName="login"`.
- `DataAnnotationsValidator` + `ValidationMessage`.
- `[SupplyParameterFromForm]` para recibir el `LoginModel`.
- `[SupplyParameterFromQuery]` para `returnUrl`.
- Mensaje de error genérico (“Email o contraseña incorrectos”) para no filtrar qué parte falló.
- `DestinoSeguro()` evita redirecciones externas.

#### Registro

**Archivo**: [Registro.razor](Components/Pages/Registro.razor)

Página de **alta de usuarios** exclusiva del administrador. Cumple dos funciones y nada más:

1. **Formulario** con nombre, email, contraseña y fecha de nacimiento.
2. **Resumen en tiempo real** de lo que se está escribiendo (la contraseña se muestra enmascarada).

**Lógica**:

- `EditForm` + `RegistroModel` + `DataAnnotationsValidator`.
- Al enviar llama a `UsuarioService.RegistrarUsuarioAsync`.
- Si el email ya existe, muestra “Ya existe un usuario registrado con ese email”.
- Si Identity rechaza la contraseña, muestra el mensaje de la excepción.
- Al terminar bien, limpia el formulario y muestra confirmación.

**Por qué así**: se separó deliberadamente de la administración (`Usuarios.razor`) para que esta página sea solo alta + resumen, sin listado ni edición.

#### Materias

**Archivo**: [Materias.razor](Components/Pages/Materias.razor)

CRUD de materias del usuario autenticado.

**Estructura**:

- Cabecera con nombre del usuario.
- Formulario de creación: nombre, descripción y color (`<input type="color">`).
- Listado en tarjetas (`MateriaCard`).

**Lógica**:

- `OnAfterRenderAsync(firstRender)` carga el usuario desde `UsuarioActualService` y después las materias.
- `CrearMateria()` valida campos, llama a `MateriaService.CrearMateriaAsync`, limpia el formulario y recarga.
- `GuardarEdicion(Materia)` y `EliminarMateria(Materia)` son callbacks que recibe cada tarjeta.
- `cargando`, `guardando` y `mensaje` manejan feedback visual.

**Por qué así**: la página orquesta; la tarjeta solo muestra y dispara eventos. Así se puede reutilizar la tarjeta en otro contexto sin arrastrar la lógica de persistencia.

#### Tareas

**Archivo**: [Tareas.razor](Components/Pages/Tareas.razor)

CRUD de tareas con formulario reutilizado para alta y edición.

**Formulario**:

- Título y descripción (`InputText`, `InputTextArea`).
- Materia (`InputSelect` con las materias del usuario).
- Fecha de entrega opcional (`InputDate`).
- Estado, prioridad y dificultad (`InputSelect` sobre los enums, con nombres en español).
- Tiempo estimado en minutos (`InputNumber`).

**Lógica**:

- `tareaNueva` es un `TareaModel`.
- `editandoTarea` + `tareaEditadaId` deciden si se crea o se actualiza.
- `PrepararEdicion(Tarea)` copia los datos de la entidad al modelo.
- `CancelarEdicion()` limpia el formulario y vuelve a modo alta.
- `SolicitarEliminacion(Tarea)` borra y recarga.
- Los nombres de enums se traducen con `switch` (`ObtenerNombreEstado`, `ObtenerNombrePrioridad`, `ObtenerNombreDificultad`).

**Por qué así**: un solo formulario para alta y edición reduce duplicación y mantiene las mismas validaciones en ambos caminos.

#### Usuarios

**Archivo**: [Usuarios.razor](Components/Pages/Usuarios.razor) · **Estilos**: [Usuarios.razor.css](Components/Pages/Usuarios.razor.css)

Administración de usuarios, solo para `Admin`.

**Vista principal**:

- `RadzenDataGrid` con columnas ID, Nombre, Email, Fecha de nacimiento, Fecha de registro y Acciones.
- Ordenamiento, paginación (5/10/20/50), filas alternas, redimensionado de columnas y líneas horizontales.
- Buscador propio por nombre o email (`usuariosFiltrados`).
- Botón para mostrar/ocultar filtros por columna de Radzen.
- Botón “Limpiar” que limpia búsqueda y filtros del grid.
- Contador de usuarios.

**Detalle**:

- Al pulsar “Ver / Editar” se selecciona el usuario y aparece un panel lateral con `UsuarioCard`.
- La grilla pasa de `col-12` a `col-lg-8`.
- `@key="usuarioSeleccionado.IdUsuario"` fuerza a Blazor a recrear la tarjeta al cambiar de usuario.

**Lógica**:

- `OnInitializedAsync` carga los usuarios.
- `GuardarEdicion` llama al servicio y **recarga siempre** (aunque falle), para descartar cambios mutados en memoria.
- `EliminarUsuario` borra, cierra el detalle y recarga.
- `claseMensaje` alterna entre `alert-success`, `alert-danger` e `alert-info`.

**Por qué Radzen**: el requerimiento era mantener Radzen y usarlo donde aporta valor real (grilla con orden, filtros y paginación). El resto de la app usa Bootstrap puro.

#### AccesoDenegado

**Archivo**: [AccesoDenegado.razor](Components/Pages/AccesoDenegado.razor) · **Estilos**: [AccesoDenegado.razor.css](Components/Pages/AccesoDenegado.razor.css)

Página simple para `AccessDeniedPath`. Reutiliza el estilo `.sf-empty` del sistema de diseño.

#### Error

**Archivo**: [Error.razor](Components/Pages/Error.razor)

Página del pipeline de errores (`UseExceptionHandler`). Usa el `HttpContext` en cascada para mostrar el `Request ID`. Es la página original de la plantilla y conserva textos en inglés.

### Componentes compartidos

Los tres siguen el mismo patrón: **modo visualización + modo edición + confirmación de borrado**, con `EventCallback` hacia la página.

#### MateriaCard

**Archivo**: [MateriaCard.razor](Shared/MateriaCard.razor)

| Parámetro | Tipo | Uso |
|---|---|---|
| `Materia` | `Materia` | Entidad a mostrar |
| `OnGuardarEdicion` | `EventCallback<Materia>` | Notifica cambios al padre |
| `OnEliminar` | `EventCallback<Materia>` | Notifica borrado |

**Lógica**:

- Franja superior con el color de la materia.
- Botón “Editar” → copia los datos a campos locales y activa `modoEdicion`.
- “Guardar” valida nombre y descripción, muta la entidad y dispara `OnGuardarEdicion`.
- “Eliminar” pide confirmación en la misma tarjeta antes de disparar `OnEliminar`.
- `guardandoEdicion` y `eliminando` deshabilitan botones para evitar dobles envíos.

#### TareaCard

**Archivo**: [TareaCard.razor](Shared/TareaCard.razor)

| Parámetro | Tipo | Uso |
|---|---|---|
| `Tarea` | `Tarea` | Entidad a mostrar |
| `OnEditar` | `EventCallback<Tarea>` | Pide edición al padre |
| `OnEliminar` | `EventCallback<Tarea>` | Pide borrado |

Muestra título, descripción, materia, estado, prioridad, dificultad, fecha de entrega y tiempo estimado. La edición no ocurre dentro de la tarjeta: dispara `OnEditar` y la página carga el formulario.

#### UsuarioCard

**Archivo**: [UsuarioCard.razor](Shared/UsuarioCard.razor)

| Parámetro | Tipo | Uso |
|---|---|---|
| `Usuario` | `Usuario` | Entidad a mostrar |
| `OnGuardarEdicion` | `EventCallback<Usuario>` | Notifica cambios |
| `OnEliminar` | `EventCallback<Usuario>` | Notifica borrado |

**Lógica**:

- Modo visualización: nombre, email, fecha de nacimiento, fecha de registro.
- Modo edición: valida nombre obligatorio, email con `EmailAddressAttribute` y fecha de nacimiento (no futura, edad entre 16 y 100).
- Muestra errores por campo.
- Confirmación de borrado dentro de la tarjeta.

**Por qué la validación está duplicada**: la tarjeta puede usarse sin un `EditForm` (como ocurre en el panel de detalle), así que valida por su cuenta antes de llamar al servicio.

### Componente auxiliar

#### RedirectToLogin

**Archivo**: [RedirectToLogin.razor](Components/RedirectToLogin.razor)

No tiene ruta ni UI. En `OnInitialized` arma `returnUrl` con la ruta actual y navega a:

```
/login?returnUrl=%2Fmaterias
```

Usa `forceLoad: true` porque el login es SSR estático y necesita una petición completa.

---

## Servicios

### UsuarioService

**Archivo**: [UsuarioServices.cs](Services/UsuarioServices.cs)

Depende de `IDbContextFactory<ApplicationDbContext>` y `UserManager<Usuario>`.

| Método | Qué hace |
|---|---|
| `RegistrarUsuarioAsync` | Valida email único, crea el usuario con `CreateAsync` (hash + normalizados + stamp) |
| `ObtenerUsuariosAsync` | Lista ordenada por `Id` |
| `ObtenerUsuarioPorIdAsync` | Busca por `Id` |
| `ActualizarUsuarioAsync` | Actualiza nombre/fecha; si cambia el email usa `SetEmailAsync` + `SetUserNameAsync` |
| `EliminarUsuarioAsync` | Borra sesiones, tareas y materias; después `DeleteAsync` |

**Decisiones**:

- **`CreateAsync` en lugar de insertar a mano**: hashea la contraseña, completa `NormalizedEmail`, `NormalizedUserName`, `SecurityStamp` y valida la política.
- **Manejo de `DuplicateEmail`**: si dos registros compiten por el mismo email, devuelve `null` en lugar de explotar.
- **Cambio de email con `UserManager`**: si se actualizara solo la propiedad, `NormalizedEmail` y `UserName` quedarían viejos y el usuario no podría iniciar sesión.
- **Borrado en orden**: por las FK `Restrict`, primero `SesionesEstudio`, después `Tareas`, después `Materias`, y por último el usuario.

### MateriaService

**Archivo**: [MateriaService.cs](Services/MateriaService.cs)

| Método | Qué hace |
|---|---|
| `ObtenerMateriasAsync` | Materias del usuario, ordenadas por nombre |
| `CrearMateriaAsync` | Verifica que el usuario exista y crea la materia |
| `ActualizarMateriaAsync` | Verifica pertenencia y actualiza |
| `EliminarMateriaAsync` | Verifica pertenencia, borra tareas y después la materia |

**Detalle importante**: la verificación de usuario usa `u.Id`, no `u.IdUsuario`:

```csharp
bool usuarioExiste = await context.Usuarios
    .AnyAsync(u => u.Id == usuarioId);
```

`IdUsuario` es `[NotMapped]`, así que EF no puede traducirlo a SQL.

### TareaService

**Archivo**: [TareaService.cs](Services/TareaService.cs)

| Método | Qué hace |
|---|---|
| `ObtenerTareasAsync` | Tareas del usuario con `Include(t => t.Materia)`, ordenadas por fecha de entrega |
| `CrearTareaAsync` | Verifica que la materia pertenezca al usuario y crea |
| `ActualizarTareaAsync` | Verifica tarea y materia del usuario, actualiza |
| `EliminarTareaAsync` | Verifica pertenencia y borra |

**Por qué `Include(Materia)`**: la tarjeta muestra el nombre de la materia; sin `Include` llegaría `null`.

**Por qué se valida la materia en cada operación**: evita que un usuario asigne una tarea a una materia ajena manipulando el formulario.

### UsuarioActualService

**Archivo**: [UsuarioActualService.cs](Services/UsuarioActualService.cs)

No toca la base. Lee el `AuthenticationStateProvider` y expone:

- `UsuarioId`
- `NombreUsuario`
- `CargarUsuarioAsync()`
- `HayUsuarioSeleccionado()`

Se usa en las páginas interactivas para saber a quién pertenecen las materias y tareas.

---

## Interfaz y sistema de diseño

### Estrategia general

- **Bootstrap 5.3.3** aporta grilla, utilidades, formularios y componentes base.
- **`data-bs-theme`** en `<html>` activa el modo oscuro nativo de Bootstrap 5.3.
- **Variables `--sf-*`** en [app.css](wwwroot/app.css) definen la identidad visual y cambian solas con el tema.
- **CSS isolation** (`.razor.css`) encapsula estilos de layouts y páginas.
- **Radzen** se usa solo en la grilla de usuarios, con ajustes de modo oscuro por variables `--rz-*`.
- **Iconos**: SVG inline, sin fuentes externas.

### Archivos de estilo

| Archivo | Alcance |
|---|---|
| [app.css](wwwroot/app.css) | Global: tokens, tipografía, botones, formularios, tarjetas, alertas, piezas compartidas, overrides de Radzen e impresión |
| [MainLayout.razor.css](Components/Layout/MainLayout.razor.css) | Shell, sidebar, topbar, usuario, contenido, footer y responsive |
| [NavMenu.razor.css](Components/Layout/NavMenu.razor.css) | Marca, navegación y pie del menú |
| [AuthLayout.razor.css](Components/Layout/AuthLayout.razor.css) | Pantalla de login en dos paneles |
| [Home.razor.css](Components/Pages/Home.razor.css) | Hero y tarjetas de acción |
| [Login.razor.css](Components/Pages/Login.razor.css) | Tarjeta y cabecera del login |
| [Usuarios.razor.css](Components/Pages/Usuarios.razor.css) | Cabecera, toolbar y buscador de la grilla |
| [AccesoDenegado.razor.css](Components/Pages/AccesoDenegado.razor.css) | Contenedor de la página |
| [Routes.razor.css](Components/Routes.razor.css) | Bloque “Sin permisos” del router |

El bundle generado por CSS isolation es `ProyectoBlazor.styles.css`, que se referencia en [App.razor](Components/App.razor).

### Tokens principales (`:root`)

| Token | Claro | Oscuro |
|---|---|---|
| `--sf-brand` | `#4f46e5` | `#818cf8` |
| `--sf-bg` | `#f4f6fb` | `#0b1220` |
| `--sf-surface` | `#ffffff` | `#111827` |
| `--sf-surface-2` | `#f8fafc` | `#0f172a` |
| `--sf-border` | `#e6e9f0` | `#1f2937` |
| `--sf-text` | `#111827` | `#e5e7eb` |
| `--sf-muted` | `#667085` | `#94a3b8` |

Además:

- `--sf-font-size: 16px` escala toda la tipografía (el resto usa `rem`).
- `--sf-radius`, `--sf-radius-sm`, `--sf-sidebar-w`, `--sf-topbar-h`.
- `--sf-shadow`, `--sf-shadow-sm`.

### Integración de Radzen

`<RadzenTheme Theme="material" />` en [App.razor](Components/App.razor) carga el tema `material-base.css`. Como ese tema es claro, en modo oscuro se sobrescriben variables y reglas puntuales en [app.css](wwwroot/app.css):

```css
html[data-bs-theme="dark"] {
    --rz-base-background-color: #111827;
    --rz-body-background-color: #0b1220;
    --rz-text-color: #e5e7eb;
    --rz-text-secondary-color: #94a3b8;
    --rz-base-100: #0f172a;
    --rz-base-200: #111827;
    --rz-base-300: #1f2937;
    --rz-base-400: #334155;

    --rz-grid-stripe-background-color: var(--sf-surface-2);
    --rz-grid-header-color: var(--sf-muted);
    --rz-grid-header-background-color: var(--sf-surface-2);
}
```

Puntos resueltos:

- **Filas alternas**: Radzen usa `--rz-grid-stripe-background-color`, que por defecto vale `--rz-base-50` (`#fafafa`). Se apunta a `--sf-surface-2`.
- **Encabezados**: el color se aplica en un `<span class="rz-column-title">` interno. Se corrige la variable `--rz-grid-header-color`, no el `<th>`.
- **Botones neutros** (calendario de filtros de fecha): `.rz-button.rz-base.rz-shade-default` se fuerza a `--sf-surface-2`.
- **Inputs y paginador**: reglas directas bajo `html[data-bs-theme="dark"]`.

### Tema claro/oscuro

Se resuelve en **tres capas** para evitar parpadeos y pérdidas al navegar:

1. **Servidor** ([App.razor](Components/App.razor)): lee la cookie `studyflow-theme` y renderiza `<html data-bs-theme="...">`.
2. **Antes del primer pintado** (script inline en [App.razor](Components/App.razor)): resuelve el tema con prioridad `localStorage` → cookie/atributo del servidor → `prefers-color-scheme`, lo aplica y sincroniza ambos almacenamientos.
3. **Después de la navegación mejorada**: `Blazor.addEventListener("enhancedload", reaplicarTema)` vuelve a aplicar el tema guardado si el documento nuevo no lo trae.

El botón ([MainLayout.razor](Components/Layout/MainLayout.razor) y [AuthLayout.razor](Components/Layout/AuthLayout.razor)) llama a [theme.js](wwwroot/js/theme.js):

```js
window.sfTheme = {
    toggle, apply, current,
    isDark: () => current() === "dark"
};
```

`apply(theme)`:

- Setea `data-bs-theme`.
- Setea `color-scheme` para que los controles nativos acompañen.
- Persiste en `localStorage` **y** en cookie.

**Por qué dos almacenamientos**:

- `localStorage` es rápido y disponible antes del primer pintado.
- La cookie es la única forma de que el **servidor** sepa el tema y renderice el `<html>` correcto en la navegación mejorada.

### Barra lateral móvil

[theme.js](wwwroot/js/theme.js) también expone `sfLayout`:

- `toggleSidebar()` / `closeSidebar()`.
- Cierra al hacer clic en un enlace del menú.
- Cierra con `Escape`.

El estado es una clase en `<body>` (`sf-sidebar-open`), y el CSS de [MainLayout.razor.css](Components/Layout/MainLayout.razor.css) la usa para mostrar el panel y el backdrop.

### Bootstrap

Los archivos están locales en `wwwroot/bootstrap/`:

- [bootstrap.min.css](wwwroot/bootstrap/bootstrap.min.css)
- [bootstrap.bundle.min.js](wwwroot/bootstrap/bootstrap.bundle.min.js)

Se referencian en [App.razor](Components/App.razor) antes de `app.css`, para que el sistema de diseño pueda sobrescribir.

### Tipografía

La familia es `Inter` con fallback a las fuentes del sistema. No se descarga ninguna fuente externa: si `Inter` no está instalada, se usa la del sistema operativo.

---

## Flujos principales

### Alta de usuario (administrador)

```
Admin → /registro
  → completa RegistroModel
  → DataAnnotationsValidator valida
  → UsuarioService.RegistrarUsuarioAsync
      → UserManager.FindByEmailAsync (duplicado)
      → UserManager.CreateAsync (hash + normalizados)
  → mensaje de éxito + formulario limpio
```

### Inicio de sesión

```
Anónimo → /materias
  → AuthorizeRouteView detecta no autenticado
  → RedirectToLogin → /login?returnUrl=%2Fmaterias
  → Login (SSR) → SignInManager.PasswordSignInAsync
  → cookie de Identity
  → NavigateTo("/materias", forceLoad: true)
```

### Cierre de sesión

```
Botón “Cerrar sesión”
  → form POST /logout + AntiforgeryToken
  → SignInManager.SignOutAsync()
  → Redirect /login
```

### Crear materia y tarea

```
/materias
  → formulario de materia
  → MateriaService.CrearMateriaAsync
      → verifica usuario (u.Id)
      → inserta Materia
  → recarga tarjetas

/tareas
  → formulario de tarea
  → TareaService.CrearTareaAsync
      → verifica que la materia sea del usuario
      → inserta Tarea
  → recarga tarjetas
```

### Editar o eliminar usuario

```
/usuarios
  → “Ver / Editar” → UsuarioCard en modo visualización
  → “Editar” → validación local → OnGuardarEdicion
      → UsuarioService.ActualizarUsuarioAsync
          → SetEmailAsync / SetUserNameAsync si cambió el email
          → UpdateAsync
      → recarga la lista siempre

  → “Eliminar” → confirmación en la tarjeta
      → UsuarioService.EliminarUsuarioAsync
          → borra SesionesEstudio
          → borra Tareas
          → borra Materias
          → DeleteAsync (roles, claims, logins)
```

---

## Validaciones

### En modelos (`DataAnnotations`)

| Modelo | Reglas |
|---|---|
| [LoginModel.cs](Models/LoginModel.cs) | Email obligatorio y válido; contraseña obligatoria |
| [RegistroModel.cs](Models/RegistroModel.cs) | Nombre, email y contraseña obligatorios; contraseña ≥ 8; fecha de nacimiento obligatoria, no futura, edad entre 16 y 100 |
| [TareaModel.cs](Models/TareaModel.cs) | Título obligatorio y ≤ 100; descripción obligatoria; materia obligatoria; tiempo entre 1 y 1440; fecha de entrega no anterior a hoy |

### Validaciones personalizadas

- **`RegistroModel.ValidarFechaNacimiento`**: usa `[CustomValidation]` y calcula la edad correctamente (resta un año si el cumpleaños todavía no pasó).
- **`TareaModel.ValidarFechaEntrega`**: permite `null`, rechaza fechas pasadas.

### En componentes

- [UsuarioCard.razor](Shared/UsuarioCard.razor) valida nombre, email y fecha antes de disparar el evento.
- [MateriaCard.razor](Shared/MateriaCard.razor) valida nombre y descripción.
- [Materias.razor](Components/Pages/Materias.razor) valida antes de llamar al servicio.
- [Registro.razor](Components/Pages/Registro.razor) delega en `RegistroModel` y en Identity.

### En Identity

- Email único.
- Contraseña de 8 caracteres, con dígito y minúscula.
- Sin confirmación de cuenta.
- Sin bloqueo por intentos fallidos (`lockoutOnFailure: false` en el login).

---

## Decisiones de diseño

| Decisión | Por qué |
|---|---|
| Blazor Web App con islas `InteractiveServer` | Solo las pantallas que necesitan interacción pagan el costo del circuito |
| Login y logout por HTTP real | La cookie de autenticación solo se puede escribir/borrar en una respuesta HTTP |
| `IDbContextFactory` en los servicios | Evita contextos de larga vida y problemas de concurrencia en Blazor Server |
| Contexto scoped adicional para Identity | Los stores de Identity lo requieren |
| `Usuario : IdentityUser<int>` | Reutiliza hash, roles, claims y cookies sin reinventarlos |
| `IdUsuario` como alias `[NotMapped]` | Compatibilidad con el código previo sin duplicar columnas |
| Email único + índice | Evita cuentas duplicadas a nivel aplicación y base |
| `Restrict` en Materia → Tarea | Impide borrados accidentales en cascada |
| Borrado explícito en `UsuarioService` | Controla el orden y evita violar FK |
| Un solo `TareaModel` para alta y edición | Menos duplicación y mismas validaciones |
| Tarjetas con `EventCallback` | Separan presentación de persistencia y permiten reutilización |
| Radzen solo en la grilla | Aporta orden, filtros y paginación sin invadir el resto del diseño |
| Bootstrap + `data-bs-theme` | Modo oscuro nativo y consistente con el ecosistema |
| Variables `--sf-*` | Un solo lugar para cambiar colores y medidas |
| Tema en `localStorage` + cookie | Evita flash y sobrevive a la navegación mejorada |
| Script inline antes del CSS | Aplica el tema antes del primer pintado |
| Iconos SVG inline | Sin dependencias de fuentes ni peticiones extra |
| `--sf-font-size` centralizado | Escala toda la interfaz porque el resto usa `rem` |
| Migraciones manuales | Control explícito sobre cuándo se toca la base |

---

## Limitaciones y notas

Cosas que conviene saber al leer o extender el proyecto:

1. **Las migraciones no se aplican solas.** Hay que ejecutar `dotnet ef database update` antes de arrancar. Si hay pendientes, el inicializador no hace nada.
2. **Credenciales de demostración en el repositorio.** La contraseña de `sa` está en [appsettings.json](appsettings.json) y las de la app en [DbInitializer.cs](Data/DbInitializer.cs). Para producción habría que moverlas a variables de entorno o un gestor de secretos.
3. **`bin/` y `obj/` están versionados.** No afectan la ejecución, pero ensucian el repositorio y los diffs.
4. **[ApplicationDbContext.cs](Data/ApplicationDbContext.cs) conserva arriba un bloque comentado** con la versión previa del contexto (la que no usaba Identity). La clase activa empieza en la línea 140. Se puede borrar sin riesgo.
5. **`BCrypt.Net-Next` ya no se usa** para hashear; solo queda la referencia y la detección de hashes viejos (`$2`) en [DbInitializer.cs](Data/DbInitializer.cs).
6. **`Blazored.LocalStorage` está registrado pero no se usa** desde C#/Razor; el tema se maneja por JavaScript.
7. **`SesionEstudio`, `Etiqueta` y `TareaEtiqueta`** están modeladas y tienen tablas, pero todavía no tienen UI ni servicios.
8. **`TareaCard` muestra los enums en inglés** (`Pendiente`, `EnProgreso`, etc.), mientras que el formulario de [Tareas.razor](Components/Pages/Tareas.razor) los muestra en español. Es una inconsistencia visual pendiente.
9. **Código sin uso**: `ProbarClicMateria()` en [Materias.razor](Components/Pages/Materias.razor) y el `OnInitializedAsync` vacío de [Tareas.razor](Components/Pages/Tareas.razor).
10. **La página [Error.razor](Components/Pages/Error.razor) conserva textos en inglés** de la plantilla original.
11. **Estilos inline en las tarjetas** (franjas de color) en lugar de tokens; funcionan, pero no se adaptan al tema.
12. **No hay tests automatizados** en el repositorio.
13. **Radzen usa el tema claro `material` + overrides oscuros**, en lugar de `material-dark`, porque el cambio de tema es en tiempo de ejecución y no se puede recargar el CSS de Radzen por tema sin recargar la página.
14. **La sesión dura 8 horas** con expiración deslizante; no hay “recordarme” persistente más allá de eso.
15. **No hay recuperación de contraseña** ni envío de emails.
16. **El registro público no existe**: solo un administrador crea cuentas.

---

## Comandos útiles

```bash
# Restaurar y compilar
dotnet restore
dotnet build

# Ejecutar en desarrollo
dotnet run

# Ejecutar en un puerto específico
dotnet run --urls http://localhost:5057

# Base de datos
docker compose up -d
dotnet ef database update
dotnet ef migrations add NombreDeLaMigracion
dotnet ef migrations list

# Publicar
dotnet publish -c Release -o ./publish
```

> Si `dotnet ef` no está instalado:
>
> ```bash
> dotnet tool install --global dotnet-ef --version 8.*
> ```

---

## Créditos

- **Inspiración visual**: [Tabler](https://tabler.io) (licencia MIT) sobre Bootstrap 5.3.
- **Bootstrap**: [getbootstrap.com](https://getbootstrap.com) (MIT).
- **Radzen.Blazor**: [radzen.com/blazor-components](https://www.radzen.com/blazor-components/) (MIT).
- **.NET / Blazor / EF Core / Identity**: [Microsoft](https://dotnet.microsoft.com).

---

*Documento generado para el proyecto StudyFlow. Si agregás páginas, servicios o entidades, actualizá las secciones de [Enrutamiento](#enrutamiento), [Componentes](#componentes) y [Modelo de datos](#modelo-de-datos).*

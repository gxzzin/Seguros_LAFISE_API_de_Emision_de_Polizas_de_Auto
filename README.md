# Seguros LAFISE S.A. — Módulo de Emisión de Pólizas de Auto

Prototipo funcional del módulo de emisión de pólizas de automóvil: catálogos de clientes y
coberturas, emisión con cálculo de prima en el servidor, consulta de detalle e historial.

> **Convención del repositorio:** el código (clases, métodos, variables, tablas y columnas) está
> escrito **en inglés**; la documentación, los comentarios y los mensajes al usuario están
> **en español**. Las rutas de la API se mantienen en español porque forman parte del
> requerimiento funcional (`POST /api/polizas/emitir`).

---

## 1. Stack y arquitectura

| Componente        | Tecnología                                   |
|-------------------|----------------------------------------------|
| Lenguaje / runtime| C# 12 sobre .NET 8 (LTS)                      |
| API               | ASP.NET Core Web API (controladores)          |
| ORM               | Entity Framework Core 8 — **Code First**      |
| Base de datos     | SQL Server 2016 o superior                    |
| Documentación API | Swagger / OpenAPI (Swashbuckle)               |
| Pruebas           | xUnit                                         |
| Arquitectura      | En capas con inversión de dependencias (Clean / Onion) |

### Separación de responsabilidades

La solución está dividida en cuatro proyectos con dependencias en una sola dirección
(`Api → Infrastructure → Application → Domain`):

```
LafiseTest.sln
├── src/
│   ├── Lafise.Insurance.Domain            Entidades y excepciones de negocio. Sin dependencias.
│   │   ├── Entities/                      Customer, Vehicle, Coverage, Policy, PolicyCoverage
│   │   ├── Enums/                         PolicyStatus
│   │   └── Exceptions/                    EntityNotFound, BusinessRuleViolation, Conflict
│   │
│   ├── Lafise.Insurance.Application       Casos de uso. Aquí vive TODA la lógica de negocio.
│   │   ├── Abstractions/                  Interfaces de repositorios y servicios técnicos
│   │   ├── Contracts/                     DTOs de entrada y salida
│   │   ├── Options/                       Underwriting, PolicyNumbering, Identification
│   │   ├── Mapping/                       Entidad → DTO
│   │   └── Services/                      PolicyService, CustomerService, CoverageService,
│   │                                      VehicleService, PlateValidator, PlateNormalizer,
│   │                                      IdentificationValidator
│   │
│   ├── Lafise.Insurance.Infrastructure    Persistencia con EF Core.
│   │   ├── Persistence/                   DbContext, Configurations, Repositories, Migrations
│   │   └── Services/                      SequencePolicyNumberGenerator, SystemDateTimeProvider
│   │
│   └── Lafise.Insurance.Api               Capa HTTP. Sin lógica de negocio.
│       ├── Controllers/                   Customers, Coverages, Vehicles, Policies
│       ├── Middleware/                    ExceptionHandlingMiddleware (ProblemDetails)
│       └── Extensions/                    Aplicación de migraciones al arrancar
│
├── tests/Lafise.Insurance.UnitTests       104 pruebas de la lógica de negocio
├── database/                              Scripts .sql listos para ejecutar
└── postman/                               Colección de Postman con pruebas incluidas
```

**El controlador no calcula ni valida nada de negocio**: recibe la petición, delega en
`IPolicyService` y traduce el resultado a HTTP. Las reglas y el cálculo de la prima están en
`Lafise.Insurance.Application/Services/PolicyService.cs`.

### Inyección de dependencias

Se usa el contenedor nativo de .NET. Cada capa expone su propio método de registro:

- `AddApplication(configuration)` — servicios de caso de uso y `UnderwritingOptions`.
- `AddInfrastructure(configuration)` — `DbContext`, repositorios, generador de números de
  póliza y proveedor de fecha/hora.

Todos los servicios y repositorios se registran con ciclo de vida **Scoped** (uno por petición),
salvo `IDateTimeProvider`, `IPlateValidator` e `IIdentificationValidator`, que son **Singleton**
por no tener estado mutable (los validadores compilan su expresión regular una sola vez).

---

## 2. Modelo de datos

```
Customers                     Vehicles
─────────                     ────────
Id            int  PK         Id              int  PK
Name          nvarchar(150)   Plate           nvarchar(10)  UNIQUE
Identification                Brand           nvarchar(60)
  Number      nvarchar(20)    Model           nvarchar(60)
              UNIQUE          Year            int
Email         nvarchar(150)   CommercialValue decimal(18,2)
IsActive      bit             IsActive        bit
CreatedAtUtc  datetime2       CreatedAtUtc    datetime2
     │                             │
     │        Policies             │
     │        ────────             │
     └───────▶ Id             int  PK          ◀───────┘
              PolicyNumber   nvarchar(30) UNIQUE
              CustomerId     int  FK → Customers
              VehicleId      int  FK → Vehicles
              IssueDate      datetime2
              ExpirationDate datetime2
              InsuredAmount  decimal(18,2)
              TotalPremium   decimal(18,2)
              Status         tinyint   (1=Active, 2=Cancelled, 3=Expired)
              CreatedAtUtc   datetime2
                    │
                    │   PolicyCoverages            Coverages
                    │   ────────────────           ─────────
                    └──▶ PolicyId    int  PK,FK    Id          int  PK
                         CoverageId  int  PK,FK ──▶Name        nvarchar(80) UNIQUE
                         AppliedRate   decimal(5,2) Description nvarchar(250)
                         PremiumAmount decimal(18,2)Rate        decimal(5,2)
                                                    IsActive    bit
```

Decisiones de modelado que vale la pena señalar:

- **`PolicyCoverages` guarda `AppliedRate` y `PremiumAmount`.** Son una "foto" del catálogo al
  momento de emitir. Si mañana cambia la tasa de *Robo*, las pólizas ya emitidas conservan la
  tasa con la que se calcularon.
- **Índice único filtrado `UX_Policies_ActiveCustomerVehicle`** sobre `(CustomerId, VehicleId)
  WHERE Status = 1`. La regla "un cliente no puede tener dos pólizas activas para la misma
  placa" se valida en el servicio *y* queda garantizada por la base de datos.
- **Dos secuencias, `PolicyNumberSequence` y `CertificateNumberSequence`**, alimentan los dos
  correlativos del número de póliza. Delegarlos al motor evita colisiones si hay emisiones
  concurrentes; ambas se piden en una sola ida a la base de datos.
- **La placa se almacena normalizada** (mayúsculas, sin espacios ni guiones) y es única, de modo
  que `M-123456`, `m 123456` y `M123456` son el mismo vehículo.
- **`IsActive` en `Customers`, `Vehicles` y `Coverages`** soporta el borrado lógico: el `DELETE`
  de la API nunca elimina filas, porque las pólizas las referencian y se perdería el historial.
- Restricciones `CHECK` sobre año, valor comercial, montos y fechas; `ON DELETE NO ACTION` en
  las llaves foráneas de `Policies` para no perder historial.
- Las columnas `IsActive` **no llevan `DEFAULT`** a propósito. El valor por defecto del CLR para
  `bool` es `false`, y EF Core lo usa como centinela: con un `DEFAULT` en la base sería imposible
  insertar una fila inactiva, porque EF interpretaría `false` como "sin asignar". El valor inicial
  lo fija la entidad.

---

## 3. Puesta en marcha

### Requisitos

- [.NET SDK 8.0.4xx](https://dotnet.microsoft.com/download/dotnet/8.0) (por ejemplo 8.0.404)
- SQL Server 2016+ (Express, Developer o LocalDB)
- Para abrir la solución en un IDE: **Visual Studio 2022 17.8 o superior**, JetBrains Rider o
  VS Code. Visual Studio 2019 **no** sirve: no soporta .NET 8.
- Opcional: `sqlcmd` o SQL Server Management Studio; Postman

> **Sobre `global.json`:** el repositorio fija el SDK a la banda `8.0.4xx`
> (`rollForward: latestFeature`) para que el CLI y Visual Studio usen exactamente el mismo SDK
> que corresponde al target `net8.0`.
>
> Si tiene instalado el SDK 10 y Visual Studio 2022, **no lo quite ni lo cambie por
> `latestMajor`**: el SDK 10 exige MSBuild 18 (Visual Studio 2026) y VS 2022 falla al abrir la
> solución con `error MSB4236: The SDK 'Microsoft.NET.Sdk' specified could not be found`.
>
> Si sólo dispone de un SDK más reciente y acepta compilar con él desde la línea de comandos,
> puede subir la versión de `global.json` o eliminar el archivo; el target seguirá siendo
> `net8.0`.

### Opción A — Dejar que la API cree el esquema (la más rápida)

1. Ajuste la cadena de conexión en `src/Lafise.Insurance.Api/appsettings.json` si su instancia
   no es la predeterminada:

   ```json
   "ConnectionStrings": {
     "InsuranceDatabase": "Server=localhost;Database=LafiseInsurance;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
   }
   ```

   Para SQL Server con usuario y contraseña:

   ```
   Server=localhost,1433;Database=LafiseInsurance;User Id=sa;Password=SuPassword;TrustServerCertificate=True
   ```

   Para LocalDB:

   ```
   Server=(localdb)\\MSSQLLocalDB;Database=LafiseInsurance;Trusted_Connection=True
   ```

2. Ejecute la API. Con `Database:ApplyMigrationsOnStartup = true` (valor por defecto) creará la
   base de datos, aplicará las migraciones e insertará el catálogo inicial:

   ```bash
   dotnet run --project src/Lafise.Insurance.Api
   ```

3. Abra <http://localhost:5199/swagger>. La raíz `/` redirige a Swagger.

> **Nota sobre el puerto:** el perfil de ejecución usa `5199` (HTTP) y `7199` (HTTPS). Si están
> ocupados, cámbielos en `src/Lafise.Insurance.Api/Properties/launchSettings.json` o ejecute
> `dotnet run --project src/Lafise.Insurance.Api --urls http://localhost:5000`.

### Opción B — Ejecutar los scripts SQL a mano

Los scripts están en `database/` y se ejecutan en orden:

```bash
sqlcmd -S localhost -E -i database/01_create_database.sql
sqlcmd -S localhost -E -d LafiseInsurance -i database/02_schema.sql
```

| Script                       | Contenido                                                              |
|------------------------------|------------------------------------------------------------------------|
| `01_create_database.sql`     | Crea la base `LafiseInsurance` si no existe.                            |
| `02_schema.sql`              | Tablas, índices, restricciones, secuencia y datos de catálogo. Idempotente. |
| `03_verification_queries.sql`| Consultas de apoyo para revisar las emisiones (opcional).               |

`02_schema.sql` se generó con `dotnet ef migrations script --idempotent`, por lo que puede
ejecutarse varias veces sin duplicar objetos ni datos.

> El script activa `SET QUOTED_IDENTIFIER ON` en su encabezado: el índice filtrado lo exige y
> `sqlcmd` lo desactiva por defecto. Si regenera el script, conserve ese encabezado o invoque
> `sqlcmd` con el modificador `-I`. Lo mismo aplica si escribe en `Policies` a mano desde
> `sqlcmd`; los clientes .NET (EF Core / `SqlClient`) activan esa opción por su cuenta.

Después ponga `Database:ApplyMigrationsOnStartup` en `false` y ejecute la API.

### Trabajar con las migraciones de EF Core

```bash
dotnet tool install --global dotnet-ef --version 8.*

# Aplicar migraciones
dotnet ef database update \
  --project src/Lafise.Insurance.Infrastructure \
  --startup-project src/Lafise.Insurance.Api

# Regenerar el script SQL
dotnet ef migrations script --idempotent \
  --project src/Lafise.Insurance.Infrastructure \
  --startup-project src/Lafise.Insurance.Api \
  --output database/02_schema.sql
```

### Ejecutar las pruebas

```bash
dotnet test
```

---

## 4. Endpoints

Base local: `http://localhost:5199`

Los tres catálogos aceptan `?onlyActive=false` en el listado para incluir los registros dados
de baja.

**Clientes**

| Método | Ruta                   | Descripción                             | Respuestas         |
|--------|------------------------|-----------------------------------------|--------------------|
| GET    | `/api/clientes`        | Lista de clientes                        | 200                |
| GET    | `/api/clientes/{id}`   | Cliente por identificador                | 200, 404           |
| POST   | `/api/clientes`        | Registra un cliente                      | 201, 400, 409      |
| PUT    | `/api/clientes/{id}`   | Actualiza un cliente                     | 200, 400, 404, 409 |
| DELETE | `/api/clientes/{id}`   | Da de baja un cliente (baja lógica)      | 204, 404, 409      |

**Coberturas**

| Método | Ruta                     | Descripción                             | Respuestas         |
|--------|--------------------------|-----------------------------------------|--------------------|
| GET    | `/api/coberturas`        | Lista de coberturas                      | 200                |
| GET    | `/api/coberturas/{id}`   | Cobertura por identificador              | 200, 404           |
| POST   | `/api/coberturas`        | Da de alta una cobertura                 | 201, 400, 409      |
| PUT    | `/api/coberturas/{id}`   | Actualiza una cobertura                  | 200, 400, 404, 409 |
| DELETE | `/api/coberturas/{id}`   | Descontinúa una cobertura (baja lógica)  | 204, 404           |

**Vehículos**

| Método | Ruta                     | Descripción                             | Respuestas         |
|--------|--------------------------|-----------------------------------------|--------------------|
| GET    | `/api/vehiculos`         | Lista de vehículos                       | 200                |
| GET    | `/api/vehiculos/{id}`    | Vehículo por identificador               | 200, 404           |
| POST   | `/api/vehiculos`         | Registra un vehículo                     | 201, 400, 409      |
| PUT    | `/api/vehiculos/{id}`    | Actualiza un vehículo                    | 200, 400, 404, 409 |
| DELETE | `/api/vehiculos/{id}`    | Da de baja un vehículo (baja lógica)     | 204, 404, 409      |

**Pólizas**

| Método   | Ruta                      | Descripción                                   | Respuestas             |
|----------|---------------------------|-----------------------------------------------|------------------------|
| **POST** | **`/api/polizas/emitir`** | **Emite una póliza y calcula la prima**       | **201, 400, 404, 409** |
| GET      | `/api/polizas/{id}`       | Detalle de una póliza emitida                 | 200, 404               |
| GET      | `/api/polizas`            | Historial de emisiones (más reciente primero) | 200                    |

### Semántica del borrado (`DELETE`)

Ningún `DELETE` elimina filas: los tres marcan `IsActive = false`. Las pólizas referencian a
clientes, vehículos y coberturas, y un borrado físico destruiría el historial de emisiones.

- **Es idempotente:** repetir el `DELETE` sobre un registro ya inactivo devuelve `204` sin
  volver a escribir en la base.
- **Clientes y vehículos con pólizas activas devuelven `409`.** Primero hay que anular o dejar
  vencer esas pólizas.
- **Las coberturas se descontinúan sin restricción.** Dejan de ofrecerse en nuevas emisiones,
  pero las pólizas ya emitidas las siguen mostrando con la tasa que se les aplicó.
- **Para reactivar** un registro se usa el `PUT` con `isActive: true`.
- Los `PUT` que pasan un registro de activo a inactivo aplican la misma regla que el `DELETE`.

### Emisión — petición

`POST /api/polizas/emitir`

```json
{
  "customerId": 1,
  "vehicle": {
    "plate": "M 123456",
    "brand": "Toyota",
    "model": "Corolla",
    "year": 2022,
    "commercialValue": 20000
  },
  "coverageIds": [1, 2]
}
```

### Emisión — respuesta `201 Created`

Con cabecera `Location: /api/polizas/1`.

```json
{
  "id": 1,
  "policyNumber": "AU-0000001-000001-0",
  "status": "Active",
  "issueDate": "2026-09-04T09:10:47.4166020Z",
  "expirationDate": "2027-09-04T09:10:47.4166020Z",
  "insuredAmount": 20000.00,
  "totalPremium": 1150.00,
  "customer": {
    "id": 1,
    "name": "Maria Fernanda Lopez",
    "identificationNumber": "001-120589-1002B",
    "email": "maria.lopez@example.com"
  },
  "vehicle": {
    "id": 1,
    "plate": "M123456",
    "brand": "Toyota",
    "model": "Corolla",
    "year": 2022,
    "commercialValue": 20000.00
  },
  "coverages": [
    { "coverageId": 1, "name": "Robo",   "appliedRate": 2.50, "premiumAmount": 500.00 },
    { "coverageId": 2, "name": "Choque", "appliedRate": 3.25, "premiumAmount": 650.00 }
  ]
}
```

---

## 5. Lógica de negocio

### Cálculo de la prima

La prima **siempre se calcula en el servidor**; el cliente nunca la envía.

```
SumaAsegurada  = ValorComercialDelVehículo

PrimaPorCobertura(i) = redondear(SumaAsegurada × Tasa(i) / 100, 2 decimales)

PrimaTotal = Σ PrimaPorCobertura(i)
```

Ejemplo con el catálogo inicial y un vehículo de 20 000.00:

| Cobertura | Tasa   | Cálculo                | Prima      |
|-----------|--------|------------------------|------------|
| Robo      | 2.50%  | 20 000.00 × 2.50 / 100 | 500.00     |
| Choque    | 3.25%  | 20 000.00 × 3.25 / 100 | 650.00     |
| **Total** | 5.75%  |                        | **1150.00** |

El redondeo es a dos decimales con `MidpointRounding.AwayFromZero` (redondeo comercial), y se
aplica **por cobertura** para que el desglose sume exactamente la prima total.

### Reglas de validación

| Regla                                                  | Dónde se aplica            | Respuesta | `code`                    |
|--------------------------------------------------------|----------------------------|-----------|---------------------------|
| Antigüedad del vehículo ≤ 20 años                       | `PolicyService`            | 400       | `VEHICLE_TOO_OLD`         |
| Año del vehículo no puede ser futuro                    | `PolicyService`            | 400       | `INVALID_VEHICLE_YEAR`    |
| Placa obligatoria                                       | DataAnnotations + servicio | 400       | `PLATE_REQUIRED`          |
| Placa con formato válido                                | `PolicyService`            | 400       | `INVALID_PLATE_FORMAT`    |
| Un cliente no puede tener dos pólizas activas por placa | `PolicyService` + índice único filtrado | 409 | `DUPLICATE_ACTIVE_POLICY` |
| Al menos una cobertura                                  | DataAnnotations + servicio | 400       | `COVERAGES_REQUIRED`      |
| Coberturas sin repetir                                  | `PolicyService`            | 400       | `DUPLICATED_COVERAGES`    |
| Coberturas existentes                                   | `PolicyService`            | 404       | —                         |
| Coberturas activas                                      | `PolicyService`            | 400       | `INACTIVE_COVERAGE`       |
| Cliente existente                                       | `PolicyService`            | 404       | —                         |
| Cliente no dado de baja                                 | `PolicyService`            | 400       | `INACTIVE_CUSTOMER`       |
| Identificación obligatoria                              | `IdentificationValidator`  | 400       | `IDENTIFICATION_REQUIRED` |
| Identificación con formato de cédula o RUC válido       | `IdentificationValidator`  | 400       | `INVALID_IDENTIFICATION_FORMAT` |
| Identificación de cliente única                         | `CustomerService` + índice único | 409 | `CUSTOMER_ALREADY_EXISTS` |
| Nombre de cobertura único                               | `CoverageService` + índice único | 409 | `COVERAGE_ALREADY_EXISTS` |
| Placa de vehículo única                                 | `VehicleService` + índice único | 409  | `VEHICLE_ALREADY_EXISTS`  |
| No dar de baja un cliente con pólizas activas           | `CustomerService`          | 409       | `CUSTOMER_HAS_ACTIVE_POLICIES` |
| No dar de baja un vehículo con pólizas activas          | `VehicleService`           | 409       | `VEHICLE_HAS_ACTIVE_POLICIES`  |

**Antigüedad.** Se calcula como `año de emisión − año del vehículo`. Un vehículo de 2006 emitido
en 2026 tiene 20 años y **sí** se puede asegurar; uno de 2005 tiene 21 y se rechaza.

### Formato del número de póliza

El enunciado sólo dice "Autogenerado". El formato implementado replica el de las pólizas de
Seguros LAFISE (`AU-1234567-000123-0`):

```
  AU   - 1234567 - 000123 - 0
  │      │         │        └─ endoso: 0 en la emisión original
  │      │         └────────── correlativo del certificado (6 dígitos)
  │      └──────────────────── correlativo de la póliza (7 dígitos)
  └─────────────────────────── código del ramo (AU = Automóvil), configurable
```

Los dos correlativos vienen de sendas secuencias de SQL Server. El dígito de endoso es siempre `0`
porque el prototipo no maneja endosos ni renovaciones; ese es el campo que se incrementaría.

> La **lectura de cada segmento** es una interpretación del ejemplo real: el documento de
> requerimientos no describe el formato, y la póliza de muestra no explica qué significa cada
> bloque. Lo que sí replica con exactitud es la forma.

### Formato de la identificación

El enunciado pide "Identificación (DNI/RUC)", así que se aceptan dos formatos y se rechaza todo
lo demás. La identificación se normaliza (recorte de espacios y mayúsculas) antes de validarla y
de comprobar que no esté repetida.

**Cédula de identidad nicaragüense** — `NNN-DDMMAA-NNNNL`

```
  281 - 140891 - 0022V
  │     │        │  └─ letra de control (A-X)
  │     │        └──── número de producción (4 dígitos)
  │     └───────────── fecha de nacimiento (DDMMAA)
  └─────────────────── código del municipio (el primer dígito va de 0 a 6)
```

El patrón está tomado de [`@nerdify/dnic`](https://github.com/nerdify/dnic) (licencia ISC), una
librería de referencia para validar la cédula nicaragüense:

```
^[0-6]\d{2}-([0-2]\d|3[01])(0[1-9]|1[0-2])\d{2}-\d{4}[A-X]$
```

Además del patrón se comprueba que **la fecha de nacimiento exista de verdad**, de modo que
`281-300294-0022V` se rechaza aunque encaje en la expresión regular: el 30 de febrero no existe.
Como el año viene con dos dígitos el siglo es ambiguo, así que la fecha se acepta si es válida en
cualquiera de los dos; de lo contrario se rechazaría a alguien nacido el 29/02/2000, ya que 1900
no fue bisiesto.

**RUC jurídico** — una letra y 13 dígitos (`J0310000234567`), que es como se identifican las
empresas.

**Por qué importa la normalización.** La letra de control se escribe indistintamente en minúscula
o mayúscula, y el índice único está sobre el texto almacenado. Sin normalizar, `281-140891-0022v`
y `281-140891-0022V` serían **dos clientes distintos** y ambos pasarían el índice único.

> **Sobre el rango `A-X`:** las infografías del Consejo Supremo Electoral muestran ejemplos
> terminados en `Y`, que este patrón rechaza. No pude resolver la contradicción con fuentes
> fiables, así que seguí la librería de referencia. Por eso `CedulaPattern` es configurable: para
> admitir hasta la `Z` basta cambiar `[A-X]` por `[A-Z]` en `appsettings.json`, sin tocar código.
> Hay una prueba unitaria que verifica justamente ese escenario.

### Formato de placa

Se normaliza (mayúsculas, sin espacios, guiones ni puntos) y luego se valida contra la expresión
regular `^[A-Z]{1,3}[0-9]{3,6}$`: de 1 a 3 letras seguidas de 3 a 6 dígitos. Cubre el formato
nicaragüense de letra departamental más seis dígitos, tal como aparece impreso en las pólizas
(`M 105432`, que se almacena como `M105432`), y también `ABC1234` o `MG987`. El patrón es
configurable en `appsettings.json` sin tocar el código. La regla vive en `PlateValidator` y la
comparten la emisión y el CRUD de vehículos.

**Antigüedad del vehículo al registrarlo.** El límite de 20 años es una regla *de suscripción*,
no del catálogo: `POST /api/vehiculos` acepta un vehículo de 1995 sin problema, y es la emisión
la que rechaza asegurarlo.

**Reactivación implícita del vehículo.** Si se emite una póliza para la placa de un vehículo dado
de baja, el vehículo se reactiva: vuelve a estar en circulación. Sin esta regla, una baja lógica
dejaría esa placa inasegurable para siempre.

**Póliza activa duplicada.** Sólo bloquea si la póliza anterior sigue en estado `Active`. Si fue
anulada o expiró, se puede emitir una nueva para la misma placa. Otro cliente sí puede asegurar
esa misma placa.

### Parámetros configurables

`appsettings.json`, sección `Underwriting`:

```json
"PolicyNumbering": {
  "BranchCode": "AU"
},
"Identification": {
  "CedulaPattern": "^[0-6]\\d{2}-([0-2]\\d|3[01])(0[1-9]|1[0-2])\\d{2}-\\d{4}[A-X]$",
  "RucPattern": "^[A-Z]\\d{13}$",
  "ValidateBirthDate": true
},
"Underwriting": {
  "MaxVehicleAgeInYears": 20,
  "PlatePattern": "^[A-Z]{1,3}[0-9]{3,6}$",
  "PolicyTermInMonths": 12,
  "MinimumPremium": 0
}
```

---

## 6. Manejo de errores

`ExceptionHandlingMiddleware` traduce las excepciones del dominio a respuestas
[ProblemDetails (RFC 7807)](https://datatracker.ietf.org/doc/html/rfc7807), de modo que los
controladores no llevan bloques `try/catch`.

| Excepción                        | HTTP | Cuándo se produce                                     |
|----------------------------------|------|--------------------------------------------------------|
| `EntityNotFoundException`        | 404  | Cliente, póliza o cobertura inexistente                 |
| `BusinessRuleViolationException` | 400  | Regla de suscripción incumplida                         |
| `ConflictException`              | 409  | Conflicto con el estado actual (duplicados)             |
| Cualquier otra                   | 500  | Error no controlado (se registra en el log)             |

Los errores de forma del payload (campos faltantes, tipos incorrectos, correo inválido) los
maneja el propio `[ApiController]` y devuelve `400` con `ValidationProblemDetails`.

Ejemplo de error de negocio:

```json
{
  "title": "Regla de negocio incumplida",
  "status": 400,
  "detail": "No se puede emitir la póliza: el vehículo tiene 27 años de antigüedad y el máximo permitido es 20.",
  "instance": "/api/polizas/emitir",
  "code": "VEHICLE_TOO_OLD",
  "traceId": "0HNOACHHSBKDR:00000001"
}
```

El detalle de las excepciones no controladas sólo se expone en el entorno `Development`.

---

## 7. Datos iniciales

Las migraciones (y el script `02_schema.sql`) insertan el catálogo mínimo para probar de
inmediato.

**Clientes**

| Id | Nombre                      | Identificación   | Activo |
|----|-----------------------------|------------------|--------|
| 1  | Maria Fernanda Lopez        | 001-120589-1002B | Sí     |
| 2  | Carlos Alberto Mendoza      | 281-030777-0005X | Sí     |
| 3  | Distribuidora El Norte S.A. | J0310000234567   | Sí     |

**Coberturas**

| Id | Nombre                | Tasa  | Activa |
|----|-----------------------|-------|--------|
| 1  | Robo                  | 2.50% | Sí     |
| 2  | Choque                | 3.25% | Sí     |
| 3  | Responsabilidad Civil | 1.75% | Sí     |
| 4  | Incendio              | 1.10% | Sí     |
| 5  | Rotura de Cristales   | 0.60% | Sí     |
| 6  | Asistencia Vial       | 0.40% | **No** |

La cobertura 6 está inactiva a propósito: sirve para comprobar que la API rechaza con `400` una
emisión que la incluya, y que `GET /api/coberturas?onlyActive=false` la devuelve.

No se siembran vehículos: los crea la emisión o `POST /api/vehiculos`.

---

## 8. Pruebas

### Pruebas unitarias

`tests/Lafise.Insurance.UnitTests` contiene **104 pruebas**, con repositorios en memoria y un reloj
fijo (`FixedDateTimeProvider`) para que las reglas dependientes de la fecha sean deterministas.

```bash
dotnet test
```

| Clase de prueba        | Qué cubre                                                                                                                              |
|------------------------|----------------------------------------------------------------------------------------------------------------------------------------|
| `PolicyServiceTests`   | Cálculo y redondeo de la prima, límite exacto de 20 años, año futuro, normalización y formato de placa, póliza activa duplicada, cliente dado de baja, reutilización y reactivación del vehículo, coberturas inexistentes/repetidas/inactivas |
| `CustomerServiceTests` | Filtro de activos, actualización, identificación duplicada de otro cliente, reactivación, baja lógica, bloqueo por pólizas activas, idempotencia del `DELETE` |
| `CoverageServiceTests` | Filtro de activos, alta, nombre duplicado, cambio de tasa, reactivación, baja lógica sin restricción, idempotencia                      |
| `VehicleServiceTests`  | Normalización y formato de placa, placa duplicada (alta y actualización), independencia del límite de antigüedad, baja lógica, bloqueo por pólizas activas |
| `IdentificationValidatorTests` | Cédula y RUC válidos, normalización a mayúsculas, rango de la letra de control, municipio y fecha fuera de rango, fechas inexistentes, 29/02 bisiesto, patrón configurable |
| `PlateNormalizerTests` | Normalización de separadores y mayúsculas                                                                                              |

### Colección de Postman

`postman/Lafise.Insurance.postman_collection.json` — 45 peticiones y 77 aserciones automáticas,
organizadas en cinco carpetas numeradas para ejecutarse en ese orden con el *Collection Runner*:

| Carpeta                    | Peticiones | Contenido                                              |
|----------------------------|------------|--------------------------------------------------------|
| 1. Clientes (CRUD)         | 14         | Listado, alta, actualización, validación de cédula y RUC, conflictos y baja lógica |
| 2. Coberturas (CRUD)       | 8          | Catálogo completo, alta, cambio de tasa, descontinuar  |
| 3. Vehículos (CRUD)        | 8          | Alta con validación de placa, actualización, baja      |
| 4. Emisión                 | 3          | Emitir, consultar detalle e historial                  |
| 5. Reglas de validación    | 12         | Los 400/404/409 de cada regla de negocio               |

La variable `baseUrl` viene configurada en `http://localhost:5199`.

**La colección se puede ejecutar varias veces seguidas sin limpiar la base.** Ninguna petición
usa datos fijos que colisionen: los scripts de *pre-request* generan por corrida la identificación
del cliente, el nombre de la cobertura y las placas, y los identificadores creados se guardan en
variables de colección (`createdCustomerId`, `createdCoverageId`, `createdVehicleId`,
`createdVehiclePlate`, `issuedPlate`) que reutilizan el `PUT`, el `DELETE` y las pruebas de
conflicto.

**Prefijos de placa.** Las placas de la carpeta 3 (CRUD de vehículos) empiezan con `QA` y las de
la carpeta 4 (emisión) con `PO`, así en `GET /api/vehiculos` se ve de un vistazo qué carpeta creó
cada vehículo. Tenga en cuenta que **la emisión también registra vehículos**: si la placa no
existe, `POST /api/polizas/emitir` la da de alta, así que la carpeta 4 aporta filas a
`Vehicles` aunque no toque el CRUD.

La placa de la emisión se envía con guion (`PO-123456`) a propósito, para que la aserción
compruebe que el servidor la normaliza a `PO123456`. La prima no depende de la placa ni del
cliente —sólo del valor comercial (20 000.00) y de las tasas de las coberturas 1 y 2—, por lo que
la aserción de 1 150.00 sigue siendo válida en cada corrida.

> Respete el orden de las carpetas: la 5 comprueba el `409` de póliza duplicada y el `409` de
> baja de cliente con pólizas activas, que dependen de la emisión de la carpeta 4; y el `400` de
> cliente dado de baja depende del cliente que la carpeta 1 desactiva.

Todos los bodies declaran `options.raw.language = "json"`, de modo que Postman los muestra y
resalta como JSON en lugar de como texto plano.

Cada carpeta y cada petición llevan un identificador propio (`id`), y la colección un
`_postman_id`. Sin ellos Postman los genera al importar y el panel lateral deja de respetar el
orden del array `item[]`, con lo que las carpetas aparecen desordenadas y su numeración pierde
sentido. Los identificadores son deterministas —se derivan del nombre—, así que regenerar la
colección produce los mismos y reimportarla actualiza la existente en vez de duplicarla.

Los scripts usan `var` y no `const`: si el sandbox de Postman reutiliza el contexto entre
ejecuciones, un `const` repetido lanza `SyntaxError`, el script no llega a asignar la variable y
la petición se enviaría con el valor de la corrida anterior.

La colección se puede ejecutar también desde la línea de comandos con
[newman](https://www.npmjs.com/package/newman):

```bash
npm install -g newman
newman run postman/Lafise.Insurance.postman_collection.json
```

---

## 9. Verificación realizada

- `dotnet build -c Release` — sin errores ni advertencias.
- `dotnet test` — 104 pruebas, todas en verde.
- Compilación verificada tanto con el CLI (SDK 8.0.424) como con el MSBuild de
  Visual Studio 2022 17.14.
- Scripts SQL ejecutados contra SQL Server 2022 partiendo de una base vacía.
- **Los 18 endpoints probados de punta a punta contra SQL Server: 48 comprobaciones, todas
  correctas**, verificando en cada caso el código HTTP, el `code` de negocio y el cuerpo de la
  respuesta. Incluye el CRUD completo de los tres catálogos, la idempotencia del `DELETE`, el
  filtro `onlyActive`, y que una fila dada de baja sobrevive y sigue consultable por id.
- Comprobado que subir la tasa de *Robo* a 9.99% por `PUT /api/coberturas/1` **no altera** la
  póliza ya emitida: conserva `appliedRate = 2.50` y `totalPremium = 1150.00`.
- **La colección de Postman ejecutada con newman: 45 peticiones y 77 aserciones, sin fallas**, y
  repetida varias veces seguidas sobre la misma base sin limpiarla para confirmar que es
  re-ejecutable.
- Comprobado contra SQL Server que la misma cédula en minúscula y en mayúscula **no** crea dos
  clientes: la segunda devuelve `409`.
- Comprobado que las restricciones de la base rechazan por su cuenta una póliza activa
  duplicada (`UX_Policies_ActiveCustomerVehicle`) y un valor comercial no positivo
  (`CK_Vehicles_CommercialValue`), aun saltándose la capa de servicios.

Para eliminar la base de datos de prueba:

```sql
ALTER DATABASE [LafiseInsurance] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [LafiseInsurance];
```

---

## 10. Alcance y decisiones

### Cobertura del CRUD

El enunciado pide "un CRUD básico y la lógica de emisión". Los cuatro endpoints numerados del
apartado 3.B están implementados, y además el CRUD está completo sobre las tres entidades de
catálogo:

| Recurso     | Create | Read | Update | Delete            |
|-------------|--------|------|--------|-------------------|
| Clientes    | ✔      | ✔    | ✔      | ✔ (baja lógica)   |
| Coberturas  | ✔      | ✔    | ✔      | ✔ (baja lógica)   |
| Vehículos   | ✔      | ✔    | ✔      | ✔ (baja lógica)   |
| Pólizas     | ✔ (`/emitir`) | ✔ | ✖   | ✖                 |

Las pólizas **no** exponen `PUT` ni `DELETE` de forma deliberada: una póliza emitida es un
documento contable y no se edita ni se borra. El camino correcto sería un endpoint de anulación
que cambie `Status` a `Cancelled` dejando rastro, que queda fuera del alcance de esta prueba.

### Decisiones y extras

Lo que **está** implementado más allá de lo pedido, porque el modelo lo hacía natural:

- Vigencia de la póliza (`ExpirationDate`) y estado (`Status`), necesarios para que la regla de
  "póliza activa" tenga sentido.
- Congelado de la tasa aplicada en cada cobertura de la póliza.
- Índice único filtrado que respalda la regla de negocio en la propia base de datos.
- Borrado lógico en los tres catálogos, con las reglas descritas en el apartado 4.
- `SumaAsegurada` se **deriva** del valor comercial del vehículo en vez de recibirse en el
  payload, siguiendo el apartado 3.B.2.b del enunciado ("tasas aplicadas al valor comercial del
  auto"). Es una interpretación: si se quisiera permitir infraseguro o sobreseguro, habría que
  aceptarla como campo de entrada y validarla contra el valor comercial.
- El **número de póliza y el formato de placa** se ajustaron a los de una póliza real de Seguros
  LAFISE, porque el enunciado no los especifica. Ver el apartado 5.

Una diferencia conocida con la póliza real, que **no** se implementó por quedar fuera del alcance:

- La vigencia real corre de `23/09/2025 a las 00:01 h` a `22/09/2026 a las 24:00 h`, es decir,
  con horas explícitas y terminando el día anterior al aniversario. Aquí `ExpirationDate` es
  simplemente `IssueDate.AddMonths(12)`, que conserva la hora de emisión.
- La póliza real es un SOA (Responsabilidad Civil obligatoria) con límites fijos por rubro
  (`USD 2.5 / 5 / 2.5 miles`), no una suma asegurada derivada del valor del vehículo. El modelo
  de este prototipo corresponde al seguro voluntario que describe el enunciado, donde la prima
  sí se calcula sobre el valor comercial.

Lo que **no** está implementado, por quedar fuera del alcance de la prueba:

- Autenticación y autorización.
- Anulación o renovación de pólizas (el estado existe, pero no hay endpoint que lo cambie).
- Paginación del historial; hoy devuelve todas las emisiones.
- Pruebas de integración con `WebApplicationFactory` (`Program` ya está expuesto como `partial`
  para poder añadirlas).

/* ============================================================================
   Seguros LAFISE S.A. - Modulo de Emision de Polizas de Auto
   Script 02: esquema completo + datos de catalogo (clientes y coberturas).

   Generado con:  dotnet ef migrations script --idempotent
   Es idempotente: puede ejecutarse varias veces sin duplicar objetos ni datos.

   Uso:  sqlcmd -S localhost -E -d LafiseInsurance -i 02_schema.sql

   Nota: el indice filtrado UX_Policies_ActiveCustomerVehicle exige QUOTED_IDENTIFIER ON,
   opcion que sqlcmd desactiva por defecto; por eso el script la activa explicitamente.
   Si regenera el script con "dotnet ef migrations script", conserve este encabezado
   o invoque sqlcmd con el modificador -I.

   Tablas: Customers, Vehicles, Coverages, Policies, PolicyCoverages
   Secuencias: PolicyNumberSequence y CertificateNumberSequence, que alimentan los dos
   correlativos del numero de poliza con el formato AU-0000001-000001-0.
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE SEQUENCE [CertificateNumberSequence] AS int START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE SEQUENCE [PolicyNumberSequence] AS int START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE TABLE [Coverages] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(80) NOT NULL,
        [Description] nvarchar(250) NULL,
        [Rate] decimal(5,2) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Coverages] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Coverages_Rate] CHECK ([Rate] >= 0 AND [Rate] <= 100)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(150) NOT NULL,
        [IdentificationNumber] nvarchar(20) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE TABLE [Vehicles] (
        [Id] int NOT NULL IDENTITY,
        [Plate] nvarchar(10) NOT NULL,
        [Brand] nvarchar(60) NOT NULL,
        [Model] nvarchar(60) NOT NULL,
        [Year] int NOT NULL,
        [CommercialValue] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Vehicles] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Vehicles_CommercialValue] CHECK ([CommercialValue] > 0),
        CONSTRAINT [CK_Vehicles_Year] CHECK ([Year] BETWEEN 1900 AND 2200)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE TABLE [Policies] (
        [Id] int NOT NULL IDENTITY,
        [PolicyNumber] nvarchar(30) NOT NULL,
        [CustomerId] int NOT NULL,
        [VehicleId] int NOT NULL,
        [IssueDate] datetime2 NOT NULL,
        [ExpirationDate] datetime2 NOT NULL,
        [InsuredAmount] decimal(18,2) NOT NULL,
        [TotalPremium] decimal(18,2) NOT NULL,
        [Status] tinyint NOT NULL DEFAULT CAST(1 AS tinyint),
        [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Policies] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Policies_Dates] CHECK ([ExpirationDate] > [IssueDate]),
        CONSTRAINT [CK_Policies_InsuredAmount] CHECK ([InsuredAmount] > 0),
        CONSTRAINT [CK_Policies_TotalPremium] CHECK ([TotalPremium] >= 0),
        CONSTRAINT [FK_Policies_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Policies_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE TABLE [PolicyCoverages] (
        [PolicyId] int NOT NULL,
        [CoverageId] int NOT NULL,
        [AppliedRate] decimal(5,2) NOT NULL,
        [PremiumAmount] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_PolicyCoverages] PRIMARY KEY ([PolicyId], [CoverageId]),
        CONSTRAINT [CK_PolicyCoverages_AppliedRate] CHECK ([AppliedRate] >= 0 AND [AppliedRate] <= 100),
        CONSTRAINT [CK_PolicyCoverages_PremiumAmount] CHECK ([PremiumAmount] >= 0),
        CONSTRAINT [FK_PolicyCoverages_Coverages_CoverageId] FOREIGN KEY ([CoverageId]) REFERENCES [Coverages] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PolicyCoverages_Policies_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [Policies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'IsActive', N'Name', N'Rate') AND [object_id] = OBJECT_ID(N'[Coverages]'))
        SET IDENTITY_INSERT [Coverages] ON;
    EXEC(N'INSERT INTO [Coverages] ([Id], [Description], [IsActive], [Name], [Rate])
    VALUES (1, N''Cubre el hurto total del vehículo asegurado.'', CAST(1 AS bit), N''Robo'', 2.5),
    (2, N''Cubre daños propios por colisión o vuelco.'', CAST(1 AS bit), N''Choque'', 3.25),
    (3, N''Cubre daños a terceros en sus bienes y personas.'', CAST(1 AS bit), N''Responsabilidad Civil'', 1.75),
    (4, N''Cubre daños por incendio, rayo o explosión.'', CAST(1 AS bit), N''Incendio'', 1.1),
    (5, N''Cubre la rotura de parabrisas y ventanas.'', CAST(1 AS bit), N''Rotura de Cristales'', 0.6),
    (6, N''Cobertura descontinuada; se conserva sólo para pólizas históricas.'', CAST(0 AS bit), N''Asistencia Vial'', 0.4)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'IsActive', N'Name', N'Rate') AND [object_id] = OBJECT_ID(N'[Coverages]'))
        SET IDENTITY_INSERT [Coverages] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'Email', N'IdentificationNumber', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Customers]'))
        SET IDENTITY_INSERT [Customers] ON;
    EXEC(N'INSERT INTO [Customers] ([Id], [CreatedAtUtc], [Email], [IdentificationNumber], [IsActive], [Name])
    VALUES (1, ''2024-01-01T00:00:00.0000000Z'', N''maria.lopez@example.com'', N''001-120589-1002B'', CAST(1 AS bit), N''Maria Fernanda Lopez''),
    (2, ''2024-01-01T00:00:00.0000000Z'', N''carlos.mendoza@example.com'', N''281-030777-0005X'', CAST(1 AS bit), N''Carlos Alberto Mendoza''),
    (3, ''2024-01-01T00:00:00.0000000Z'', N''compras@elnorte.example.com'', N''J0310000234567'', CAST(1 AS bit), N''Distribuidora El Norte S.A.'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAtUtc', N'Email', N'IdentificationNumber', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Customers]'))
        SET IDENTITY_INSERT [Customers] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Coverages_Name] ON [Coverages] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Customers_IdentificationNumber] ON [Customers] ([IdentificationNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Policies_IssueDate] ON [Policies] ([IssueDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Policies_VehicleId] ON [Policies] ([VehicleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Policies_ActiveCustomerVehicle] ON [Policies] ([CustomerId], [VehicleId]) WHERE [Status] = 1');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Policies_PolicyNumber] ON [Policies] ([PolicyNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PolicyCoverages_CoverageId] ON [PolicyCoverages] ([CoverageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Vehicles_Plate] ON [Vehicles] ([Plate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904170906_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260904170906_InitialCreate', N'8.0.10');
END;
GO

COMMIT;
GO


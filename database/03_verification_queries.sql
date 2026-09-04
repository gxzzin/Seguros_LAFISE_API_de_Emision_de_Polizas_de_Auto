/* ============================================================================
   Seguros LAFISE S.A. - Modulo de Emision de Polizas de Auto
   Script 03: consultas de verificacion (opcional).

   Sirven para revisar el resultado de las emisiones hechas desde la API.

   Nota: si va a INSERTAR o ACTUALIZAR filas de Policies desde sqlcmd, invoquelo con el
   modificador -I (QUOTED_IDENTIFIER ON). El indice filtrado de la tabla lo exige y sqlcmd
   lo desactiva por defecto. Los clientes .NET (EF Core / SqlClient) ya lo activan solos.
   ============================================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE [LafiseInsurance];
GO

-- Catalogo de coberturas y su tasa.
SELECT Id, Name, Rate, IsActive
FROM   dbo.Coverages
ORDER  BY Name;
GO

-- Clientes registrados.
SELECT Id, Name, IdentificationNumber, Email
FROM   dbo.Customers
ORDER  BY Name;
GO

-- Historial de polizas emitidas con su cliente y vehiculo.
SELECT p.PolicyNumber,
       p.IssueDate,
       p.ExpirationDate,
       CASE p.Status WHEN 1 THEN 'Active' WHEN 2 THEN 'Cancelled' ELSE 'Expired' END AS Status,
       c.Name        AS CustomerName,
       v.Plate,
       v.Brand,
       v.Model,
       v.[Year],
       p.InsuredAmount,
       p.TotalPremium
FROM   dbo.Policies  p
JOIN   dbo.Customers c ON c.Id = p.CustomerId
JOIN   dbo.Vehicles  v ON v.Id = p.VehicleId
ORDER  BY p.IssueDate DESC;
GO

-- Desglose de la prima por cobertura de cada poliza.
SELECT p.PolicyNumber,
       cov.Name        AS Coverage,
       pc.AppliedRate,
       pc.PremiumAmount,
       p.TotalPremium
FROM   dbo.PolicyCoverages pc
JOIN   dbo.Policies        p   ON p.Id   = pc.PolicyId
JOIN   dbo.Coverages       cov ON cov.Id = pc.CoverageId
ORDER  BY p.PolicyNumber, cov.Name;
GO

-- Comprobacion de la regla "una sola poliza activa por cliente y placa".
SELECT p.CustomerId, v.Plate, COUNT(*) AS ActivePolicies
FROM   dbo.Policies p
JOIN   dbo.Vehicles v ON v.Id = p.VehicleId
WHERE  p.Status = 1
GROUP  BY p.CustomerId, v.Plate
HAVING COUNT(*) > 1;   -- No debe devolver filas.
GO

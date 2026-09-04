/* ============================================================================
   Seguros LAFISE S.A. - Modulo de Emision de Polizas de Auto
   Script 01: creacion de la base de datos.

   Ejecutar antes de 02_schema.sql. No borra nada si la base ya existe.
   Uso:  sqlcmd -S localhost -E -i 01_create_database.sql
   ============================================================================ */

IF DB_ID(N'LafiseInsurance') IS NULL
BEGIN
    PRINT N'Creando la base de datos LafiseInsurance...';
    CREATE DATABASE [LafiseInsurance];
END
ELSE
BEGIN
    PRINT N'La base de datos LafiseInsurance ya existe; no se realizan cambios.';
END
GO

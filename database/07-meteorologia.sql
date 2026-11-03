USE SantosConectaDB;
GO
IF OBJECT_ID('dbo.AlertaMeteorologico','U') IS NULL
BEGIN
 CREATE TABLE dbo.AlertaMeteorologico (
  Id nvarchar(128) NOT NULL PRIMARY KEY, Origem nvarchar(40) NOT NULL,
  Fonte nvarchar(160) NOT NULL, Titulo nvarchar(240) NOT NULL,
  Descricao nvarchar(4000) NOT NULL, Severidade nvarchar(40) NOT NULL,
  Regiao nvarchar(200) NOT NULL, Referencia nvarchar(1000) NOT NULL,
  PublicadoEm datetime2 NOT NULL, Inicio datetime2 NOT NULL, Fim datetime2 NOT NULL,
  Ativo bit NOT NULL, CONSTRAINT CK_AlertaMeteorologico_Validade CHECK(Fim>Inicio)
 );
 CREATE INDEX IX_AlertaMeteorologico_Ativo ON dbo.AlertaMeteorologico(Ativo,Fim);
END;
IF OBJECT_ID('dbo.PreferenciaMeteorologica','U') IS NULL
 CREATE TABLE dbo.PreferenciaMeteorologica (
  IdCidadao int NOT NULL PRIMARY KEY REFERENCES dbo.Cidadao(IdCidadao),
  Habilitada bit NOT NULL, Token nvarchar(2048) NULL, AtualizadaEm datetime2 NOT NULL
 );
IF OBJECT_ID('dbo.EntregaMeteorologica','U') IS NULL
 CREATE TABLE dbo.EntregaMeteorologica (
  IdAlerta nvarchar(128) NOT NULL REFERENCES dbo.AlertaMeteorologico(Id),
  IdCidadao int NOT NULL REFERENCES dbo.Cidadao(IdCidadao),
  ReservadaEm datetime2 NOT NULL, EnviadaEm datetime2 NULL, Estado nvarchar(40) NOT NULL,
  CONSTRAINT PK_EntregaMeteorologica PRIMARY KEY(IdAlerta,IdCidadao)
 );
GO

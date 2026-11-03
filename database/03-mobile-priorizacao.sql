-- Incremental e idempotente. Preserva registros. Requer 01-banco.sql.
USE SantosConectaDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.Cidadao','SenhaHash') IS NULL
 ALTER TABLE dbo.Cidadao ADD SenhaHash NVARCHAR(500) NULL;
ALTER TABLE dbo.HistoricoOcorrencia ALTER COLUMN IdFuncionario INT NULL;
IF COL_LENGTH('dbo.Ocorrencia','Gravidade') IS NULL
 ALTER TABLE dbo.Ocorrencia ADD Gravidade INT NOT NULL CONSTRAINT DF_Ocorrencia_Gravidade DEFAULT 20 CONSTRAINT CK_Gravidade CHECK(Gravidade BETWEEN 0 AND 40);
IF COL_LENGTH('dbo.Ocorrencia','ImpactoColetivo') IS NULL
 ALTER TABLE dbo.Ocorrencia ADD ImpactoColetivo INT NOT NULL CONSTRAINT DF_Ocorrencia_Impacto DEFAULT 5 CONSTRAINT CK_Impacto CHECK(ImpactoColetivo BETWEEN 0 AND 15);
IF COL_LENGTH('dbo.Ocorrencia','RiscoImediato') IS NULL
 ALTER TABLE dbo.Ocorrencia ADD RiscoImediato BIT NOT NULL CONSTRAINT DF_Ocorrencia_Risco DEFAULT 0;
IF COL_LENGTH('dbo.Ocorrencia','PontuacaoManual') IS NULL
 ALTER TABLE dbo.Ocorrencia ADD PontuacaoManual INT NULL CONSTRAINT CK_PontuacaoManual CHECK(PontuacaoManual BETWEEN 0 AND 100);
IF COL_LENGTH('dbo.Ocorrencia','JustificativaPrioridade') IS NULL
 ALTER TABLE dbo.Ocorrencia ADD JustificativaPrioridade NVARCHAR(500) NULL;
IF OBJECT_ID('dbo.IndicadorTerritorial','U') IS NULL
 CREATE TABLE dbo.IndicadorTerritorial (
 IdIndicador INT IDENTITY PRIMARY KEY, Bairro NVARCHAR(100) NOT NULL UNIQUE,
 IndiceVulnerabilidade INT NOT NULL CHECK(IndiceVulnerabilidade BETWEEN 0 AND 100),
 Fonte NVARCHAR(200) NOT NULL, AnoReferencia INT NOT NULL, TipoDado NVARCHAR(60) NOT NULL);
COMMIT;
GO
-- Índice filtrado: registros antigos sem conta continuam preservados.
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Cidadao_Email' AND object_id=OBJECT_ID('dbo.Cidadao'))
 EXEC(N'CREATE UNIQUE INDEX IX_Cidadao_Email ON dbo.Cidadao(Email) WHERE SenhaHash IS NOT NULL');
GO
-- Valores deliberadamente fictícios: não são estatísticas de Santos.
INSERT dbo.IndicadorTerritorial(Bairro,IndiceVulnerabilidade,Fonte,AnoReferencia,TipoDado)
SELECT v.Bairro,v.Indice,N'Cenário didático fictício, sem base estatística oficial',2026,N'SIMULAÇÃO ACADÊMICA'
FROM (VALUES (N'Centro',65),(N'Gonzaga',25),(N'Rádio Clube',80),(N'Ponta da Praia',30),(N'Valongo',70),(N'Macuco',55),(N'Embaré',35),(N'Chico de Paula',75),(N'Boqueirão',40),(N'Aparecida',45),(N'José Menino',50)) v(Bairro,Indice)
WHERE NOT EXISTS(SELECT 1 FROM dbo.IndicadorTerritorial i WHERE i.Bairro=v.Bairro);
GO
INSERT dbo.Categoria(Nome) SELECT v.Nome FROM (VALUES(N'Buracos nas vias'),(N'Coleta de lixo'),(N'Drenagem'),(N'Iluminação pública'),(N'Calçadas'),(N'Sinalização'),(N'Outros'))v(Nome)
WHERE NOT EXISTS(SELECT 1 FROM dbo.Categoria c WHERE c.Nome=v.Nome);
GO
CREATE OR ALTER PROCEDURE dbo.sp_IndicadoresTerritoriais
AS
 SELECT i.Bairro,i.IndiceVulnerabilidade,i.TipoDado,COUNT(o.IdOcorrencia) AS Quantidade
 FROM dbo.IndicadorTerritorial i LEFT JOIN dbo.Ocorrencia o ON o.Bairro=i.Bairro
 GROUP BY i.Bairro,i.IndiceVulnerabilidade,i.TipoDado;
GO

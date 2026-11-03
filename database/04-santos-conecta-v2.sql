-- Incremental e repetível. Executar após 03-mobile-priorizacao.sql. Não exclui registros.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.Funcionario','Telefone') IS NULL ALTER TABLE dbo.Funcionario ADD Telefone nvarchar(20) NOT NULL CONSTRAINT DF_Funcionario_Telefone DEFAULT '';
IF COL_LENGTH('dbo.Funcionario','Cargo') IS NULL ALTER TABLE dbo.Funcionario ADD Cargo nvarchar(100) NOT NULL CONSTRAINT DF_Funcionario_Cargo DEFAULT '';
IF COL_LENGTH('dbo.Funcionario','Ativo') IS NULL ALTER TABLE dbo.Funcionario ADD Ativo bit NOT NULL CONSTRAINT DF_Funcionario_Ativo DEFAULT 1;
IF OBJECT_ID('dbo.MensagemOcorrencia','U') IS NULL
BEGIN
 CREATE TABLE dbo.MensagemOcorrencia (
 IdMensagem int IDENTITY PRIMARY KEY, IdOcorrencia int NOT NULL REFERENCES dbo.Ocorrencia(IdOcorrencia),
 IdRemetente int NOT NULL, TipoRemetente nvarchar(20) NOT NULL CHECK(TipoRemetente IN ('Cidadao','Equipe')),
 NomeRemetente nvarchar(120) NOT NULL, Conteudo nvarchar(4000) NOT NULL CHECK(LEN(LTRIM(RTRIM(Conteudo)))>0),
 DataEnvio datetime2 NOT NULL, DataLeitura datetime2 NULL);
 CREATE INDEX IX_MensagemOcorrencia_Conversa ON dbo.MensagemOcorrencia(IdOcorrencia,DataEnvio);
END;
COMMIT;

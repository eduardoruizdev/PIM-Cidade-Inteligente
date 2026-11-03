SET XACT_ABORT ON;
IF COL_LENGTH('dbo.Ocorrencia','LocalizacaoConfirmada') IS NULL ALTER TABLE dbo.Ocorrencia ADD LocalizacaoConfirmada bit NOT NULL CONSTRAINT DF_Ocorrencia_LocalizacaoConfirmada DEFAULT 1;

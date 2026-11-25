IF OBJECT_ID('dbo.FotoOcorrencia', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FotoOcorrencia (
        IdFoto INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FotoOcorrencia PRIMARY KEY,
        IdOcorrencia INT NOT NULL,
        NomeArmazenado VARCHAR(80) NOT NULL,
        TipoConteudo VARCHAR(40) NOT NULL,
        TamanhoBytes BIGINT NOT NULL,
        DataEnvio DATETIME2 NOT NULL CONSTRAINT DF_FotoOcorrencia_DataEnvio DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_FotoOcorrencia_Tamanho CHECK (TamanhoBytes BETWEEN 1 AND 8388608),
        CONSTRAINT CK_FotoOcorrencia_Tipo CHECK (TipoConteudo IN ('image/jpeg', 'image/png', 'image/webp')),
        CONSTRAINT FK_FotoOcorrencia_Ocorrencia FOREIGN KEY (IdOcorrencia)
            REFERENCES dbo.Ocorrencia(IdOcorrencia)
    );
    CREATE INDEX IX_FotoOcorrencia_IdOcorrencia ON dbo.FotoOcorrencia(IdOcorrencia, DataEnvio);
END;

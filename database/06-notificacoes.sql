USE SantosConectaDB;
GO
-- Cada mensagem persistida é uma notificação única, sem duplicar conteúdo.
-- Cidadão: leitura no registro existente. Equipe: leitura individual por conta.
IF OBJECT_ID('dbo.LeituraMensagem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeituraMensagem (
        IdMensagem INT NOT NULL,
        IdFuncionario INT NOT NULL,
        DataLeitura DATETIME2 NOT NULL CONSTRAINT DF_LeituraMensagem_Data DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_LeituraMensagem PRIMARY KEY (IdMensagem, IdFuncionario),
        CONSTRAINT FK_LeituraMensagem_Mensagem FOREIGN KEY (IdMensagem) REFERENCES dbo.MensagemOcorrencia(IdMensagem),
        CONSTRAINT FK_LeituraMensagem_Funcionario FOREIGN KEY (IdFuncionario) REFERENCES dbo.Funcionario(IdFuncionario)
    );
    CREATE INDEX IX_LeituraMensagem_Funcionario ON dbo.LeituraMensagem(IdFuncionario, IdMensagem) INCLUDE(DataLeitura);
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Mensagem_Notificacoes' AND object_id = OBJECT_ID('dbo.MensagemOcorrencia'))
    CREATE INDEX IX_Mensagem_Notificacoes ON dbo.MensagemOcorrencia(TipoRemetente, IdOcorrencia, DataEnvio DESC) INCLUDE(DataLeitura);
GO

-- Banco acadêmico. Execute no SQL Server; nunca remove um banco existente.
USE master;
GO
IF DB_ID(N'SantosConectaDB') IS NULL
  CREATE DATABASE SantosConectaDB;
GO
USE SantosConectaDB;
GO
IF OBJECT_ID(N'dbo.Cidadao', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Cidadao (
    IdCidadao INT IDENTITY PRIMARY KEY,
    Nome NVARCHAR(120) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    Telefone VARCHAR(20) NOT NULL
  );
END;
GO
IF OBJECT_ID(N'dbo.Funcionario', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Funcionario (
    IdFuncionario INT IDENTITY PRIMARY KEY,
    Nome NVARCHAR(120) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    SenhaHash NVARCHAR(255) NOT NULL,
    Perfil VARCHAR(30) NOT NULL,
    CONSTRAINT UQ_Funcionario_Email UNIQUE (Email),
    CONSTRAINT CK_Funcionario_Perfil CHECK (Perfil IN ('Administrador', 'Funcionário'))
  );
END;
GO
IF OBJECT_ID(N'dbo.Categoria', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Categoria (
    IdCategoria INT IDENTITY PRIMARY KEY,
    Nome NVARCHAR(100) NOT NULL
  );
END;
GO
IF OBJECT_ID(N'dbo.Ocorrencia', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.Ocorrencia (
    IdOcorrencia INT IDENTITY PRIMARY KEY,
    Protocolo VARCHAR(30) NOT NULL,
    Titulo NVARCHAR(150) NOT NULL,
    Descricao NVARCHAR(MAX) NOT NULL,
    Bairro NVARCHAR(100) NOT NULL,
    Endereco NVARCHAR(200) NOT NULL,
    Latitude DECIMAL(10,7) NOT NULL,
    Longitude DECIMAL(10,7) NOT NULL,
    DataAbertura DATETIME2 NOT NULL,
    Status VARCHAR(30) NOT NULL,
    Prioridade VARCHAR(20) NOT NULL,
    IdCidadao INT NOT NULL,
    IdFuncionario INT NULL,
    IdCategoria INT NOT NULL,
    CONSTRAINT UQ_Ocorrencia_Protocolo UNIQUE (Protocolo),
    CONSTRAINT FK_Ocorrencia_Cidadao FOREIGN KEY (IdCidadao) REFERENCES dbo.Cidadao(IdCidadao),
    CONSTRAINT FK_Ocorrencia_Funcionario FOREIGN KEY (IdFuncionario) REFERENCES dbo.Funcionario(IdFuncionario),
    CONSTRAINT FK_Ocorrencia_Categoria FOREIGN KEY (IdCategoria) REFERENCES dbo.Categoria(IdCategoria),
    CONSTRAINT CK_Ocorrencia_Status CHECK (Status IN ('Aberta', 'Em análise', 'Em andamento', 'Resolvida', 'Cancelada')),
    CONSTRAINT CK_Ocorrencia_Prioridade CHECK (Prioridade IN ('Baixa', 'Média', 'Alta', 'Crítica')),
    CONSTRAINT CK_Ocorrencia_Coordenadas CHECK (Latitude BETWEEN -90 AND 90 AND Longitude BETWEEN -180 AND 180)
  );
END;
GO
IF OBJECT_ID(N'dbo.HistoricoOcorrencia', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.HistoricoOcorrencia (
    IdHistorico INT IDENTITY PRIMARY KEY,
    IdOcorrencia INT NOT NULL,
    IdFuncionario INT NOT NULL,
    Descricao NVARCHAR(500) NOT NULL,
    DataAlteracao DATETIME2 NOT NULL,
    CONSTRAINT FK_Historico_Ocorrencia FOREIGN KEY (IdOcorrencia) REFERENCES dbo.Ocorrencia(IdOcorrencia),
    CONSTRAINT FK_Historico_Funcionario FOREIGN KEY (IdFuncionario) REFERENCES dbo.Funcionario(IdFuncionario)
  );
  CREATE INDEX IX_Historico_IdOcorrencia ON dbo.HistoricoOcorrencia(IdOcorrencia);
END;
GO
IF OBJECT_ID(N'dbo.EmailEnviado', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.EmailEnviado (
    IdEmail INT IDENTITY PRIMARY KEY,
    IdOcorrencia INT NOT NULL,
    IdFuncionario INT NOT NULL,
    Destinatario VARCHAR(150) NOT NULL,
    Assunto NVARCHAR(200) NOT NULL,
    Mensagem NVARCHAR(MAX) NOT NULL,
    DataEnvio DATETIME2 NULL,
    StatusEnvio VARCHAR(30) NOT NULL,
    Erro NVARCHAR(500) NULL,
    CONSTRAINT FK_Email_Ocorrencia FOREIGN KEY (IdOcorrencia) REFERENCES dbo.Ocorrencia(IdOcorrencia),
    CONSTRAINT FK_Email_Funcionario FOREIGN KEY (IdFuncionario) REFERENCES dbo.Funcionario(IdFuncionario),
    CONSTRAINT CK_Email_Status CHECK (StatusEnvio IN ('Pendente', 'Simulado', 'Enviado', 'Falhou'))
  );
  -- Histórico e comunicação são consultados por ocorrência.
  CREATE INDEX IX_Email_IdOcorrencia ON dbo.EmailEnviado(IdOcorrencia);
END;
GO
-- Dados fictícios: executar a API com --seed após criar as tabelas.

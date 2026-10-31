USE SantosConectaDB;
GO
CREATE OR ALTER PROCEDURE dbo.sp_ListarOcorrenciasPorStatus @Status VARCHAR(30)
AS
BEGIN
  SELECT o.IdOcorrencia, o.Protocolo, o.Titulo, o.Bairro, o.Status,
         o.Prioridade, c.Nome AS Cidadao, f.Nome AS Funcionario
  FROM dbo.Ocorrencia o
  INNER JOIN dbo.Cidadao c ON c.IdCidadao = o.IdCidadao
  LEFT JOIN dbo.Funcionario f ON f.IdFuncionario = o.IdFuncionario
  WHERE o.Status = @Status
  ORDER BY o.DataAbertura DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_ContarOcorrenciasPorBairro
AS
BEGIN
  SELECT Bairro, COUNT(*) AS Quantidade
  FROM dbo.Ocorrencia
  GROUP BY Bairro
  ORDER BY Quantidade DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_AtualizarStatusOcorrencia
  @IdOcorrencia INT, @NovoStatus VARCHAR(30)
AS
BEGIN
  UPDATE dbo.Ocorrencia SET Status = @NovoStatus
  WHERE IdOcorrencia = @IdOcorrencia;
END;
GO

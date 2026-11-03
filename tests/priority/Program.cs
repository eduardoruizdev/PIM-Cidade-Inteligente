using SantosConecta.Application.Services;
using SantosConecta.Domain.Entities;
var agora=new DateTime(2026,10,31,12,0,0,DateTimeKind.Utc);
var o=new Ocorrencia {DataAbertura=agora.AddDays(-20),Gravidade=30,ImpactoColetivo=10};
var i=new IndicadorTerritorial {IndiceVulnerabilidade=80};
var p=CalculoPrioridade.Calcular(o,i,agora);
Verificar(p.Calculada==70,"Exemplo didático: 30 + 10 + 20 + 10 = 70");
o.Gravidade=40;o.ImpactoColetivo=15;o.DataAbertura=agora.AddDays(-400);i.IndiceVulnerabilidade=100;
Verificar(CalculoPrioridade.Calcular(o,i,agora).Calculada==100,"Teto 100 e espera limitada a 20");
o.DataAbertura=agora.AddDays(5);
Verificar(CalculoPrioridade.Calcular(o,null,agora).Tempo==0,"Data futura não gera espera negativa");
Verificar(CalculoPrioridade.Calcular(o,null,agora).Vulnerabilidade==0,"Ausência de indicador identificada, sem dados inventados");
o.RiscoImediato=true;o.PontuacaoManual=0;
Verificar(CalculoPrioridade.Calcular(o,null,agora).RevisaoUrgente,"Revisão manual zero não remove alerta urgente");
Verificar(CalculoPrioridade.Calcular(o,null,agora).Efetiva==0,"Revisão manual explícita");
Console.WriteLine("6 verificações determinísticas aprovadas.");
static void Verificar(bool result,string nome){if(!result)throw new Exception(nome);Console.WriteLine($"OK: {nome}");}

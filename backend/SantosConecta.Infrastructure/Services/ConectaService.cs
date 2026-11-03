using System.Globalization;
using System.Text.RegularExpressions;

namespace SantosConecta.Infrastructure.Services;

public class ConectaService(IConectaConsultas dados,IConectaModelo modelo,ConectaHistorico historico)
{
    private static AcaoConecta Acao(string id,string label,int? ocorrencia=null,string? via=null)=>new(id,label,ocorrencia,via);
    private static readonly CultureInfo Pt=CultureInfo.GetCultureInfo("pt-BR");
    public async Task<MensagemConecta[]> HistoricoAsync(int cidadao,CancellationToken ct)
    {
        var c=historico.Obter(cidadao);await c.Gate.WaitAsync(ct);
        try{return c.Mensagens.ToArray();}finally{c.Gate.Release();}
    }
    public async Task LimparAsync(int cidadao,CancellationToken ct)
    {
        var c=historico.Obter(cidadao);await c.Gate.WaitAsync(ct);
        try{c.Mensagens.Clear();}finally{c.Gate.Release();}
    }
    public async Task<MensagemConecta> ConversarAsync(int cidadao,string pergunta,CancellationToken ct)
    {
        pergunta=pergunta.Trim();
        if(pergunta.Length is <1 or >1000 || pergunta.Any(c=>char.IsControl(c)&&c!='\n'&&c!='\t'))throw new ArgumentException("Escreva uma pergunta de até 1.000 caracteres.");
        if(Regex.IsMatch(IntencoesConecta.Normalizar(pergunta),@"\b(senha|password|bearer|api[_ -]?key|token)\b") || Regex.IsMatch(pergunta,@"(?:gsk_|sk-|eyJ)[A-Za-z0-9_-]{12,}"))
            return new("assistant","Não envie senhas, tokens ou chaves no chat. Essa mensagem não foi armazenada nem enviada à IA.",DateTime.UtcNow,"orientativo",[]);
        var c=historico.Obter(cidadao);await c.Gate.WaitAsync(ct);
        try
        {
            // Não enviar e-mails/telefones espontâneos ao provedor nem ao histórico.
            pergunta=Regex.Replace(pergunta,@"[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}","[e-mail omitido]");
            pergunta=Regex.Replace(pergunta,@"(?<!\w)(?:\+55\s*)?\(?\d{2}\)?\s*9?\d{4}[- ]?\d{4}(?!\w)","[telefone omitido]");
            var contexto=c.Mensagens.ToArray();
            var local=IntencoesConecta.Local(pergunta,contexto);
            // Recusas explícitas não são delegadas ao modelo.
            var blocked=Regex.IsMatch(IntencoesConecta.Normalizar(pergunta),@"\b(sql|ignore|ignorar|permissoes|outro cidadao|outra pessoa)\b");
            var plan=blocked?null:await modelo.InterpretarAsync(cidadao,pergunta,contexto,ct);
            var modo=plan is null?"orientativo":"inteligente";
            plan=blocked?new("fora"):plan??local;
            string text;AcaoConecta[] actions;
            try {(text,actions)=await ResponderAsync(cidadao,plan,ct);}
            catch(Exception) when(!ct.IsCancellationRequested)
            {text="Não consegui consultar esse serviço agora. Não é possível confirmar dados atualizados. Você pode tentar novamente ou abrir a seção correspondente.";actions=AcoesPara(plan);}
            if(text.Length>6000)text=text[..5800]+"\nConsulte os demais detalhes na seção correspondente.";
            var answer=new MensagemConecta("assistant",text,DateTime.UtcNow,modo,actions);
            c.Mensagens.Add(new("user",pergunta,DateTime.UtcNow,modo,[]));c.Mensagens.Add(answer);
            if(c.Mensagens.Count>20)c.Mensagens.RemoveRange(0,c.Mensagens.Count-20);
            return answer;
        }
        finally{c.Gate.Release();}
    }
    private static AcaoConecta[] AcoesPara(PlanoConecta p)=>p.Intencao switch
    {
        "nova"=>[Acao("NOVA_RECLAMACAO","Abrir formulário")],
        "reclamacoes" or "respostas"=>[Acao("MINHAS_RECLAMACOES","Minhas reclamações")],
        "mensagens"=>[Acao("MENSAGENS","Ver mensagens")],
        "clima"=>[Acao("CLIMA","Clima em Santos")],
        "alertas"=>[Acao("ALERTAS","Central de Alertas")],
        "transito"=>[Acao("TRANSITO","Abrir mapa",via:p.Via)],
        _=>[Acao("NOVA_RECLAMACAO","Nova reclamação"),Acao("MINHAS_RECLAMACOES","Acompanhar protocolos"),Acao("CLIMA","Consultar clima")]
    };
    private async Task<(string,AcaoConecta[])> ResponderAsync(int cidadao,PlanoConecta p,CancellationToken ct)
    {
        var actions=AcoesPara(p);
        switch(p.Intencao)
        {
            case "nova":return ("Posso ajudar você a registrar o problema. Abra o formulário, escolha a categoria, descreva o ocorrido e confirme a localização. Revise os dados antes de enviar. Nenhuma reclamação foi criada pelo chat.",actions);
            case "mensagens":return ("As respostas da equipe ficam nas mensagens da sua reclamação e no sino de notificações. Abra a lista e selecione o protocolo desejado.",actions);
            case "reclamacoes":case "respostas":
            {
                var all=await dados.MinhasAsync(cidadao,ct);
                var selected=all.Where(o=>p.Protocolo==null || o.Protocolo.Equals(p.Protocolo,StringComparison.OrdinalIgnoreCase));
                if(!string.IsNullOrWhiteSpace(p.Categoria))selected=selected.Where(o=>IntencoesConecta.Normalizar(o.Categoria).Contains(IntencoesConecta.Normalizar(p.Categoria)));
                if(p.Abertas)selected=selected.Where(o=>o.Status is not "Resolvida" and not "Cancelada");
                var items=selected.Take(8).ToList();
                if(items.Count==0)return ("Não encontrei reclamações correspondentes na sua conta. Confira o protocolo completo na sua lista. Só posso consultar registros vinculados ao seu login.",actions);
                var lines=new List<string>();
                foreach(var o in items)
                {
                    var line=$"Protocolo {o.Protocolo} · {o.Categoria}: {o.Status}.";
                    if(p.Intencao=="respostas") {var r=await dados.RespostasAsync(cidadao,o.Id,ct);line+=r.Quantidade>0?$" Há {r.Quantidade} resposta(s) da equipe; última em {Utc(r.Ultima!.Value)}.":" Nenhuma mensagem da equipe registrada nesta consulta.";}
                    lines.Add(line);
                }
                return (string.Join("\n\n",lines)+$"\n\nConsulta realizada em {Utc(DateTime.UtcNow)}. Exibindo até oito resultados; a lista completa está em Minhas reclamações.",items.Select(o=>Acao("DETALHE_RECLAMACAO","Abrir "+o.Protocolo,o.Id)).Append(actions[0]).ToArray());
            }
            case "clima":
            {
                var climate=await dados.ClimaAsync(ct);
                if(climate.ValidoAte<DateTime.UtcNow)return ("Os dados meteorológicos disponíveis estão desatualizados. Abra o clima e tente atualizar.",actions);
                var current=climate.Dados.GetProperty("current");var daily=climate.Dados.GetProperty("daily");
                var day=Math.Clamp(p.Dia,0,daily.GetProperty("time").GetArrayLength()-1);
                string Number(System.Text.Json.JsonElement parent,string property,int? index=null)
                {var v=parent.GetProperty(property);if(index.HasValue)v=v[index.Value];return v.ValueKind==System.Text.Json.JsonValueKind.Number?v.GetDouble().ToString("0.#",Pt):"indisponível";}
                var date=DateTimeOffset.FromUnixTimeSeconds(daily.GetProperty("time")[day].GetInt64()).ToOffset(TimeSpan.FromHours(-3)).ToString("dd/MM",Pt);
                var time=DateTimeOffset.FromUnixTimeSeconds(current.GetProperty("time").GetInt64()).UtcDateTime;
                return ($"Santos: temperatura do modelo em {Utc(time)}: {Number(current,"temperature_2m")} °C; sensação térmica {Number(current,"apparent_temperature")} °C.\nPrevisão para {date}: mínima {Number(daily,"temperature_2m_min",day)} °C e máxima {Number(daily,"temperature_2m_max",day)} °C. Probabilidade máxima de chuva no dia: {Number(daily,"precipitation_probability_max",day)}%.\nFonte: Open-Meteo. Última consulta: {Utc(climate.UltimaAtualizacao)}. Previsão pontual, sujeita a mudanças; não confirma chuva em todos os bairros.",actions);
            }
            case "alertas":
            {
                var alerts=await dados.AlertasAsync(ct);
                return (alerts.Count==0?"Nenhum aviso ativo foi retornado pela base do Santos Conector nesta consulta. Isso não garante ausência de riscos nem cobertura completa de avisos oficiais. Consulte a Central de Alertas para detalhes.":
                    string.Join("\n\n",alerts.Select(a=>$"{(a.Origem=="Automatico"?"Aviso preventivo automático":"Alerta oficial")} · {a.Titulo}\nFonte: {a.Fonte}; região: {a.Regiao}; validade: {Utc(a.Inicio)} a {Utc(a.Fim)}.\n{a.Descricao}"))+"\n\nConsulta: "+Utc(DateTime.UtcNow),actions);
            }
            case "transito":
            {
                if(p.Via==null)return ("Não tenho cobertura de todas as vias de Santos. Posso consultar os trechos de referência da Avenida Ana Costa ou da Avenida Conselheiro Nébias. Qual delas você deseja? O mapa mostra a disponibilidade dos dados reais.",actions);
                var region=p.Via=="Avenida Ana Costa"?"Av. Ana Costa":"Av. Conselheiro Nébias";
                var traffic=await dados.TransitoAsync(region,ct);
                if(!traffic.Disponivel || traffic.ValidoAte is null || traffic.ValidoAte<DateTime.UtcNow || traffic.Trechos.Count==0)return ($"Não há dados de trânsito válidos disponíveis agora para o ponto de referência de {p.Via}. Isso não significa que o trânsito esteja livre. Abra o mapa para consultar novamente.",actions);
                return (string.Join("\n",traffic.Trechos.Select(t=>$"Trecho próximo à referência {p.Via}: {(t.Interditado?"interdição informada":$"velocidade atual {t.VelocidadeAtual} km/h; fluxo livre {t.VelocidadeLivre} km/h")}."))+ $"\nFonte: TomTom; consulta em {Utc(traffic.UltimaAtualizacao!.Value)}. Os dados não descrevem a avenida inteira.",actions);
            }
            case "ajuda":return ("Olá! Sou o Conecta. Posso orientar você a registrar e acompanhar reclamações, verificar respostas da equipe, consultar o clima, avisos e dados disponíveis de trânsito em Santos. O que você precisa?",actions);
            default:return ("Posso ajudar com o Santos Conector: reclamações da sua conta, mensagens, clima, alertas e trânsito. Não acesso dados de outras pessoas, não altero permissões e não executo comandos. Conte o que você precisa nessas áreas.",actions);
        }
    }
    private static string Utc(DateTime value)=>DateTime.SpecifyKind(value,DateTimeKind.Utc).AddHours(-3).ToString("dd/MM/yyyy HH:mm",Pt)+" (Santos, UTC−3)";
}

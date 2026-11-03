# Priorização Territorial Justa

Esta pontuação é uma ferramenta de apoio à triagem, baseada em critérios técnicos e territoriais. A decisão final permanece sob responsabilidade da equipe administrativa.

## Fórmula no C#

`Pontuação = Gravidade + Tempo + Vulnerabilidade + Impacto`

| Componente | Regra acadêmica inicial |
|---|---|
| Gravidade | Inteiro 0–40, revisado pela equipe; inicial 20 |
| Tempo | `min(20, dias completos / 2)`, divisão inteira; 1 ponto por 2 dias, teto em 40 dias |
| Vulnerabilidade | Índice territorial 0–100 × 0,25, arredondado; máximo 25 |
| Impacto coletivo | Inteiro 0–15, revisado pela equipe; inicial 5 |

Exemplo: gravidade 30 + espera de 20 dias (10) + índice 80 (20) + impacto 10 = **70/100**.

Implementação: `CalculoPrioridade.Calcular`, em `backend/SantosConecta.Application/Services/PriorizacaoService.cs`. O total é calculado em cada consulta para não congelar a espera; critérios e revisões são persistidos. UTC, datas futuras sem espera negativa. Sem indicador, o componente territorial vale zero com aviso explícito: isso não significa ausência de vulnerabilidade.

## Fila e revisão humana

1. Risco imediato marcado pela equipe → revisão urgente, antes de qualquer pontuação.
2. Pontuação efetiva decrescente (manual quando preenchida; calculada nos demais casos).
3. Maior espera como desempate.

Resolvidas/canceladas ficam fora da fila. A revisão manual aceita 0–100; removê-la restaura o cálculo. Nem revisão para zero remove o alerta urgente. Justificativa de 10–350 caracteres é obrigatória; histórico registra critérios anteriores e novos, funcionário e horário.

A classificação administrativa Baixa/Média/Alta/Crítica foi preservada, distinta da pontuação da fila. Sua alteração também exige justificativa. Nenhuma pontuação envia equipes, distribui recursos ou fecha registros automaticamente.

## Indicadores e limites

`IndicadorTerritorial` contém IdIndicador, Bairro, IndiceVulnerabilidade, Fonte, AnoReferencia e TipoDado. Os 11 valores iniciais de 2026 são **SIMULAÇÃO ACADÊMICA**, sem validade estatística e sem vínculo com estatísticas oficiais de Santos.

O vínculo é com o bairro da ocorrência, nunca com a condição pessoal do cidadão. Não se usam renda individual, raça, religião ou atributos sensíveis. O índice não descreve todos os moradores nem bloqueia registros de qualquer região. Volume de reclamações não participa da fórmula.

O mapa mostra círculos somente onde há correspondência com pontos aproximados do catálogo de bairros, sem polígonos oficiais ou cobertura estatística. Bairros sem ponto continuam na fila e tabelas. Calor representa concentração de registros e não substitui os critérios territoriais.

Tempo médio usa somente resoluções com evento explícito no histórico; ausência aparece como **Sem dado**, sem inventar data. Antigas são as pendentes há mais de 30 dias; maior vulnerabilidade usa índice simulado ≥ 60. Cortes e pesos são parâmetros acadêmicos iniciais, não política pública validada. A Groq não cria indicadores nem decide a prioridade final.

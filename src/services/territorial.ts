export interface Indicator {
  idIndicador: number;
  bairro: string;
  indiceVulnerabilidade: number;
  fonte: string;
  anoReferencia: number;
  tipoDado: string;
}
export interface QueueItem {
  idOcorrencia: number;
  protocolo: string;
  categoria: string;
  bairro: string;
  status: string;
  gravidade: number;
  impactoColetivo: number;
  riscoImediato: boolean;
  pontuacaoManual: number | null;
  indicador: Indicator | null;
  pontuacao: {
    gravidade: number;
    tempo: number;
    vulnerabilidade: number;
    impacto: number;
    calculada: number;
    efetiva: number;
    diasEspera: number;
    revisaoUrgente: boolean;
    justificativa: string;
  };
}
export interface Equity {
  antigasNaoResolvidas: number;
  maiorVulnerabilidade: number;
  porPrioridade: { prioridade: string; quantidade: number }[];
  regioes: { bairro: string; quantidade: number; tempoMedioDias: number | null }[];
}

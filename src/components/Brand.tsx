export function Brand({ compact = false }: { compact?: boolean }) {
  return (
    <div className={`conecta-brand ${compact ? 'compact' : ''}`}>
      <svg viewBox="0 0 48 48" fill="none" aria-hidden="true">
        <path d="M8 31V17l16-9 16 9v14L24 40 8 31Z" stroke="currentColor" strokeWidth="2" />
        <path
          d="M14 25c5-7 15 7 20 0M14 31c5-7 15 7 20 0M24 14v7"
          stroke="currentColor"
          strokeWidth="2.5"
          strokeLinecap="round"
        />
        <circle cx="24" cy="13" r="2" fill="currentColor" />
      </svg>
      <span>
        Santos <strong>Conecta</strong>
        <small>CIDADE PRÓXIMA. GESTÃO PRESENTE.</small>
      </span>
    </div>
  );
}

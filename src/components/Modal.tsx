import { useEffect, useRef, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { X } from 'lucide-react';

export function Modal({ title, busy = false, onClose, children, compact = false }: {
  title: string; busy?: boolean; onClose: () => void; children: ReactNode; compact?: boolean;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const close = useRef(onClose);
  close.current = onClose;
  useEffect(() => {
    const previous = document.activeElement as HTMLElement | null;
    const dialog = ref.current!;
    const overflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    dialog.showModal();
    return () => {
      dialog.close();
      document.body.style.overflow = overflow;
      previous?.focus();
    };
  }, []);
  return createPortal(
    <dialog ref={ref} className={'sc-modal' + (compact ? ' compact' : '')}
      aria-labelledby={compact ? 'confirmation-title' : 'modal-title'} aria-busy={busy}
      onCancel={(event) => { event.preventDefault(); if (!busy) close.current(); }}>
      <header className="sc-modal-header">
        <div><small>SANTOS CONECTA · EQUIPE</small><h2 id={compact ? 'confirmation-title' : 'modal-title'}>{title}</h2></div>
        <button type="button" className="icon-button" aria-label="Fechar modal" disabled={busy} onClick={onClose}><X size={20} /></button>
      </header>
      <div className="sc-modal-content">{children}</div>
    </dialog>, document.body,
  );
}

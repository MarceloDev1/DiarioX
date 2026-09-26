import { useEffect, useId, useRef, type ReactNode } from 'react';
import './ConfirmDialog.css';

type ConfirmDialogVariant = 'danger' | 'warning' | 'success' | 'default';

const confirmButtonClass: Record<ConfirmDialogVariant, string> = {
    danger: 'btn-danger-solid',
    warning: 'btn-warning-solid',
    success: 'btn-success-solid',
    default: 'btn-primary',
};

const svgProps = {
    viewBox: '0 0 24 24',
    width: 24,
    height: 24,
    fill: 'none',
    stroke: 'currentColor',
    strokeWidth: 2,
    strokeLinecap: 'round' as const,
    strokeLinejoin: 'round' as const,
};

const variantIcon: Record<ConfirmDialogVariant, ReactNode> = {
    danger: (
        <svg {...svgProps}>
            <path d="M3 6h18" />
            <path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
            <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
            <path d="M10 11v6M14 11v6" />
        </svg>
    ),
    warning: (
        <svg {...svgProps}>
            <path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z" />
            <path d="M12 9v4M12 17h.01" />
        </svg>
    ),
    success: (
        <svg {...svgProps}>
            <circle cx="12" cy="12" r="10" />
            <path d="m8 12 3 3 5-6" />
        </svg>
    ),
    default: (
        <svg {...svgProps}>
            <circle cx="12" cy="12" r="10" />
            <path d="M12 8v4M12 16h.01" />
        </svg>
    ),
};

interface ConfirmDialogProps {
    open: boolean;
    title: string;
    children: ReactNode;
    confirmLabel?: string;
    cancelLabel?: string;
    variant?: ConfirmDialogVariant;
    isLoading?: boolean;
    onConfirm: () => void;
    onCancel: () => void;
}

function ConfirmDialog({
    open,
    title,
    children,
    confirmLabel = 'Confirmar',
    cancelLabel = 'Cancelar',
    variant = 'default',
    isLoading = false,
    onConfirm,
    onCancel,
}: ConfirmDialogProps) {
    const titleId = useId();
    const descriptionId = useId();
    const cancelButtonRef = useRef<HTMLButtonElement>(null);

    useEffect(() => {
        if (!open) return;

        // Foco inicial em "Cancelar": Enter acidental não confirma uma ação destrutiva
        cancelButtonRef.current?.focus();

        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key === 'Escape' && !isLoading) onCancel();
        };
        document.addEventListener('keydown', handleKeyDown);
        return () => document.removeEventListener('keydown', handleKeyDown);
    }, [open, isLoading, onCancel]);

    if (!open) return null;

    return (
        <div className="confirm-dialog-backdrop" onClick={() => !isLoading && onCancel()}>
            <div
                className={`confirm-dialog confirm-dialog-${variant}`}
                role="alertdialog"
                aria-modal="true"
                aria-labelledby={titleId}
                aria-describedby={descriptionId}
                onClick={event => event.stopPropagation()}
            >
                <div className="confirm-dialog-icon" aria-hidden="true">
                    {variantIcon[variant]}
                </div>

                <div className="confirm-dialog-body">
                    <h2 id={titleId} className="confirm-dialog-title">{title}</h2>
                    <div id={descriptionId} className="confirm-dialog-message">{children}</div>
                </div>

                <div className="confirm-dialog-actions">
                    <button
                        ref={cancelButtonRef}
                        type="button"
                        className="btn btn-secondary"
                        onClick={onCancel}
                        disabled={isLoading}
                    >
                        {cancelLabel}
                    </button>
                    <button
                        type="button"
                        className={`btn ${confirmButtonClass[variant]}`}
                        onClick={onConfirm}
                        disabled={isLoading}
                    >
                        {isLoading ? 'Aguarde...' : confirmLabel}
                    </button>
                </div>
            </div>
        </div>
    );
}

export default ConfirmDialog;

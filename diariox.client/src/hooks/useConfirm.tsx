import { useCallback, useRef, useState, type ReactNode } from 'react';
import ConfirmDialog from '../components/ui/ConfirmDialog';

export interface ConfirmOptions {
    title: string;
    message: ReactNode;
    confirmLabel?: string;
    variant?: 'danger' | 'warning' | 'success' | 'default';
}

/**
 * Substituto de window.confirm com o ConfirmDialog do sistema.
 * Uso: `if (!(await confirm({ ... }))) return;` e renderizar `confirmDialog` na página.
 */
export function useConfirm() {
    const [options, setOptions] = useState<ConfirmOptions | null>(null);
    const resolveRef = useRef<((confirmed: boolean) => void) | null>(null);

    const confirm = useCallback((next: ConfirmOptions) => {
        resolveRef.current?.(false);
        setOptions(next);
        return new Promise<boolean>(resolve => {
            resolveRef.current = resolve;
        });
    }, []);

    const close = useCallback((confirmed: boolean) => {
        resolveRef.current?.(confirmed);
        resolveRef.current = null;
        setOptions(null);
    }, []);

    const handleConfirm = useCallback(() => close(true), [close]);
    const handleCancel = useCallback(() => close(false), [close]);

    const confirmDialog = (
        <ConfirmDialog
            open={options !== null}
            title={options?.title ?? ''}
            variant={options?.variant}
            confirmLabel={options?.confirmLabel}
            onConfirm={handleConfirm}
            onCancel={handleCancel}
        >
            {options?.message}
        </ConfirmDialog>
    );

    return { confirm, confirmDialog };
}

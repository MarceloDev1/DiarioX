type Status = 'ATIVO' | 'INATIVO' | 'BLOQUEADO' | 'ATIVO_AGUARDANDO_ENTURMACAO';

const statusClass: Record<Status, string> = {
    ATIVO: 'status-active',
    INATIVO: 'status-inactive',
    BLOQUEADO: 'status-blocked',
    ATIVO_AGUARDANDO_ENTURMACAO: 'status-active',
};

interface StatusPillProps {
    status: Status;
    label?: string;
}

function StatusPill({ status, label }: StatusPillProps) {
    return <span className={`status-pill ${statusClass[status]}`}>{label ?? status}</span>;
}

export default StatusPill;

type Status = 'ATIVO' | 'INATIVO' | 'BLOQUEADO';

const statusClass: Record<Status, string> = {
    ATIVO: 'status-active',
    INATIVO: 'status-inactive',
    BLOQUEADO: 'status-blocked',
};

interface StatusPillProps {
    status: Status;
}

function StatusPill({ status }: StatusPillProps) {
    return <span className={`status-pill ${statusClass[status]}`}>{status}</span>;
}

export default StatusPill;

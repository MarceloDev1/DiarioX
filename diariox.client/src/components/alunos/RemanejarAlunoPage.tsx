import { useEffect, useState } from 'react';
import { readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import '../MainContent.css';

interface Aluno {
    id: number;
    matricula: string;
    nome: string;
}

interface Turma {
    id: number;
    escolaId: number;
    anoLetivoId: number;
    nomeCompleto: string;
    turno: string;
    status: string;
}

interface EnturmacaoAtiva {
    alunoId: number;
    escolaId: number;
    escolaNome: string;
    turmaId: number;
    turmaNome: string;
    anoLetivoId: number;
    anoReferencia: number;
    turno: string;
    dataInicio: string;
}

const turnos: Record<string, string> = { MANHA: 'Manhã', TARDE: 'Tarde', NOITE: 'Noite', INTEGRAL: 'Integral' };

const authHeaders = (): Record<string, string> => {
    const token = sessionStorage.getItem('diariox_token');
    return token ? { Authorization: `Bearer ${token}` } : {};
};

function RemanejarAlunoPage() {
    const [alunos, setAlunos] = useState<Aluno[]>([]);
    const [turmas, setTurmas] = useState<Turma[]>([]);
    const [alunoId, setAlunoId] = useState('');
    const [enturmacao, setEnturmacao] = useState<EnturmacaoAtiva | null>(null);
    const [turmaDestinoId, setTurmaDestinoId] = useState('');
    const [dataMovimentacao, setDataMovimentacao] = useState(new Date().toISOString().slice(0, 10));
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    useEffect(() => {
        void Promise.all([
            fetch('/api/alunos', { headers: authHeaders() }),
            fetch('/api/turmas', { headers: authHeaders() }),
        ]).then(async ([alunosResponse, turmasResponse]) => {
            if (!alunosResponse.ok) throw new Error(await readApiError(alunosResponse));
            if (!turmasResponse.ok) throw new Error(await readApiError(turmasResponse));
            setAlunos(await alunosResponse.json() as Aluno[]);
            setTurmas(await turmasResponse.json() as Turma[]);
        }).catch(reason => setError(reason instanceof Error ? reason.message : 'Falha ao carregar dados para remanejamento.'))
            .finally(() => setLoading(false));
    }, []);

    const selectAluno = async (id: string) => {
        setAlunoId(id);
        setEnturmacao(null);
        setTurmaDestinoId('');
        setSuccess(null);
        setError(null);
        if (!id) return;

        try {
            const response = await fetch(`/api/alunos/${id}/enturmacao-ativa`, { headers: authHeaders() });
            if (!response.ok) throw new Error(await readApiError(response));
            setEnturmacao(await response.json() as EnturmacaoAtiva);
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao carregar a enturmação atual.');
        }
    };

    const confirmRemanejamento = async () => {
        if (!alunoId || !turmaDestinoId || !dataMovimentacao) {
            setError('Informe a data da movimentação e a nova turma.');
            return;
        }

        setSaving(true);
        setError(null);
        setSuccess(null);
        try {
            const response = await fetch(`/api/alunos/${alunoId}/remanejamentos`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', ...authHeaders() },
                body: JSON.stringify({ turmaDestinoId: Number(turmaDestinoId), dataMovimentacao }),
            });
            if (!response.ok) throw new Error(await readApiError(response));
            const result = await response.json() as { message: string };
            setSuccess(result.message);
            await selectAluno(alunoId);
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao remanejar aluno.');
        } finally {
            setSaving(false);
        }
    };

    const turmasDestino = enturmacao ? turmas.filter(turma =>
        turma.status === 'ATIVO' && turma.escolaId === enturmacao.escolaId && turma.anoLetivoId === enturmacao.anoLetivoId) : [];

    return (
        <div className="school-page">
            <section className="content-card">
                <div className="section-header"><div><h2>Remanejar Aluno</h2></div></div>
                <FeedbackMessage message={error} type="error" />
                <FeedbackMessage message={success} type="success" />
                <div className="cadastro-form escola-form">
                    <div className="form-field">
                        <label htmlFor="aluno-remanejamento">Aluno</label>
                        <select id="aluno-remanejamento" value={alunoId} disabled={loading} onChange={event => void selectAluno(event.target.value)}>
                            <option value="">Selecione por matrícula ou nome</option>
                            {alunos.map(aluno => <option key={aluno.id} value={aluno.id}>{aluno.matricula} - {aluno.nome}</option>)}
                        </select>
                    </div>
                </div>
            </section>

            {enturmacao && <>
                <section className="content-card">
                    <div className="section-header"><div><h2>Enturmação Atual</h2></div></div>
                    <div className="table-responsive"><table className="data-table"><thead><tr><th>Escola</th><th>Turma</th><th>Turno</th><th>Ano Letivo</th></tr></thead>
                        <tbody><tr><td>{enturmacao.escolaNome}</td><td>{enturmacao.turmaNome}</td><td>{turnos[enturmacao.turno] ?? enturmacao.turno}</td><td>{enturmacao.anoReferencia}</td></tr></tbody></table></div>
                </section>
                <section className="content-card">
                    <div className="section-header"><div><h2>Dados do Remanejamento</h2></div></div>
                    <div className="cadastro-form escola-form form-grid">
                        <div className="form-field"><label htmlFor="data-movimentacao">Data da Movimentação</label><input id="data-movimentacao" type="date" value={dataMovimentacao} onChange={event => setDataMovimentacao(event.target.value)} /></div>
                        <div className="form-field"><label htmlFor="turma-destino">Nova Turma</label><select id="turma-destino" value={turmaDestinoId} onChange={event => setTurmaDestinoId(event.target.value)}><option value="">Selecione a nova turma</option>{turmasDestino.map(turma => <option key={turma.id} value={turma.id}>{turma.nomeCompleto} - {turnos[turma.turno] ?? turma.turno}</option>)}</select></div>
                    </div>
                    <div className="form-actions"><button className="primary-button" type="button" disabled={saving} onClick={() => void confirmRemanejamento()}>Confirmar Remanejamento</button><button className="secondary-button cancel-button" type="button" disabled={saving} onClick={() => void selectAluno('')}>Cancelar</button></div>
                </section>
            </>}
            {!loading && alunos.length === 0 && <EmptyState emptyMessage="Nenhum aluno disponível para remanejamento." />}
        </div>
    );
}

export default RemanejarAlunoPage;
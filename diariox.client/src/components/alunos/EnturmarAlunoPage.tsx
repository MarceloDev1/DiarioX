import { useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';

interface EnturmarAlunoPageProps {
    initialAlunoId: number | null;
}

interface Aluno {
    id: number;
    matricula: string;
    nome: string;
    escolaId: number;
    escolaNome: string;
    status: string;
}

interface Turma {
    id: number;
    escolaId: number;
    escolaNome: string;
    anoReferencia: number;
    nomeCompleto: string;
    turno: string;
    status: string;
    vagasOfertadas: number;
}

const turnos: Record<string, string> = {
    MANHA: 'Manhã',
    TARDE: 'Tarde',
    NOITE: 'Noite',
    INTEGRAL: 'Integral',
};

function EnturmarAlunoPage({ initialAlunoId }: EnturmarAlunoPageProps) {
    const [alunos, setAlunos] = useState<Aluno[]>([]);
    const [turmas, setTurmas] = useState<Turma[]>([]);
    const [alunoId, setAlunoId] = useState(initialAlunoId ? String(initialAlunoId) : '');
    const [turmaId, setTurmaId] = useState('');
    const [dataInicio, setDataInicio] = useState(new Date().toISOString().slice(0, 10));
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    useEffect(() => {
        void Promise.all([
            apiFetch('/api/alunos'),
            apiFetch('/api/turmas'),
        ])
            .then(async ([alunosResponse, turmasResponse]) => {
                if (!alunosResponse.ok) throw new Error(await readApiError(alunosResponse));
                if (!turmasResponse.ok) throw new Error(await readApiError(turmasResponse));

                const alunosData = (await alunosResponse.json()) as Aluno[];
                setAlunos(alunosData);
                setTurmas((await turmasResponse.json()) as Turma[]);
            })
            .catch(reason => setError(reason instanceof Error ? reason.message : 'Falha ao carregar dados para enturmação.'))
            .finally(() => setLoading(false));
    }, []);

    const alunoSelecionado = alunos.find(aluno => String(aluno.id) === alunoId);
    const turmasDisponiveis = alunoSelecionado
        ? turmas.filter(turma => turma.status === 'ATIVO' && turma.escolaId === alunoSelecionado.escolaId)
        : [];

    const handleAlunoChange = (value: string) => {
        setAlunoId(value);
        setTurmaId('');
        setSuccess(null);
        setError(null);
    };

    const handleSubmit = async () => {
        if (!alunoId || !turmaId || !dataInicio) {
            setError('Informe o aluno, a turma e a data de início.');
            return;
        }

        setSaving(true);
        setError(null);
        setSuccess(null);

        try {
            const response = await apiFetch(`/api/alunos/${alunoId}/enturmacoes`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ turmaId: Number(turmaId), dataInicio }),
            });

            if (!response.ok) throw new Error(await readApiError(response));

            const result = (await response.json()) as { message: string };
            setSuccess(result.message);
            setAlunos(current => current.map(aluno =>
                aluno.id === Number(alunoId) ? { ...aluno, status: 'ATIVO' } : aluno,
            ));
            setTurmaId('');
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao realizar a enturmação.');
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="school-page">
            <section className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Enturmar Aluno</h2>
                        <p>Vincule o aluno a uma turma ativa da escola selecionada.</p>
                    </div>
                </div>

                <FeedbackMessage message={error} type="error" />
                <FeedbackMessage message={success} type="success" />

                <div className="cadastro-form escola-form form-grid">
                    <div className="form-field">
                        <label htmlFor="enturmacao-aluno">Aluno</label>
                        <select
                            id="enturmacao-aluno"
                            value={alunoId}
                            disabled={loading || saving}
                            onChange={event => handleAlunoChange(event.target.value)}
                        >
                            <option value="">Selecione por matrícula ou nome</option>
                            {alunos.map(aluno => (
                                <option key={aluno.id} value={aluno.id}>
                                    {aluno.matricula} - {aluno.nome}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="form-field">
                        <label htmlFor="enturmacao-data">Data de Início</label>
                        <input
                            id="enturmacao-data"
                            type="date"
                            value={dataInicio}
                            disabled={saving}
                            onChange={event => setDataInicio(event.target.value)}
                        />
                    </div>

                    <div className="form-field form-field-full">
                        <label htmlFor="enturmacao-turma">Turma</label>
                        <select
                            id="enturmacao-turma"
                            value={turmaId}
                            disabled={!alunoSelecionado || saving}
                            onChange={event => setTurmaId(event.target.value)}
                        >
                            <option value="">Selecione uma turma ativa</option>
                            {turmasDisponiveis.map(turma => (
                                <option key={turma.id} value={turma.id}>
                                    {turma.nomeCompleto} - {turnos[turma.turno] ?? turma.turno} ({turma.anoReferencia})
                                </option>
                            ))}
                        </select>
                    </div>
                </div>

                <div className="form-actions">
                    <button className="primary-button" type="button" disabled={saving || loading} onClick={() => void handleSubmit()}>
                        {saving ? 'Vinculando...' : 'Vincular à Turma'}
                    </button>
                    <button
                        className="secondary-button cancel-button"
                        type="button"
                        disabled={saving}
                        onClick={() => {
                            setAlunoId('');
                            setTurmaId('');
                            setSuccess(null);
                            setError(null);
                        }}
                    >
                        Limpar
                    </button>
                </div>
            </section>

            {!loading && alunos.length === 0 && (
                <EmptyState emptyMessage="Nenhum aluno cadastrado para enturmação." />
            )}
            {!loading && alunoSelecionado && turmasDisponiveis.length === 0 && (
                <EmptyState emptyMessage="Nenhuma turma ativa disponível para a escola deste aluno." />
            )}
        </div>
    );
}

export default EnturmarAlunoPage;

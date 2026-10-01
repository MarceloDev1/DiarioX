import { useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import { hojeIso } from '../../utils/formatters';
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

interface VagasTurma {
    turmaId: number;
    data: string;
    vagasOfertadas: number;
    ocupadas: number;
    disponiveis: number;
}

interface EnturmacaoFalha {
    alunoId: number;
    motivo: string;
}

const STATUS_AGUARDANDO = 'ATIVO_AGUARDANDO_ENTURMACAO';

const normalizar = (texto: string) => texto.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase();

const plural = (quantidade: number, singular: string, pluralForma: string) =>
    `${quantidade} ${quantidade === 1 ? singular : pluralForma}`;

function EnturmarAlunoPage({ initialAlunoId }: EnturmarAlunoPageProps) {
    const [alunos, setAlunos] = useState<Aluno[]>([]);
    const [turmas, setTurmas] = useState<Turma[]>([]);
    const [escolaId, setEscolaId] = useState('');
    const [turmaId, setTurmaId] = useState('');
    const [dataInicio, setDataInicio] = useState(hojeIso);
    const [busca, setBusca] = useState('');
    const [selecionados, setSelecionados] = useState<Set<number>>(() => new Set(initialAlunoId ? [initialAlunoId] : []));
    const [falhas, setFalhas] = useState<Record<number, string>>({});
    const [vagas, setVagas] = useState<VagasTurma | null>(null);
    const [vagasVersao, setVagasVersao] = useState(0);
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
                const turmasData = (await turmasResponse.json()) as Turma[];
                setAlunos(alunosData);
                setTurmas(turmasData);

                // Vindo do atalho da tela de Alunos, a escola é a do aluno; com uma escola só, não há o que escolher.
                const alunoInicial = alunosData.find(aluno => aluno.id === initialAlunoId);
                const escolasAtivas = new Set(turmasData.filter(turma => turma.status === 'ATIVO').map(turma => turma.escolaId));
                if (alunoInicial) setEscolaId(String(alunoInicial.escolaId));
                else if (escolasAtivas.size === 1) setEscolaId(String([...escolasAtivas][0]));
            })
            .catch(reason => setError(reason instanceof Error ? reason.message : 'Falha ao carregar dados para enturmação.'))
            .finally(() => setLoading(false));
    }, [initialAlunoId]);

    useEffect(() => {
        if (!turmaId || !dataInicio) return;

        let cancelado = false;
        void apiFetch(`/api/turmas/${turmaId}/vagas?data=${dataInicio}`)
            .then(async response => {
                if (!response.ok) throw new Error(await readApiError(response));
                const data = (await response.json()) as VagasTurma;
                if (!cancelado) setVagas(data);
            })
            .catch(reason => {
                if (!cancelado) setError(reason instanceof Error ? reason.message : 'Falha ao consultar as vagas da turma.');
            });

        return () => { cancelado = true; };
    }, [turmaId, dataInicio, vagasVersao]);

    const escolas = [...new Map(turmas
        .filter(turma => turma.status === 'ATIVO')
        .map(turma => [turma.escolaId, turma.escolaNome])).entries()]
        .sort(([, a], [, b]) => a.localeCompare(b));

    const turmasDaEscola = turmas.filter(turma => turma.status === 'ATIVO' && String(turma.escolaId) === escolaId);

    const alunosAguardando = alunos.filter(aluno => aluno.status === STATUS_AGUARDANDO && String(aluno.escolaId) === escolaId);
    const termo = normalizar(busca.trim());
    const alunosVisiveis = termo
        ? alunosAguardando.filter(aluno => normalizar(aluno.nome).includes(termo) || aluno.matricula.includes(termo))
        : alunosAguardando;

    // Só vale a consulta da turma e data selecionadas; uma resposta antiga fica de fora até a nova chegar.
    const vagasAtuais = vagas && String(vagas.turmaId) === turmaId && vagas.data === dataInicio ? vagas : null;

    const idsSelecionados = alunosAguardando.filter(aluno => selecionados.has(aluno.id)).map(aluno => aluno.id);
    const quantidadeSelecionada = idsSelecionados.length;
    const excedeVagas = vagasAtuais !== null && quantidadeSelecionada > vagasAtuais.disponiveis;

    const visiveisSelecionados = alunosVisiveis.filter(aluno => selecionados.has(aluno.id)).length;
    const todosVisiveisSelecionados = alunosVisiveis.length > 0 && visiveisSelecionados === alunosVisiveis.length;

    const limparMensagens = () => {
        setSuccess(null);
        setError(null);
        setFalhas({});
    };

    const handleEscolaChange = (value: string) => {
        setEscolaId(value);
        setTurmaId('');
        setSelecionados(new Set());
        limparMensagens();
    };

    const toggleAluno = (id: number) => {
        setSelecionados(current => {
            const next = new Set(current);
            if (next.has(id)) next.delete(id);
            else next.add(id);
            return next;
        });
    };

    const toggleTodosVisiveis = () => {
        setSelecionados(current => {
            const next = new Set(current);
            alunosVisiveis.forEach(aluno => {
                if (todosVisiveisSelecionados) next.delete(aluno.id);
                else next.add(aluno.id);
            });
            return next;
        });
    };

    const handleSubmit = async () => {
        if (!turmaId || !dataInicio) {
            setError('Informe a turma e a data de início.');
            return;
        }
        if (quantidadeSelecionada === 0) {
            setError('Selecione ao menos um aluno para enturmar.');
            return;
        }

        setSaving(true);
        limparMensagens();

        try {
            const response = await apiFetch('/api/alunos/enturmacoes', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ turmaId: Number(turmaId), dataInicio, alunoIds: idsSelecionados }),
            });

            if (!response.ok) {
                const payload = (await response.clone().json().catch(() => null)) as { falhas?: EnturmacaoFalha[] } | null;
                setFalhas(Object.fromEntries((payload?.falhas ?? []).map(falha => [falha.alunoId, falha.motivo])));
                throw new Error(await readApiError(response));
            }

            const result = (await response.json()) as { message: string };
            const enturmados = new Set(idsSelecionados);
            setSuccess(result.message);
            setAlunos(current => current.map(aluno => (enturmados.has(aluno.id) ? { ...aluno, status: 'ATIVO' } : aluno)));
            setSelecionados(new Set());
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao realizar a enturmação.');
        } finally {
            setSaving(false);
            setVagasVersao(versao => versao + 1);
        }
    };

    return (
        <div className="school-page">
            <section className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Enturmar Alunos</h2>
                        <p>Escolha a turma e marque os alunos que aguardam enturmação. Todos são vinculados de uma só vez.</p>
                    </div>
                </div>

                <FeedbackMessage message={error} type="error" />
                <FeedbackMessage message={success} type="success" />

                <div className="cadastro-form escola-form form-grid">
                    <div className="form-field">
                        <label htmlFor="enturmacao-escola">Escola</label>
                        <select
                            id="enturmacao-escola"
                            value={escolaId}
                            disabled={loading || saving}
                            onChange={event => handleEscolaChange(event.target.value)}
                        >
                            <option value="">Selecione a escola</option>
                            {escolas.map(([id, nome]) => (
                                <option key={id} value={id}>{nome}</option>
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
                            disabled={!escolaId || saving}
                            onChange={event => {
                                setTurmaId(event.target.value);
                                limparMensagens();
                            }}
                        >
                            <option value="">Selecione uma turma ativa</option>
                            {turmasDaEscola.map(turma => (
                                <option key={turma.id} value={turma.id}>
                                    {turma.nomeCompleto} ({turma.anoReferencia})
                                </option>
                            ))}
                        </select>
                        {vagasAtuais && (
                            <span className="field-hint">
                                {vagasAtuais.ocupadas} de {vagasAtuais.vagasOfertadas} vagas ocupadas ·{' '}
                                <strong>{plural(vagasAtuais.disponiveis, 'vaga disponível', 'vagas disponíveis')}</strong>
                            </span>
                        )}
                    </div>
                </div>
            </section>

            {escolaId && !loading && (
                <section className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>Alunos Aguardando Enturmação</h2>
                            <p>{plural(alunosAguardando.length, 'aluno aguardando', 'alunos aguardando')} nesta escola.</p>
                        </div>
                    </div>

                    {alunosAguardando.length === 0 ? (
                        <EmptyState emptyMessage="Nenhum aluno desta escola aguarda enturmação." />
                    ) : (
                        <>
                            <div className="filter-bar">
                                <div className="filter-field">
                                    <label htmlFor="enturmacao-busca">Buscar por nome ou matrícula</label>
                                    <input
                                        id="enturmacao-busca"
                                        type="text"
                                        className="filter-input"
                                        value={busca}
                                        onChange={event => setBusca(event.target.value)}
                                    />
                                </div>
                            </div>

                            <div className="table-container">
                                <table className="data-table">
                                    <thead>
                                        <tr>
                                            <th>
                                                <label className="checkbox-label">
                                                    <input
                                                        type="checkbox"
                                                        aria-label="Selecionar todos os alunos listados"
                                                        checked={todosVisiveisSelecionados}
                                                        ref={input => {
                                                            if (input) input.indeterminate = visiveisSelecionados > 0 && !todosVisiveisSelecionados;
                                                        }}
                                                        disabled={saving || alunosVisiveis.length === 0}
                                                        onChange={toggleTodosVisiveis}
                                                    />
                                                </label>
                                            </th>
                                            <th>Matrícula</th>
                                            <th>Nome</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {alunosVisiveis.length === 0 ? (
                                            <tr>
                                                <td colSpan={3}>Nenhum aluno encontrado para "{busca}".</td>
                                            </tr>
                                        ) : alunosVisiveis.map(aluno => (
                                            <tr key={aluno.id}>
                                                <td>
                                                    <label className="checkbox-label">
                                                        <input
                                                            type="checkbox"
                                                            aria-label={`Selecionar ${aluno.nome}`}
                                                            checked={selecionados.has(aluno.id)}
                                                            disabled={saving}
                                                            onChange={() => toggleAluno(aluno.id)}
                                                        />
                                                    </label>
                                                </td>
                                                <td>{aluno.matricula}</td>
                                                <td>
                                                    {aluno.nome}
                                                    {falhas[aluno.id] && <div className="field-error">{falhas[aluno.id]}</div>}
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        </>
                    )}

                    <p className={excedeVagas ? 'field-error' : 'field-hint'}>
                        {plural(quantidadeSelecionada, 'aluno selecionado', 'alunos selecionados')}
                        {vagasAtuais && ` · ${plural(vagasAtuais.disponiveis, 'vaga disponível', 'vagas disponíveis')} na turma`}
                        {excedeVagas && '. Desmarque alunos ou escolha outra turma.'}
                    </p>

                    <div className="form-actions">
                        <button
                            className="primary-button"
                            type="button"
                            disabled={saving || !turmaId || quantidadeSelecionada === 0 || excedeVagas}
                            onClick={() => void handleSubmit()}
                        >
                            {saving
                                ? 'Enturmando...'
                                : quantidadeSelecionada === 0
                                    ? 'Enturmar alunos'
                                    : `Enturmar ${plural(quantidadeSelecionada, 'aluno', 'alunos')}`}
                        </button>
                        <button
                            className="secondary-button cancel-button"
                            type="button"
                            disabled={saving || quantidadeSelecionada === 0}
                            onClick={() => {
                                setSelecionados(new Set());
                                limparMensagens();
                            }}
                        >
                            Limpar seleção
                        </button>
                    </div>
                </section>
            )}

            {!loading && escolas.length === 0 && (
                <EmptyState emptyMessage="Nenhuma turma ativa cadastrada para enturmação." />
            )}
        </div>
    );
}

export default EnturmarAlunoPage;

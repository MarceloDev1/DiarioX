import { useCallback, useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import ConfirmDialog from '../ui/ConfirmDialog';
import StatusPill, { type Status } from '../ui/StatusPill';
import { rotuloStatusAluno } from './statusAluno';
import './DesenturmarAlunoPage.css';

interface Turma {
    id: number;
    escolaId: number;
    escolaNome: string;
    anoReferencia: number;
    modalidadeEnsinoId: number;
    modalidadeEnsinoNome: string;
    etapaEnsinoId: number;
    etapaEnsinoNome: string;
    nomeCompleto: string;
    turno: string;
    turnoDescricao: string;
    status: string;
}

interface AlunoEnturmado {
    alunoId: number;
    matricula: string;
    nome: string;
    status: string;
    dataInicio: string;
}

interface VagasTurma {
    turmaId: number;
    vagasOfertadas: number;
    ocupadas: number;
    disponiveis: number;
}

interface DesenturmacaoFalha {
    alunoId: number;
    motivo: string;
}

const MOTIVO_OUTROS = 'OUTROS';
const MAX_OBSERVACAO = 500;

const motivos = [
    { value: 'REESTRUTURACAO_INTERNA', label: 'Reestruturação Interna' },
    { value: 'NAO_COMPARECEU', label: 'Nunca Compareceu / Não Frequentou' },
    { value: 'FALECIMENTO', label: 'Falecimento' },
    { value: 'ERRO_MATRICULA_ENTURMACAO', label: 'Erro de Matrícula/Enturmação' },
    { value: MOTIVO_OUTROS, label: 'Outros' },
];

// RN01: mostra o efeito do motivo antes da confirmação.
const efeitoDoMotivo: Record<string, string> = {
    REESTRUTURACAO_INTERNA: 'O aluno volta para "Aguardando Enturmação" e fica disponível para outra turma.',
    ERRO_MATRICULA_ENTURMACAO: 'O aluno volta para "Aguardando Enturmação" e fica disponível para outra turma.',
    OUTROS: 'O aluno volta para "Aguardando Enturmação" e fica disponível para outra turma.',
    NAO_COMPARECEU: 'O aluno passa para "Não Compareceu" e deixa de contar faltas nesta turma.',
    FALECIMENTO: 'O aluno passa para "Inativo - Óbito" e a matrícula é encerrada definitivamente.',
};

const plural = (quantidade: number, singular: string, pluralForma: string) =>
    `${quantidade} ${quantidade === 1 ? singular : pluralForma}`;

const formatarData = (iso: string) => iso.split('-').reverse().join('/');

/** Pares [id, nome] únicos, em ordem alfabética. */
function opcoesUnicas(turmas: Turma[], id: (turma: Turma) => number | string, nome: (turma: Turma) => string) {
    return [...new Map(turmas.map(turma => [String(id(turma)), nome(turma)])).entries()]
        .sort(([, a], [, b]) => a.localeCompare(b));
}

function DesenturmarAlunoPage() {
    const [turmas, setTurmas] = useState<Turma[]>([]);
    const [escolaId, setEscolaId] = useState('');
    const [modalidadeId, setModalidadeId] = useState('');
    const [etapaId, setEtapaId] = useState('');
    const [turno, setTurno] = useState('');
    const [turmaId, setTurmaId] = useState('');
    const [alunos, setAlunos] = useState<AlunoEnturmado[]>([]);
    const [vagas, setVagas] = useState<VagasTurma | null>(null);
    const [versao, setVersao] = useState(0);
    const [selecionados, setSelecionados] = useState<Set<number>>(new Set());
    const [falhas, setFalhas] = useState<Record<number, string>>({});
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    const [modalAberto, setModalAberto] = useState(false);
    const [motivo, setMotivo] = useState('');
    const [observacao, setObservacao] = useState('');
    const [erroModal, setErroModal] = useState<string | null>(null);
    const [saving, setSaving] = useState(false);

    useEffect(() => {
        void apiFetch('/api/turmas')
            .then(async response => {
                if (!response.ok) throw new Error(await readApiError(response));
                const data = (await response.json()) as Turma[];
                setTurmas(data);

                // Com uma escola só, não há o que escolher.
                const escolas = new Set(data.map(turma => turma.escolaId));
                if (escolas.size === 1) setEscolaId(String([...escolas][0]));
            })
            .catch(reason => setError(reason instanceof Error ? reason.message : 'Falha ao carregar as turmas.'))
            .finally(() => setLoading(false));
    }, []);

    useEffect(() => {
        if (!turmaId) return;

        let cancelado = false;
        void Promise.all([
            apiFetch(`/api/turmas/${turmaId}/alunos`),
            apiFetch(`/api/turmas/${turmaId}/vagas`),
        ])
            .then(async ([alunosResponse, vagasResponse]) => {
                if (!alunosResponse.ok) throw new Error(await readApiError(alunosResponse));
                if (!vagasResponse.ok) throw new Error(await readApiError(vagasResponse));
                const alunosData = (await alunosResponse.json()) as AlunoEnturmado[];
                const vagasData = (await vagasResponse.json()) as VagasTurma;
                if (cancelado) return;
                setAlunos(alunosData);
                setVagas(vagasData);
            })
            .catch(reason => {
                if (!cancelado) setError(reason instanceof Error ? reason.message : 'Falha ao carregar os alunos da turma.');
            });

        return () => { cancelado = true; };
    }, [turmaId, versao]);

    // Cada filtro só oferece o que existe dentro dos anteriores.
    const daEscola = turmas.filter(turma => String(turma.escolaId) === escolaId);
    const daModalidade = daEscola.filter(turma => !modalidadeId || String(turma.modalidadeEnsinoId) === modalidadeId);
    const daEtapa = daModalidade.filter(turma => !etapaId || String(turma.etapaEnsinoId) === etapaId);
    const doTurno = daEtapa
        .filter(turma => !turno || turma.turno === turno)
        .sort((a, b) => b.anoReferencia - a.anoReferencia || a.nomeCompleto.localeCompare(b.nomeCompleto));

    const escolas = opcoesUnicas(turmas, turma => turma.escolaId, turma => turma.escolaNome);
    const modalidades = opcoesUnicas(daEscola, turma => turma.modalidadeEnsinoId, turma => turma.modalidadeEnsinoNome);
    const etapas = opcoesUnicas(daModalidade, turma => turma.etapaEnsinoId, turma => turma.etapaEnsinoNome);
    const turnos = opcoesUnicas(daEtapa, turma => turma.turno, turma => turma.turnoDescricao);

    const turmaSelecionada = turmas.find(turma => String(turma.id) === turmaId) ?? null;
    // Dados de outra turma ficam de fora até a consulta da turma atual chegar.
    const alunosDaTurma = vagas && String(vagas.turmaId) === turmaId ? alunos : [];
    const vagasAtuais = vagas && String(vagas.turmaId) === turmaId ? vagas : null;

    const idsSelecionados = alunosDaTurma.filter(aluno => selecionados.has(aluno.alunoId)).map(aluno => aluno.alunoId);
    const quantidadeSelecionada = idsSelecionados.length;
    const todosSelecionados = alunosDaTurma.length > 0 && quantidadeSelecionada === alunosDaTurma.length;
    // Alternativa 01: com todos marcados, a desenturmação é da turma inteira.
    const emLote = todosSelecionados && alunosDaTurma.length > 1;

    const limparMensagens = () => {
        setSuccess(null);
        setError(null);
        setFalhas({});
    };

    const trocarTurma = (id: string) => {
        setTurmaId(id);
        setSelecionados(new Set());
        limparMensagens();
    };

    // Ao mudar um filtro, a turma escolhida só continua se ainda estiver na lista.
    const aplicarFiltro = (setter: (value: string) => void, value: string, manterTurma: (turma: Turma) => boolean) => {
        setter(value);
        if (turmaSelecionada && !manterTurma(turmaSelecionada)) trocarTurma('');
    };

    const toggleAluno = (id: number) => {
        setSelecionados(current => {
            const next = new Set(current);
            if (next.has(id)) next.delete(id);
            else next.add(id);
            return next;
        });
    };

    const toggleTodos = () => {
        setSelecionados(todosSelecionados ? new Set() : new Set(alunosDaTurma.map(aluno => aluno.alunoId)));
    };

    const abrirModal = () => {
        setMotivo('');
        setObservacao('');
        setErroModal(null);
        setModalAberto(true);
    };

    const desenturmarSelecionados = () => {
        limparMensagens();
        if (quantidadeSelecionada === 0) {
            setError('Selecione ao menos um aluno para realizar a desenturmação.');
            return;
        }
        abrirModal();
    };

    const desenturmarTurmaInteira = () => {
        limparMensagens();
        setSelecionados(new Set(alunosDaTurma.map(aluno => aluno.alunoId)));
        abrirModal();
    };

    // Estável: o ConfirmDialog refaz o foco inicial quando onCancel muda.
    const fecharModal = useCallback(() => setModalAberto(false), []);

    const confirmarDesenturmacao = async () => {
        if (!motivo) {
            setErroModal('Por favor, selecione o motivo da desenturmação para continuar.');
            return;
        }
        if (motivo === MOTIVO_OUTROS && !observacao.trim()) {
            setErroModal('Informe a observação/justificativa quando o motivo for "Outros".');
            return;
        }

        setSaving(true);
        setErroModal(null);
        limparMensagens();

        try {
            const response = await apiFetch('/api/alunos/desenturmacoes', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    turmaId: Number(turmaId),
                    alunoIds: idsSelecionados,
                    motivo,
                    observacao: observacao.trim() || null,
                }),
            });

            if (!response.ok) {
                const payload = (await response.clone().json().catch(() => null)) as { falhas?: DesenturmacaoFalha[] } | null;
                setFalhas(Object.fromEntries((payload?.falhas ?? []).map(falha => [falha.alunoId, falha.motivo])));
                throw new Error(await readApiError(response));
            }

            const result = (await response.json()) as { message: string };
            setSuccess(result.message);
            setSelecionados(new Set());
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao realizar a desenturmação.');
        } finally {
            setSaving(false);
            setModalAberto(false);
            setVersao(atual => atual + 1);
        }
    };

    const alunosSelecionados = alunosDaTurma.filter(aluno => selecionados.has(aluno.alunoId));

    return (
        <div className="school-page">
            <section className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Desenturmar Aluno</h2>
                        <p>Escolha a turma e marque os alunos que deixarão de fazer parte dela. As vagas são liberadas na hora.</p>
                    </div>
                </div>

                <FeedbackMessage message={error} type="error" />
                <FeedbackMessage message={success} type="success" />

                <div className="cadastro-form escola-form form-grid">
                    <div className="form-field">
                        <label htmlFor="desenturmacao-escola">Escola</label>
                        <select
                            id="desenturmacao-escola"
                            value={escolaId}
                            disabled={loading || saving}
                            onChange={event => {
                                setEscolaId(event.target.value);
                                setModalidadeId('');
                                setEtapaId('');
                                setTurno('');
                                trocarTurma('');
                            }}
                        >
                            <option value="">Selecione a escola</option>
                            {escolas.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                        </select>
                    </div>

                    <div className="form-field">
                        <label htmlFor="desenturmacao-modalidade">Modalidade</label>
                        <select
                            id="desenturmacao-modalidade"
                            value={modalidadeId}
                            disabled={!escolaId || saving}
                            onChange={event => {
                                const value = event.target.value;
                                setEtapaId('');
                                setTurno('');
                                aplicarFiltro(setModalidadeId, value, turma => !value || String(turma.modalidadeEnsinoId) === value);
                            }}
                        >
                            <option value="">Todas</option>
                            {modalidades.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                        </select>
                    </div>

                    <div className="form-field">
                        <label htmlFor="desenturmacao-etapa">Etapa</label>
                        <select
                            id="desenturmacao-etapa"
                            value={etapaId}
                            disabled={!escolaId || saving}
                            onChange={event => {
                                const value = event.target.value;
                                setTurno('');
                                aplicarFiltro(setEtapaId, value, turma => !value || String(turma.etapaEnsinoId) === value);
                            }}
                        >
                            <option value="">Todas</option>
                            {etapas.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                        </select>
                    </div>

                    <div className="form-field">
                        <label htmlFor="desenturmacao-turno">Turno</label>
                        <select
                            id="desenturmacao-turno"
                            value={turno}
                            disabled={!escolaId || saving}
                            onChange={event => {
                                const value = event.target.value;
                                aplicarFiltro(setTurno, value, turma => !value || turma.turno === value);
                            }}
                        >
                            <option value="">Todos</option>
                            {turnos.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                        </select>
                    </div>

                    <div className="form-field form-field-full">
                        <label htmlFor="desenturmacao-turma">Turma</label>
                        <select
                            id="desenturmacao-turma"
                            value={turmaId}
                            disabled={!escolaId || saving}
                            onChange={event => trocarTurma(event.target.value)}
                        >
                            <option value="">Selecione a turma</option>
                            {doTurno.map(turma => (
                                <option key={turma.id} value={turma.id}>
                                    {turma.nomeCompleto} ({turma.anoReferencia}){turma.status !== 'ATIVO' ? ' · inativa' : ''}
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

            {turmaSelecionada && (
                <section className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>Alunos da Turma</h2>
                            <p>
                                {turmaSelecionada.nomeCompleto}
                                {vagasAtuais && <> · {plural(alunosDaTurma.length, 'aluno enturmado', 'alunos enturmados')}</>}
                            </p>
                        </div>
                    </div>

                    {!vagasAtuais ? (
                        // Ainda sem a resposta desta turma (ou a consulta falhou: o erro aparece no topo).
                        <p className="field-hint">Carregando alunos...</p>
                    ) : alunosDaTurma.length === 0 ? (
                        <EmptyState
                            emptyMessage="Nenhum aluno enturmado nesta turma."
                            emptySubMessage="Só é possível desenturmar de uma turma com pelo menos um aluno matriculado."
                        />
                    ) : (
                        <>
                            <div className="table-container">
                                <table className="data-table">
                                    <thead>
                                        <tr>
                                            <th>
                                                <label className="checkbox-label">
                                                    <input
                                                        type="checkbox"
                                                        aria-label="Selecionar todos os alunos da turma"
                                                        checked={todosSelecionados}
                                                        ref={input => {
                                                            if (input) input.indeterminate = quantidadeSelecionada > 0 && !todosSelecionados;
                                                        }}
                                                        disabled={saving}
                                                        onChange={toggleTodos}
                                                    />
                                                </label>
                                            </th>
                                            <th>Matrícula</th>
                                            <th>Nome</th>
                                            <th>Situação</th>
                                            <th>Enturmado desde</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {alunosDaTurma.map(aluno => (
                                            <tr key={aluno.alunoId}>
                                                <td>
                                                    <label className="checkbox-label">
                                                        <input
                                                            type="checkbox"
                                                            aria-label={`Selecionar ${aluno.nome}`}
                                                            checked={selecionados.has(aluno.alunoId)}
                                                            disabled={saving}
                                                            onChange={() => toggleAluno(aluno.alunoId)}
                                                        />
                                                    </label>
                                                </td>
                                                <td>{aluno.matricula}</td>
                                                <td>
                                                    {aluno.nome}
                                                    {falhas[aluno.alunoId] && <div className="field-error">{falhas[aluno.alunoId]}</div>}
                                                </td>
                                                <td>
                                                    <StatusPill status={aluno.status as Status} label={rotuloStatusAluno(aluno.status)} />
                                                </td>
                                                <td className="nowrap-cell">{formatarData(aluno.dataInicio)}</td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>

                            <p className="field-hint">
                                {plural(quantidadeSelecionada, 'aluno selecionado', 'alunos selecionados')} de {alunosDaTurma.length}
                            </p>

                            <div className="form-actions">
                                <button
                                    className="primary-button"
                                    type="button"
                                    disabled={saving}
                                    onClick={desenturmarSelecionados}
                                >
                                    Desenturmar Selecionados
                                </button>
                                <button
                                    className="secondary-button cancel-button"
                                    type="button"
                                    disabled={saving}
                                    onClick={desenturmarTurmaInteira}
                                >
                                    Desenturmar Turma Inteira
                                </button>
                            </div>
                        </>
                    )}
                </section>
            )}

            {!loading && turmas.length === 0 && <EmptyState emptyMessage="Nenhuma turma cadastrada." />}

            <ConfirmDialog
                open={modalAberto}
                title={emLote ? 'Desenturmar turma inteira' : quantidadeSelecionada === 1 ? 'Desenturmar aluno' : 'Desenturmar alunos'}
                variant="warning"
                confirmLabel={emLote ? 'Confirmar Desenturmação em Lote' : 'Confirmar Desenturmação'}
                isLoading={saving}
                onConfirm={() => void confirmarDesenturmacao()}
                onCancel={fecharModal}
            >
                <p>
                    {emLote ? (
                        <>Todos os <strong>{alunosDaTurma.length} alunos</strong> serão retirados da turma</>
                    ) : quantidadeSelecionada === 1 ? (
                        <><strong>{alunosSelecionados[0]?.nome}</strong> será retirado(a) da turma</>
                    ) : (
                        <><strong>{quantidadeSelecionada} alunos</strong> serão retirados da turma</>
                    )}{' '}
                    <strong>{turmaSelecionada?.nomeCompleto}</strong> a partir de hoje. Frequências e notas já lançadas são mantidas.
                </p>
                {!emLote && quantidadeSelecionada > 1 && quantidadeSelecionada <= 5 && (
                    <ul className="confirm-dialog-list">
                        {alunosSelecionados.map(aluno => <li key={aluno.alunoId}>{aluno.nome}</li>)}
                    </ul>
                )}

                <div className="desenturmacao-campos">
                    <div className="form-field">
                        <label htmlFor="desenturmacao-motivo">
                            Motivo da Desenturmação <span className="required">*</span>
                        </label>
                        <select
                            id="desenturmacao-motivo"
                            value={motivo}
                            disabled={saving}
                            aria-invalid={erroModal !== null && !motivo}
                            onChange={event => {
                                setMotivo(event.target.value);
                                setErroModal(null);
                            }}
                        >
                            <option value="">Selecione o motivo</option>
                            {motivos.map(opcao => <option key={opcao.value} value={opcao.value}>{opcao.label}</option>)}
                        </select>
                        {motivo && <span className="field-hint">{efeitoDoMotivo[motivo]}</span>}
                    </div>

                    <div className="form-field">
                        <label htmlFor="desenturmacao-observacao">
                            Observação / Justificativa{' '}
                            {motivo === MOTIVO_OUTROS
                                ? <span className="required">*</span>
                                : <span className="label-optional">(opcional)</span>}
                        </label>
                        <textarea
                            id="desenturmacao-observacao"
                            rows={3}
                            maxLength={MAX_OBSERVACAO}
                            value={observacao}
                            disabled={saving}
                            onChange={event => {
                                setObservacao(event.target.value);
                                setErroModal(null);
                            }}
                        />
                    </div>

                    {erroModal && <p className="field-error" role="alert">{erroModal}</p>}
                </div>
            </ConfirmDialog>
        </div>
    );
}

export default DesenturmarAlunoPage;

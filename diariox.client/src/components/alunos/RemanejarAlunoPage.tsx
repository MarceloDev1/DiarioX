import { useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import DadosRemanejamento, { type DadosRemanejamentoValores } from './DadosRemanejamento';
import '../MainContent.css';

type Aba = 'matricula' | 'turma';

interface Enturmacao {
    alunoId: number;
    matricula: string;
    nome: string;
    status: string;
    dataInicio: string;
    turmaId: number;
    turmaNomeCompleto: string;
    escolaId: number;
    escolaNome: string;
    modalidadeEnsinoId: number;
    modalidadeEnsinoNome: string;
    etapaEnsinoId: number;
    etapaEnsinoNome: string;
    turno: string;
}

const abas: { id: Aba; label: string }[] = [
    { id: 'matricula', label: 'Por Matrícula' },
    { id: 'turma', label: 'Por Turma' },
];

const turnos: Record<string, string> = { MANHA: 'Manhã', TARDE: 'Tarde', NOITE: 'Noite', INTEGRAL: 'Integral' };

const formatarData = (iso: string) => iso.split('-').reverse().join('/');
const rotuloAluno = (item: Enturmacao) => `${item.matricula} - ${item.nome}`;

/** Pares [id, nome] únicos, em ordem alfabética. */
function opcoesUnicas(itens: Enturmacao[], id: (item: Enturmacao) => number | string, nome: (item: Enturmacao) => string) {
    return [...new Map(itens.map(item => [String(id(item)), nome(item)])).entries()]
        .sort(([, a], [, b]) => a.localeCompare(b, 'pt-BR', { numeric: true }));
}

interface Resultado {
    ok: boolean;
    message: string;
    falhas: Record<number, string>;
}

async function enviar(url: string, corpo: unknown): Promise<Resultado> {
    try {
        const response = await apiFetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(corpo),
        });
        if (!response.ok) {
            const payload = (await response.clone().json().catch(() => null)) as { falhas?: { alunoId: number; motivo: string }[] } | null;
            return {
                ok: false,
                message: await readApiError(response),
                falhas: Object.fromEntries((payload?.falhas ?? []).map(falha => [falha.alunoId, falha.motivo])),
            };
        }
        return { ok: true, message: ((await response.json()) as { message: string }).message, falhas: {} };
    } catch (reason) {
        return { ok: false, message: reason instanceof Error ? reason.message : 'Falha ao remanejar.', falhas: {} };
    }
}

function RemanejarAlunoPage() {
    const [aba, setAba] = useState<Aba>('matricula');
    const [enturmacoes, setEnturmacoes] = useState<Enturmacao[]>([]);
    const [versao, setVersao] = useState(0);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);
    // Muda a cada cancelamento/sucesso para o painel de dados recomeçar em branco.
    const [formularioVersao, setFormularioVersao] = useState(0);

    // Por Matrícula
    const [escolaAluno, setEscolaAluno] = useState('');
    const [buscaAluno, setBuscaAluno] = useState('');

    // Por Turma
    const [escolaId, setEscolaId] = useState('');
    const [modalidadeId, setModalidadeId] = useState('');
    const [etapaId, setEtapaId] = useState('');
    const [turmaId, setTurmaId] = useState('');
    const [turno, setTurno] = useState('');
    const [selecionados, setSelecionados] = useState<Set<number>>(new Set());
    const [falhas, setFalhas] = useState<Record<number, string>>({});

    useEffect(() => {
        let cancelado = false;
        void apiFetch('/api/alunos/enturmacoes')
            .then(async response => {
                if (!response.ok) throw new Error(await readApiError(response));
                const data = (await response.json()) as Enturmacao[];
                if (!cancelado) setEnturmacoes(data);
            })
            .catch(reason => {
                if (!cancelado) setError(reason instanceof Error ? reason.message : 'Falha ao carregar os alunos enturmados.');
            })
            .finally(() => {
                if (!cancelado) setLoading(false);
            });

        return () => { cancelado = true; };
    }, [versao]);

    const limparMensagens = () => {
        setError(null);
        setSuccess(null);
        setFalhas({});
    };

    const escolas = opcoesUnicas(enturmacoes, item => item.escolaId, item => item.escolaNome);

    // ---------- Por Matrícula ----------
    const alunosDaEscola = enturmacoes.filter(item => !escolaAluno || String(item.escolaId) === escolaAluno);
    const alunoSelecionado = alunosDaEscola.find(item => rotuloAluno(item) === buscaAluno) ?? null;

    // ---------- Por Turma ----------
    const daEscola = enturmacoes.filter(item => String(item.escolaId) === escolaId);
    const daModalidade = daEscola.filter(item => !modalidadeId || String(item.modalidadeEnsinoId) === modalidadeId);
    const daEtapa = daModalidade.filter(item => !etapaId || String(item.etapaEnsinoId) === etapaId);
    const doTurno = daEtapa.filter(item => !turno || item.turno === turno);

    const modalidades = opcoesUnicas(daEscola, item => item.modalidadeEnsinoId, item => item.modalidadeEnsinoNome);
    const etapas = opcoesUnicas(daModalidade, item => item.etapaEnsinoId, item => item.etapaEnsinoNome);
    const turmas = opcoesUnicas(doTurno, item => item.turmaId, item => item.turmaNomeCompleto);
    const turnosDisponiveis = opcoesUnicas(daEtapa, item => item.turno, item => turnos[item.turno] ?? item.turno);

    const alunosDaTurma = enturmacoes.filter(item => String(item.turmaId) === turmaId);
    const idsSelecionados = alunosDaTurma.filter(item => selecionados.has(item.alunoId)).map(item => item.alunoId);
    const todosSelecionados = alunosDaTurma.length > 0 && idsSelecionados.length === alunosDaTurma.length;

    const trocarTurma = (id: string) => {
        setTurmaId(id);
        setSelecionados(new Set());
        limparMensagens();
    };

    // Ao mudar um filtro, os que dependem dele voltam ao início e a turma escolhida só fica se ainda bater.
    const aplicarFiltro = (setter: (value: string) => void, value: string, manter: (item: Enturmacao) => boolean) => {
        setter(value);
        const turmaAtual = enturmacoes.find(item => String(item.turmaId) === turmaId);
        if (turmaAtual && !manter(turmaAtual)) trocarTurma('');
    };

    const toggleAluno = (id: number) => {
        setSelecionados(atual => {
            const next = new Set(atual);
            if (next.has(id)) next.delete(id);
            else next.add(id);
            return next;
        });
    };

    // ---------- Ações ----------
    const concluir = (resultado: Resultado) => {
        if (resultado.ok) {
            setSuccess(resultado.message);
            setBuscaAluno('');
            setSelecionados(new Set());
            setFormularioVersao(atual => atual + 1);
            setVersao(atual => atual + 1);
        } else {
            setError(resultado.message);
            setFalhas(resultado.falhas);
        }
    };

    const remanejarAluno = async (valores: DadosRemanejamentoValores) => {
        if (!alunoSelecionado) return;
        setSaving(true);
        limparMensagens();
        concluir(await enviar(`/api/alunos/${alunoSelecionado.alunoId}/remanejamentos`, valores));
        setSaving(false);
    };

    const remanejarTurma = async (valores: DadosRemanejamentoValores) => {
        limparMensagens();
        if (idsSelecionados.length === 0) {
            setError('Selecione ao menos um aluno para remanejar.');
            return;
        }
        setSaving(true);
        concluir(await enviar('/api/alunos/remanejamentos', { ...valores, turmaOrigemId: Number(turmaId), alunoIds: idsSelecionados }));
        setSaving(false);
    };

    // Cancelar Operação: descarta seleção e dados preenchidos, sem alterar nenhum vínculo.
    const cancelar = () => {
        limparMensagens();
        if (aba === 'matricula') setBuscaAluno('');
        else setSelecionados(new Set());
        setFormularioVersao(atual => atual + 1);
    };

    const trocarAba = (nova: Aba) => {
        setAba(nova);
        limparMensagens();
        setFormularioVersao(atual => atual + 1);
    };

    return (
        <div className="school-page">
            <section className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Remanejar Aluno</h2>
                        <p>Mova um aluno, ou vários alunos da mesma turma, para outra turma da mesma etapa.</p>
                    </div>
                </div>

                <div className="form-tabs" role="tablist" aria-label="Remanejar aluno">
                    {abas.map(item => (
                        <button
                            key={item.id}
                            type="button"
                            role="tab"
                            aria-selected={aba === item.id}
                            className={`form-tab${aba === item.id ? ' active' : ''}`}
                            disabled={saving}
                            onClick={() => trocarAba(item.id)}
                        >
                            {item.label}
                        </button>
                    ))}
                </div>

                <FeedbackMessage message={error} type="error" />
                <FeedbackMessage message={success} type="success" />

                <div role="tabpanel">
                    {aba === 'matricula' ? (
                        <div className="cadastro-form escola-form form-grid">
                            <div className="form-field">
                                <label htmlFor="remanejamento-escola-aluno">Escola</label>
                                <select
                                    id="remanejamento-escola-aluno"
                                    value={escolaAluno}
                                    disabled={loading || saving}
                                    onChange={event => {
                                        setEscolaAluno(event.target.value);
                                        setBuscaAluno('');
                                        limparMensagens();
                                    }}
                                >
                                    <option value="">Todas as escolas</option>
                                    {escolas.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                                </select>
                            </div>

                            <div className="form-field">
                                <label htmlFor="remanejamento-aluno">Aluno</label>
                                <input
                                    id="remanejamento-aluno"
                                    type="text"
                                    list="remanejamento-alunos"
                                    placeholder="Digite a matrícula ou o nome"
                                    autoComplete="off"
                                    value={buscaAluno}
                                    disabled={loading || saving}
                                    onChange={event => {
                                        setBuscaAluno(event.target.value);
                                        limparMensagens();
                                    }}
                                />
                                <datalist id="remanejamento-alunos">
                                    {alunosDaEscola.map(item => <option key={item.alunoId} value={rotuloAluno(item)} />)}
                                </datalist>
                                {buscaAluno && !alunoSelecionado && (
                                    <span className="field-hint">Escolha um aluno da lista de sugestões.</span>
                                )}
                            </div>
                        </div>
                    ) : (
                        <div className="cadastro-form escola-form form-grid">
                            <div className="form-field">
                                <label htmlFor="remanejamento-escola">Escola</label>
                                <select
                                    id="remanejamento-escola"
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
                                <label htmlFor="remanejamento-modalidade">Modalidade</label>
                                <select
                                    id="remanejamento-modalidade"
                                    value={modalidadeId}
                                    disabled={!escolaId || saving}
                                    onChange={event => {
                                        const value = event.target.value;
                                        setEtapaId('');
                                        setTurno('');
                                        aplicarFiltro(setModalidadeId, value, item => !value || String(item.modalidadeEnsinoId) === value);
                                    }}
                                >
                                    <option value="">Todas</option>
                                    {modalidades.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                                </select>
                            </div>

                            <div className="form-field">
                                <label htmlFor="remanejamento-etapa">Etapa</label>
                                <select
                                    id="remanejamento-etapa"
                                    value={etapaId}
                                    disabled={!escolaId || saving}
                                    onChange={event => {
                                        const value = event.target.value;
                                        setTurno('');
                                        aplicarFiltro(setEtapaId, value, item => !value || String(item.etapaEnsinoId) === value);
                                    }}
                                >
                                    <option value="">Todas</option>
                                    {etapas.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                                </select>
                            </div>

                            <div className="form-field">
                                <label htmlFor="remanejamento-turma">Turma</label>
                                <select
                                    id="remanejamento-turma"
                                    value={turmaId}
                                    disabled={!escolaId || saving}
                                    onChange={event => trocarTurma(event.target.value)}
                                >
                                    <option value="">Selecione a turma</option>
                                    {turmas.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                                </select>
                            </div>

                            <div className="form-field">
                                <label htmlFor="remanejamento-turno">Turno</label>
                                <select
                                    id="remanejamento-turno"
                                    value={turno}
                                    disabled={!escolaId || saving}
                                    onChange={event => {
                                        const value = event.target.value;
                                        aplicarFiltro(setTurno, value, item => !value || item.turno === value);
                                    }}
                                >
                                    <option value="">Todos</option>
                                    {turnosDisponiveis.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                                </select>
                            </div>
                        </div>
                    )}
                </div>

                {!loading && enturmacoes.length === 0 && (
                    <EmptyState emptyMessage="Nenhum aluno enturmado para remanejar." />
                )}
            </section>

            {aba === 'matricula' && alunoSelecionado && (
                <>
                    <section className="content-card">
                        <div className="section-header"><div><h2>Enturmação Atual</h2></div></div>
                        <div className="table-container">
                            <table className="data-table">
                                <thead>
                                    <tr><th>Aluno</th><th>Escola</th><th>Turma</th><th>Turno</th><th>Enturmado desde</th></tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>{alunoSelecionado.nome}</td>
                                        <td>{alunoSelecionado.escolaNome}</td>
                                        <td>{alunoSelecionado.turmaNomeCompleto}</td>
                                        <td>{turnos[alunoSelecionado.turno] ?? alunoSelecionado.turno}</td>
                                        <td>{formatarData(alunoSelecionado.dataInicio)}</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </section>

                    <DadosRemanejamento
                        key={`aluno-${alunoSelecionado.alunoId}-${formularioVersao}`}
                        turmaOrigemId={alunoSelecionado.turmaId}
                        quantidade={1}
                        saving={saving}
                        onConfirm={valores => void remanejarAluno(valores)}
                        onCancel={cancelar}
                    />
                </>
            )}

            {aba === 'turma' && turmaId && (
                <>
                    <section className="content-card">
                        <div className="section-header">
                            <div>
                                <h2>Alunos da Turma</h2>
                                <p>Marque os alunos que serão remanejados juntos.</p>
                            </div>
                        </div>

                        {alunosDaTurma.length === 0 ? (
                            <EmptyState emptyMessage="Nenhum aluno enturmado nesta turma." />
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
                                                                if (input) input.indeterminate = idsSelecionados.length > 0 && !todosSelecionados;
                                                            }}
                                                            disabled={saving}
                                                            onChange={() => setSelecionados(todosSelecionados
                                                                ? new Set()
                                                                : new Set(alunosDaTurma.map(item => item.alunoId)))}
                                                        />
                                                    </label>
                                                </th>
                                                <th>Matrícula</th>
                                                <th>Nome</th>
                                                <th>Enturmado desde</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {alunosDaTurma.map(item => (
                                                <tr key={item.alunoId}>
                                                    <td>
                                                        <label className="checkbox-label">
                                                            <input
                                                                type="checkbox"
                                                                aria-label={`Selecionar ${item.nome}`}
                                                                checked={selecionados.has(item.alunoId)}
                                                                disabled={saving}
                                                                onChange={() => toggleAluno(item.alunoId)}
                                                            />
                                                        </label>
                                                    </td>
                                                    <td>{item.matricula}</td>
                                                    <td>
                                                        {item.nome}
                                                        {falhas[item.alunoId] && <div className="field-error">{falhas[item.alunoId]}</div>}
                                                    </td>
                                                    <td className="nowrap-cell">{formatarData(item.dataInicio)}</td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>
                                <p className="field-hint">
                                    {idsSelecionados.length} de {alunosDaTurma.length} {alunosDaTurma.length === 1 ? 'aluno selecionado' : 'alunos selecionados'}
                                </p>
                            </>
                        )}
                    </section>

                    {alunosDaTurma.length > 0 && (
                        <DadosRemanejamento
                            key={`turma-${turmaId}-${formularioVersao}`}
                            turmaOrigemId={Number(turmaId)}
                            quantidade={idsSelecionados.length}
                            saving={saving}
                            onConfirm={valores => void remanejarTurma(valores)}
                            onCancel={cancelar}
                        />
                    )}
                </>
            )}
        </div>
    );
}

export default RemanejarAlunoPage;

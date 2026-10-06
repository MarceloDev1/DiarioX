import { useEffect, useState, type SubmitEvent } from 'react';
import { apiFetch, baixarArquivo, readApiError } from '../../utils/api';
import { hojeIso } from '../../utils/formatters';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import StatusPill, { type Status } from '../ui/StatusPill';
import { rotuloStatusAluno } from './statusAluno';

interface Aluno {
    id: number;
    matricula: string;
    nome: string;
    escolaId: number;
    escolaNome: string;
    status: string;
}

interface Escola {
    id: number;
    nome: string;
    status: string;
}

interface EnturmacaoAtiva {
    turmaId: number;
    turmaNome: string;
    anoReferencia: number;
    dataInicio: string;
}

interface Transferencia {
    id: number;
    dataTransferencia: string;
    tipoDescricao: string;
    escolaDestino: string;
    escolaOrigemNome: string;
}

interface Formulario {
    dataTransferencia: string;
    tipo: string;
    escolaDestino: string;
    motivo: string;
}

const TIPO_ENTRE_ESCOLAS = 'ENTRE_ESCOLAS_DA_REDE';

const tipos = [
    { value: 'OUTRA_REDE', label: 'Transferência para Outra Rede' },
    { value: TIPO_ENTRE_ESCOLAS, label: 'Transferência Entre Escolas da Rede' },
    { value: 'MUDANCA_MUNICIPIO_ESTADO', label: 'Mudança de Município/Estado' },
];

// Sugestões para o motivo; o campo aceita qualquer texto.
const motivosSugeridos = ['Mudança de endereço', 'Opção da família', 'Trabalho dos responsáveis', 'Proximidade da residência'];

const STATUS_PERMITIDOS = ['ATIVO', 'ATIVO_AGUARDANDO_ENTURMACAO'];
const MAX_RESULTADOS = 20;

const formularioVazio = (): Formulario => ({ dataTransferencia: hojeIso(), tipo: '', escolaDestino: '', motivo: '' });

const normalizar = (texto: string) => texto.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase();
const formatarData = (iso: string) => iso.split('-').reverse().join('/');

// A tela fala em "Situação da Matrícula": o aluno ATIVO está matriculado em uma turma.
const situacaoDaMatricula = (status: string) => status === 'ATIVO' ? 'Matriculado' : rotuloStatusAluno(status) ?? status;

const baixarDeclaracao = (transferencia: Transferencia) =>
    baixarArquivo(`/api/transferencias/${transferencia.id}/declaracao`, 'declaracao-transferencia.pdf');

interface TransferirAlunoPageProps {
    onVoltar: () => void;
}

/** RF014: transferência externa (saída definitiva) do aluno, com a emissão da Declaração de Transferência. */
function TransferirAlunoPage({ onVoltar }: TransferirAlunoPageProps) {
    const [alunos, setAlunos] = useState<Aluno[]>([]);
    const [escolas, setEscolas] = useState<Escola[]>([]);
    const [busca, setBusca] = useState('');
    const [aluno, setAluno] = useState<Aluno | null>(null);
    const [enturmacao, setEnturmacao] = useState<EnturmacaoAtiva | null>(null);
    const [transferencias, setTransferencias] = useState<Transferencia[]>([]);
    const [carregandoAluno, setCarregandoAluno] = useState(false);
    const [form, setForm] = useState<Formulario>(formularioVazio);
    const [concluida, setConcluida] = useState<Transferencia | null>(null);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    useEffect(() => {
        void Promise.all([apiFetch('/api/alunos'), apiFetch('/api/escolas')])
            .then(async ([alunosResponse, escolasResponse]) => {
                if (!alunosResponse.ok) throw new Error(await readApiError(alunosResponse));
                if (!escolasResponse.ok) throw new Error(await readApiError(escolasResponse));
                setAlunos((await alunosResponse.json()) as Aluno[]);
                setEscolas(((await escolasResponse.json()) as Escola[]).filter(escola => escola.status === 'ATIVO'));
            })
            .catch(reason => setError(reason instanceof Error ? reason.message : 'Falha ao carregar os alunos.'))
            .finally(() => setLoading(false));
    }, []);

    const termo = normalizar(busca.trim());
    const resultados = termo
        ? alunos.filter(item => normalizar(item.nome).includes(termo) || item.matricula.includes(termo))
        : [];

    const selecionarAluno = async (item: Aluno) => {
        setAluno(item);
        setEnturmacao(null);
        setTransferencias([]);
        setForm(formularioVazio());
        setConcluida(null);
        setError(null);
        setSuccess(null);
        setCarregandoAluno(true);

        try {
            const [enturmacaoResponse, transferenciasResponse] = await Promise.all([
                apiFetch(`/api/alunos/${item.id}/enturmacao-ativa`),
                apiFetch(`/api/alunos/${item.id}/transferencias`),
            ]);
            // 404 na enturmação ativa = aluno sem turma (aguardando enturmação).
            if (enturmacaoResponse.ok) setEnturmacao((await enturmacaoResponse.json()) as EnturmacaoAtiva);
            else if (enturmacaoResponse.status !== 404) throw new Error(await readApiError(enturmacaoResponse));

            if (!transferenciasResponse.ok) throw new Error(await readApiError(transferenciasResponse));
            setTransferencias((await transferenciasResponse.json()) as Transferencia[]);
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao carregar os dados do aluno.');
        } finally {
            setCarregandoAluno(false);
        }
    };

    // Alternativa 01: limpa o formulário e volta para a busca sem alterar nada.
    const voltarParaBusca = () => {
        setAluno(null);
        setEnturmacao(null);
        setTransferencias([]);
        setForm(formularioVazio());
        setConcluida(null);
        setBusca('');
        setError(null);
        setSuccess(null);
    };

    const alterar = (campo: keyof Formulario, valor: string) => setForm(atual => ({ ...atual, [campo]: valor }));

    // Trocar o tipo troca o modo do campo (lista da rede x texto livre), então o destino digitado/escolhido não vale mais.
    const alterarTipo = (tipo: string) => setForm(atual => ({ ...atual, tipo, escolaDestino: '' }));

    const entreEscolasDaRede = form.tipo === TIPO_ENTRE_ESCOLAS;
    // A escola de origem não é destino possível da transferência.
    const escolasDestino = escolas
        .filter(escola => escola.id !== aluno?.escolaId)
        .sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR', { numeric: true }));

    const confirmarTransferencia = async (event: SubmitEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!aluno) return;

        setError(null);
        setSuccess(null);
        if (!form.dataTransferencia || !form.tipo || !form.escolaDestino.trim()) {
            setError('Preencha a data do desligamento, o tipo de transferência e a escola de destino.');
            return;
        }
        if (form.dataTransferencia > hojeIso()) {
            setError('A data de transferência deve estar dentro do período do Ano Letivo vigente e não pode ser uma data futura.');
            return;
        }

        setSaving(true);
        try {
            const response = await apiFetch(`/api/alunos/${aluno.id}/transferencias`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    dataTransferencia: form.dataTransferencia,
                    tipo: form.tipo,
                    escolaDestino: form.escolaDestino.trim(),
                    motivo: form.motivo.trim() || null,
                }),
            });
            if (!response.ok) throw new Error(await readApiError(response));

            const result = (await response.json()) as { message: string; transferencia: Transferencia };
            setSuccess(result.message);
            setConcluida(result.transferencia);
            setAluno(atual => atual && { ...atual, status: 'TRANSFERIDO' });
            setAlunos(atual => atual.map(item => (item.id === aluno.id ? { ...item, status: 'TRANSFERIDO' } : item)));
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao registrar a transferência.');
        } finally {
            setSaving(false);
        }
    };

    const emitirDeclaracao = async (transferencia: Transferencia) => {
        setError(null);
        try {
            await baixarDeclaracao(transferencia);
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao emitir a declaração.');
        }
    };

    const ultimaTransferencia = concluida ?? transferencias[0] ?? null;
    const jaTransferido = aluno?.status === 'TRANSFERIDO';
    const podeTransferir = aluno !== null && STATUS_PERMITIDOS.includes(aluno.status);

    return (
        <div className="school-page">
            <section className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Transferência Externa</h2>
                        <p>Registre a saída definitiva do aluno para outra instituição e emita a Declaração de Transferência.</p>
                    </div>
                    <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
                        {aluno && (
                            <button className="secondary-button" type="button" disabled={saving} onClick={voltarParaBusca}>
                                ← Nova busca
                            </button>
                        )}
                        <button className="secondary-button" type="button" disabled={saving} onClick={onVoltar}>
                            ← Voltar para a lista
                        </button>
                    </div>
                </div>

                <FeedbackMessage message={error} type="error" />
                <FeedbackMessage message={success} type="success" />

                {!aluno && (
                    <>
                        <div className="filter-bar">
                            <div className="filter-field">
                                <label htmlFor="transferencia-busca">Nome do aluno ou matrícula</label>
                                <input
                                    id="transferencia-busca"
                                    type="text"
                                    className="filter-input"
                                    value={busca}
                                    disabled={loading}
                                    placeholder="Digite para pesquisar"
                                    onChange={event => setBusca(event.target.value)}
                                />
                            </div>
                        </div>

                        {termo && (resultados.length === 0 ? (
                            <EmptyState emptyMessage={`Nenhum aluno encontrado para "${busca.trim()}".`} />
                        ) : (
                            <div className="table-container">
                                <table className="data-table">
                                    <thead>
                                        <tr>
                                            <th>Matrícula</th>
                                            <th>Nome</th>
                                            <th>Escola</th>
                                            <th>Situação</th>
                                            <th>Ações</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {resultados.slice(0, MAX_RESULTADOS).map(item => (
                                            <tr key={item.id}>
                                                <td>{item.matricula}</td>
                                                <td>{item.nome}</td>
                                                <td className="nowrap-cell">{item.escolaNome}</td>
                                                <td><StatusPill status={item.status as Status} label={situacaoDaMatricula(item.status)} /></td>
                                                <td>
                                                    <button type="button" className="table-action-button" onClick={() => void selecionarAluno(item)}>
                                                        Selecionar
                                                    </button>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                                {resultados.length > MAX_RESULTADOS && (
                                    <p className="field-hint">
                                        Mostrando {MAX_RESULTADOS} de {resultados.length} alunos. Refine a busca para encontrar o aluno.
                                    </p>
                                )}
                            </div>
                        ))}
                    </>
                )}
            </section>

            {aluno && (
                <section className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>{aluno.nome}</h2>
                            <p>Matrícula {aluno.matricula}</p>
                        </div>
                    </div>

                    {carregandoAluno ? (
                        <p className="field-hint">Carregando dados do aluno...</p>
                    ) : (
                        <div className="table-container">
                            <table className="data-table">
                                <thead>
                                    <tr>
                                        <th>Escola atual</th>
                                        <th>Turma atual</th>
                                        <th>Situação da Matrícula</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>{aluno.escolaNome}</td>
                                        <td>
                                            {enturmacao
                                                ? <>{enturmacao.turmaNome} ({enturmacao.anoReferencia}) · desde {formatarData(enturmacao.dataInicio)}</>
                                                : 'Sem turma'}
                                        </td>
                                        <td><StatusPill status={aluno.status as Status} label={situacaoDaMatricula(aluno.status)} /></td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    )}
                </section>
            )}

            {aluno && !carregandoAluno && jaTransferido && (
                <section className="content-card">
                    {/* EX02, ou a confirmação logo após a transferência. */}
                    {!concluida && <FeedbackMessage message="Este aluno já possui o status de Transferido no sistema." type="error" />}
                    {ultimaTransferencia && (
                        <>
                            <div className="section-header">
                                <div>
                                    <h2>Declaração de Transferência</h2>
                                    <p>
                                        Transferido(a) em {formatarData(ultimaTransferencia.dataTransferencia)} de {ultimaTransferencia.escolaOrigemNome} para{' '}
                                        <strong>{ultimaTransferencia.escolaDestino}</strong> ({ultimaTransferencia.tipoDescricao}).
                                    </p>
                                </div>
                            </div>
                            <div className="form-actions">
                                <button className="primary-button" type="button" onClick={() => void emitirDeclaracao(ultimaTransferencia)}>
                                    Baixar Declaração (PDF)
                                </button>
                            </div>
                        </>
                    )}
                </section>
            )}

            {aluno && !carregandoAluno && !jaTransferido && !podeTransferir && (
                <FeedbackMessage message="Somente alunos matriculados ou aguardando enturmação podem ser transferidos." type="error" />
            )}

            {aluno && !carregandoAluno && podeTransferir && (
                <section className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>Dados da Saída</h2>
                            <p>
                                {enturmacao
                                    ? 'A enturmação é encerrada na data do desligamento e a vaga na turma é liberada.'
                                    : 'O aluno não está em nenhuma turma.'}{' '}
                                Frequências e notas registradas até essa data ficam congeladas.
                            </p>
                        </div>
                    </div>

                    <form onSubmit={event => void confirmarTransferencia(event)}>
                        <div className="cadastro-form escola-form form-grid">
                            <div className="form-field">
                                <label htmlFor="transferencia-data">
                                    Data do Desligamento / Transferência <span className="required">*</span>
                                </label>
                                <input
                                    id="transferencia-data"
                                    type="date"
                                    value={form.dataTransferencia}
                                    max={hojeIso()}
                                    disabled={saving}
                                    onChange={event => alterar('dataTransferencia', event.target.value)}
                                />
                            </div>

                            <div className="form-field">
                                <label htmlFor="transferencia-tipo">
                                    Tipo de Transferência <span className="required">*</span>
                                </label>
                                <select
                                    id="transferencia-tipo"
                                    value={form.tipo}
                                    disabled={saving}
                                    onChange={event => alterarTipo(event.target.value)}
                                >
                                    <option value="">Selecione o tipo</option>
                                    {tipos.map(tipo => <option key={tipo.value} value={tipo.value}>{tipo.label}</option>)}
                                </select>
                            </div>

                            <div className="form-field">
                                <label htmlFor="transferencia-destino">
                                    Escola de Destino <span className="required">*</span>
                                </label>
                                {entreEscolasDaRede ? (
                                    <select
                                        id="transferencia-destino"
                                        value={form.escolaDestino}
                                        disabled={saving}
                                        onChange={event => alterar('escolaDestino', event.target.value)}
                                    >
                                        <option value="">Selecione a escola de destino</option>
                                        {escolasDestino.map(escola => <option key={escola.id} value={escola.nome}>{escola.nome}</option>)}
                                    </select>
                                ) : (
                                    <input
                                        id="transferencia-destino"
                                        type="text"
                                        maxLength={200}
                                        placeholder="Nome da escola para onde o aluno irá"
                                        value={form.escolaDestino}
                                        disabled={saving}
                                        onChange={event => alterar('escolaDestino', event.target.value)}
                                    />
                                )}
                            </div>

                            <div className="form-field">
                                <label htmlFor="transferencia-motivo">
                                    Motivo da Saída <span className="label-optional">(opcional)</span>
                                </label>
                                <input
                                    id="transferencia-motivo"
                                    type="text"
                                    list="transferencia-motivos"
                                    maxLength={500}
                                    placeholder="Escolha uma sugestão ou digite"
                                    value={form.motivo}
                                    disabled={saving}
                                    onChange={event => alterar('motivo', event.target.value)}
                                />
                                <datalist id="transferencia-motivos">
                                    {motivosSugeridos.map(motivo => <option key={motivo} value={motivo} />)}
                                </datalist>
                            </div>
                        </div>

                        <div className="form-actions">
                            <button type="submit" className="primary-button" disabled={saving}>
                                {saving ? 'Transferindo...' : 'Confirmar Transferência'}
                            </button>
                            <button type="button" className="secondary-button cancel-button" disabled={saving} onClick={voltarParaBusca}>
                                Cancelar
                            </button>
                        </div>
                    </form>
                </section>
            )}
        </div>
    );
}

export default TransferirAlunoPage;

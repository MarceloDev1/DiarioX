import { useCallback, useEffect, useState, type SubmitEvent } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import DesenturmacaoDialog from './DesenturmacaoDialog';
import { desenturmar } from './desenturmacao';

interface AlunosEnturmadosPageProps {
    onEnturmar: () => void;
}

interface Enturmacao {
    alunoId: number;
    matricula: string;
    nome: string;
    status: string;
    dataInicio: string;
    turmaId: number;
    turmaNomeIdentificador: string;
    turmaNomeCompleto: string;
    escolaId: number;
    escolaNome: string;
    modalidadeEnsinoId: number;
    modalidadeEnsinoNome: string;
    etapaEnsinoId: number;
    etapaEnsinoNome: string;
    turno: string;
}

interface Filtros {
    escolaId: string;
    modalidadeId: string;
    etapaId: string;
    turmaId: string;
    matricula: string;
}

const filtrosVazios: Filtros = { escolaId: '', modalidadeId: '', etapaId: '', turmaId: '', matricula: '' };

/** Ao trocar um filtro, os que dependem dele voltam para "Todas". */
const filtrosDependentes: Partial<Record<keyof Filtros, (keyof Filtros)[]>> = {
    escolaId: ['modalidadeId', 'etapaId', 'turmaId'],
    modalidadeId: ['etapaId', 'turmaId'],
    etapaId: ['turmaId'],
};

const turnos: Record<string, string> = { MANHA: 'Manhã', TARDE: 'Tarde', NOITE: 'Noite', INTEGRAL: 'Integral' };

/** Pares [id, nome] únicos, em ordem alfabética. */
function opcoesUnicas(itens: Enturmacao[], id: (item: Enturmacao) => number, nome: (item: Enturmacao) => string) {
    return [...new Map(itens.map(item => [String(id(item)), nome(item)])).entries()]
        .sort(([, a], [, b]) => a.localeCompare(b, 'pt-BR', { numeric: true }));
}

const atende = (item: Enturmacao, filtros: Filtros) =>
    (!filtros.escolaId || String(item.escolaId) === filtros.escolaId)
    && (!filtros.modalidadeId || String(item.modalidadeEnsinoId) === filtros.modalidadeId)
    && (!filtros.etapaId || String(item.etapaEnsinoId) === filtros.etapaId)
    && (!filtros.turmaId || String(item.turmaId) === filtros.turmaId)
    && item.matricula.toLowerCase().includes(filtros.matricula.trim().toLowerCase());

/** Lista dos alunos enturmados, com atalho para enturmar e a desenturmação de um aluno (RF013, a partir do passo 6). */
function AlunosEnturmadosPage({ onEnturmar }: AlunosEnturmadosPageProps) {
    const [enturmacoes, setEnturmacoes] = useState<Enturmacao[]>([]);
    const [versao, setVersao] = useState(0);
    const [rascunho, setRascunho] = useState<Filtros>(filtrosVazios);
    const [filtros, setFiltros] = useState<Filtros>(filtrosVazios);
    const [alvo, setAlvo] = useState<Enturmacao | null>(null);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

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

    // Cada filtro só oferece o que existe dentro dos anteriores.
    const daEscola = enturmacoes.filter(item => !rascunho.escolaId || String(item.escolaId) === rascunho.escolaId);
    const daModalidade = daEscola.filter(item => !rascunho.modalidadeId || String(item.modalidadeEnsinoId) === rascunho.modalidadeId);
    const daEtapa = daModalidade.filter(item => !rascunho.etapaId || String(item.etapaEnsinoId) === rascunho.etapaId);

    const escolas = opcoesUnicas(enturmacoes, item => item.escolaId, item => item.escolaNome);
    const modalidades = opcoesUnicas(daEscola, item => item.modalidadeEnsinoId, item => item.modalidadeEnsinoNome);
    const etapas = opcoesUnicas(daModalidade, item => item.etapaEnsinoId, item => item.etapaEnsinoNome);
    // Sem escola escolhida, turmas de escolas diferentes podem ter o mesmo nome.
    const turmas = opcoesUnicas(daEtapa, item => item.turmaId,
        item => rascunho.escolaId ? item.turmaNomeCompleto : `${item.turmaNomeCompleto} · ${item.escolaNome}`);

    const visiveis = enturmacoes.filter(item => atende(item, filtros));

    const alterarFiltro = (campo: keyof Filtros, valor: string) => {
        setRascunho(atual => ({
            ...atual,
            [campo]: valor,
            ...Object.fromEntries((filtrosDependentes[campo] ?? []).map(dependente => [dependente, ''])),
        }));
    };

    const handleConsultar = (event: SubmitEvent<HTMLFormElement>) => {
        event.preventDefault();
        setFiltros(rascunho);
    };

    const handleLimpar = () => {
        setRascunho(filtrosVazios);
        setFiltros(filtrosVazios);
    };

    const abrirDesenturmacao = (item: Enturmacao) => {
        setSuccess(null);
        setError(null);
        setAlvo(item);
    };

    const fecharModal = useCallback(() => setAlvo(null), []);

    const confirmarDesenturmacao = async (motivo: string, observacao: string | null) => {
        if (!alvo) return;

        setSaving(true);
        const resultado = await desenturmar(alvo.turmaId, [alvo.alunoId], motivo, observacao);
        if (resultado.ok) setSuccess(resultado.message);
        else setError(resultado.falhas[alvo.alunoId] ?? resultado.message);

        setSaving(false);
        setAlvo(null);
        setVersao(atual => atual + 1);
    };

    return (
        <div className="school-page">
            <section className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Alunos Enturmados</h2>
                        <p>Consulte os alunos enturmados e desenturme quando necessário.</p>
                    </div>
                    <button className="primary-button" type="button" onClick={onEnturmar}>
                        + Enturmar Alunos
                    </button>
                </div>

                <FeedbackMessage message={error} type="error" />
                <FeedbackMessage message={success} type="success" />

                <form className="filter-bar" onSubmit={handleConsultar}>
                    <div className="filter-field">
                        <label htmlFor="enturmados-escola">Escola</label>
                        <select id="enturmados-escola" className="filter-input" value={rascunho.escolaId} onChange={e => alterarFiltro('escolaId', e.target.value)}>
                            <option value="">Todas</option>
                            {escolas.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="enturmados-modalidade">Modalidade</label>
                        <select id="enturmados-modalidade" className="filter-input" value={rascunho.modalidadeId} onChange={e => alterarFiltro('modalidadeId', e.target.value)}>
                            <option value="">Todas</option>
                            {modalidades.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="enturmados-etapa">Etapa</label>
                        <select id="enturmados-etapa" className="filter-input" value={rascunho.etapaId} onChange={e => alterarFiltro('etapaId', e.target.value)}>
                            <option value="">Todas</option>
                            {etapas.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="enturmados-turma">Turma</label>
                        <select id="enturmados-turma" className="filter-input" value={rascunho.turmaId} onChange={e => alterarFiltro('turmaId', e.target.value)}>
                            <option value="">Todas</option>
                            {turmas.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="enturmados-matricula">Matrícula</label>
                        <input
                            id="enturmados-matricula"
                            type="text"
                            className="filter-input"
                            value={rascunho.matricula}
                            onChange={e => alterarFiltro('matricula', e.target.value)}
                        />
                    </div>
                    <button type="submit" className="filter-button">Consultar</button>
                    <button type="button" className="filter-button filter-button-static" onClick={handleLimpar}>Limpar</button>
                </form>

                {loading ? (
                    <div className="loading">Carregando alunos enturmados...</div>
                ) : visiveis.length === 0 ? (
                    <EmptyState emptyMessage={enturmacoes.length === 0
                        ? "Nenhum aluno enturmado. Clique em '+ Enturmar Alunos' para começar."
                        : 'Nenhum aluno encontrado. Tente ajustar os filtros.'} />
                ) : (
                    <div className="table-container">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Matrícula</th>
                                    <th>Aluno</th>
                                    <th>Escola</th>
                                    <th>Modalidade</th>
                                    <th className="nowrap-cell">Etapa</th>
                                    <th className="nowrap-cell">Turma</th>
                                    <th>Turno</th>
                                    <th>Ações</th>
                                </tr>
                            </thead>
                            <tbody>
                                {visiveis.map(item => (
                                    <tr key={item.alunoId}>
                                        <td>{item.matricula}</td>
                                        <td>{item.nome}</td>
                                        <td className="nowrap-cell">{item.escolaNome}</td>
                                        <td>{item.modalidadeEnsinoNome}</td>
                                        <td>{item.etapaEnsinoNome}</td>
                                        <td title={item.turmaNomeCompleto}>{item.turmaNomeIdentificador}</td>
                                        <td>{turnos[item.turno] ?? item.turno}</td>
                                        <td>
                                            <button
                                                type="button"
                                                className="table-action-button danger"
                                                disabled={saving}
                                                onClick={() => abrirDesenturmacao(item)}
                                            >
                                                Desenturmar
                                            </button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </section>

            {alvo && (
                <DesenturmacaoDialog
                    turmaNome={alvo.turmaNomeCompleto}
                    alunos={[{ alunoId: alvo.alunoId, nome: alvo.nome }]}
                    emLote={false}
                    isLoading={saving}
                    onConfirm={(motivo, observacao) => void confirmarDesenturmacao(motivo, observacao)}
                    onCancel={fecharModal}
                />
            )}
        </div>
    );
}

export default AlunosEnturmadosPage;

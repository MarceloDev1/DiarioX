import { useEffect, useState, type SubmitEvent } from 'react';
import { apiFetch, baixarArquivo, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';

interface AlunosTransferidosPageProps {
    onTransferir: () => void;
}

interface Transferido {
    id: number;
    alunoId: number;
    matricula: string;
    alunoNome: string;
    escolaId: number;
    escolaNome: string;
    // Nulos quando o aluno aguardava enturmação ao ser transferido.
    modalidadeEnsinoId: number | null;
    modalidadeEnsinoNome: string | null;
    etapaEnsinoId: number | null;
    etapaEnsinoNome: string | null;
    turmaId: number | null;
    turmaNomeIdentificador: string | null;
    turmaNomeCompleto: string | null;
    turno: string | null;
    dataTransferencia: string;
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

/** Pares [id, nome] únicos, em ordem alfabética; itens sem o dado (aluno que não tinha turma) ficam de fora. */
function opcoesUnicas(itens: Transferido[], id: (item: Transferido) => number | null, nome: (item: Transferido) => string | null) {
    const pares = itens.flatMap(item => {
        const valor = id(item);
        const texto = nome(item);
        return valor === null || texto === null ? [] : [[String(valor), texto] as const];
    });
    return [...new Map(pares).entries()].sort(([, a], [, b]) => a.localeCompare(b, 'pt-BR', { numeric: true }));
}

const atende = (item: Transferido, filtros: Filtros) =>
    (!filtros.escolaId || String(item.escolaId) === filtros.escolaId)
    && (!filtros.modalidadeId || String(item.modalidadeEnsinoId) === filtros.modalidadeId)
    && (!filtros.etapaId || String(item.etapaEnsinoId) === filtros.etapaId)
    && (!filtros.turmaId || String(item.turmaId) === filtros.turmaId)
    && item.matricula.toLowerCase().includes(filtros.matricula.trim().toLowerCase());

const baixarDeclaracao = (item: Transferido) =>
    baixarArquivo(`/api/transferencias/${item.id}/declaracao`, `declaracao-transferencia-${item.matricula}.pdf`);

/** Lista dos alunos transferidos (RF014), com atalho para uma nova transferência e a emissão da declaração. */
function AlunosTransferidosPage({ onTransferir }: AlunosTransferidosPageProps) {
    const [transferidos, setTransferidos] = useState<Transferido[]>([]);
    const [rascunho, setRascunho] = useState<Filtros>(filtrosVazios);
    const [filtros, setFiltros] = useState<Filtros>(filtrosVazios);
    const [gerandoId, setGerandoId] = useState<number | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelado = false;
        void apiFetch('/api/transferencias')
            .then(async response => {
                if (!response.ok) throw new Error(await readApiError(response));
                const data = (await response.json()) as Transferido[];
                if (!cancelado) setTransferidos(data);
            })
            .catch(reason => {
                if (!cancelado) setError(reason instanceof Error ? reason.message : 'Falha ao carregar os alunos transferidos.');
            })
            .finally(() => {
                if (!cancelado) setLoading(false);
            });

        return () => { cancelado = true; };
    }, []);

    // Cada filtro só oferece o que existe dentro dos anteriores.
    const daEscola = transferidos.filter(item => !rascunho.escolaId || String(item.escolaId) === rascunho.escolaId);
    const daModalidade = daEscola.filter(item => !rascunho.modalidadeId || String(item.modalidadeEnsinoId) === rascunho.modalidadeId);
    const daEtapa = daModalidade.filter(item => !rascunho.etapaId || String(item.etapaEnsinoId) === rascunho.etapaId);

    const escolas = opcoesUnicas(transferidos, item => item.escolaId, item => item.escolaNome);
    const modalidades = opcoesUnicas(daEscola, item => item.modalidadeEnsinoId, item => item.modalidadeEnsinoNome);
    const etapas = opcoesUnicas(daModalidade, item => item.etapaEnsinoId, item => item.etapaEnsinoNome);
    // Sem escola escolhida, turmas de escolas diferentes podem ter o mesmo nome.
    const turmas = opcoesUnicas(daEtapa, item => item.turmaId,
        item => item.turmaNomeCompleto && (rascunho.escolaId ? item.turmaNomeCompleto : `${item.turmaNomeCompleto} · ${item.escolaNome}`));

    const visiveis = transferidos.filter(item => atende(item, filtros));

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

    const gerarDeclaracao = async (item: Transferido) => {
        setError(null);
        setGerandoId(item.id);
        try {
            await baixarDeclaracao(item);
        } catch (reason) {
            setError(reason instanceof Error ? reason.message : 'Falha ao gerar a declaração.');
        } finally {
            setGerandoId(null);
        }
    };

    return (
        <div className="school-page">
            <section className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Alunos Transferidos</h2>
                        <p>Consulte os alunos transferidos e imprima a declaração quando necessário.</p>
                    </div>
                    <button className="primary-button" type="button" onClick={onTransferir}>
                        + Transferir Aluno
                    </button>
                </div>

                <FeedbackMessage message={error} type="error" />

                <form className="filter-bar" onSubmit={handleConsultar}>
                    <div className="filter-field">
                        <label htmlFor="transferidos-escola">Escola</label>
                        <select id="transferidos-escola" className="filter-input" value={rascunho.escolaId} onChange={e => alterarFiltro('escolaId', e.target.value)}>
                            <option value="">Todas</option>
                            {escolas.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="transferidos-modalidade">Modalidade</label>
                        <select id="transferidos-modalidade" className="filter-input" value={rascunho.modalidadeId} onChange={e => alterarFiltro('modalidadeId', e.target.value)}>
                            <option value="">Todas</option>
                            {modalidades.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="transferidos-etapa">Etapa</label>
                        <select id="transferidos-etapa" className="filter-input" value={rascunho.etapaId} onChange={e => alterarFiltro('etapaId', e.target.value)}>
                            <option value="">Todas</option>
                            {etapas.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="transferidos-turma">Turma</label>
                        <select id="transferidos-turma" className="filter-input" value={rascunho.turmaId} onChange={e => alterarFiltro('turmaId', e.target.value)}>
                            <option value="">Todas</option>
                            {turmas.map(([id, nome]) => <option key={id} value={id}>{nome}</option>)}
                        </select>
                    </div>
                    <div className="filter-field">
                        <label htmlFor="transferidos-matricula">Matrícula</label>
                        <input
                            id="transferidos-matricula"
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
                    <div className="loading">Carregando alunos transferidos...</div>
                ) : visiveis.length === 0 ? (
                    <EmptyState emptyMessage={transferidos.length === 0
                        ? "Nenhum aluno transferido. Clique em '+ Transferir Aluno' para registrar uma transferência."
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
                                    <tr key={item.id}>
                                        <td>{item.matricula}</td>
                                        <td>{item.alunoNome}</td>
                                        <td className="nowrap-cell">{item.escolaNome}</td>
                                        <td>{item.modalidadeEnsinoNome ?? '—'}</td>
                                        <td>{item.etapaEnsinoNome ?? '—'}</td>
                                        <td title={item.turmaNomeCompleto ?? undefined}>{item.turmaNomeIdentificador ?? '—'}</td>
                                        <td>{item.turno ? turnos[item.turno] ?? item.turno : '—'}</td>
                                        <td>
                                            <button
                                                type="button"
                                                className="table-action-button"
                                                disabled={gerandoId === item.id}
                                                onClick={() => void gerarDeclaracao(item)}
                                            >
                                                {gerandoId === item.id ? 'Gerando...' : 'Gerar Declaração'}
                                            </button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </section>
        </div>
    );
}

export default AlunosTransferidosPage;

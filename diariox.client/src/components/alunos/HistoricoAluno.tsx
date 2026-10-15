import { useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import { formatDataBr } from '../../utils/formatters';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import StatusPill, { type Status } from '../ui/StatusPill';
import { rotuloStatusAluno } from './statusAluno';

interface SituacaoEscola {
    escolaId: number;
    escola: string;
    situacao: string;
    atual: boolean;
}

interface Movimentacao {
    data: string;
    tipo: string;
    descricao: string;
    escola: string | null;
    turma: string | null;
    situacao: string | null;
    observacao: string | null;
}

interface Historico {
    alunoId: number;
    nome: string;
    matricula: string;
    escolas: SituacaoEscola[];
    movimentacoes: Movimentacao[];
}

const tiposMovimentacao: Record<string, string> = {
    CADASTRO: 'Cadastro',
    ENTURMACAO: 'Enturmação',
    REMANEJAMENTO: 'Remanejamento',
    DESENTURMACAO: 'Desenturmação',
    TRANSFERENCIA_SAIDA: 'Transferência (saída)',
    TRANSFERENCIA_ENTRADA: 'Transferência (entrada)',
};

// ATIVO e INATIVO não têm rótulo próprio em rotuloStatusAluno; no histórico ficam por extenso.
const rotuloSituacao = (status: string) =>
    status === 'ATIVO' ? 'Ativo' : status === 'INATIVO' ? 'Inativo' : rotuloStatusAluno(status) ?? status;

interface HistoricoAlunoProps {
    alunoId: number;
}

/** Histórico de movimentação: situação do aluno em cada escola da rede e as movimentações em ordem cronológica. */
function HistoricoAluno({ alunoId }: HistoricoAlunoProps) {
    const [historico, setHistorico] = useState<Historico | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // Os pais montam o componente com key={alunoId}: outro aluno começa do estado inicial (carregando).
    useEffect(() => {
        let cancelado = false;

        void apiFetch(`/api/alunos/${alunoId}/historico`)
            .then(async response => {
                if (!response.ok) throw new Error(await readApiError(response));
                const dados = (await response.json()) as Historico;
                if (!cancelado) setHistorico(dados);
            })
            .catch(reason => {
                if (!cancelado) setError(reason instanceof Error ? reason.message : 'Falha ao carregar o histórico do aluno.');
            })
            .finally(() => {
                if (!cancelado) setLoading(false);
            });

        return () => { cancelado = true; };
    }, [alunoId]);

    if (loading) return <div className="loading">Carregando histórico...</div>;
    if (error || !historico) return <FeedbackMessage message={error ?? 'Histórico indisponível.'} type="error" />;

    return (
        <div className="historico-aluno">
            <section>
                <h3 className="historico-titulo">Situação por escola</h3>
                <div className="table-container">
                    <table className="data-table">
                        <thead>
                            <tr>
                                <th>Escola</th>
                                <th>Situação</th>
                            </tr>
                        </thead>
                        <tbody>
                            {historico.escolas.map(escola => (
                                <tr key={escola.escolaId}>
                                    <td>
                                        {escola.escola}
                                        {escola.atual && <span className="historico-selo">Escola atual</span>}
                                    </td>
                                    <td>
                                        <StatusPill status={escola.situacao as Status} label={rotuloSituacao(escola.situacao)} />
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </section>

            <section>
                <h3 className="historico-titulo">Movimentações</h3>
                {historico.movimentacoes.length === 0 ? (
                    <EmptyState emptyMessage="Nenhuma movimentação registrada." />
                ) : (
                    <div className="table-container">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Data</th>
                                    <th>Movimentação</th>
                                    <th>Escola</th>
                                    <th>Turma</th>
                                    <th>Situação</th>
                                </tr>
                            </thead>
                            <tbody>
                                {historico.movimentacoes.map((mov, indice) => (
                                    <tr key={`${mov.tipo}-${mov.data}-${indice}`}>
                                        <td className="nowrap-cell">{formatDataBr(mov.data)}</td>
                                        <td>
                                            <strong>{tiposMovimentacao[mov.tipo] ?? mov.tipo}</strong>
                                            <div>{mov.descricao}</div>
                                            {mov.observacao && <div className="field-hint">Motivo: {mov.observacao}</div>}
                                        </td>
                                        <td>{mov.escola ?? '—'}</td>
                                        <td>{mov.turma ?? '—'}</td>
                                        <td>
                                            {mov.situacao
                                                ? <StatusPill status={mov.situacao as Status} label={rotuloSituacao(mov.situacao)} />
                                                : '—'}
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

export default HistoricoAluno;

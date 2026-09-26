import { useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import { formatarData, type ChamadaTurma, type Frequencia } from './tipos';

interface FrequenciaChamadaProps {
    turma: ChamadaTurma;
    disciplinaId: number;
}

const formatarPercentual = (valor: number) =>
    `${valor.toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })}%`;

function FrequenciaChamada({ turma, disciplinaId }: FrequenciaChamadaProps) {
    // '' = ano letivo inteiro.
    const [periodoId, setPeriodoId] = useState('');
    const [resultado, setResultado] = useState<{ chave: string; frequencia: Frequencia } | null>(null);
    const [erro, setErro] = useState<{ chave: string; message: string } | null>(null);

    const chave = `${turma.turmaId}|${disciplinaId}|${periodoId}`;

    useEffect(() => {
        let cancelled = false;
        const chaveAtual = `${turma.turmaId}|${disciplinaId}|${periodoId}`;

        async function load() {
            try {
                const params = new URLSearchParams({ turmaId: String(turma.turmaId), disciplinaId: String(disciplinaId) });
                if (periodoId) params.set('periodoId', periodoId);
                const response = await apiFetch(`/api/chamadas/frequencia?${params}`);
                if (!response.ok) throw new Error(await readApiError(response));
                const frequencia = (await response.json()) as Frequencia;
                if (!cancelled) setResultado({ chave: chaveAtual, frequencia });
            } catch (e) {
                if (!cancelled) setErro({ chave: chaveAtual, message: e instanceof Error ? e.message : 'Falha ao calcular a frequência.' });
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [turma.turmaId, disciplinaId, periodoId]);

    const frequencia = resultado?.chave === chave ? resultado.frequencia : null;
    const erroAtual = erro?.chave === chave ? erro.message : null;
    const abaixo = frequencia?.alunos.filter(a => a.abaixoDoMinimo).length ?? 0;

    return (
        <>
            <div className="chamada-cabecalho">
                <div className="form-group">
                    <label htmlFor="frequencia-periodo">Período</label>
                    <select id="frequencia-periodo" value={periodoId} onChange={e => setPeriodoId(e.target.value)}>
                        <option value="">Ano letivo inteiro</option>
                        {turma.periodos.map(p => (
                            <option key={p.id} value={p.id}>
                                {p.nome} ({formatarData(p.dataInicio)} a {formatarData(p.dataTermino)})
                            </option>
                        ))}
                    </select>
                </div>
            </div>

            <FeedbackMessage message={erroAtual} type="error" />

            {!frequencia ? (
                !erroAtual && <div className="loading">Calculando frequência...</div>
            ) : frequencia.alunos.length === 0 ? (
                <EmptyState emptyMessage="Nenhum aluno enturmado nesta turma no período." />
            ) : (
                <>
                    <p className="chamada-status">
                        <strong>{frequencia.aulasDadas}</strong> {frequencia.aulasDadas === 1 ? 'aula dada' : 'aulas dadas'} de{' '}
                        {formatarData(frequencia.de)} a {formatarData(frequencia.ate)}. Frequência mínima exigida:{' '}
                        <strong>{frequencia.frequenciaMinima}%</strong>.
                        {abaixo > 0 && <> <span className="status-pill situacao-pill-falta">{abaixo} abaixo do mínimo</span></>}
                    </p>

                    <div className="table-container">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Matrícula</th>
                                    <th>Aluno</th>
                                    <th>Aulas</th>
                                    <th>Faltas</th>
                                    <th>Justificadas</th>
                                    <th>Frequência</th>
                                </tr>
                            </thead>
                            <tbody>
                                {frequencia.alunos.map(a => (
                                    <tr key={a.alunoId} className={a.abaixoDoMinimo ? 'frequencia-baixa' : undefined}>
                                        <td className="nowrap-cell">{a.matricula}</td>
                                        <td>{a.nome}</td>
                                        <td>{a.aulas}</td>
                                        <td>{a.faltas}</td>
                                        <td>{a.faltasJustificadas}</td>
                                        <td>
                                            {a.percentualFrequencia === null ? '—' : (
                                                <span className={`status-pill ${a.abaixoDoMinimo ? 'situacao-pill-falta' : 'situacao-pill-presente'}`}>
                                                    {formatarPercentual(a.percentualFrequencia)}
                                                </span>
                                            )}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                    <p className="field-hint">Faltas justificadas contam como ausência no cálculo da frequência.</p>
                </>
            )}
        </>
    );
}

export default FrequenciaChamada;

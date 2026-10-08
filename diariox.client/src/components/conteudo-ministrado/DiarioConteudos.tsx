import { useEffect, useState } from 'react';
import { FiAlertTriangle, FiCheckCircle } from 'react-icons/fi';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import { formatarData } from '../chamada/tipos';
import type { ConteudoTurma, Diario } from './tipos';

interface DiarioConteudosProps {
    turma: ConteudoTurma;
    disciplinaId: number | null;
    /** Abre o registro de conteúdo da data (aba "Registrar"). */
    onAbrir: (data: string) => void;
}

const TODO_O_ANO = '';

function DiarioConteudos({ turma, disciplinaId, onAbrir }: DiarioConteudosProps) {
    const [periodoId, setPeriodoId] = useState(TODO_O_ANO);
    const [apenasPendencias, setApenasPendencias] = useState(false);
    const [diario, setDiario] = useState<Diario | null>(null);
    const [error, setError] = useState<string | null>(null);
    const diaria = turma.tipoFrequencia === 'DIARIA';

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const params = new URLSearchParams({ turmaId: String(turma.turmaId) });
                // Na frequência diária a chamada é da turma: o diário cobre todas as disciplinas.
                if (!diaria && disciplinaId !== null) params.set('disciplinaId', String(disciplinaId));
                const periodo = turma.periodos.find(p => String(p.id) === periodoId);
                if (periodo) {
                    params.set('de', periodo.dataInicio);
                    params.set('ate', periodo.dataTermino);
                }

                const response = await apiFetch(`/api/conteudosministrados/diario?${params}`);
                if (!response.ok) throw new Error(await readApiError(response));
                const data = (await response.json()) as Diario;
                if (!cancelled) {
                    setDiario(data);
                    setError(null);
                }
            } catch (e) {
                if (!cancelled) {
                    setDiario(null);
                    setError(e instanceof Error ? e.message : 'Falha ao carregar o diário.');
                }
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [turma, disciplinaId, diaria, periodoId]);

    if (error) return <FeedbackMessage message={error} type="error" />;
    if (!diario) return <div className="loading">Carregando diário...</div>;

    const dias = apenasPendencias ? diario.dias.filter(d => d.pendencia) : diario.dias;

    return (
        <>
            <div className="chamada-barra">
                <div className="form-group">
                    <label htmlFor="diario-periodo">Período</label>
                    <select id="diario-periodo" value={periodoId} onChange={e => setPeriodoId(e.target.value)}>
                        <option value={TODO_O_ANO}>Todo o ano letivo</option>
                        {turma.periodos.map(p => <option key={p.id} value={p.id}>{p.nome}</option>)}
                    </select>
                </div>
                <label className="checkbox-field">
                    <input
                        type="checkbox"
                        checked={apenasPendencias}
                        onChange={e => setApenasPendencias(e.target.checked)}
                    />
                    <span>Mostrar só as pendências</span>
                </label>
            </div>

            {diario.totalPendencias > 0 ? (
                <div className="aviso-financeiro aviso-bloqueio" role="alert">
                    <span>
                        <FiAlertTriangle aria-hidden="true" /> {diario.totalPendencias}{' '}
                        {diario.totalPendencias === 1 ? 'dia com divergência' : 'dias com divergência'} entre a frequência e o conteúdo ministrado.
                    </span>
                </div>
            ) : (
                <p className="chamada-status">
                    <FiCheckCircle aria-hidden="true" /> Nenhuma divergência entre frequência e conteúdo ministrado no período.
                </p>
            )}

            {dias.length === 0 ? (
                <EmptyState
                    emptyMessage={apenasPendencias ? 'Nenhuma pendência no período.' : 'Nenhuma frequência ou conteúdo registrado no período.'}
                />
            ) : (
                <div className="table-container">
                    <table className="data-table">
                        <thead>
                            <tr>
                                <th>Data</th>
                                <th>Frequência</th>
                                <th>Conteúdo ministrado</th>
                                <th>Situação</th>
                                <th>Ações</th>
                            </tr>
                        </thead>
                        <tbody>
                            {dias.map(dia => (
                                <tr key={dia.data} className={dia.pendencia ? 'diario-linha-pendente' : undefined}>
                                    <td className="nowrap-cell">{formatarData(dia.data)}</td>
                                    <td>
                                        {dia.frequencia ? (
                                            <small>
                                                {!diaria && <>{dia.frequencia.quantidadeAulas} {dia.frequencia.quantidadeAulas === 1 ? 'aula' : 'aulas'}<br /></>}
                                                {dia.frequencia.presentes} P · {dia.frequencia.faltas} F · {dia.frequencia.faltasJustificadas} FJ
                                            </small>
                                        ) : '—'}
                                    </td>
                                    <td>
                                        {dia.conteudos.length === 0 ? '—' : dia.conteudos.map(c => (
                                            <div key={c.id} className="diario-conteudo">
                                                {(diaria || disciplinaId === null) && <strong>{c.disciplinaNome}: </strong>}
                                                {c.descricao}
                                                {c.habilidades.length > 0 && (
                                                    <div className="anos-list">
                                                        {c.habilidades.map(h => (
                                                            <span key={h.id} className="ano-tag" title={h.descricao}>{h.codigo}</span>
                                                        ))}
                                                    </div>
                                                )}
                                            </div>
                                        ))}
                                    </td>
                                    <td>
                                        {dia.pendencia ? (
                                            <span className="status-pill diario-pendencia" title={dia.alerta ?? undefined}>
                                                <FiAlertTriangle aria-hidden="true" />&nbsp;
                                                {dia.pendencia === 'SEM_CONTEUDO' ? 'Sem conteúdo' : 'Sem frequência'}
                                            </span>
                                        ) : (
                                            <span className="status-pill status-active">Em dia</span>
                                        )}
                                    </td>
                                    <td>
                                        <button type="button" className="table-action-button" onClick={() => onAbrir(dia.data)}>
                                            {dia.conteudos.length === 0 ? 'Registrar conteúdo' : 'Abrir'}
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </>
    );
}

export default DiarioConteudos;

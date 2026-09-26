import { useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import { formatarData, formatarDataHora, type ChamadaResumo } from './tipos';

interface HistoricoChamadasProps {
    turmaId: number;
    disciplinaId: number;
    onAbrir: (data: string) => void;
}

function HistoricoChamadas({ turmaId, disciplinaId, onAbrir }: HistoricoChamadasProps) {
    const [chamadas, setChamadas] = useState<ChamadaResumo[] | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const params = new URLSearchParams({ turmaId: String(turmaId), disciplinaId: String(disciplinaId) });
                const response = await apiFetch(`/api/chamadas?${params}`);
                if (!response.ok) throw new Error(await readApiError(response));
                const data = (await response.json()) as ChamadaResumo[];
                if (!cancelled) setChamadas(data);
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar o histórico.');
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [turmaId, disciplinaId]);

    if (error) return <FeedbackMessage message={error} type="error" />;
    if (!chamadas) return <div className="loading">Carregando histórico...</div>;
    if (chamadas.length === 0) {
        return <EmptyState emptyMessage="Nenhuma chamada registrada para esta turma e disciplina." />;
    }

    const totalAulas = chamadas.reduce((soma, c) => soma + c.quantidadeAulas, 0);

    return (
        <>
            <p className="chamada-status">
                {chamadas.length} {chamadas.length === 1 ? 'chamada registrada' : 'chamadas registradas'}, somando {totalAulas} {totalAulas === 1 ? 'aula' : 'aulas'}.
            </p>
            <div className="table-container">
                <table className="data-table">
                    <thead>
                        <tr>
                            <th>Data</th>
                            <th>Aulas</th>
                            <th>Presentes</th>
                            <th>Faltas</th>
                            <th>Justificadas</th>
                            <th>Conteúdo</th>
                            <th>Registrada por</th>
                            <th>Ações</th>
                        </tr>
                    </thead>
                    <tbody>
                        {chamadas.map(c => (
                            <tr key={c.id}>
                                <td className="nowrap-cell">{formatarData(c.data)}</td>
                                <td>{c.quantidadeAulas}</td>
                                <td>{c.presentes}</td>
                                <td>{c.faltas}</td>
                                <td>{c.faltasJustificadas}</td>
                                <td className="chamada-conteudo-cell" title={c.conteudo ?? undefined}>{c.conteudo ?? '—'}</td>
                                <td>
                                    <small>{c.registradoPor ?? '—'}<br />{formatarDataHora(c.registradoEm)}</small>
                                </td>
                                <td>
                                    <button type="button" className="table-action-button" onClick={() => onAbrir(c.data)}>
                                        Abrir
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </>
    );
}

export default HistoricoChamadas;

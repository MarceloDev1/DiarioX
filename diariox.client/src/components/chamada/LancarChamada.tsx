import { useEffect, useState } from 'react';
import { useConfirm } from '../../hooks/useConfirm';
import { usePermissoes } from '../../hooks/usePermissoes';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import {
    MAX_QUANTIDADE_AULAS, formatarData, formatarDataHora, hojeIso, situacoes,
    type Chamada, type ChamadaTurma, type Situacao,
} from './tipos';

interface LancarChamadaProps {
    turma: ChamadaTurma;
    disciplinaId: number;
    data: string;
    onDataChange: (data: string) => void;
}

interface Rascunho {
    quantidadeAulas: number;
    conteudo: string;
    registros: Record<number, { situacao: Situacao; justificativa: string }>;
}

const plural = (quantidade: number, singular: string, pluralTexto: string) =>
    `${quantidade} ${quantidade === 1 ? singular : pluralTexto}`;

// Chamada nova começa com todos presentes: o professor só marca as faltas.
function rascunhoDe(chamada: Chamada): Rascunho {
    return {
        quantidadeAulas: chamada.quantidadeAulas,
        conteudo: chamada.conteudo ?? '',
        registros: Object.fromEntries(chamada.alunos.map(a => [
            a.alunoId,
            { situacao: a.situacao ?? 'PRESENTE', justificativa: a.justificativa ?? '' },
        ])),
    };
}

function LancarChamada({ turma, disciplinaId, data, onDataChange }: LancarChamadaProps) {
    const { can } = usePermissoes();
    const { confirm, confirmDialog } = useConfirm();

    // A chamada carregada guarda a data a que se refere; enquanto não bate com a data atual, está carregando.
    const [carregada, setCarregada] = useState<{ data: string; chamada: Chamada } | null>(null);
    const [erroCarga, setErroCarga] = useState<{ data: string; message: string } | null>(null);
    const [rascunho, setRascunho] = useState<Rascunho | null>(null);
    const [recarregar, setRecarregar] = useState(0);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const params = new URLSearchParams({ turmaId: String(turma.turmaId), disciplinaId: String(disciplinaId), data });
                const response = await apiFetch(`/api/chamadas/aula?${params}`);
                if (!response.ok) throw new Error(await readApiError(response));
                const chamada = (await response.json()) as Chamada;
                if (cancelled) return;

                setCarregada({ data, chamada });
                setRascunho(rascunhoDe(chamada));
                setErroCarga(null);
            } catch (e) {
                if (cancelled) return;
                setCarregada(null);
                setErroCarga({ data, message: e instanceof Error ? e.message : 'Falha ao carregar a chamada.' });
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [turma.turmaId, disciplinaId, data, recarregar]);

    const chamada = carregada?.data === data ? carregada.chamada : null;
    const isLoading = !chamada && erroCarga?.data !== data;
    const isNova = chamada?.chamadaId === null;
    const podeSalvar = chamada ? (isNova ? can('chamada.criar') : can('chamada.editar')) : false;
    const somenteLeitura = !podeSalvar || isSaving;
    const isDirty = !!chamada && !!rascunho && JSON.stringify(rascunho) !== JSON.stringify(rascunhoDe(chamada));

    const hoje = hojeIso();
    const dataMaxima = turma.anoLetivoTermino < hoje ? turma.anoLetivoTermino : hoje;

    const contagem = (situacao: Situacao) =>
        rascunho ? Object.values(rascunho.registros).filter(r => r.situacao === situacao).length : 0;

    const handleDataChange = async (novaData: string) => {
        if (!novaData || novaData === data) return;
        if (isDirty && !(await confirm({
            title: 'Descartar alterações',
            variant: 'warning',
            confirmLabel: 'Descartar',
            message: <p>A chamada de <strong>{formatarData(data)}</strong> tem alterações não salvas. Deseja descartá-las?</p>,
        }))) return;

        setError(null);
        setSuccessMessage(null);
        onDataChange(novaData);
    };

    const setRegistro = (alunoId: number, alteracao: Partial<Rascunho['registros'][number]>) => {
        setRascunho(atual => atual && {
            ...atual,
            registros: { ...atual.registros, [alunoId]: { ...atual.registros[alunoId], ...alteracao } },
        });
        setError(null);
        setSuccessMessage(null);
    };

    const handleTodosPresentes = () => {
        setRascunho(atual => atual && {
            ...atual,
            registros: Object.fromEntries(Object.keys(atual.registros).map(id => [id, { situacao: 'PRESENTE', justificativa: '' }])),
        });
    };

    const handleSalvar = async () => {
        if (!chamada || !rascunho) return;

        const semJustificativa = chamada.alunos.find(a =>
            rascunho.registros[a.alunoId].situacao === 'FALTA_JUSTIFICADA' && !rascunho.registros[a.alunoId].justificativa.trim());
        if (semJustificativa) {
            setError(`Informe a justificativa da falta de ${semJustificativa.nome}.`);
            return;
        }

        setIsSaving(true);
        setError(null);
        setSuccessMessage(null);
        try {
            const body = {
                turmaId: turma.turmaId,
                disciplinaId,
                data,
                quantidadeAulas: rascunho.quantidadeAulas,
                conteudo: rascunho.conteudo.trim() || null,
                alunos: chamada.alunos.map(a => {
                    const registro = rascunho.registros[a.alunoId];
                    return {
                        alunoId: a.alunoId,
                        situacao: registro.situacao,
                        justificativa: registro.situacao === 'FALTA_JUSTIFICADA' ? registro.justificativa.trim() : null,
                    };
                }),
            };
            const response = await apiFetch(isNova ? '/api/chamadas' : `/api/chamadas/${chamada.chamadaId}`, {
                method: isNova ? 'POST' : 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });
            if (!response.ok) throw new Error(await readApiError(response));

            const salva = (await response.json()) as Chamada;
            setCarregada({ data, chamada: salva });
            setRascunho(rascunhoDe(salva));
            setSuccessMessage(isNova ? 'Chamada registrada com sucesso!' : 'Chamada atualizada com sucesso!');
            setTimeout(() => setSuccessMessage(null), 3000);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao salvar a chamada.');
        } finally {
            setIsSaving(false);
        }
    };

    const handleExcluir = async () => {
        if (!chamada?.chamadaId) return;
        const confirmed = await confirm({
            title: 'Excluir chamada',
            variant: 'danger',
            confirmLabel: 'Excluir',
            message: (
                <>
                    <p>Deseja excluir a chamada de <strong>{formatarData(data)}</strong>?</p>
                    <p>As presenças, faltas e o conteúdo desta aula deixam de contar na frequência. Esta ação não pode ser desfeita.</p>
                </>
            ),
        });
        if (!confirmed) return;

        setIsSaving(true);
        setError(null);
        try {
            const response = await apiFetch(`/api/chamadas/${chamada.chamadaId}`, { method: 'DELETE' });
            if (!response.ok) throw new Error(await readApiError(response));

            setSuccessMessage('Chamada excluída com sucesso!');
            setTimeout(() => setSuccessMessage(null), 3000);
            setRecarregar(n => n + 1);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao excluir a chamada.');
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <div className="chamada-lancamento">
            <div className="chamada-cabecalho">
                <div className="form-group">
                    <label htmlFor="chamada-data">Data da aula</label>
                    <input
                        id="chamada-data"
                        type="date"
                        value={data}
                        min={turma.anoLetivoInicio}
                        max={dataMaxima}
                        onChange={e => void handleDataChange(e.target.value)}
                        disabled={isSaving}
                    />
                </div>
                <div className="form-group">
                    <label htmlFor="chamada-aulas">Quantidade de aulas</label>
                    <input
                        id="chamada-aulas"
                        type="number"
                        min={1}
                        max={MAX_QUANTIDADE_AULAS}
                        value={rascunho?.quantidadeAulas ?? 1}
                        onChange={e => setRascunho(atual => atual && {
                            ...atual,
                            quantidadeAulas: Math.min(MAX_QUANTIDADE_AULAS, Math.max(1, Number(e.target.value) || 1)),
                        })}
                        disabled={somenteLeitura || !rascunho}
                    />
                    <span className="field-hint">Aulas seguidas da disciplina no dia (aula dupla = 2).</span>
                </div>
            </div>

            <FeedbackMessage message={erroCarga?.data === data ? erroCarga.message : null} type="error" />
            <FeedbackMessage message={error} type="error" />
            <FeedbackMessage message={successMessage} type="success" />

            {isLoading ? (
                <div className="loading">Carregando chamada...</div>
            ) : chamada && rascunho && (
                <>
                    <p className="chamada-status">
                        {isNova ? (
                            <>Chamada de <strong>{formatarData(data)}</strong> ainda não registrada.</>
                        ) : (
                            <>
                                Registrada por <strong>{chamada.registradoPor ?? '—'}</strong>
                                {chamada.registradoEm && <> em {formatarDataHora(chamada.registradoEm)}</>}
                                {chamada.atualizadoEm && (
                                    <> · alterada por <strong>{chamada.atualizadoPor ?? '—'}</strong> em {formatarDataHora(chamada.atualizadoEm)}</>
                                )}
                            </>
                        )}
                        {!podeSalvar && <> · Seu perfil pode apenas consultar {isNova ? 'o lançamento' : 'esta chamada'}.</>}
                    </p>

                    <div className="form-group">
                        <label htmlFor="chamada-conteudo">Conteúdo da aula</label>
                        <textarea
                            id="chamada-conteudo"
                            rows={3}
                            maxLength={2000}
                            placeholder="Conteúdo ministrado (ex.: Frações equivalentes — exercícios da página 42)"
                            value={rascunho.conteudo}
                            onChange={e => setRascunho(atual => atual && { ...atual, conteudo: e.target.value })}
                            disabled={somenteLeitura}
                        />
                    </div>

                    {chamada.alunos.length === 0 ? (
                        <EmptyState
                            emptyMessage="Não há alunos enturmados nesta turma nesta data."
                            emptySubMessage="A lista considera as enturmações e remanejamentos vigentes no dia da aula."
                        />
                    ) : (
                        <>
                            <div className="chamada-barra">
                                <div className="chamada-contagem">
                                    <span className="status-pill situacao-pill-presente">{plural(contagem('PRESENTE'), 'presente', 'presentes')}</span>
                                    <span className="status-pill situacao-pill-falta">{plural(contagem('FALTA'), 'falta', 'faltas')}</span>
                                    <span className="status-pill situacao-pill-falta_justificada">{plural(contagem('FALTA_JUSTIFICADA'), 'justificada', 'justificadas')}</span>
                                </div>
                                {!somenteLeitura && (
                                    <button type="button" className="btn btn-secondary btn-sm" onClick={handleTodosPresentes}>
                                        Marcar todos como presentes
                                    </button>
                                )}
                            </div>

                            <div className="table-container">
                                <table className="data-table chamada-table">
                                    <thead>
                                        <tr>
                                            <th>Nº</th>
                                            <th>Matrícula</th>
                                            <th>Aluno</th>
                                            <th>Situação</th>
                                            <th>Justificativa</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {chamada.alunos.map((aluno, index) => {
                                            const registro = rascunho.registros[aluno.alunoId];
                                            return (
                                                <tr key={aluno.alunoId} className={`chamada-linha-${registro.situacao.toLowerCase()}`}>
                                                    <td>{index + 1}</td>
                                                    <td className="nowrap-cell">{aluno.matricula}</td>
                                                    <td>{aluno.nome}</td>
                                                    <td>
                                                        <div className="situacao-toggle" role="radiogroup" aria-label={`Situação de ${aluno.nome}`}>
                                                            {situacoes.map(s => (
                                                                <button
                                                                    key={s.value}
                                                                    type="button"
                                                                    role="radio"
                                                                    aria-checked={registro.situacao === s.value}
                                                                    title={s.label}
                                                                    className={`situacao-btn situacao-${s.value.toLowerCase()}${registro.situacao === s.value ? ' active' : ''}`}
                                                                    onClick={() => setRegistro(aluno.alunoId, { situacao: s.value })}
                                                                    disabled={somenteLeitura}
                                                                >
                                                                    {s.sigla}
                                                                </button>
                                                            ))}
                                                        </div>
                                                    </td>
                                                    <td>
                                                        {registro.situacao === 'FALTA_JUSTIFICADA' && (
                                                            <input
                                                                type="text"
                                                                className="chamada-justificativa"
                                                                maxLength={255}
                                                                placeholder="Motivo (ex.: atestado médico)"
                                                                aria-label={`Justificativa da falta de ${aluno.nome}`}
                                                                value={registro.justificativa}
                                                                onChange={e => setRegistro(aluno.alunoId, { justificativa: e.target.value })}
                                                                disabled={somenteLeitura}
                                                            />
                                                        )}
                                                    </td>
                                                </tr>
                                            );
                                        })}
                                    </tbody>
                                </table>
                            </div>
                        </>
                    )}

                    {(podeSalvar || (!isNova && can('chamada.excluir'))) && (
                        <div className="form-actions">
                            {podeSalvar && chamada.alunos.length > 0 && (
                                <button
                                    type="button"
                                    className="btn btn-primary"
                                    onClick={handleSalvar}
                                    disabled={isSaving || (!isNova && !isDirty)}
                                >
                                    {isSaving ? 'Salvando...' : isNova ? 'Registrar chamada' : 'Salvar alterações'}
                                </button>
                            )}
                            {!isNova && can('chamada.excluir') && (
                                <button type="button" className="btn btn-danger" onClick={handleExcluir} disabled={isSaving}>
                                    Excluir chamada
                                </button>
                            )}
                        </div>
                    )}
                </>
            )}

            {confirmDialog}
        </div>
    );
}

export default LancarChamada;

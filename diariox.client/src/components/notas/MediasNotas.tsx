import { useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import { descreverRegra, formatarNumero, situacaoMediaLabels, type Medias, type NotaTurma, type SituacaoMedia } from './tipos';

interface MediasNotasProps {
    turma: NotaTurma;
    disciplinaId: number;
}

const classeSituacao: Record<SituacaoMedia, string> = {
    SEM_NOTAS: 'status-inactive',
    EM_ANDAMENTO: 'situacao-pill-falta_justificada',
    MEDIA_ATINGIDA: 'situacao-pill-presente',
    ABAIXO_DA_MEDIA: 'situacao-pill-falta',
};

function MediasNotas({ turma, disciplinaId }: MediasNotasProps) {
    const [resultado, setResultado] = useState<{ chave: string; medias: Medias } | null>(null);
    const [erro, setErro] = useState<{ chave: string; message: string } | null>(null);

    const chave = `${turma.turmaId}|${disciplinaId}`;

    useEffect(() => {
        let cancelled = false;
        const chaveAtual = `${turma.turmaId}|${disciplinaId}`;

        async function load() {
            try {
                const params = new URLSearchParams({ turmaId: String(turma.turmaId), disciplinaId: String(disciplinaId) });
                const response = await apiFetch(`/api/notas/medias?${params}`);
                if (!response.ok) throw new Error(await readApiError(response));
                const medias = (await response.json()) as Medias;
                if (!cancelled) setResultado({ chave: chaveAtual, medias });
            } catch (e) {
                if (!cancelled) setErro({ chave: chaveAtual, message: e instanceof Error ? e.message : 'Falha ao calcular as médias.' });
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [turma.turmaId, disciplinaId]);

    const medias = resultado?.chave === chave ? resultado.medias : null;
    const erroAtual = erro?.chave === chave ? erro.message : null;

    if (!medias) {
        return (
            <>
                <FeedbackMessage message={erroAtual} type="error" />
                {!erroAtual && <div className="loading">Calculando médias...</div>}
            </>
        );
    }

    const { regra } = medias;
    const abaixo = medias.alunos.filter(a => a.mediaFinal !== null && a.mediaFinal < regra.mediaAprovacao).length;

    return (
        <>
            <div className="chamada-barra">
                <p className="chamada-status">
                    {descreverRegra(regra)}.
                    {abaixo > 0 && <> <span className="status-pill situacao-pill-falta">{abaixo} abaixo da média</span></>}
                </p>
            </div>

            {medias.alunos.length === 0 ? (
                <EmptyState emptyMessage="Nenhum aluno enturmado nesta turma no ano letivo." />
            ) : (
                <div className="table-container">
                    <table className="data-table notas-table">
                        <thead>
                            <tr>
                                <th>Nº</th>
                                <th>Aluno</th>
                                {medias.periodos.map(p => <th key={p.id} className="nota-coluna-resultado">{p.nome}</th>)}
                                <th className="nota-coluna-resultado">Média</th>
                                <th>Situação</th>
                            </tr>
                        </thead>
                        <tbody>
                            {medias.alunos.map((aluno, i) => (
                                <tr key={aluno.alunoId} className={aluno.situacao === 'ABAIXO_DA_MEDIA' ? 'frequencia-baixa' : undefined}>
                                    <td>{i + 1}</td>
                                    <td>
                                        <span className="nota-aluno-nome">{aluno.nome}</span>
                                        <span className="nota-aluno-meta">{aluno.matricula}{aluno.inativo && <> · inativo</>}</span>
                                    </td>
                                    {aluno.periodos.map(p => (
                                        <td
                                            key={p.periodoId}
                                            className={`nota-coluna-resultado${p.nota !== null && p.nota < regra.mediaAprovacao ? ' texto-alerta' : ''}`}
                                            title={p.pendentes > 0 ? `${p.pendentes} avaliação(ões) sem nota` : undefined}
                                        >
                                            {formatarNumero(p.nota, regra.casasDecimais)}
                                            {p.pendentes > 0 && <span className="nota-pendente-marca" aria-label="com pendências">*</span>}
                                        </td>
                                    ))}
                                    <td className="nota-coluna-resultado">
                                        <strong>{formatarNumero(aluno.mediaFinal, regra.casasDecimais)}</strong>
                                    </td>
                                    <td>
                                        <span className={`status-pill ${classeSituacao[aluno.situacao]}`}>{situacaoMediaLabels[aluno.situacao]}</span>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            <p className="field-hint">
                A média é a média aritmética das notas dos períodos. Enquanto houver período sem nota, ela é parcial
                (situação "Em andamento"). * = período com avaliações ainda sem nota. A situação considera só as notas:
                frequência e recuperação final não entram neste cálculo.
            </p>
        </>
    );
}

export default MediasNotas;

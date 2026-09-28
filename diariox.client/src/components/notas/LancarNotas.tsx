import { useEffect, useState, type FormEvent, type KeyboardEvent } from 'react';
import { useConfirm } from '../../hooks/useConfirm';
import { usePermissoes } from '../../hooks/usePermissoes';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import { abaixoDaMedia, calcularPeriodo, notaMaximaDa } from './calculo';
import {
    descreverRegra, formatarData, formatarNumero, lerNota, tipoAvaliacaoLabel, tiposAvaliacao,
    type Avaliacao, type NotasPeriodo, type NotaTurma, type TipoAvaliacao,
} from './tipos';

interface LancarNotasProps {
    turma: NotaTurma;
    disciplinaId: number;
    periodoId: number;
    onPeriodoChange: (periodoId: number) => void;
}

interface AvaliacaoForm {
    id: number | null;
    nome: string;
    tipo: TipoAvaliacao;
    data: string;
    peso: string;
    valorMaximo: string;
}

/** Texto digitado em cada célula, por "avaliacaoId:alunoId". */
type Rascunho = Record<string, string>;

const chaveCelula = (avaliacaoId: number, alunoId: number) => `${avaliacaoId}:${alunoId}`;

function rascunhoDe(dados: NotasPeriodo): Rascunho {
    const rascunho: Rascunho = {};
    for (const aluno of dados.alunos) {
        for (const avaliacao of dados.avaliacoes) rascunho[chaveCelula(avaliacao.id, aluno.alunoId)] = '';
        for (const nota of aluno.notas) rascunho[chaveCelula(nota.avaliacaoId, aluno.alunoId)] = formatarNumero(nota.valor);
    }
    return rascunho;
}

function LancarNotas({ turma, disciplinaId, periodoId, onPeriodoChange }: LancarNotasProps) {
    const { can } = usePermissoes();
    const { confirm, confirmDialog } = useConfirm();

    // Os dados carregados guardam o período a que se referem; enquanto não batem, está carregando.
    const [carregado, setCarregado] = useState<{ periodoId: number; dados: NotasPeriodo } | null>(null);
    const [erroCarga, setErroCarga] = useState<{ periodoId: number; message: string } | null>(null);
    const [rascunho, setRascunho] = useState<Rascunho>({});
    const [form, setForm] = useState<AvaliacaoForm | null>(null);
    const [recarregar, setRecarregar] = useState(0);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const params = new URLSearchParams({
                    turmaId: String(turma.turmaId), disciplinaId: String(disciplinaId), periodoId: String(periodoId),
                });
                const response = await apiFetch(`/api/notas/periodo?${params}`);
                if (!response.ok) throw new Error(await readApiError(response));
                const dados = (await response.json()) as NotasPeriodo;
                if (cancelled) return;

                setCarregado({ periodoId, dados });
                setRascunho(rascunhoDe(dados));
                setErroCarga(null);
            } catch (e) {
                if (cancelled) return;
                setCarregado(null);
                setErroCarga({ periodoId, message: e instanceof Error ? e.message : 'Falha ao carregar as notas.' });
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [turma.turmaId, disciplinaId, periodoId, recarregar]);

    const dados = carregado?.periodoId === periodoId ? carregado.dados : null;
    const isLoading = !dados && erroCarga?.periodoId !== periodoId;
    const regra = dados?.regra ?? turma.regra;
    const turmaAtiva = dados?.turmaAtiva ?? turma.ativa;
    const podeLancar = can('notas.criar') && turmaAtiva;
    const podeEditarAvaliacao = can('notas.editar') && turmaAtiva;
    const podeExcluirAvaliacao = can('notas.excluir') && turmaAtiva;

    const original = dados ? rascunhoDe(dados) : {};
    const alteradas = Object.keys(rascunho).filter(chave => rascunho[chave] !== original[chave]);
    const isDirty = alteradas.length > 0;

    const valorDigitado = (avaliacaoId: number, alunoId: number) => {
        const valor = lerNota(rascunho[chaveCelula(avaliacaoId, alunoId)] ?? '');
        return Number.isNaN(valor) ? null : valor;
    };

    const celulaInvalida = (avaliacao: Avaliacao, alunoId: number) => {
        const valor = lerNota(rascunho[chaveCelula(avaliacao.id, alunoId)] ?? '');
        return valor !== null && (Number.isNaN(valor) || valor > notaMaximaDa(avaliacao, regra));
    };

    const temInvalida = !!dados && dados.alunos.some(aluno => dados.avaliacoes.some(a => celulaInvalida(a, aluno.alunoId)));

    const limparMensagens = () => {
        setError(null);
        setSuccessMessage(null);
    };

    const mostrarSucesso = (mensagem: string) => {
        setSuccessMessage(mensagem);
        setTimeout(() => setSuccessMessage(null), 3000);
    };

    const handlePeriodoChange = async (valor: string) => {
        const novo = Number(valor);
        if (!novo || novo === periodoId) return;
        if (isDirty && !(await confirm({
            title: 'Descartar alterações',
            variant: 'warning',
            confirmLabel: 'Descartar',
            message: <p>Há notas digitadas e não salvas neste período. Deseja descartá-las?</p>,
        }))) return;

        limparMensagens();
        setForm(null);
        onPeriodoChange(novo);
    };

    // Enter desce para o próximo aluno na mesma avaliação, como numa planilha.
    const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>, coluna: number, linha: number) => {
        if (e.key !== 'Enter') return;
        e.preventDefault();
        const proxima = document.querySelector<HTMLInputElement>(`[data-nota="${coluna}-${linha + (e.shiftKey ? -1 : 1)}"]`);
        proxima?.focus();
        proxima?.select();
    };

    const handleSalvarNotas = async () => {
        if (!dados) return;
        if (temInvalida) {
            setError('Corrija as notas destacadas em vermelho antes de salvar.');
            return;
        }

        setIsSaving(true);
        limparMensagens();
        try {
            const notas = alteradas.map(chave => {
                const [avaliacaoId, alunoId] = chave.split(':').map(Number);
                return { avaliacaoId, alunoId, valor: lerNota(rascunho[chave]) };
            });
            const response = await apiFetch('/api/notas/lancamentos', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ turmaId: turma.turmaId, disciplinaId, periodoAvaliativoId: periodoId, notas }),
            });
            if (!response.ok) throw new Error(await readApiError(response));

            const salvo = (await response.json()) as NotasPeriodo;
            setCarregado({ periodoId, dados: salvo });
            setRascunho(rascunhoDe(salvo));
            mostrarSucesso('Notas salvas com sucesso!');
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao salvar as notas.');
        } finally {
            setIsSaving(false);
        }
    };

    const abrirNovaAvaliacao = () => {
        limparMensagens();
        setForm({ id: null, nome: '', tipo: 'PROVA', data: '', peso: '1', valorMaximo: '' });
    };

    const abrirEdicao = (avaliacao: Avaliacao) => {
        limparMensagens();
        setForm({
            id: avaliacao.id,
            nome: avaliacao.nome,
            tipo: avaliacao.tipo,
            data: avaliacao.data ?? '',
            peso: formatarNumero(avaliacao.peso),
            valorMaximo: avaliacao.valorMaximo === null ? '' : formatarNumero(avaliacao.valorMaximo),
        });
    };

    const handleSalvarAvaliacao = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        if (!form) return;

        if (isDirty && !(await confirm({
            title: 'Notas não salvas',
            variant: 'warning',
            confirmLabel: 'Continuar',
            message: <p>Salvar a avaliação recarrega a tabela e descarta as notas digitadas e não salvas. Deseja continuar?</p>,
        }))) return;

        const peso = lerNota(form.peso);
        const valorMaximo = lerNota(form.valorMaximo);
        if (Number.isNaN(peso) || Number.isNaN(valorMaximo)) {
            setError('Informe números válidos (até duas casas decimais) no peso e nos pontos.');
            return;
        }

        setIsSaving(true);
        limparMensagens();
        try {
            const body = {
                turmaId: turma.turmaId,
                disciplinaId,
                periodoAvaliativoId: periodoId,
                nome: form.nome.trim(),
                tipo: form.tipo,
                data: form.data || null,
                peso,
                valorMaximo,
            };
            const response = await apiFetch(form.id === null ? '/api/notas/avaliacoes' : `/api/notas/avaliacoes/${form.id}`, {
                method: form.id === null ? 'POST' : 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });
            if (!response.ok) throw new Error(await readApiError(response));

            mostrarSucesso(form.id === null ? 'Avaliação cadastrada com sucesso!' : 'Avaliação atualizada com sucesso!');
            setForm(null);
            setRecarregar(n => n + 1);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Falha ao salvar a avaliação.');
        } finally {
            setIsSaving(false);
        }
    };

    const handleExcluirAvaliacao = async (avaliacao: Avaliacao) => {
        const confirmed = await confirm({
            title: 'Excluir avaliação',
            variant: 'danger',
            confirmLabel: 'Excluir',
            message: (
                <>
                    <p>Deseja excluir a avaliação <strong>{avaliacao.nome}</strong>?</p>
                    {avaliacao.notasLancadas > 0 && (
                        <p>As <strong>{avaliacao.notasLancadas}</strong> nota(s) lançada(s) nela também serão excluídas e as notas do período serão recalculadas.</p>
                    )}
                    <p>Esta ação não pode ser desfeita.</p>
                </>
            ),
        });
        if (!confirmed) return;

        setIsSaving(true);
        limparMensagens();
        try {
            const response = await apiFetch(`/api/notas/avaliacoes/${avaliacao.id}`, { method: 'DELETE' });
            if (!response.ok) throw new Error(await readApiError(response));

            mostrarSucesso('Avaliação excluída com sucesso!');
            setRecarregar(n => n + 1);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao excluir a avaliação.');
        } finally {
            setIsSaving(false);
        }
    };

    const periodo = turma.periodos.find(p => p.id === periodoId);
    const temRecuperacao = !!dados?.avaliacoes.some(a => a.recuperacao);
    const pontosDistribuidos = dados?.avaliacoes.filter(a => !a.recuperacao).reduce((soma, a) => soma + (a.valorMaximo ?? 0), 0) ?? 0;
    const formRecuperacao = form?.tipo === 'RECUPERACAO';

    return (
        <div className="chamada-lancamento">
            <div className="chamada-cabecalho">
                <div className="form-group">
                    <label htmlFor="notas-periodo">Período avaliativo</label>
                    <select id="notas-periodo" value={periodoId} onChange={e => void handlePeriodoChange(e.target.value)} disabled={isSaving}>
                        {turma.periodos.map(p => (
                            <option key={p.id} value={p.id}>
                                {p.nome} ({formatarData(p.dataInicio)} a {formatarData(p.dataTermino)})
                            </option>
                        ))}
                    </select>
                </div>
                <div className="regra-resumo" title={regra.id === null ? 'Etapa sem regra própria: usa a regra padrão do sistema.' : undefined}>
                    <span className="regra-resumo-nome">{regra.nome}</span>
                    <span>{descreverRegra(regra)}</span>
                </div>
            </div>

            <FeedbackMessage message={erroCarga?.periodoId === periodoId ? erroCarga.message : null} type="error" />
            <FeedbackMessage message={error} type="error" />
            <FeedbackMessage message={successMessage} type="success" />

            {!turmaAtiva && (
                <div className="aviso-financeiro aviso-info">Turma inativa: as notas ficam disponíveis apenas para consulta.</div>
            )}

            {isLoading ? (
                <div className="loading">Carregando notas...</div>
            ) : dados && (
                <>
                    <div className="chamada-barra">
                        <p className="chamada-status">
                            {dados.avaliacoes.length === 0
                                ? <>Nenhuma avaliação cadastrada em <strong>{periodo?.nome}</strong>.</>
                                : <><strong>{dados.avaliacoes.length}</strong> avaliação(ões) em <strong>{periodo?.nome}</strong>.</>}
                            {regra.calculoNotaPeriodo === 'SOMA' && dados.avaliacoes.length > 0 && (
                                <> Pontos distribuídos: <strong>{formatarNumero(pontosDistribuidos)}</strong> de {formatarNumero(regra.notaMaxima)}.</>
                            )}
                            {!can('notas.criar') && <> · Seu perfil pode apenas consultar as notas.</>}
                        </p>
                        {podeLancar && !form && (
                            <button type="button" className="btn btn-secondary btn-sm" onClick={abrirNovaAvaliacao} disabled={isSaving}>
                                + Nova avaliação
                            </button>
                        )}
                    </div>

                    {form && (
                        <form className="avaliacao-form" onSubmit={handleSalvarAvaliacao}>
                            <strong>{form.id === null ? 'Nova avaliação' : 'Editar avaliação'}</strong>
                            <div className="avaliacao-form-campos">
                                <div className="form-group avaliacao-form-nome">
                                    <label htmlFor="avaliacao-nome">Nome <span className="required">*</span></label>
                                    <input
                                        id="avaliacao-nome"
                                        type="text"
                                        maxLength={100}
                                        placeholder="Ex.: Prova 1"
                                        value={form.nome}
                                        onChange={e => setForm({ ...form, nome: e.target.value })}
                                        autoFocus
                                        required
                                    />
                                </div>
                                <div className="form-group">
                                    <label htmlFor="avaliacao-tipo">Tipo</label>
                                    <select
                                        id="avaliacao-tipo"
                                        value={form.tipo}
                                        onChange={e => setForm({ ...form, tipo: e.target.value as TipoAvaliacao })}
                                    >
                                        {tiposAvaliacao
                                            .filter(t => t.value !== 'RECUPERACAO' || regra.permiteRecuperacao)
                                            .map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                                    </select>
                                </div>
                                <div className="form-group">
                                    <label htmlFor="avaliacao-data">Data</label>
                                    <input
                                        id="avaliacao-data"
                                        type="date"
                                        min={periodo?.dataInicio}
                                        max={periodo?.dataTermino}
                                        value={form.data}
                                        onChange={e => setForm({ ...form, data: e.target.value })}
                                    />
                                </div>
                                {!formRecuperacao && regra.calculoNotaPeriodo === 'MEDIA_PONDERADA' && (
                                    <div className="form-group avaliacao-form-numero">
                                        <label htmlFor="avaliacao-peso">Peso</label>
                                        <input
                                            id="avaliacao-peso"
                                            type="text"
                                            inputMode="decimal"
                                            value={form.peso}
                                            onChange={e => setForm({ ...form, peso: e.target.value })}
                                        />
                                    </div>
                                )}
                                {!formRecuperacao && regra.calculoNotaPeriodo === 'SOMA' && (
                                    <div className="form-group avaliacao-form-numero">
                                        <label htmlFor="avaliacao-pontos">Vale (pontos) <span className="required">*</span></label>
                                        <input
                                            id="avaliacao-pontos"
                                            type="text"
                                            inputMode="decimal"
                                            placeholder="Ex.: 4"
                                            value={form.valorMaximo}
                                            onChange={e => setForm({ ...form, valorMaximo: e.target.value })}
                                            required
                                        />
                                    </div>
                                )}
                            </div>
                            <span className="field-hint">
                                {formRecuperacao
                                    ? 'A recuperação fica fora da média: se a nota dela for maior, passa a ser a nota do período.'
                                    : regra.calculoNotaPeriodo === 'SOMA'
                                        ? `A nota do período é a soma dos pontos (até ${formatarNumero(regra.notaMaxima)}).`
                                        : 'A nota do período é a média das avaliações, ponderada pelos pesos.'}
                            </span>
                            <div className="form-actions">
                                <button type="submit" className="btn btn-primary btn-sm" disabled={isSaving}>
                                    {isSaving ? 'Salvando...' : 'Salvar avaliação'}
                                </button>
                                <button type="button" className="btn btn-secondary btn-sm" onClick={() => setForm(null)} disabled={isSaving}>
                                    Cancelar
                                </button>
                            </div>
                        </form>
                    )}

                    {dados.avaliacoes.length === 0 ? (
                        <EmptyState
                            emptyMessage="Cadastre as avaliações do período para lançar as notas."
                            emptySubMessage={podeLancar ? 'Use "+ Nova avaliação" (prova, trabalho, atividade...).' : undefined}
                        />
                    ) : dados.alunos.length === 0 ? (
                        <EmptyState
                            emptyMessage="Não há alunos enturmados nesta turma no período."
                            emptySubMessage="A lista considera as enturmações e remanejamentos vigentes durante o período."
                        />
                    ) : (
                        <div className="table-container">
                            <table className="data-table notas-table">
                                <thead>
                                    <tr>
                                        <th>Nº</th>
                                        <th>Aluno</th>
                                        {dados.avaliacoes.map(a => (
                                            <th key={a.id} className={`nota-coluna${a.recuperacao ? ' nota-coluna-recuperacao' : ''}`}>
                                                <span className="nota-coluna-nome" title={a.nome}>{a.nome}</span>
                                                <span className="nota-coluna-meta">
                                                    {tipoAvaliacaoLabel(a.tipo)}
                                                    {a.data && <> · {formatarData(a.data)}</>}
                                                    {!a.recuperacao && regra.calculoNotaPeriodo === 'MEDIA_PONDERADA' && a.peso !== 1 && <> · peso {formatarNumero(a.peso)}</>}
                                                    {!a.recuperacao && regra.calculoNotaPeriodo === 'SOMA' && a.valorMaximo !== null && <> · {formatarNumero(a.valorMaximo)} pts</>}
                                                </span>
                                                {(podeEditarAvaliacao || podeExcluirAvaliacao) && (
                                                    <span className="nota-coluna-acoes">
                                                        {podeEditarAvaliacao && (
                                                            <button type="button" className="link-button" onClick={() => abrirEdicao(a)} disabled={isSaving}>Editar</button>
                                                        )}
                                                        {podeExcluirAvaliacao && (
                                                            <button type="button" className="link-button danger" onClick={() => void handleExcluirAvaliacao(a)} disabled={isSaving}>Excluir</button>
                                                        )}
                                                    </span>
                                                )}
                                            </th>
                                        ))}
                                        {temRecuperacao && <th className="nota-coluna-resultado">Média</th>}
                                        <th className="nota-coluna-resultado">Nota do período</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {dados.alunos.map((aluno, linha) => {
                                        const previa = calcularPeriodo(regra, dados.avaliacoes, id => valorDigitado(id, aluno.alunoId));
                                        const abaixo = abaixoDaMedia(regra, previa.nota);
                                        return (
                                            <tr key={aluno.alunoId} className={abaixo ? 'frequencia-baixa' : undefined}>
                                                <td>{linha + 1}</td>
                                                <td>
                                                    <span className="nota-aluno-nome">{aluno.nome}</span>
                                                    <span className="nota-aluno-meta">
                                                        {aluno.matricula}
                                                        {aluno.saiuEm && <> · saiu da turma em {formatarData(aluno.saiuEm)}</>}
                                                        {aluno.inativo && <> · inativo</>}
                                                    </span>
                                                </td>
                                                {dados.avaliacoes.map((a, coluna) => {
                                                    const chave = chaveCelula(a.id, aluno.alunoId);
                                                    const invalida = celulaInvalida(a, aluno.alunoId);
                                                    const alterada = rascunho[chave] !== original[chave];
                                                    return (
                                                        <td key={a.id} className={a.recuperacao ? 'nota-coluna-recuperacao' : undefined}>
                                                            <input
                                                                type="text"
                                                                inputMode="decimal"
                                                                data-nota={`${coluna}-${linha}`}
                                                                className={`nota-input${invalida ? ' nota-input-invalida' : ''}${alterada ? ' nota-input-alterada' : ''}`}
                                                                aria-label={`Nota de ${aluno.nome} em ${a.nome}`}
                                                                title={`De 0 a ${formatarNumero(notaMaximaDa(a, regra))}`}
                                                                value={rascunho[chave] ?? ''}
                                                                onChange={e => {
                                                                    setRascunho(atual => ({ ...atual, [chave]: e.target.value }));
                                                                    setSuccessMessage(null);
                                                                }}
                                                                onKeyDown={e => handleKeyDown(e, coluna, linha)}
                                                                onFocus={e => e.target.select()}
                                                                disabled={!podeLancar || isSaving}
                                                            />
                                                        </td>
                                                    );
                                                })}
                                                {temRecuperacao && (
                                                    <td className="nota-coluna-resultado">{formatarNumero(previa.media, regra.casasDecimais)}</td>
                                                )}
                                                <td className="nota-coluna-resultado">
                                                    {previa.nota === null ? '—' : (
                                                        <span className={`status-pill ${abaixo ? 'situacao-pill-falta' : 'situacao-pill-presente'}`}>
                                                            {formatarNumero(previa.nota, regra.casasDecimais)}
                                                        </span>
                                                    )}
                                                    {previa.pendentes > 0 && (
                                                        <span className="nota-pendentes" title="Avaliações sem nota não entram no cálculo enquanto o período está aberto.">
                                                            {previa.pendentes} pendente{previa.pendentes === 1 ? '' : 's'}
                                                        </span>
                                                    )}
                                                </td>
                                            </tr>
                                        );
                                    })}
                                </tbody>
                            </table>
                        </div>
                    )}

                    {dados.avaliacoes.length > 0 && dados.alunos.length > 0 && (
                        <p className="field-hint">
                            Digite as notas e tecle Enter para ir ao próximo aluno. Deixe em branco o que ainda não foi corrigido:
                            avaliações sem nota não entram no cálculo enquanto o período está aberto.
                            Média para aprovação: <strong>{formatarNumero(regra.mediaAprovacao)}</strong>.
                        </p>
                    )}

                    {podeLancar && dados.avaliacoes.length > 0 && dados.alunos.length > 0 && (
                        <div className="form-actions">
                            <button type="button" className="btn btn-primary" onClick={handleSalvarNotas} disabled={isSaving || !isDirty}>
                                {isSaving ? 'Salvando...' : isDirty ? `Salvar notas (${alteradas.length})` : 'Salvar notas'}
                            </button>
                            {isDirty && (
                                <button type="button" className="btn btn-secondary" onClick={() => { setRascunho(original); limparMensagens(); }} disabled={isSaving}>
                                    Descartar alterações
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

export default LancarNotas;

import { useEffect, useState } from 'react';
import { FiLock } from 'react-icons/fi';
import { useConfirm } from '../../hooks/useConfirm';
import { usePermissoes } from '../../hooks/usePermissoes';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import { formatarData, formatarDataHora, hojeIso } from '../chamada/tipos';
import { MAX_DESCRICAO, type ConteudoMinistrado, type ConteudoTurma, type HabilidadeResumo } from './tipos';

interface RegistrarConteudoProps {
    turma: ConteudoTurma;
    disciplinaId: number;
    data: string;
    onDataChange: (data: string) => void;
}

interface Rascunho {
    descricao: string;
    habilidades: HabilidadeResumo[];
}

function rascunhoDe(conteudo: ConteudoMinistrado): Rascunho {
    return { descricao: conteudo.descricao ?? '', habilidades: conteudo.habilidades };
}

function RegistrarConteudo({ turma, disciplinaId, data, onDataChange }: RegistrarConteudoProps) {
    const { can } = usePermissoes();
    const { confirm, confirmDialog } = useConfirm();

    // O conteúdo carregado guarda a data a que se refere; enquanto não bate com a data atual, está carregando.
    const [carregado, setCarregado] = useState<{ data: string; conteudo: ConteudoMinistrado } | null>(null);
    const [erroCarga, setErroCarga] = useState<{ data: string; message: string } | null>(null);
    const [rascunho, setRascunho] = useState<Rascunho | null>(null);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    const [busca, setBusca] = useState('');
    const [sugestoes, setSugestoes] = useState<HabilidadeResumo[] | null>(null);
    const [erroSugestoes, setErroSugestoes] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const params = new URLSearchParams({ turmaId: String(turma.turmaId), disciplinaId: String(disciplinaId), data });
                const response = await apiFetch(`/api/conteudosministrados/aula?${params}`);
                if (!response.ok) throw new Error(await readApiError(response));
                const conteudo = (await response.json()) as ConteudoMinistrado;
                if (cancelled) return;

                setCarregado({ data, conteudo });
                setRascunho(rascunhoDe(conteudo));
                setErroCarga(null);
            } catch (e) {
                if (cancelled) return;
                setCarregado(null);
                setErroCarga({ data, message: e instanceof Error ? e.message : 'Falha ao carregar o conteúdo.' });
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [turma.turmaId, disciplinaId, data]);

    // RN02: habilidades da BNCC cadastradas para a etapa da turma e a disciplina.
    useEffect(() => {
        let cancelled = false;
        const espera = setTimeout(async () => {
            try {
                const params = new URLSearchParams({ turmaId: String(turma.turmaId), disciplinaId: String(disciplinaId) });
                if (busca.trim()) params.set('busca', busca.trim());
                const response = await apiFetch(`/api/conteudosministrados/sugestoes-bncc?${params}`);
                if (!response.ok) throw new Error(await readApiError(response));
                const lista = (await response.json()) as Omit<HabilidadeResumo, 'ativa'>[];
                if (!cancelled) {
                    setSugestoes(lista.map(h => ({ ...h, ativa: true })));
                    setErroSugestoes(null);
                }
            } catch (e) {
                if (!cancelled) setErroSugestoes(e instanceof Error ? e.message : 'Falha ao carregar as habilidades da BNCC.');
            }
        }, busca ? 300 : 0);

        return () => { cancelled = true; clearTimeout(espera); };
    }, [turma.turmaId, disciplinaId, busca]);

    const conteudo = carregado?.data === data ? carregado.conteudo : null;
    const isLoading = !conteudo && erroCarga?.data !== data;
    const isNovo = conteudo?.conteudoId === null;
    const podeSalvar = conteudo ? (isNovo ? can('conteudo-ministrado.criar') : can('conteudo-ministrado.editar')) : false;
    const somenteLeitura = !podeSalvar || isSaving || !!conteudo?.bloqueio;
    const isDirty = !!conteudo && !!rascunho && JSON.stringify(rascunho) !== JSON.stringify(rascunhoDe(conteudo));

    const hoje = hojeIso();
    const dataMaxima = turma.anoLetivoTermino < hoje ? turma.anoLetivoTermino : hoje;
    const selecionadas = new Set(rascunho?.habilidades.map(h => h.id));

    const handleDataChange = async (novaData: string) => {
        if (!novaData || novaData === data) return;
        if (isDirty && !(await confirm({
            title: 'Descartar alterações',
            variant: 'warning',
            confirmLabel: 'Descartar',
            message: <p>O conteúdo de <strong>{formatarData(data)}</strong> tem alterações não salvas. Deseja descartá-las?</p>,
        }))) return;

        setError(null);
        setSuccessMessage(null);
        onDataChange(novaData);
    };

    const alternarHabilidade = (habilidade: HabilidadeResumo) => {
        setRascunho(atual => atual && {
            ...atual,
            habilidades: atual.habilidades.some(h => h.id === habilidade.id)
                ? atual.habilidades.filter(h => h.id !== habilidade.id)
                : [...atual.habilidades, habilidade],
        });
        setSuccessMessage(null);
    };

    const handleSalvar = async () => {
        if (!conteudo || !rascunho) return;
        if (!rascunho.descricao.trim()) {
            setError('Informe o conteúdo ministrado.');
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
                descricao: rascunho.descricao.trim(),
                habilidadesIds: rascunho.habilidades.map(h => h.id),
            };
            const response = await apiFetch(isNovo ? '/api/conteudosministrados' : `/api/conteudosministrados/${conteudo.conteudoId}`, {
                method: isNovo ? 'POST' : 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });
            if (!response.ok) throw new Error(await readApiError(response));

            const salvo = (await response.json()) as ConteudoMinistrado;
            setCarregado({ data, conteudo: salvo });
            setRascunho(rascunhoDe(salvo));
            setSuccessMessage(isNovo ? 'Conteúdo registrado com sucesso!' : 'Conteúdo atualizado com sucesso!');
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao salvar o conteúdo.');
        } finally {
            setIsSaving(false);
        }
    };

    const handleExcluir = async () => {
        if (!conteudo?.conteudoId) return;
        if (!(await confirm({
            title: 'Excluir conteúdo',
            variant: 'danger',
            confirmLabel: 'Excluir',
            message: <p>Excluir o conteúdo ministrado de <strong>{formatarData(data)}</strong>? Esta ação não pode ser desfeita.</p>,
        }))) return;

        setIsSaving(true);
        setError(null);
        try {
            const response = await apiFetch(`/api/conteudosministrados/${conteudo.conteudoId}`, { method: 'DELETE' });
            if (!response.ok) throw new Error(await readApiError(response));

            const vazio: ConteudoMinistrado = {
                ...conteudo, conteudoId: null, descricao: null, habilidades: [],
                registradoPor: null, registradoEm: null, atualizadoPor: null, atualizadoEm: null,
            };
            setCarregado({ data, conteudo: vazio });
            setRascunho(rascunhoDe(vazio));
            setSuccessMessage('Conteúdo excluído com sucesso!');
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao excluir o conteúdo.');
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <div className="chamada-lancamento">
            <div className="chamada-cabecalho">
                <div className="form-group">
                    <label htmlFor="conteudo-data">Data da aula</label>
                    <input
                        id="conteudo-data"
                        type="date"
                        value={data}
                        min={turma.anoLetivoInicio}
                        max={dataMaxima}
                        onChange={e => void handleDataChange(e.target.value)}
                        disabled={isSaving}
                    />
                </div>
            </div>

            <FeedbackMessage message={erroCarga?.data === data ? erroCarga.message : null} type="error" />
            <FeedbackMessage message={error} type="error" />
            <FeedbackMessage message={successMessage} type="success" />

            {isLoading ? (
                <div className="loading">Carregando conteúdo...</div>
            ) : conteudo?.bloqueio ? (
                // EX01: dia sem aula no Calendário Letivo; o registro fica bloqueado nesta data.
                <>
                    <div className="aviso-financeiro aviso-bloqueio" role="alert">
                        <span><FiLock aria-hidden="true" /> {conteudo.bloqueio}</span>
                    </div>
                    {!isNovo && (
                        <>
                            <p className="chamada-status">
                                Há um conteúdo registrado nesta data antes do bloqueio. Ele não pode ser alterado
                                {can('conteudo-ministrado.excluir') ? ', mas pode ser excluído.' : '.'}
                            </p>
                            {can('conteudo-ministrado.excluir') && (
                                <div className="form-actions">
                                    <button type="button" className="btn btn-danger" onClick={handleExcluir} disabled={isSaving}>
                                        Excluir conteúdo
                                    </button>
                                </div>
                            )}
                        </>
                    )}
                </>
            ) : conteudo && rascunho && (
                <>
                    <p className="chamada-status">
                        {isNovo ? (
                            <>Conteúdo de <strong>{conteudo.disciplinaNome}</strong> em <strong>{formatarData(data)}</strong> ainda não registrado.</>
                        ) : (
                            <>
                                Registrado por <strong>{conteudo.registradoPor ?? '—'}</strong>
                                {conteudo.registradoEm && <> em {formatarDataHora(conteudo.registradoEm)}</>}
                                {conteudo.atualizadoEm && (
                                    <> · alterado por <strong>{conteudo.atualizadoPor ?? '—'}</strong> em {formatarDataHora(conteudo.atualizadoEm)}</>
                                )}
                            </>
                        )}
                        {!podeSalvar && <> · Seu perfil pode apenas consultar {isNovo ? 'o registro' : 'este conteúdo'}.</>}
                    </p>

                    <div className="form-group">
                        <label htmlFor="conteudo-descricao">Conteúdo ministrado</label>
                        <textarea
                            id="conteudo-descricao"
                            rows={4}
                            maxLength={MAX_DESCRICAO}
                            placeholder="Ex.: Frações equivalentes — exercícios da página 42"
                            value={rascunho.descricao}
                            onChange={e => setRascunho(atual => atual && { ...atual, descricao: e.target.value })}
                            disabled={somenteLeitura}
                        />
                    </div>

                    <div className="form-group">
                        <label htmlFor="conteudo-bncc-busca">Habilidades da BNCC</label>
                        {rascunho.habilidades.length > 0 && (
                            <div className="anos-list" aria-label="Habilidades selecionadas">
                                {rascunho.habilidades.map(h => (
                                    <span key={h.id} className="ano-tag bncc-selecionada" title={h.descricao}>
                                        {h.codigo}
                                        {!h.ativa && ' (inativa)'}
                                        {!somenteLeitura && (
                                            <button
                                                type="button"
                                                className="bncc-remover"
                                                aria-label={`Remover ${h.codigo}`}
                                                onClick={() => alternarHabilidade(h)}
                                            >
                                                ×
                                            </button>
                                        )}
                                    </span>
                                ))}
                            </div>
                        )}
                        {!somenteLeitura && (
                            <>
                                <input
                                    id="conteudo-bncc-busca"
                                    type="search"
                                    placeholder="Buscar por código ou descrição (ex.: EF06MA01, frações)"
                                    value={busca}
                                    onChange={e => setBusca(e.target.value)}
                                />
                                <span className="field-hint">
                                    Sugestões da BNCC cadastradas para a etapa da turma e a disciplina {conteudo.disciplinaNome}.
                                </span>
                                <FeedbackMessage message={erroSugestoes} type="error" />
                                {sugestoes === null ? (
                                    <span className="field-hint">Carregando sugestões...</span>
                                ) : sugestoes.length === 0 ? (
                                    <span className="field-hint">
                                        {busca.trim()
                                            ? 'Nenhuma habilidade encontrada para a busca.'
                                            : 'Nenhuma habilidade da BNCC cadastrada para esta etapa e disciplina.'}
                                    </span>
                                ) : (
                                    <ul className="bncc-sugestoes">
                                        {sugestoes.map(h => (
                                            <li key={h.id}>
                                                <label className="checkbox-field">
                                                    <input
                                                        type="checkbox"
                                                        checked={selecionadas.has(h.id)}
                                                        onChange={() => alternarHabilidade(h)}
                                                    />
                                                    <span><strong>{h.codigo}</strong> — {h.descricao}</span>
                                                </label>
                                            </li>
                                        ))}
                                    </ul>
                                )}
                            </>
                        )}
                    </div>

                    {podeSalvar && (
                        <div className="form-actions">
                            <button type="button" onClick={handleSalvar} disabled={isSaving || (!isNovo && !isDirty)}>
                                {isNovo ? 'Registrar conteúdo' : 'Atualizar conteúdo'}
                            </button>
                            {!isNovo && can('conteudo-ministrado.excluir') && (
                                <button type="button" className="btn btn-danger" onClick={handleExcluir} disabled={isSaving}>
                                    Excluir conteúdo
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

export default RegistrarConteudo;

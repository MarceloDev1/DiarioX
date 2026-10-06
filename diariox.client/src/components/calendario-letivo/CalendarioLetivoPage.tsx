import { useCallback, useEffect, useMemo, useState } from 'react';
import { FiChevronLeft, FiChevronRight, FiLock, FiUsers } from 'react-icons/fi';
import './CalendarioLetivo.css';
import { useConfirm } from '../../hooks/useConfirm';
import { usePermissoes } from '../../hooks/usePermissoes';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import GradeMes from './GradeMes';
import PainelEvento, { type EventoForm } from './PainelEvento';
import {
    CORES_PERIODOS, NOMES_MESES, formatarData, formatarDataHora, mesesEntre,
    type CalendarioLetivo, type CalendarioOpcoes, type CalendarioResposta, type EventoCalendario,
} from './tipos';

type Visao = 'anual' | 'mensal';
type Selecao = { de: string; ate: string };

const REDE = 'rede';

function hojeIso(): string {
    const hoje = new Date();
    return `${hoje.getFullYear()}-${String(hoje.getMonth() + 1).padStart(2, '0')}-${String(hoje.getDate()).padStart(2, '0')}`;
}

const ordenar = (a: string, b: string): Selecao => (a <= b ? { de: a, ate: b } : { de: b, ate: a });

/** RF005A: configuração visual do Calendário Letivo da rede ou de uma escola. */
function CalendarioLetivoPage() {
    const { can } = usePermissoes();
    const { confirm, confirmDialog } = useConfirm();
    const hoje = hojeIso();

    const [opcoes, setOpcoes] = useState<CalendarioOpcoes | null>(null);
    const [anoLetivoId, setAnoLetivoId] = useState<number | null>(null);
    const [escola, setEscola] = useState<string>(REDE);
    const [calendario, setCalendario] = useState<CalendarioLetivo | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    const [visao, setVisao] = useState<Visao>('anual');
    const [mesAberto, setMesAberto] = useState<{ ano: number; mes: number } | null>(null);

    // Seleção por arrasto (ou Shift+clique a partir da âncora); ao soltar, abre o painel.
    const [arrasto, setArrasto] = useState<{ inicio: string; fim: string } | null>(null);
    const [ancora, setAncora] = useState<string | null>(null);
    const [selecao, setSelecao] = useState<Selecao | null>(null);
    const [aberturaPainel, setAberturaPainel] = useState(0);
    const [isSaving, setIsSaving] = useState(false);
    const [erroPainel, setErroPainel] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const response = await apiFetch('/api/calendarioletivo/opcoes');
                if (!response.ok) throw new Error(await readApiError(response));
                const carregadas = (await response.json()) as CalendarioOpcoes;
                if (cancelled) return;

                setOpcoes(carregadas);
                // Começa pelo ano letivo vigente (ou o mais recente) e, para quem só edita a escola, pela escola.
                const vigente = carregadas.anosLetivos.find(a => a.dataInicio <= hoje && a.dataTermino >= hoje)
                    ?? carregadas.anosLetivos[0];
                setAnoLetivoId(vigente?.id ?? null);
                if (!carregadas.podeEditarRede && carregadas.escolas.length > 0) setEscola(String(carregadas.escolas[0].id));
                if (!vigente) setIsLoading(false);
            } catch (e) {
                if (!cancelled) {
                    setError(e instanceof Error ? e.message : 'Falha ao carregar os anos letivos.');
                    setIsLoading(false);
                }
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [hoje]);

    useEffect(() => {
        if (anoLetivoId === null) return;
        let cancelled = false;

        async function load() {
            setIsLoading(true);
            try {
                const params = new URLSearchParams({ anoLetivoId: String(anoLetivoId) });
                if (escola !== REDE) params.set('escolaId', escola);
                const response = await apiFetch(`/api/calendarioletivo?${params}`);
                if (!response.ok) throw new Error(await readApiError(response));
                const { calendario: carregado } = (await response.json()) as CalendarioResposta;
                if (cancelled) return;

                setCalendario(carregado);
                setError(null);
                setMesAberto(atual => {
                    if (atual && mesesEntre(carregado.dataInicio, carregado.dataTermino)
                        .some(m => m.ano === atual.ano && m.mes === atual.mes)) return atual;
                    const referencia = hoje >= carregado.dataInicio && hoje <= carregado.dataTermino ? hoje : carregado.dataInicio;
                    return { ano: Number(referencia.slice(0, 4)), mes: Number(referencia.slice(5, 7)) - 1 };
                });
            } catch (e) {
                if (!cancelled) {
                    setCalendario(null);
                    setError(e instanceof Error ? e.message : 'Falha ao carregar o calendário.');
                }
            } finally {
                if (!cancelled) setIsLoading(false);
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [anoLetivoId, escola, hoje]);

    const eventos = useMemo(
        () => new Map<string, EventoCalendario>((calendario?.eventos ?? []).map(e => [e.data, e])),
        [calendario],
    );

    const podeEditar = !!calendario && calendario.podeEditar && can('calendario-letivo.editar');
    const meses = calendario ? mesesEntre(calendario.dataInicio, calendario.dataTermino) : [];
    const indiceMes = mesAberto ? meses.findIndex(m => m.ano === mesAberto.ano && m.mes === mesAberto.mes) : -1;

    const abrirPainel = useCallback((nova: Selecao) => {
        setSelecao(nova);
        setAberturaPainel(n => n + 1);
        setErroPainel(null);
        setSuccessMessage(null);
    }, []);

    const fecharPainel = useCallback(() => {
        setSelecao(null);
        setErroPainel(null);
    }, []);

    // Solta o arrasto em qualquer ponto da tela.
    useEffect(() => {
        if (!arrasto) return;
        const finalizar = () => {
            setArrasto(null);
            setAncora(arrasto.inicio);
            abrirPainel(ordenar(arrasto.inicio, arrasto.fim));
        };
        window.addEventListener('pointerup', finalizar);
        return () => window.removeEventListener('pointerup', finalizar);
    }, [arrasto, abrirPainel]);

    const handleDiaPointerDown = (data: string, shift: boolean) => {
        if (shift && ancora && podeEditar) {
            abrirPainel(ordenar(ancora, data));
            return;
        }
        // Quem só consulta vê os detalhes do dia; arrastar só faz sentido para quem edita.
        if (!podeEditar) {
            abrirPainel({ de: data, ate: data });
            return;
        }
        setArrasto({ inicio: data, fim: data });
    };

    const handleDiaPointerEnter = (data: string) => {
        setArrasto(atual => (atual ? { ...atual, fim: data } : atual));
    };

    const handleDiaTeclado = (data: string, shift: boolean) => {
        if (shift && ancora && podeEditar) {
            abrirPainel(ordenar(ancora, data));
        } else {
            setAncora(data);
            abrirPainel({ de: data, ate: data });
        }
    };

    const aplicarResposta = async (response: Response) => {
        if (!response.ok) throw new Error(await readApiError(response));
        const resposta = (await response.json()) as CalendarioResposta;
        setCalendario(resposta.calendario);
        setSuccessMessage(resposta.message);
        setTimeout(() => setSuccessMessage(atual => (atual === resposta.message ? null : atual)), 4000);
    };

    const handleSalvar = async (form: EventoForm) => {
        if (!calendario || !selecao) return;
        setIsSaving(true);
        setErroPainel(null);
        try {
            const response = await apiFetch('/api/calendarioletivo/eventos', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    anoLetivoId: calendario.anoLetivoId,
                    escolaId: calendario.escolaId,
                    dataInicio: selecao.de,
                    dataFim: selecao.ate,
                    tipo: form.tipo,
                    descricao: form.descricao.trim(),
                    comAula: form.comAula,
                }),
            });
            await aplicarResposta(response);
            setSelecao(null);
        } catch (e) {
            setErroPainel(e instanceof Error ? e.message : 'Falha ao salvar o evento.');
        } finally {
            setIsSaving(false);
        }
    };

    const handleRemover = async () => {
        if (!calendario || !selecao) return;
        const confirmed = await confirm({
            title: 'Remover evento',
            variant: 'danger',
            confirmLabel: 'Remover',
            message: selecao.de === selecao.ate
                ? <p>Remover o evento de <strong>{formatarData(selecao.de)}</strong>? O dia volta a seguir o padrão do calendário.</p>
                : <p>Remover os eventos de <strong>{formatarData(selecao.de)}</strong> a <strong>{formatarData(selecao.ate)}</strong>? Os dias voltam a seguir o padrão do calendário.</p>,
        });
        if (!confirmed) return;

        setIsSaving(true);
        setErroPainel(null);
        try {
            const params = new URLSearchParams({ anoLetivoId: String(calendario.anoLetivoId), de: selecao.de, ate: selecao.ate });
            if (calendario.escolaId !== null) params.set('escolaId', String(calendario.escolaId));
            const response = await apiFetch(`/api/calendarioletivo/eventos?${params}`, { method: 'DELETE' });
            await aplicarResposta(response);
            setSelecao(null);
        } catch (e) {
            setErroPainel(e instanceof Error ? e.message : 'Falha ao remover os eventos.');
        } finally {
            setIsSaving(false);
        }
    };

    const handlePublicar = async () => {
        if (!calendario) return;
        const abaixoDaMeta = calendario.diasLetivos < calendario.metaDiasLetivos;
        const confirmed = await confirm({
            title: 'Publicar Calendário Letivo',
            variant: abaixoDaMeta ? 'warning' : 'success',
            confirmLabel: 'Publicar',
            message: (
                <>
                    <p>Publicar o calendário de <strong>{calendario.anoReferencia}</strong> — {calendario.escolaNome ?? 'Rede / Geral (todas as escolas)'}?</p>
                    <p>A partir da publicação, os dias sem aula (e fins de semana sem sábado letivo) bloqueiam a frequência e o conteúdo no diário de classe. Alterações posteriores valem imediatamente.</p>
                    {abaixoDaMeta && (
                        <p><strong>Atenção:</strong> o calendário tem {calendario.diasLetivos} dias letivos, abaixo da meta mínima de {calendario.metaDiasLetivos}.</p>
                    )}
                </>
            ),
        });
        if (!confirmed) return;

        setIsSaving(true);
        setError(null);
        try {
            const response = await apiFetch('/api/calendarioletivo/publicar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ anoLetivoId: calendario.anoLetivoId, escolaId: calendario.escolaId }),
            });
            await aplicarResposta(response);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao publicar o calendário.');
        } finally {
            setIsSaving(false);
        }
    };

    const selecaoVisivel = arrasto ? ordenar(arrasto.inicio, arrasto.fim) : selecao;
    const gradeProps = calendario && {
        inicioAno: calendario.dataInicio,
        terminoAno: calendario.dataTermino,
        hoje,
        eventos,
        periodos: calendario.periodos,
        selecao: selecaoVisivel,
        onDiaPointerDown: handleDiaPointerDown,
        onDiaPointerEnter: handleDiaPointerEnter,
        onDiaTeclado: handleDiaTeclado,
    };

    const percentual = calendario ? Math.min(100, Math.round((calendario.diasLetivos / calendario.metaDiasLetivos) * 100)) : 0;
    const metaAtingida = !!calendario && calendario.diasLetivos >= calendario.metaDiasLetivos;

    return (
        <div className="school-page">
            <div className="content-card cal-pagina">
                <div className="section-header">
                    <div>
                        <h2>Calendário Letivo</h2>
                        <p>
                            Marque feriados, recessos, conselhos de classe e sábados letivos. Depois de publicado, o calendário
                            trava os lançamentos do diário de classe nos dias sem aula.
                        </p>
                    </div>
                    {podeEditar && (
                        <button type="button" className="primary-button" onClick={handlePublicar} disabled={isSaving || isLoading}>
                            Publicar Calendário
                        </button>
                    )}
                </div>

                {opcoes && opcoes.anosLetivos.length > 0 && (
                    <div className="cal-filtros">
                        <div className="form-group">
                            <label htmlFor="cal-ano">Ano letivo</label>
                            <select id="cal-ano" value={anoLetivoId ?? ''} onChange={e => setAnoLetivoId(Number(e.target.value))}>
                                {opcoes.anosLetivos.map(a => <option key={a.id} value={a.id}>{a.anoReferencia}</option>)}
                            </select>
                        </div>
                        <div className="form-group">
                            <label htmlFor="cal-escola">Escola</label>
                            <select id="cal-escola" value={escola} onChange={e => setEscola(e.target.value)}>
                                <option value={REDE}>Rede / Geral (todas as escolas)</option>
                                {opcoes.escolas.map(e => <option key={e.id} value={e.id}>{e.nome}</option>)}
                            </select>
                        </div>
                    </div>
                )}

                <FeedbackMessage message={error} type="error" />
                <FeedbackMessage message={successMessage} type="success" />

                {opcoes && opcoes.anosLetivos.length === 0 ? (
                    <EmptyState
                        emptyMessage="Nenhum ano letivo cadastrado."
                        emptySubMessage="Cadastre o ano letivo, com as datas e os períodos avaliativos, em Cadastros › Anos Letivos."
                    />
                ) : isLoading && !calendario ? (
                    <EmptyState loading loadingMessage="Carregando calendário..." emptyMessage="" />
                ) : calendario && gradeProps && (
                    <>
                        <div className="cal-resumo">
                            <div className={`cal-contador${metaAtingida ? ' cal-contador-ok' : ''}`} aria-live="polite">
                                <div className="cal-contador-numeros">
                                    <strong>{calendario.diasLetivos}</strong>
                                    <span>dias letivos cadastrados · meta mínima {calendario.metaDiasLetivos}</span>
                                </div>
                                <div className="cal-contador-barra" role="progressbar" aria-valuemin={0} aria-valuemax={calendario.metaDiasLetivos}
                                    aria-valuenow={calendario.diasLetivos} aria-label="Dias letivos em relação à meta mínima">
                                    <span style={{ width: `${percentual}%` }} />
                                </div>
                                <span className="cal-contador-texto">
                                    {metaAtingida
                                        ? `${calendario.metaDiasLetivos} dias letivos atingidos`
                                        : `Faltam ${calendario.metaDiasLetivos - calendario.diasLetivos} dias para a meta mínima`}
                                </span>
                            </div>

                            <div className="cal-status">
                                {calendario.publicado ? (
                                    <span className="status-pill status-active">Publicado em {formatarDataHora(calendario.publicadoEm!)}</span>
                                ) : (
                                    <span className="status-pill status-inactive">Rascunho — ainda não trava o diário</span>
                                )}
                                <span className="cal-status-texto">
                                    {calendario.escolaId === null
                                        ? 'Vale para todas as escolas; cada escola pode ter eventos próprios.'
                                        : calendario.redePublicada
                                            ? 'Inclui os eventos do calendário da rede; os desta escola prevalecem no mesmo dia.'
                                            : 'O calendário da rede ainda não foi publicado: só os eventos desta escola aparecem.'}
                                </span>
                                <span className="cal-status-texto">
                                    Ano letivo de {formatarData(calendario.dataInicio)} a {formatarData(calendario.dataTermino)}.
                                </span>
                            </div>
                        </div>

                        <div className="cal-legenda" aria-label="Legenda">
                            <span className="cal-legenda-item"><i className="cal-cor cal-cor-letivo" />Dia letivo</span>
                            <span className="cal-legenda-item"><i className="cal-cor cal-cor-fim-de-semana" />Fim de semana</span>
                            <span className="cal-legenda-item"><i className="cal-cor cal-cor-sem-aula" />Feriado / recesso / ponto facultativo</span>
                            <span className="cal-legenda-item"><i className="cal-cor cal-cor-pedagogico"><FiUsers /></i>Conselho de classe / plantão / formação</span>
                            <span className="cal-legenda-item"><i className="cal-cor cal-cor-sabado-letivo" />Sábado letivo</span>
                            <span className="cal-legenda-item"><FiLock className="cal-legenda-icone" />Sem aula (bloqueia o diário)</span>
                            {calendario.periodos.map((p, i) => (
                                <span key={p.id} className="cal-legenda-item">
                                    <i className="cal-cor cal-cor-periodo" style={{ background: CORES_PERIODOS[i % CORES_PERIODOS.length] }} />
                                    {p.nome}: {formatarData(p.dataInicio)} a {formatarData(p.dataTermino)}
                                </span>
                            ))}
                        </div>

                        <div className="cal-barra">
                            <div className="cal-visao" role="tablist" aria-label="Visualização">
                                {(['anual', 'mensal'] as Visao[]).map(v => (
                                    <button key={v} type="button" role="tab" aria-selected={visao === v}
                                        className={`form-tab${visao === v ? ' active' : ''}`} onClick={() => setVisao(v)}>
                                        {v === 'anual' ? 'Anual' : 'Mensal'}
                                    </button>
                                ))}
                            </div>
                            {visao === 'mensal' && mesAberto && (
                                <div className="cal-navegacao">
                                    <button type="button" className="btn btn-secondary btn-sm" aria-label="Mês anterior"
                                        disabled={indiceMes <= 0} onClick={() => setMesAberto(meses[indiceMes - 1])}>
                                        <FiChevronLeft />
                                    </button>
                                    <strong>{NOMES_MESES[mesAberto.mes]} de {mesAberto.ano}</strong>
                                    <button type="button" className="btn btn-secondary btn-sm" aria-label="Próximo mês"
                                        disabled={indiceMes < 0 || indiceMes >= meses.length - 1} onClick={() => setMesAberto(meses[indiceMes + 1])}>
                                        <FiChevronRight />
                                    </button>
                                </div>
                            )}
                            <span className="field-hint">
                                {podeEditar
                                    ? 'Clique em um dia para cadastrar um evento. Para um intervalo, arraste sobre os dias ou use Shift+clique no último dia.'
                                    : 'Clique em um dia para ver os detalhes. Seu perfil pode apenas consultar este calendário.'}
                            </span>
                        </div>

                        <div className={visao === 'anual' ? 'cal-ano' : 'cal-mensal'}>
                            {visao === 'anual'
                                ? meses.map(m => <GradeMes key={`${m.ano}-${m.mes}`} ano={m.ano} mes={m.mes} compacto {...gradeProps} />)
                                : mesAberto && <GradeMes ano={mesAberto.ano} mes={mesAberto.mes} compacto={false} {...gradeProps} />}
                        </div>
                    </>
                )}
            </div>

            {calendario && selecao && (
                <PainelEvento
                    key={aberturaPainel}
                    calendario={calendario}
                    selecao={selecao}
                    eventos={eventos}
                    podeEditar={podeEditar}
                    isSaving={isSaving}
                    error={erroPainel}
                    onSelecaoChange={setSelecao}
                    onSalvar={handleSalvar}
                    onRemover={handleRemover}
                    onFechar={fecharPainel}
                />
            )}
            {confirmDialog}
        </div>
    );
}

export default CalendarioLetivoPage;

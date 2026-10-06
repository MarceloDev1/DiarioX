import { useEffect, useRef, useState, type FormEvent } from 'react';
import { FiX } from 'react-icons/fi';
import FeedbackMessage from '../ui/FeedbackMessage';
import {
    adicionarDias, descreverDia, diasNoIntervalo, fimDeSemana, formatarData, nomeDiaDaSemana, tiposEvento,
    type CalendarioLetivo, type EventoCalendario, type TipoEvento,
} from './tipos';

export interface EventoForm {
    tipo: TipoEvento | '';
    descricao: string;
    comAula: boolean | null;
}

interface PainelEventoProps {
    calendario: CalendarioLetivo;
    selecao: { de: string; ate: string };
    eventos: Map<string, EventoCalendario>;
    podeEditar: boolean;
    isSaving: boolean;
    error: string | null;
    onSelecaoChange: (selecao: { de: string; ate: string }) => void;
    onSalvar: (form: EventoForm) => void;
    onRemover: () => void;
    onFechar: () => void;
}

const MAX_DESCRICAO = 150;

function* diasDe(de: string, ate: string) {
    for (let dia = de; dia <= ate; dia = adicionarDias(dia, 1)) yield dia;
}

/** Evento igual em todos os dias da seleção (ex.: um recesso já cadastrado em lote), para editar de uma vez. */
function eventoComum(eventos: Map<string, EventoCalendario>, de: string, ate: string): EventoCalendario | null {
    let comum: EventoCalendario | null = null;
    for (const dia of diasDe(de, ate)) {
        const evento = eventos.get(dia);
        if (!evento) return null;
        if (!comum) comum = evento;
        else if (evento.tipo !== comum.tipo || evento.descricao !== comum.descricao || evento.comAula !== comum.comAula) return null;
    }
    return comum;
}

function formInicial(eventos: Map<string, EventoCalendario>, de: string, ate: string): EventoForm {
    const comum = eventoComum(eventos, de, ate);
    if (comum) return { tipo: comum.tipo, descricao: comum.descricao, comAula: comum.comAula };

    // Alternativa 02: um sábado (ou domingo) sem evento costuma virar Sábado Letivo.
    if (de === ate && fimDeSemana(de)) return { tipo: 'SABADO_LETIVO', descricao: '', comAula: true };
    return { tipo: '', descricao: '', comAula: null };
}

/** Painel lateral de edição de um dia ou de um intervalo de dias (fluxo principal e Alternativa 01). */
function PainelEvento({
    calendario, selecao, eventos, podeEditar, isSaving, error, onSelecaoChange, onSalvar, onRemover, onFechar,
}: PainelEventoProps) {
    const [form, setForm] = useState<EventoForm>(() => formInicial(eventos, selecao.de, selecao.ate));
    const [erroLocal, setErroLocal] = useState<string | null>(null);
    const primeiroCampo = useRef<HTMLSelectElement>(null);

    useEffect(() => {
        primeiroCampo.current?.focus();
        const onKeyDown = (e: KeyboardEvent) => {
            if (e.key === 'Escape') onFechar();
        };
        window.addEventListener('keydown', onKeyDown);
        return () => window.removeEventListener('keydown', onKeyDown);
    }, [onFechar]);

    const { de, ate } = selecao;
    const unicoDia = de === ate;
    const totalDias = diasNoIntervalo(de, ate);
    const dias = [...diasDe(de, ate)];
    const proprios = dias.filter(d => eventos.get(d) && !eventos.get(d)!.herdado).length;
    const herdados = dias.filter(d => eventos.get(d)?.herdado).length;
    const eventoDoDia = unicoDia ? eventos.get(de) : undefined;

    const handleTipo = (tipo: TipoEvento | '') => {
        const sugestao = tiposEvento.find(t => t.value === tipo)?.comAulaSugerido;
        setForm(f => ({ ...f, tipo, comAula: sugestao ?? f.comAula }));
        setErroLocal(null);
    };

    const handleSubmit = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        if (de < calendario.dataInicio || ate > calendario.dataTermino) {
            setErroLocal('A data selecionada está fora do período do Ano Letivo configurado.');
            return;
        }
        if (!form.tipo) {
            setErroLocal('Selecione o tipo do evento.');
            return;
        }
        if (!form.descricao.trim()) {
            setErroLocal('Informe a descrição (nome) do evento.');
            return;
        }
        if (form.comAula === null) {
            setErroLocal('Informe se o dia é considerado letivo (com aula) ou sem aula.');
            return;
        }
        setErroLocal(null);
        onSalvar(form);
    };

    const diaDaSemana = nomeDiaDaSemana(de);
    const titulo = unicoDia
        ? <>{diaDaSemana[0].toUpperCase() + diaDaSemana.slice(1)}, <strong>{formatarData(de)}</strong></>
        : <><strong>{formatarData(de)}</strong> a <strong>{formatarData(ate)}</strong> · {totalDias} dias</>;

    return (
        <div className="cal-painel-fundo" onPointerDown={e => { if (e.target === e.currentTarget) onFechar(); }}>
            <aside className="cal-painel" role="dialog" aria-modal="true" aria-labelledby="cal-painel-titulo">
                <header className="cal-painel-cabecalho">
                    <div>
                        <h3 id="cal-painel-titulo">{podeEditar ? (unicoDia ? 'Evento do dia' : 'Evento em lote') : 'Dia do calendário'}</h3>
                        <p>{titulo}</p>
                    </div>
                    <button type="button" className="cal-painel-fechar" onClick={onFechar} aria-label="Fechar painel">
                        <FiX />
                    </button>
                </header>

                <div className="cal-painel-situacao">
                    {unicoDia ? (
                        <span>{descreverDia(de, eventoDoDia)}</span>
                    ) : (
                        <>
                            {proprios > 0 && <span>{proprios} dia(s) já têm evento neste calendário e serão substituídos.</span>}
                            {proprios === 0 && <span>Nenhum dia do intervalo tem evento neste calendário.</span>}
                        </>
                    )}
                    {herdados > 0 && podeEditar && (
                        <span className="cal-painel-herdado">
                            {herdados === 1 && unicoDia ? 'Evento' : `${herdados} dia(s) com evento`} do calendário da rede: salvar aqui
                            substitui o evento da rede apenas para esta escola.
                        </span>
                    )}
                </div>

                {podeEditar && (
                    <form className="cal-painel-form" onSubmit={handleSubmit} noValidate>
                        <div className="cal-painel-datas">
                            <div className="form-group">
                                <label htmlFor="cal-de">De</label>
                                <input
                                    id="cal-de"
                                    type="date"
                                    value={de}
                                    min={calendario.dataInicio}
                                    max={calendario.dataTermino}
                                    onChange={e => e.target.value && onSelecaoChange({ de: e.target.value, ate: e.target.value > ate ? e.target.value : ate })}
                                    disabled={isSaving}
                                />
                            </div>
                            <div className="form-group">
                                <label htmlFor="cal-ate">Até</label>
                                <input
                                    id="cal-ate"
                                    type="date"
                                    value={ate}
                                    min={de}
                                    max={calendario.dataTermino}
                                    onChange={e => e.target.value && onSelecaoChange({ de: e.target.value < de ? e.target.value : de, ate: e.target.value })}
                                    disabled={isSaving}
                                />
                            </div>
                        </div>

                        <div className="form-group">
                            <label htmlFor="cal-tipo">Tipo de evento <span className="required">*</span></label>
                            <select
                                id="cal-tipo"
                                ref={primeiroCampo}
                                value={form.tipo}
                                onChange={e => handleTipo(e.target.value as TipoEvento | '')}
                                disabled={isSaving}
                                required
                            >
                                <option value="">Selecione...</option>
                                {tiposEvento.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                            </select>
                        </div>

                        <div className="form-group">
                            <label htmlFor="cal-descricao">Descrição / nome do evento <span className="required">*</span></label>
                            <input
                                id="cal-descricao"
                                type="text"
                                maxLength={MAX_DESCRICAO}
                                placeholder="Ex.: Paixão de Cristo, I Conselho de Classe, Reunião de Pais"
                                value={form.descricao}
                                onChange={e => { setForm(f => ({ ...f, descricao: e.target.value })); setErroLocal(null); }}
                                disabled={isSaving}
                                required
                            />
                        </div>

                        <fieldset className="cal-painel-aula">
                            <legend>Considerado dia letivo / com aula? <span className="required">*</span></legend>
                            <label className={`cal-opcao-aula${form.comAula === true ? ' ativa cal-opcao-sim' : ''}`}>
                                <input
                                    type="radio"
                                    name="cal-com-aula"
                                    checked={form.comAula === true}
                                    onChange={() => { setForm(f => ({ ...f, comAula: true })); setErroLocal(null); }}
                                    disabled={isSaving}
                                />
                                Sim (permite registro de aula)
                            </label>
                            <label className={`cal-opcao-aula${form.comAula === false ? ' ativa cal-opcao-nao' : ''}`}>
                                <input
                                    type="radio"
                                    name="cal-com-aula"
                                    checked={form.comAula === false}
                                    onChange={() => { setForm(f => ({ ...f, comAula: false })); setErroLocal(null); }}
                                    disabled={isSaving}
                                />
                                Não (bloqueia registro de aula)
                            </label>
                        </fieldset>

                        <FeedbackMessage message={erroLocal ?? error} type="error" />

                        <div className="cal-painel-acoes">
                            <button type="submit" className="btn btn-primary" disabled={isSaving}>
                                {isSaving ? 'Salvando...' : 'Salvar Evento'}
                            </button>
                            {proprios > 0 && (
                                <button type="button" className="btn btn-danger" onClick={onRemover} disabled={isSaving}>
                                    {proprios === 1 ? 'Remover evento' : `Remover ${proprios} eventos`}
                                </button>
                            )}
                            <button type="button" className="btn btn-secondary" onClick={onFechar} disabled={isSaving}>
                                Cancelar
                            </button>
                        </div>
                    </form>
                )}
            </aside>
        </div>
    );
}

export default PainelEvento;

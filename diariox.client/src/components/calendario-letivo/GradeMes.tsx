import type { PointerEvent, MouseEvent } from 'react';
import { FiLock, FiUsers } from 'react-icons/fi';
import {
    CORES_PERIODOS, DIAS_SEMANA_CURTOS, NOMES_MESES, categoriaDoDia, descreverDia, formatarData, semanasDoMes,
    type EventoCalendario, type PeriodoCalendario,
} from './tipos';

interface GradeMesProps {
    ano: number;
    mes: number;
    /** Visão anual: só o número do dia; a mensal mostra também o nome do evento. */
    compacto: boolean;
    inicioAno: string;
    terminoAno: string;
    hoje: string;
    eventos: Map<string, EventoCalendario>;
    periodos: PeriodoCalendario[];
    selecao: { de: string; ate: string } | null;
    onDiaPointerDown: (data: string, shift: boolean) => void;
    onDiaPointerEnter: (data: string) => void;
    onDiaTeclado: (data: string, shift: boolean) => void;
}

function GradeMes({
    ano, mes, compacto, inicioAno, terminoAno, hoje, eventos, periodos, selecao,
    onDiaPointerDown, onDiaPointerEnter, onDiaTeclado,
}: GradeMesProps) {
    const periodoDoDia = (data: string) => periodos.findIndex(p => data >= p.dataInicio && data <= p.dataTermino);

    return (
        <section className={`cal-mes${compacto ? ' cal-mes-compacto' : ''}`} aria-label={`${NOMES_MESES[mes]} de ${ano}`}>
            {compacto && <h3 className="cal-mes-titulo">{NOMES_MESES[mes]} <span>{ano}</span></h3>}
            <div className="cal-grade" role="grid">
                <div className="cal-semana cal-cabecalho" role="row">
                    {DIAS_SEMANA_CURTOS.map((d, i) => (
                        <span key={i} role="columnheader" className={i === 0 || i === 6 ? 'cal-cabecalho-fds' : undefined}>{d}</span>
                    ))}
                </div>
                {semanasDoMes(ano, mes).map((semana, i) => (
                    <div key={i} className="cal-semana" role="row">
                        {semana.map((data, j) => {
                            if (!data) return <span key={j} className="cal-dia cal-dia-vazio" role="gridcell" />;

                            const foraDoAno = data < inicioAno || data > terminoAno;
                            const evento = eventos.get(data);
                            const categoria = categoriaDoDia(data, evento);
                            const indicePeriodo = periodoDoDia(data);
                            const periodo = indicePeriodo >= 0 ? periodos[indicePeriodo] : null;
                            const selecionado = !!selecao && data >= selecao.de && data <= selecao.ate;
                            const inicioPeriodo = periodo?.dataInicio === data;
                            // Conselho/plantão/formação têm ícone próprio (RN03); nos demais, o cadeado marca o dia sem aula.
                            const Icone = categoria === 'pedagogico' ? FiUsers : evento && !evento.comAula ? FiLock : null;

                            const classes = [
                                'cal-dia',
                                foraDoAno ? 'cal-dia-fora' : `cal-dia-${categoria}`,
                                selecionado ? 'cal-dia-selecionado' : '',
                                data === hoje ? 'cal-dia-hoje' : '',
                                evento?.herdado ? 'cal-dia-herdado' : '',
                                categoria === 'pedagogico' && !evento?.comAula ? 'cal-dia-sem-aula-marca' : '',
                            ].filter(Boolean).join(' ');

                            const titulo = foraDoAno
                                ? `${formatarData(data)} — fora do ano letivo`
                                : `${formatarData(data)} — ${descreverDia(data, evento)}${periodo ? ` · ${periodo.nome}` : ''}`;

                            return (
                                <button
                                    key={j}
                                    type="button"
                                    role="gridcell"
                                    className={classes}
                                    title={titulo}
                                    aria-label={titulo}
                                    aria-selected={selecionado}
                                    disabled={foraDoAno}
                                    style={periodo ? { ['--cal-periodo' as string]: CORES_PERIODOS[indicePeriodo % CORES_PERIODOS.length] } : undefined}
                                    onPointerDown={(e: PointerEvent<HTMLButtonElement>) => {
                                        if (e.button !== 0) return;
                                        e.preventDefault();
                                        onDiaPointerDown(data, e.shiftKey);
                                    }}
                                    onPointerEnter={() => onDiaPointerEnter(data)}
                                    // Clique pelo teclado (Enter/Espaço) chega com detail 0; o do mouse já foi tratado no pointerdown.
                                    onClick={(e: MouseEvent<HTMLButtonElement>) => {
                                        if (e.detail === 0) onDiaTeclado(data, e.shiftKey);
                                    }}
                                >
                                    <span className="cal-dia-numero">{Number(data.slice(8))}</span>
                                    {Icone && <Icone className="cal-dia-icone" aria-hidden="true" />}
                                    {!compacto && (
                                        <span className="cal-dia-texto">
                                            {evento ? evento.descricao : inicioPeriodo ? `Início do ${periodo!.nome}` : ''}
                                            {evento?.herdado && <em> (rede)</em>}
                                        </span>
                                    )}
                                </button>
                            );
                        })}
                    </div>
                ))}
            </div>
        </section>
    );
}

export default GradeMes;

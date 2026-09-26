import { useEffect, useState, type FormEvent } from 'react';
import { Link, useParams, useSearchParams } from 'react-router';
import { FiArrowLeft, FiDownload } from 'react-icons/fi';
import { apiFetch, baixarArquivo, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import GraficoRelatorio from './GraficoRelatorio';
import {
    colunaNumerica, formatarValor, turnos,
    type ChaveFiltro, type Opcoes, type RelatorioDefinicao, type RelatorioGerado,
} from './tipos';
import './Relatorios.css';

type Rascunho = Partial<Record<ChaveFiltro, string>>;

const chavesFiltro: ChaveFiltro[] = ['anoLetivoId', 'escolaId', 'turmaId', 'turno'];

function lerFiltros(params: URLSearchParams): Rascunho {
    return Object.fromEntries(chavesFiltro.flatMap(chave => (params.get(chave) ? [[chave, params.get(chave)!]] : [])));
}

/** Parâmetros da consulta em ordem fixa: a mesma combinação de filtros gera sempre a mesma chave. */
function montarConsulta(filtros: Rascunho): string {
    const params = new URLSearchParams();
    for (const chave of chavesFiltro) {
        if (filtros[chave]) params.set(chave, filtros[chave]!);
    }
    return params.toString();
}

/**
 * Tela genérica de relatório: os filtros aplicados ficam na URL (o link pode ser compartilhado e o
 * relatório é gerado de novo ao abrir), o resultado mostra resumo, gráfico e tabela, e a exportação
 * baixa o mesmo relatório em Excel ou PDF.
 */
function RelatorioPage() {
    const { relatorioId = '' } = useParams();
    const [searchParams, setSearchParams] = useSearchParams();
    // undefined = carregando; null = relatório inexistente ou sem permissão.
    const [definicao, setDefinicao] = useState<RelatorioDefinicao | null | undefined>(undefined);
    const [opcoes, setOpcoes] = useState<Opcoes | null>(null);
    const [erroCarga, setErroCarga] = useState<string | null>(null);
    const [rascunho, setRascunho] = useState<Rascunho>(() => lerFiltros(searchParams));
    const [erroFiltro, setErroFiltro] = useState<string | null>(null);
    const [versao, setVersao] = useState(0);
    const [resultado, setResultado] = useState<{ chave: string; relatorio: RelatorioGerado } | null>(null);
    const [erro, setErro] = useState<{ chave: string; message: string } | null>(null);
    const [baixando, setBaixando] = useState<string | null>(null);
    const [erroDownload, setErroDownload] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const [respCatalogo, respOpcoes] = await Promise.all([apiFetch('/api/relatorios'), apiFetch('/api/relatorios/opcoes')]);
                if (!respCatalogo.ok) throw new Error(await readApiError(respCatalogo));
                if (!respOpcoes.ok) throw new Error(await readApiError(respOpcoes));
                const catalogo = (await respCatalogo.json()) as RelatorioDefinicao[];
                const carregadas = (await respOpcoes.json()) as Opcoes;
                if (cancelled) return;

                const encontrada = catalogo.find(r => r.id === relatorioId) ?? null;
                setDefinicao(encontrada);
                setOpcoes(carregadas);

                // Sem filtros na URL, o ano letivo começa no mais recente.
                const temAno = encontrada?.filtros.some(f => f.chave === 'anoLetivoId');
                if (temAno && carregadas.anosLetivos.length > 0 && montarConsulta(lerFiltros(new URLSearchParams(window.location.search))) === '') {
                    const inicial = { anoLetivoId: String(carregadas.anosLetivos[0].id) };
                    setRascunho(inicial);
                    setSearchParams(montarConsulta(inicial), { replace: true });
                }
            } catch (e) {
                if (!cancelled) setErroCarga(e instanceof Error ? e.message : 'Falha ao carregar o relatório.');
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [relatorioId, setSearchParams]);

    const aplicados = lerFiltros(searchParams);
    const consulta = montarConsulta(aplicados);
    const faltando = definicao?.filtros.filter(f => f.obrigatorio && !aplicados[f.chave]) ?? [];
    const podeGerar = !!definicao && faltando.length === 0;
    const chave = `${relatorioId}?${consulta}#${versao}`;

    useEffect(() => {
        if (!podeGerar) return;
        let cancelled = false;
        const chaveAtual = `${relatorioId}?${consulta}#${versao}`;

        async function gerar() {
            try {
                const response = await apiFetch(`/api/relatorios/${relatorioId}?${consulta}`);
                if (!response.ok) throw new Error(await readApiError(response));
                const relatorio = (await response.json()) as RelatorioGerado;
                if (!cancelled) setResultado({ chave: chaveAtual, relatorio });
            } catch (e) {
                if (!cancelled) setErro({ chave: chaveAtual, message: e instanceof Error ? e.message : 'Falha ao gerar o relatório.' });
            }
        }

        void gerar();
        return () => { cancelled = true; };
    }, [podeGerar, relatorioId, consulta, versao]);

    const relatorio = resultado?.chave === chave ? resultado.relatorio : null;
    const erroAtual = erro?.chave === chave ? erro.message : null;
    const gerando = podeGerar && !relatorio && !erroAtual;

    const turmasDisponiveis = (opcoes?.turmas ?? []).filter(t =>
        t.ativa &&
        (!rascunho.escolaId || String(t.escolaId) === rascunho.escolaId) &&
        (!rascunho.anoLetivoId || String(t.anoLetivoId) === rascunho.anoLetivoId));

    const handleFiltro = (chaveFiltro: ChaveFiltro, valor: string) => {
        setErroFiltro(null);
        setRascunho(atual => {
            const proximo = { ...atual, [chaveFiltro]: valor };
            // A turma escolhida precisa continuar coerente com a escola e o ano selecionados.
            if (chaveFiltro !== 'turmaId' && proximo.turmaId) {
                const turma = opcoes?.turmas.find(t => String(t.id) === proximo.turmaId);
                const coerente = turma &&
                    (!proximo.escolaId || String(turma.escolaId) === proximo.escolaId) &&
                    (!proximo.anoLetivoId || String(turma.anoLetivoId) === proximo.anoLetivoId);
                if (!coerente) proximo.turmaId = '';
            }
            return proximo;
        });
    };

    const handleGerar = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        const pendentes = definicao?.filtros.filter(f => f.obrigatorio && !rascunho[f.chave]) ?? [];
        if (pendentes.length > 0) {
            setErroFiltro(`Selecione: ${pendentes.map(f => f.rotulo).join(', ')}.`);
            return;
        }

        const novaConsulta = montarConsulta(rascunho);
        if (novaConsulta === consulta) setVersao(v => v + 1);
        else setSearchParams(novaConsulta);
    };

    const handleExportar = async (formato: 'xlsx' | 'pdf') => {
        setBaixando(formato);
        setErroDownload(null);
        try {
            await baixarArquivo(`/api/relatorios/${relatorioId}?${consulta}${consulta ? '&' : ''}formato=${formato}`, `${relatorioId}.${formato}`);
        } catch (e) {
            setErroDownload(e instanceof Error ? e.message : 'Falha ao exportar o relatório.');
        } finally {
            setBaixando(null);
        }
    };

    const voltar = (
        <Link to="/relatorios" className="relatorio-voltar">
            <FiArrowLeft aria-hidden="true" /> Relatórios
        </Link>
    );

    if (definicao === null) {
        return (
            <div className="page-container">
                {voltar}
                <EmptyState emptyMessage="Relatório não encontrado." emptySubMessage="Ele não existe ou o seu perfil não tem permissão para vê-lo." />
            </div>
        );
    }

    return (
        <div className="page-container">
            {voltar}
            <div className="page-header relatorio-header">
                <div>
                    <h1>{definicao?.nome ?? 'Relatório'}</h1>
                    {definicao && <p className="page-intro">{definicao.descricao}</p>}
                </div>
                <div className="relatorio-exportar">
                    <button type="button" className="btn btn-secondary btn-sm" onClick={() => handleExportar('xlsx')} disabled={!relatorio || baixando !== null}>
                        <FiDownload aria-hidden="true" /> {baixando === 'xlsx' ? 'Gerando...' : 'Excel'}
                    </button>
                    <button type="button" className="btn btn-secondary btn-sm" onClick={() => handleExportar('pdf')} disabled={!relatorio || baixando !== null}>
                        <FiDownload aria-hidden="true" /> {baixando === 'pdf' ? 'Gerando...' : 'PDF'}
                    </button>
                </div>
            </div>

            <FeedbackMessage message={erroCarga ?? erroDownload} type="error" />

            {definicao && opcoes && (
                <form className="relatorio-filtros" onSubmit={handleGerar}>
                    {definicao.filtros.map(filtro => (
                        <div key={filtro.chave} className="form-group">
                            <label htmlFor={`filtro-${filtro.chave}`}>
                                {filtro.rotulo}{filtro.obrigatorio && <span className="required"> *</span>}
                            </label>
                            <select
                                id={`filtro-${filtro.chave}`}
                                value={rascunho[filtro.chave] ?? ''}
                                onChange={e => handleFiltro(filtro.chave, e.target.value)}
                            >
                                <option value="">{filtro.obrigatorio ? 'Selecione...' : 'Todos'}</option>
                                {filtro.chave === 'anoLetivoId' && opcoes.anosLetivos.map(a => <option key={a.id} value={a.id}>{a.anoReferencia}</option>)}
                                {filtro.chave === 'escolaId' && opcoes.escolas.map(e => <option key={e.id} value={e.id}>{e.nome}</option>)}
                                {filtro.chave === 'turmaId' && turmasDisponiveis.map(t => <option key={t.id} value={t.id}>{t.nome}</option>)}
                                {filtro.chave === 'turno' && turnos.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
                            </select>
                        </div>
                    ))}
                    <button type="submit" className="btn btn-primary" disabled={gerando}>
                        {gerando ? 'Gerando...' : 'Gerar relatório'}
                    </button>
                </form>
            )}

            <FeedbackMessage message={erroFiltro ?? erroAtual} type="error" />

            {definicao === undefined && !erroCarga && <div className="loading">Carregando relatório...</div>}

            {definicao && faltando.length > 0 && (
                <EmptyState emptyMessage={`Selecione ${faltando.map(f => f.rotulo.toLowerCase()).join(', ')} e clique em Gerar relatório.`} />
            )}

            {gerando && <div className="loading">Gerando relatório...</div>}

            {relatorio && (
                <div className="relatorio-resultado">
                    {relatorio.filtros.length > 0 && (
                        <p className="relatorio-aplicados">
                            {relatorio.filtros.map(f => <span key={f.rotulo}><strong>{f.rotulo}:</strong> {f.valor}</span>)}
                        </p>
                    )}

                    {relatorio.indicadores.length > 0 && (
                        <div className="kpi-grid">
                            {relatorio.indicadores.map(i => (
                                <div key={i.rotulo} className="kpi-card">
                                    <span className="kpi-label">{i.rotulo}</span>
                                    <strong className="kpi-valor">{i.valor}</strong>
                                </div>
                            ))}
                        </div>
                    )}

                    {relatorio.grafico && <GraficoRelatorio grafico={relatorio.grafico} />}

                    {relatorio.linhas.length === 0 ? (
                        <EmptyState emptyMessage="Nenhum registro encontrado para os filtros informados." />
                    ) : (
                        <>
                            <div className="table-container">
                                <table className="data-table relatorio-tabela">
                                    <thead>
                                        <tr>
                                            {relatorio.colunas.map(c => (
                                                <th key={c.titulo} className={colunaNumerica(c.tipo) ? 'numero' : undefined}>{c.titulo}</th>
                                            ))}
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {relatorio.linhas.map((linha, i) => (
                                            <tr key={i}>
                                                {relatorio.colunas.map((c, j) => (
                                                    <td key={c.titulo} className={colunaNumerica(c.tipo) ? 'numero' : undefined}>
                                                        {formatarValor(linha[j], c.tipo)}
                                                    </td>
                                                ))}
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                            <p className="field-hint">
                                {relatorio.linhas.length} {relatorio.linhas.length === 1 ? 'registro' : 'registros'} · Emitido em{' '}
                                {new Date(relatorio.geradoEm).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })}
                            </p>
                        </>
                    )}
                </div>
            )}
        </div>
    );
}

export default RelatorioPage;

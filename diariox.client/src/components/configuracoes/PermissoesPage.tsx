import { useEffect, useMemo, useState } from 'react';
import { usePermissoes } from '../../hooks/usePermissoes';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';

interface Acao {
    id: string;
    nome: string;
}

interface Modulo {
    id: string;
    nome: string;
    acoes: string[];
}

interface Catalogo {
    acoes: Acao[];
    modulos: Modulo[];
}

interface PerfilPermissoes {
    perfilId: number;
    perfilNome: string;
    perfilDescricao: string;
    permissoes: string[];
}

const VISUALIZAR = 'visualizar';
const GERENCIA = 'gerência';
// A Gerência não pode perder o acesso à própria tela de permissões.
const PERMISSOES_TRAVADAS_GERENCIA = ['configuracoes.visualizar', 'configuracoes.editar'];

const codigo = (modulo: string, acao: string) => `${modulo}.${acao}`;

function PermissoesPage() {
    const { can, reload: reloadMinhasPermissoes } = usePermissoes();
    const podeEditar = can('configuracoes.editar');

    const [catalogo, setCatalogo] = useState<Catalogo | null>(null);
    const [perfis, setPerfis] = useState<PerfilPermissoes[]>([]);
    // Alterações ainda não salvas, por perfil.
    const [rascunhos, setRascunhos] = useState<Record<number, Set<string>>>({});
    const [perfilId, setPerfilId] = useState<number | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const [catalogoRes, perfisRes] = await Promise.all([
                    apiFetch('/api/permissoes/catalogo'),
                    apiFetch('/api/permissoes/perfis'),
                ]);
                if (!catalogoRes.ok) throw new Error(await readApiError(catalogoRes));
                if (!perfisRes.ok) throw new Error(await readApiError(perfisRes));

                const catalogoData = (await catalogoRes.json()) as Catalogo;
                const perfisData = (await perfisRes.json()) as PerfilPermissoes[];
                if (cancelled) return;

                setCatalogo(catalogoData);
                setPerfis(perfisData);
                setPerfilId(perfisData[0]?.perfilId ?? null);
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar as permissões.');
            } finally {
                if (!cancelled) setIsLoading(false);
            }
        }

        void load();
        return () => { cancelled = true; };
    }, []);

    const perfil = perfis.find(p => p.perfilId === perfilId) ?? null;
    const isGerencia = perfil?.perfilNome.toLowerCase() === GERENCIA;

    const salvas = useMemo(() => new Set(perfil?.permissoes ?? []), [perfil]);
    const atuais = (perfilId !== null && rascunhos[perfilId]) || salvas;

    const isDirty = (id: number) => {
        const rascunho = rascunhos[id];
        const original = perfis.find(p => p.perfilId === id)?.permissoes ?? [];
        return !!rascunho && (rascunho.size !== original.length || original.some(p => !rascunho.has(p)));
    };

    const isTravada = (permissao: string) => isGerencia && PERMISSOES_TRAVADAS_GERENCIA.includes(permissao);

    const atualizarRascunho = (alterar: (next: Set<string>) => void) => {
        if (perfilId === null) return;
        const next = new Set(atuais);
        alterar(next);
        PERMISSOES_TRAVADAS_GERENCIA.forEach(p => { if (isGerencia) next.add(p); });
        setRascunhos(current => ({ ...current, [perfilId]: next }));
        setSuccessMessage(null);
    };

    const handleToggle = (modulo: Modulo, acao: string) => {
        const permissao = codigo(modulo.id, acao);
        atualizarRascunho(next => {
            if (next.has(permissao)) {
                // Sem visualizar, nenhuma outra ação do módulo faz sentido.
                if (acao === VISUALIZAR) modulo.acoes.forEach(a => next.delete(codigo(modulo.id, a)));
                else next.delete(permissao);
            } else {
                next.add(permissao);
                next.add(codigo(modulo.id, VISUALIZAR));
            }
        });
    };

    const handleToggleModulo = (modulo: Modulo, marcar: boolean) => {
        atualizarRascunho(next => {
            modulo.acoes.forEach(a => {
                if (marcar) next.add(codigo(modulo.id, a));
                else next.delete(codigo(modulo.id, a));
            });
        });
    };

    const handleDescartar = () => {
        if (perfilId === null) return;
        setRascunhos(current => {
            const next = { ...current };
            delete next[perfilId];
            return next;
        });
        setError(null);
    };

    const handleSalvar = async () => {
        if (perfilId === null || !perfil) return;

        setIsSaving(true);
        setError(null);
        setSuccessMessage(null);
        try {
            const response = await apiFetch(`/api/permissoes/perfis/${perfilId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ permissoes: [...atuais] }),
            });
            if (!response.ok) throw new Error(await readApiError(response));

            const atualizado = (await response.json()) as PerfilPermissoes;
            setPerfis(current => current.map(p => (p.perfilId === atualizado.perfilId ? atualizado : p)));
            handleDescartar();
            setSuccessMessage(`Permissões do perfil ${atualizado.perfilNome} salvas com sucesso!`);
            setTimeout(() => setSuccessMessage(null), 3000);
            // O usuário logado pode ter o perfil alterado: atualiza menu e ações.
            await reloadMinhasPermissoes();
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao salvar as permissões.');
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <div className="page-container">
            <div className="page-header">
                <h1>Permissões</h1>
            </div>

            <p className="page-intro">
                Defina o que cada perfil pode fazer nesta instituição. Um usuário com mais de um perfil
                recebe a soma das permissões de todos eles. As alterações valem imediatamente.
            </p>

            <FeedbackMessage message={error} type="error" />
            <FeedbackMessage message={successMessage} type="success" />

            {isLoading ? (
                <div className="loading">Carregando permissões...</div>
            ) : !catalogo || perfis.length === 0 ? (
                <EmptyState emptyMessage="Nenhum perfil disponível para configurar." />
            ) : (
                <div className="form-grid">
                    <div className="form-tabs" role="tablist" aria-label="Perfis">
                        {perfis.map(p => (
                            <button
                                key={p.perfilId}
                                type="button"
                                role="tab"
                                aria-selected={p.perfilId === perfilId}
                                className={`form-tab${p.perfilId === perfilId ? ' active' : ''}`}
                                onClick={() => setPerfilId(p.perfilId)}
                            >
                                {p.perfilNome}
                                {isDirty(p.perfilId) && <span className="form-tab-dirty" title="Alterações não salvas"> •</span>}
                            </button>
                        ))}
                    </div>

                    {perfil && (
                        <div className="permissoes-panel" role="tabpanel">
                            {perfil.perfilDescricao && <p className="form-tab-hint">{perfil.perfilDescricao}</p>}
                            {!podeEditar && (
                                <p className="form-tab-hint">Você pode consultar, mas não alterar as permissões.</p>
                            )}

                            <div className="table-container">
                                <table className="data-table permissoes-table">
                                    <thead>
                                        <tr>
                                            <th>Módulo</th>
                                            {catalogo.acoes.map(acao => <th key={acao.id}>{acao.nome}</th>)}
                                            <th>Todas</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {catalogo.modulos.map(modulo => {
                                            const todas = modulo.acoes.every(a => atuais.has(codigo(modulo.id, a)));
                                            const algumaTravada = modulo.acoes.some(a => isTravada(codigo(modulo.id, a)));
                                            return (
                                                <tr key={modulo.id}>
                                                    <th scope="row">{modulo.nome}</th>
                                                    {catalogo.acoes.map(acao => {
                                                        if (!modulo.acoes.includes(acao.id)) {
                                                            return <td key={acao.id} className="permissao-na" aria-label="Não se aplica">—</td>;
                                                        }
                                                        const permissao = codigo(modulo.id, acao.id);
                                                        const travada = isTravada(permissao);
                                                        return (
                                                            <td key={acao.id}>
                                                                <input
                                                                    type="checkbox"
                                                                    checked={atuais.has(permissao)}
                                                                    onChange={() => handleToggle(modulo, acao.id)}
                                                                    disabled={!podeEditar || isSaving || travada}
                                                                    aria-label={`${modulo.nome}: ${acao.nome}`}
                                                                    title={travada ? 'A Gerência sempre mantém acesso às Configurações.' : undefined}
                                                                />
                                                            </td>
                                                        );
                                                    })}
                                                    <td>
                                                        <input
                                                            type="checkbox"
                                                            checked={todas}
                                                            onChange={() => handleToggleModulo(modulo, !todas)}
                                                            disabled={!podeEditar || isSaving || (todas && algumaTravada)}
                                                            aria-label={`${modulo.nome}: todas as ações`}
                                                        />
                                                    </td>
                                                </tr>
                                            );
                                        })}
                                    </tbody>
                                </table>
                            </div>

                            {podeEditar && (
                                <div className="form-actions">
                                    <button
                                        type="button"
                                        className="btn btn-primary"
                                        onClick={handleSalvar}
                                        disabled={isSaving || !isDirty(perfil.perfilId)}
                                    >
                                        {isSaving ? 'Salvando...' : `Salvar permissões de ${perfil.perfilNome}`}
                                    </button>
                                    <button
                                        type="button"
                                        className="btn btn-secondary"
                                        onClick={handleDescartar}
                                        disabled={isSaving || !isDirty(perfil.perfilId)}
                                    >
                                        Descartar alterações
                                    </button>
                                </div>
                            )}
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}

export default PermissoesPage;

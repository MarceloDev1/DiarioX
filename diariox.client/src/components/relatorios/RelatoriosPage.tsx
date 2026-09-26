import { useEffect, useState } from 'react';
import { Link } from 'react-router';
import { FiChevronRight } from 'react-icons/fi';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import type { RelatorioDefinicao } from './tipos';
import './Relatorios.css';

/** Catálogo: só aparecem os relatórios cujos dados o perfil do usuário pode ver. */
function RelatoriosPage() {
    const [relatorios, setRelatorios] = useState<RelatorioDefinicao[] | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const response = await apiFetch('/api/relatorios');
                if (!response.ok) throw new Error(await readApiError(response));
                const data = (await response.json()) as RelatorioDefinicao[];
                if (!cancelled) setRelatorios(data);
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar os relatórios.');
            }
        }

        void load();
        return () => { cancelled = true; };
    }, []);

    const categorias = [...new Set(relatorios?.map(r => r.categoria) ?? [])];

    return (
        <div className="page-container">
            <div className="page-header">
                <h1>Relatórios</h1>
            </div>

            <FeedbackMessage message={error} type="error" />

            {!relatorios ? (
                !error && <div className="loading">Carregando relatórios...</div>
            ) : relatorios.length === 0 ? (
                <EmptyState
                    emptyMessage="Nenhum relatório disponível para o seu perfil."
                    emptySubMessage="Cada relatório exige também a permissão de ver o módulo dos dados (ex.: Alunos, Turmas)."
                />
            ) : (
                categorias.map(categoria => (
                    <section key={categoria} className="relatorios-categoria">
                        <h2 className="secao-titulo">{categoria}</h2>
                        <div className="relatorios-catalogo">
                            {relatorios.filter(r => r.categoria === categoria).map(r => (
                                <Link key={r.id} to={`/relatorios/${r.id}`} className="relatorio-card">
                                    <span>
                                        <strong>{r.nome}</strong>
                                        <span>{r.descricao}</span>
                                    </span>
                                    <FiChevronRight aria-hidden="true" />
                                </Link>
                            ))}
                        </div>
                    </section>
                ))
            )}
        </div>
    );
}

export default RelatoriosPage;

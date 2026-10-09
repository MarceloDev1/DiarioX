import { useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import PainelGeral from './PainelGeral';
import PainelProfessor from './PainelProfessor';
import type { PainelProfessor as DadosPainelProfessor } from './tiposProfessor';
import './Home.css';

const CHAVE_ESCOLA = 'diariox.home.escola';

function lerEscolaSalva(): number | null {
    try {
        return Number(localStorage.getItem(CHAVE_ESCOLA)) || null;
    } catch {
        return null;
    }
}

function salvarEscola(escolaId: number | null) {
    try {
        if (escolaId === null) localStorage.removeItem(CHAVE_ESCOLA);
        else localStorage.setItem(CHAVE_ESCOLA, String(escolaId));
    } catch {
        // Sem armazenamento local a escolha vale só nesta visita.
    }
}

type Estado =
    | { tipo: 'carregando' }
    | { tipo: 'geral' }
    | { tipo: 'professor'; painel: DadosPainelProfessor }
    | { tipo: 'erro'; mensagem: string };

/**
 * Página inicial. Usuário vinculado a um cadastro de professor vê a home do professor (aulas do dia,
 * pendências, diários e notas); os demais perfis veem o painel geral.
 */
function HomePage() {
    const [estado, setEstado] = useState<Estado>({ tipo: 'carregando' });
    const [escolaId, setEscolaId] = useState<number | null>(lerEscolaSalva);
    const [atualizando, setAtualizando] = useState(false);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            setAtualizando(true);
            try {
                const response = await apiFetch(`/api/dashboard/professor${escolaId ? `?escolaId=${escolaId}` : ''}`);
                if (!response.ok) throw new Error(await readApiError(response));
                if (cancelled) return;

                if (response.status === 204) {
                    setEstado({ tipo: 'geral' });
                    return;
                }
                const painel = (await response.json()) as DadosPainelProfessor;
                if (!cancelled) setEstado({ tipo: 'professor', painel });
            } catch (e) {
                if (!cancelled) setEstado({ tipo: 'erro', mensagem: e instanceof Error ? e.message : 'Falha ao carregar o painel.' });
            } finally {
                if (!cancelled) setAtualizando(false);
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [escolaId]);

    const handleEscolaChange = (id: number | null) => {
        salvarEscola(id);
        setEscolaId(id);
    };

    switch (estado.tipo) {
        case 'carregando':
            return <div className="loading">Carregando painel...</div>;
        case 'erro':
            return <FeedbackMessage message={estado.mensagem} type="error" />;
        case 'geral':
            return <PainelGeral />;
        case 'professor':
            return (
                <PainelProfessor
                    painel={estado.painel}
                    atualizando={atualizando}
                    onEscolaChange={handleEscolaChange}
                />
            );
    }
}

export default HomePage;

import { useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import { hojeIso } from '../chamada/tipos';
import DiarioConteudos from './DiarioConteudos';
import RegistrarConteudo from './RegistrarConteudo';
import type { ConteudoTurma } from './tipos';

type Aba = 'diario' | 'registrar';

const abas: { id: Aba; label: string }[] = [
    { id: 'diario', label: 'Diário' },
    { id: 'registrar', label: 'Registrar conteúdo' },
];

function ConteudoMinistradoPage() {
    const [turmas, setTurmas] = useState<ConteudoTurma[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [turmaId, setTurmaId] = useState<number | null>(null);
    const [disciplinaId, setDisciplinaId] = useState<number | null>(null);
    const [aba, setAba] = useState<Aba>('diario');
    // Data do conteúdo aberto na aba "Registrar"; o diário também abre dias anteriores por aqui.
    const [data, setData] = useState(hojeIso);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const response = await apiFetch('/api/conteudosministrados/turmas');
                if (!response.ok) throw new Error(await readApiError(response));
                const carregadas = (await response.json()) as ConteudoTurma[];
                if (cancelled) return;

                setTurmas(carregadas);
                // Professor com uma única turma/disciplina já cai direto nela.
                if (carregadas.length === 1) {
                    setTurmaId(carregadas[0].turmaId);
                    if (carregadas[0].disciplinas.length === 1) setDisciplinaId(carregadas[0].disciplinas[0].id);
                }
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar as turmas.');
            } finally {
                if (!cancelled) setIsLoading(false);
            }
        }

        void load();
        return () => { cancelled = true; };
    }, []);

    const turma = turmas.find(t => t.turmaId === turmaId) ?? null;
    const disciplina = turma?.disciplinas.find(d => d.id === disciplinaId) ?? null;
    // Nos Anos Iniciais a chamada é diária, sem disciplina: o diário cobre todas as disciplinas da turma.
    const diaria = turma?.tipoFrequencia === 'DIARIA';
    const chave = `${turma?.turmaId}-${disciplina?.id ?? 'todas'}`;

    const handleTurmaChange = (value: string) => {
        const proxima = turmas.find(t => t.turmaId === Number(value)) ?? null;
        setTurmaId(proxima?.turmaId ?? null);
        setDisciplinaId(proxima?.disciplinas.length === 1 ? proxima.disciplinas[0].id : null);
        setData(hojeIso());
    };

    const handleAbrir = (dataConteudo: string) => {
        setData(dataConteudo);
        setAba('registrar');
    };

    return (
        <div className="page-container">
            <div className="page-header">
                <h1>Conteúdo Ministrado</h1>
            </div>

            <FeedbackMessage message={error} type="error" />

            {isLoading ? (
                <div className="loading">Carregando turmas...</div>
            ) : turmas.length === 0 ? (
                <EmptyState
                    emptyMessage="Nenhuma turma disponível para registro de conteúdo."
                    emptySubMessage="Professores só veem as turmas e disciplinas em que estão alocados (Alocação de Professor)."
                />
            ) : (
                <>
                    <div className="chamada-filtros">
                        <div className="form-group">
                            <label htmlFor="conteudo-turma">Turma</label>
                            <select id="conteudo-turma" value={turmaId ?? ''} onChange={e => handleTurmaChange(e.target.value)}>
                                <option value="">Selecione...</option>
                                {turmas.map(t => (
                                    <option key={t.turmaId} value={t.turmaId}>
                                        {t.turmaNome} — {t.anoReferencia} ({t.escolaNome})
                                    </option>
                                ))}
                            </select>
                        </div>
                        <div className="form-group">
                            <label htmlFor="conteudo-disciplina">Disciplina</label>
                            <select
                                id="conteudo-disciplina"
                                value={disciplinaId ?? ''}
                                onChange={e => setDisciplinaId(e.target.value ? Number(e.target.value) : null)}
                                disabled={!turma}
                            >
                                <option value="">{diaria ? 'Todas (diário)' : 'Selecione...'}</option>
                                {turma?.disciplinas.map(d => <option key={d.id} value={d.id}>{d.nome}</option>)}
                            </select>
                            {diaria && (
                                <span className="field-hint">
                                    Frequência diária: a chamada é da turma. Escolha a disciplina para registrar o conteúdo.
                                </span>
                            )}
                        </div>
                    </div>

                    {!turma || (!diaria && !disciplina) ? (
                        <EmptyState emptyMessage="Selecione a turma e a disciplina para consultar o diário ou registrar o conteúdo." />
                    ) : (
                        <div className="form-grid">
                            <div className="form-tabs" role="tablist" aria-label="Conteúdo ministrado">
                                {abas.map(a => (
                                    <button
                                        key={a.id}
                                        type="button"
                                        role="tab"
                                        aria-selected={aba === a.id}
                                        className={`form-tab${aba === a.id ? ' active' : ''}`}
                                        onClick={() => setAba(a.id)}
                                    >
                                        {a.label}
                                    </button>
                                ))}
                            </div>

                            <div className="chamada-painel" role="tabpanel">
                                {aba === 'diario' && (
                                    <DiarioConteudos key={chave} turma={turma} disciplinaId={disciplina?.id ?? null} onAbrir={handleAbrir} />
                                )}
                                {aba === 'registrar' && (disciplina ? (
                                    <RegistrarConteudo
                                        key={chave}
                                        turma={turma}
                                        disciplinaId={disciplina.id}
                                        data={data}
                                        onDataChange={setData}
                                    />
                                ) : (
                                    <EmptyState emptyMessage="Selecione a disciplina para registrar o conteúdo ministrado." />
                                ))}
                            </div>
                        </div>
                    )}
                </>
            )}
        </div>
    );
}

export default ConteudoMinistradoPage;

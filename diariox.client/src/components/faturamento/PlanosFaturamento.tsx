import { useEffect, useState, type FormEvent } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import { calcularValorPlano, descreverPlano, formatarMoeda, type Plano, type Resultado } from './tipos';

interface PlanoForm {
    nome: string;
    descricao: string;
    valorFixo: string;
    valorPorAluno: string;
    valorMinimo: string;
    ativo: boolean;
}

const formVazio: PlanoForm = { nome: '', descricao: '', valorFixo: '0', valorPorAluno: '0', valorMinimo: '0', ativo: true };

const numero = (valor: string) => {
    const n = Number(valor.replace(',', '.'));
    return Number.isFinite(n) ? n : 0;
};

function PlanosFaturamento() {
    const [planos, setPlanos] = useState<Plano[] | null>(null);
    const [versao, setVersao] = useState(0);
    const [form, setForm] = useState<PlanoForm | null>(null);
    const [editingId, setEditingId] = useState<number | null>(null);
    const [alunosSimulacao, setAlunosSimulacao] = useState('150');
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            try {
                const response = await apiFetch('/api/faturamento/planos');
                if (!response.ok) throw new Error(await readApiError(response));
                const data = (await response.json()) as Plano[];
                if (!cancelled) setPlanos(data);
            } catch (e) {
                if (!cancelled) setError(e instanceof Error ? e.message : 'Falha ao carregar os planos.');
            }
        }

        void load();
        return () => { cancelled = true; };
    }, [versao]);

    const abrirForm = (plano: Plano | null) => {
        setEditingId(plano?.id ?? null);
        setForm(plano
            ? {
                nome: plano.nome, descricao: plano.descricao ?? '', valorFixo: String(plano.valorFixo),
                valorPorAluno: String(plano.valorPorAluno), valorMinimo: String(plano.valorMinimo), ativo: plano.ativo,
            }
            : formVazio);
        setError(null);
        setSuccess(null);
    };

    const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (!form) return;

        setIsSaving(true);
        setError(null);
        try {
            const response = await apiFetch(editingId ? `/api/faturamento/planos/${editingId}` : '/api/faturamento/planos', {
                method: editingId ? 'PUT' : 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    nome: form.nome,
                    descricao: form.descricao || null,
                    valorFixo: numero(form.valorFixo),
                    valorPorAluno: numero(form.valorPorAluno),
                    valorMinimo: numero(form.valorMinimo),
                    ativo: form.ativo,
                }),
            });
            if (!response.ok) throw new Error(await readApiError(response));
            const resultado = (await response.json()) as Resultado<Plano>;

            setSuccess(resultado.message);
            setForm(null);
            setEditingId(null);
            setVersao(v => v + 1);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao salvar o plano.');
        } finally {
            setIsSaving(false);
        }
    };

    if (form) {
        const valores = { valorFixo: numero(form.valorFixo), valorPorAluno: numero(form.valorPorAluno), valorMinimo: numero(form.valorMinimo) };
        const alunos = Math.max(0, Math.floor(numero(alunosSimulacao)));

        return (
            <form className="form-grid" onSubmit={handleSubmit}>
                <h3 className="secao-titulo form-field-full">{editingId ? 'Editar plano' : 'Novo plano'}</h3>
                <FeedbackMessage message={error} type="error" />

                <div className="form-group">
                    <label htmlFor="plano-nome">Nome</label>
                    <input id="plano-nome" value={form.nome} maxLength={100} required
                        onChange={e => setForm({ ...form, nome: e.target.value })} />
                </div>
                <div className="form-group">
                    <label htmlFor="plano-ativo">Situação</label>
                    <select id="plano-ativo" value={form.ativo ? 'S' : 'N'} onChange={e => setForm({ ...form, ativo: e.target.value === 'S' })}>
                        <option value="S">Ativo (disponível para novas assinaturas)</option>
                        <option value="N">Inativo (só mantém as assinaturas atuais)</option>
                    </select>
                </div>
                <div className="form-group form-field-full">
                    <label htmlFor="plano-descricao">Descrição</label>
                    <input id="plano-descricao" value={form.descricao} maxLength={500}
                        onChange={e => setForm({ ...form, descricao: e.target.value })} />
                </div>
                <div className="form-group">
                    <label htmlFor="plano-fixo">Valor fixo mensal (R$)</label>
                    <input id="plano-fixo" type="number" min={0} step="0.01" value={form.valorFixo}
                        onChange={e => setForm({ ...form, valorFixo: e.target.value })} />
                </div>
                <div className="form-group">
                    <label htmlFor="plano-aluno">Valor por aluno ativo (R$)</label>
                    <input id="plano-aluno" type="number" min={0} step="0.01" value={form.valorPorAluno}
                        onChange={e => setForm({ ...form, valorPorAluno: e.target.value })} />
                </div>
                <div className="form-group">
                    <label htmlFor="plano-minimo">Valor mínimo mensal (R$)</label>
                    <input id="plano-minimo" type="number" min={0} step="0.01" value={form.valorMinimo}
                        onChange={e => setForm({ ...form, valorMinimo: e.target.value })} />
                </div>

                <div className="plano-simulacao form-field-full">
                    <strong>Simulação:</strong> máx(mínimo, fixo + por aluno × alunos ativos)
                    <div className="plano-simulacao-linha">
                        <label htmlFor="plano-simular">Com</label>
                        <input id="plano-simular" type="number" min={0} value={alunosSimulacao}
                            onChange={e => setAlunosSimulacao(e.target.value)} />
                        <span>alunos ativos: <strong>{formatarMoeda(calcularValorPlano(valores, alunos))}</strong> por mês</span>
                    </div>
                    <span className="field-hint">
                        Alterar valores vale para as próximas faturas; as já geradas guardam o cálculo da época.
                    </span>
                </div>

                <div className="form-actions">
                    <button type="submit" className="btn btn-primary" disabled={isSaving}>
                        {isSaving ? 'Salvando...' : editingId ? 'Salvar plano' : 'Cadastrar plano'}
                    </button>
                    <button type="button" className="btn btn-secondary" onClick={() => setForm(null)} disabled={isSaving}>
                        Cancelar
                    </button>
                </div>
            </form>
        );
    }

    return (
        <>
            <FeedbackMessage message={error} type="error" />
            <FeedbackMessage message={success} type="success" />

            <div className="chamada-barra">
                <p className="chamada-status">
                    Combine valor fixo, valor por aluno ativo e um mínimo mensal. Ex.: só fixo, só por aluno ou os dois.
                </p>
                <button type="button" className="btn btn-primary btn-sm" onClick={() => abrirForm(null)}>+ Novo plano</button>
            </div>

            {!planos ? (
                <div className="loading">Carregando planos...</div>
            ) : planos.length === 0 ? (
                <EmptyState emptyMessage="Nenhum plano cadastrado." emptySubMessage="Cadastre um plano para criar as assinaturas." />
            ) : (
                <div className="table-container">
                    <table className="data-table">
                        <thead>
                            <tr>
                                <th>Plano</th>
                                <th>Preço</th>
                                <th>50 alunos</th>
                                <th>200 alunos</th>
                                <th>Assinaturas</th>
                                <th>Situação</th>
                                <th>Ações</th>
                            </tr>
                        </thead>
                        <tbody>
                            {planos.map(p => (
                                <tr key={p.id}>
                                    <td>
                                        <strong>{p.nome}</strong>
                                        {p.descricao && <><br /><small>{p.descricao}</small></>}
                                    </td>
                                    <td>{descreverPlano(p)}</td>
                                    <td className="nowrap-cell">{formatarMoeda(calcularValorPlano(p, 50))}</td>
                                    <td className="nowrap-cell">{formatarMoeda(calcularValorPlano(p, 200))}</td>
                                    <td>{p.assinaturas}</td>
                                    <td>
                                        <span className={`status-pill ${p.ativo ? 'status-active' : 'status-inactive'}`}>
                                            {p.ativo ? 'Ativo' : 'Inativo'}
                                        </span>
                                    </td>
                                    <td>
                                        <button type="button" className="table-action-button" onClick={() => abrirForm(p)}>Editar</button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </>
    );
}

export default PlanosFaturamento;

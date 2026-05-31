import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { useCrudData } from '../../hooks/useCrudData';
import { useCrudForm } from '../../hooks/useCrudForm';
import { formatCnpj, formatCpf, formatTelefone } from '../../utils/formatters';
import { validateCnpj, validateCpf } from '../../utils/validators';
import FeedbackMessage from '../ui/FeedbackMessage';
import StatusPill from '../ui/StatusPill';
import EmptyState from '../ui/EmptyState';

type EscolaStatus = 'ATIVO' | 'INATIVO';
type View = 'list' | 'form';

interface EscolaFormState {
    codigoInep: string;
    nome: string;
    cnpj: string;
    telefone: string;
    emailInstitucional: string;
    municipio: string;
    enderecoCompleto: string;
    status: EscolaStatus;
    cpfDiretor: string;
}

interface Escola extends EscolaFormState {
    id: number;
}

const emptyEscolaForm: EscolaFormState = {
    codigoInep: '',
    nome: '',
    cnpj: '',
    telefone: '',
    emailInstitucional: '',
    municipio: '',
    enderecoCompleto: '',
    status: 'ATIVO',
    cpfDiretor: '',
};

function EscolasPage() {
    const { items: escolas, isLoading, isSaving, error, load, save, remove } = useCrudData<Escola>('/api/escolas');
    const { form, setForm, editingId, handleFieldChange, startEdit, clear } = useCrudForm<EscolaFormState & Record<string, unknown>>(
        emptyEscolaForm as EscolaFormState & Record<string, unknown>
    );
    const [formError, setFormError] = useState<string | null>(null);

    const [view, setView] = useState<View>('list');

    const [filterNome, setFilterNome] = useState('');
    const [filterCnpj, setFilterCnpj] = useState('');
    const [filterInep, setFilterInep] = useState('');
    const [appliedNome, setAppliedNome] = useState('');
    const [appliedCnpj, setAppliedCnpj] = useState('');
    const [appliedInep, setAppliedInep] = useState('');

    useEffect(() => {
        void load();
    }, []);

    const handleLimparFiltros = () => {
        setFilterNome('');
        setFilterCnpj('');
        setFilterInep('');
        setAppliedNome('');
        setAppliedCnpj('');
        setAppliedInep('');
    };

    const handleConsultar = (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setAppliedNome(filterNome.trim());
        setAppliedCnpj(filterCnpj.trim());
        setAppliedInep(filterInep.trim());
    };

    const handleEscolaFieldChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const { name, value } = event.target;

        if (name === 'cnpj') {
            setForm((current) => ({ ...current, cnpj: formatCnpj(value) }));
            return;
        }

        if (name === 'telefone') {
            setForm((current) => ({ ...current, telefone: formatTelefone(value) }));
            return;
        }

        if (name === 'cpfDiretor') {
            setForm((current) => ({ ...current, cpfDiretor: formatCpf(value) }));
            return;
        }

        handleFieldChange(event);
    };

    const filteredEscolas = escolas.filter(escola => {
        const matchNome = !appliedNome || escola.nome.toLowerCase().includes(appliedNome.toLowerCase());
        const matchCnpj = !appliedCnpj || escola.cnpj.replace(/\D/g, '').includes(appliedCnpj.replace(/\D/g, ''));
        const matchInep = !appliedInep || escola.codigoInep.includes(appliedInep);
        return matchNome && matchCnpj && matchInep;
    });

    const handleNovaEscola = () => {
        setFormError(null);
        clear();
        setView('form');
    };

    const handleEdit = (escola: Escola) => {
        setFormError(null);
        startEdit(escola.id, {
            codigoInep: escola.codigoInep,
            nome: escola.nome,
            cnpj: escola.cnpj,
            telefone: escola.telefone,
            emailInstitucional: escola.emailInstitucional,
            municipio: escola.municipio,
            enderecoCompleto: escola.enderecoCompleto,
            status: escola.status,
            cpfDiretor: escola.cpfDiretor ?? '',
        });
        setView('form');
    };

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setFormError(null);

        if (!validateCnpj(form.cnpj)) {
            setFormError('Informe um CNPJ valido.');
            return;
        }

        if (form.cpfDiretor.trim() && !validateCpf(form.cpfDiretor)) {
            setFormError('Informe um CPF de diretor valido.');
            return;
        }

        const saved = await save(editingId, form);
        if (saved) {
            clear();
            setView('list');
        }
    };

    const handleCancel = () => {
        setFormError(null);
        clear();
        setView('list');
    };

    const handleDelete = async (id: number) => {
        await remove(id);
    };

    if (view === 'form') {
        return (
            <div className="school-page">
                <div className="content-card">
                    <div className="section-header">
                        <div>
                            <h2>{editingId ? 'Editar Escola' : 'Nova Escola'}</h2>
                            <p>{editingId ? 'Altere os dados e salve para atualizar.' : 'Preencha os dados para cadastrar uma nova escola.'}</p>
                        </div>
                        <button type="button" className="secondary-button cancel-button" onClick={handleCancel}>
                            Cancelar
                        </button>
                    </div>

                    <FeedbackMessage message={formError ?? error} />

                    <form className="cadastro-form escola-form" onSubmit={handleSubmit}>
                        <div className="form-grid">
                            <div className="form-field">
                                <label htmlFor="escola-codigoInep">Código INEP</label>
                                <input id="escola-codigoInep" name="codigoInep" value={form.codigoInep} onChange={handleFieldChange} type="text" required />
                            </div>
                            <div className="form-field">
                                <label htmlFor="escola-nome">Nome da escola</label>
                                <input id="escola-nome" name="nome" value={form.nome} onChange={handleFieldChange} type="text" required />
                            </div>
                            <div className="form-field">
                                <label htmlFor="escola-cnpj">CNPJ</label>
                                <input id="escola-cnpj" name="cnpj" value={form.cnpj} onChange={handleEscolaFieldChange} type="text" required />
                            </div>
                            <div className="form-field">
                                <label htmlFor="escola-telefone">Telefone</label>
                                <input id="escola-telefone" name="telefone" value={form.telefone} onChange={handleEscolaFieldChange} type="text" maxLength={15} required />
                            </div>
                            <div className="form-field">
                                <label htmlFor="escola-emailInstitucional">E-mail institucional</label>
                                <input id="escola-emailInstitucional" name="emailInstitucional" value={form.emailInstitucional} onChange={handleFieldChange} type="email" required />
                            </div>
                            <div className="form-field">
                                <label htmlFor="escola-municipio">Município</label>
                                <input id="escola-municipio" name="municipio" value={form.municipio} onChange={handleFieldChange} type="text" required />
                            </div>
                            <div className="form-field">
                                <label htmlFor="escola-enderecoCompleto">Endereço completo</label>
                                <input id="escola-enderecoCompleto" name="enderecoCompleto" value={form.enderecoCompleto} onChange={handleFieldChange} type="text" required />
                            </div>
                            <div className="form-field">
                                <label htmlFor="escola-status">Status</label>
                                <select id="escola-status" name="status" value={form.status} onChange={handleFieldChange}>
                                    <option value="ATIVO">Ativa</option>
                                    <option value="INATIVO">Inativa</option>
                                </select>
                            </div>
                            <div className="form-field">
                                <label htmlFor="escola-cpfDiretor">CPF do diretor</label>
                                <input id="escola-cpfDiretor" name="cpfDiretor" value={form.cpfDiretor} onChange={handleEscolaFieldChange} type="text" maxLength={14} />
                            </div>
                        </div>
                        <div className="form-actions">
                            <button type="submit" disabled={isSaving}>
                                {editingId !== null ? 'Atualizar Escola' : 'Salvar Escola'}
                            </button>
                            <button
                                type="button"
                                onClick={() => {
                                    setFormError(null);
                                    clear();
                                }}
                            >
                                Limpar
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        );
    }

    return (
        <div className="school-page">
            <div className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Escolas</h2>
                        <p>Consulte, edite e remova escolas cadastradas.</p>
                    </div>
                    <button type="button" className="primary-button" onClick={handleNovaEscola}>
                        + Nova Escola
                    </button>
                </div>

                <form className="filter-bar" onSubmit={handleConsultar}>
                    <div className="filter-field">
                        <label htmlFor="filtro-nome">Nome</label>
                        <input
                            id="filtro-nome"
                            type="text"
                            value={filterNome}
                            onChange={e => setFilterNome(e.target.value)}
                            className="filter-input"
                        />
                    </div>
                    <div className="filter-field">
                        <label htmlFor="filtro-cnpj">CNPJ</label>
                        <input
                            id="filtro-cnpj"
                            type="text"
                            value={filterCnpj}
                            onChange={e => setFilterCnpj(formatCnpj(e.target.value))}
                            className="filter-input"
                        />
                    </div>
                    <div className="filter-field">
                        <label htmlFor="filtro-inep">Código INEP</label>
                        <input
                            id="filtro-inep"
                            type="text"
                            value={filterInep}
                            onChange={e => setFilterInep(e.target.value)}
                            className="filter-input"
                        />
                    </div>
                    <button type="submit" className="filter-button">Consultar</button>
                    <button type="button" className="filter-button filter-button-static" onClick={handleLimparFiltros}>Limpar</button>
                </form>

                <FeedbackMessage message={error} />

                {isLoading || filteredEscolas.length === 0 ? (
                    <EmptyState
                        loading={isLoading}
                        loadingMessage="Carregando escolas..."
                        emptyMessage="Nenhuma escola encontrada."
                        emptySubMessage={escolas.length === 0 ? 'Clique em Nova Escola para cadastrar.' : 'Tente ajustar os filtros.'}
                    />
                ) : (
                    <div className="table-responsive">
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Nome</th>
                                    <th>INEP</th>
                                    <th>Município</th>
                                    <th>Diretor (CPF)</th>
                                    <th>Status</th>
                                    <th>Ações</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredEscolas.map((escola) => (
                                    <tr key={escola.id}>
                                        <td>{escola.nome}</td>
                                        <td>{escola.codigoInep}</td>
                                        <td>{escola.municipio}</td>
                                        <td>{escola.cpfDiretor ? formatCpf(escola.cpfDiretor) : '—'}</td>
                                        <td><StatusPill status={escola.status} /></td>
                                        <td>
                                            <div className="action-group">
                                                <button type="button" className="table-action-button" onClick={() => handleEdit(escola)}>Editar</button>
                                                <button type="button" className="table-action-button danger" onClick={() => handleDelete(escola.id)}>Excluir</button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>
        </div>
    );
}

export default EscolasPage;

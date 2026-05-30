import { useEffect, useState, type FormEvent } from 'react';
import { useCrudData } from '../../hooks/useCrudData';
import { useCrudForm } from '../../hooks/useCrudForm';
import { formatCpf } from '../../utils/formatters';
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
    const { form, editingId, handleFieldChange, startEdit, clear } = useCrudForm(emptyEscolaForm);

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

    const filteredEscolas = escolas.filter(escola => {
        const matchNome = !appliedNome || escola.nome.toLowerCase().includes(appliedNome.toLowerCase());
        const matchCnpj = !appliedCnpj || escola.cnpj.replace(/\D/g, '').includes(appliedCnpj.replace(/\D/g, ''));
        const matchInep = !appliedInep || escola.codigoInep.includes(appliedInep);
        return matchNome && matchCnpj && matchInep;
    });

    const handleNovaEscola = () => {
        clear();
        setView('form');
    };

    const handleEdit = (escola: Escola) => {
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
        const saved = await save(editingId, form);
        if (saved) {
            clear();
            setView('list');
        }
    };

    const handleCancel = () => {
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
                        <button type="button" className="secondary-button" onClick={handleCancel}>
                            Cancelar
                        </button>
                    </div>

                    <FeedbackMessage message={error} />

                    <form className="cadastro-form escola-form" onSubmit={handleSubmit}>
                        <div className="form-grid">
                            <input name="codigoInep" value={form.codigoInep} onChange={handleFieldChange} type="text" placeholder="Código INEP" required />
                            <input name="nome" value={form.nome} onChange={handleFieldChange} type="text" placeholder="Nome da escola" required />
                            <input name="cnpj" value={form.cnpj} onChange={handleFieldChange} type="text" placeholder="CNPJ" required />
                            <input name="telefone" value={form.telefone} onChange={handleFieldChange} type="text" placeholder="Telefone" required />
                            <input name="emailInstitucional" value={form.emailInstitucional} onChange={handleFieldChange} type="email" placeholder="E-mail institucional" required />
                            <input name="municipio" value={form.municipio} onChange={handleFieldChange} type="text" placeholder="Município" required />
                            <input name="enderecoCompleto" value={form.enderecoCompleto} onChange={handleFieldChange} type="text" placeholder="Endereço completo" required />
                            <select name="status" value={form.status} onChange={handleFieldChange}>
                                <option value="ATIVO">Ativa</option>
                                <option value="INATIVO">Inativa</option>
                            </select>
                            <input name="cpfDiretor" value={form.cpfDiretor} onChange={handleFieldChange} type="text" placeholder="CPF do Diretor (somente números)" maxLength={14} />
                        </div>
                        <div className="form-actions">
                            <button type="submit" disabled={isSaving}>
                                {editingId !== null ? 'Atualizar Escola' : 'Salvar Escola'}
                            </button>
                            <button type="button" className="secondary-button" onClick={clear}>
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
                    <input
                        type="text"
                        placeholder="Nome"
                        value={filterNome}
                        onChange={e => setFilterNome(e.target.value)}
                        className="filter-input"
                    />
                    <input
                        type="text"
                        placeholder="CNPJ"
                        value={filterCnpj}
                        onChange={e => setFilterCnpj(e.target.value)}
                        className="filter-input"
                    />
                    <input
                        type="text"
                        placeholder="Código INEP"
                        value={filterInep}
                        onChange={e => setFilterInep(e.target.value)}
                        className="filter-input"
                    />
                    <button type="submit" className="filter-button">Consultar</button>
                    <button type="button" className="secondary-button" onClick={handleLimparFiltros}>Limpar</button>
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

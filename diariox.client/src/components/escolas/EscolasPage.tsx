import { useEffect, type FormEvent } from 'react';
import { useCrudData } from '../../hooks/useCrudData';
import { useCrudForm } from '../../hooks/useCrudForm';
import { formatCpf } from '../../utils/formatters';
import FeedbackMessage from '../ui/FeedbackMessage';
import StatusPill from '../ui/StatusPill';
import EmptyState from '../ui/EmptyState';

type EscolaStatus = 'ATIVO' | 'INATIVO';

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

    useEffect(() => {
        void load();
    }, []);

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
    };

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        const saved = await save(editingId, form);
        if (saved) clear();
    };

    const handleDelete = async (id: number) => {
        const deleted = await remove(id);
        if (deleted && editingId === id) clear();
    };

    return (
        <div className="school-page">
            <div className="content-card">
                <div className="section-header">
                    <div>
                        <h2>{editingId ? 'Editar Escola' : 'Cadastro de Escolas'}</h2>
                        <p>Cadastre, edite e remova escolas usando os dados do modelo principal.</p>
                    </div>
                    {editingId !== null ? (
                        <button type="button" className="secondary-button" onClick={clear}>
                            Cancelar edição
                        </button>
                    ) : null}
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
                    <button type="submit" disabled={isSaving}>
                        {editingId !== null ? 'Atualizar Escola' : 'Salvar Escola'}
                    </button>
                </form>
            </div>

            <div className="content-card">
                <div className="section-header">
                    <div>
                        <h2>Escolas cadastradas</h2>
                        <p>Lista sincronizada com o backend para visualização, edição e remoção.</p>
                    </div>
                </div>

                {isLoading || escolas.length === 0 ? (
                    <EmptyState
                        loading={isLoading}
                        loadingMessage="Carregando escolas..."
                        emptyMessage="Nenhuma escola cadastrada ainda."
                        emptySubMessage="Use o formulário acima para criar o primeiro registro."
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
                                {escolas.map((escola) => (
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

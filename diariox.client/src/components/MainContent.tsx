import { useState, type ChangeEvent, type FormEvent } from 'react';
import './MainContent.css';

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
};

interface MainContentProps {
    page: string;
}

function MainContent({ page }: MainContentProps) {
    const [escolas, setEscolas] = useState<Escola[]>([]);
    const [form, setForm] = useState<EscolaFormState>(emptyEscolaForm);
    const [editingId, setEditingId] = useState<number | null>(null);

    const handleFieldChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const { name, value } = event.target;

        setForm((currentForm) => ({
            ...currentForm,
            [name]: value,
        }));
    };

    const clearForm = () => {
        setForm(emptyEscolaForm);
        setEditingId(null);
    };

    const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        const escolaToSave: Escola = {
            ...form,
            id: editingId ?? Date.now(),
        };

        if (editingId !== null) {
            setEscolas((currentEscolas) =>
                currentEscolas.map((escola) =>
                    escola.id === editingId ? escolaToSave : escola
                )
            );
        } else {
            setEscolas((currentEscolas) => [...currentEscolas, escolaToSave]);
        }

        clearForm();
    };

    const handleEdit = (escola: Escola) => {
        setEditingId(escola.id);
        setForm({
            codigoInep: escola.codigoInep,
            nome: escola.nome,
            cnpj: escola.cnpj,
            telefone: escola.telefone,
            emailInstitucional: escola.emailInstitucional,
            municipio: escola.municipio,
            enderecoCompleto: escola.enderecoCompleto,
            status: escola.status,
        });
    };

    const handleDelete = (id: number) => {
        setEscolas((currentEscolas) => currentEscolas.filter((escola) => escola.id !== id));

        if (editingId === id) {
            clearForm();
        }
    };

    switch (page) {
        case 'escolas':
            return (
                <div className="school-page">
                    <div className="content-card">
                        <div className="section-header">
                            <div>
                                <h2>{editingId ? 'Editar Escola' : 'Cadastro de Escolas'}</h2>
                                <p>Cadastre, edite e remova escolas usando os dados do modelo principal.</p>
                            </div>
                            {editingId !== null ? (
                                <button type="button" className="secondary-button" onClick={clearForm}>
                                    Cancelar edição
                                </button>
                            ) : null}
                        </div>

                        <form className="cadastro-form escola-form" onSubmit={handleSubmit}>
                            <div className="form-grid">
                                <input
                                    name="codigoInep"
                                    value={form.codigoInep}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="Código INEP"
                                    required
                                />
                                <input
                                    name="nome"
                                    value={form.nome}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="Nome da escola"
                                    required
                                />
                                <input
                                    name="cnpj"
                                    value={form.cnpj}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="CNPJ"
                                    required
                                />
                                <input
                                    name="telefone"
                                    value={form.telefone}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="Telefone"
                                    required
                                />
                                <input
                                    name="emailInstitucional"
                                    value={form.emailInstitucional}
                                    onChange={handleFieldChange}
                                    type="email"
                                    placeholder="E-mail institucional"
                                    required
                                />
                                <input
                                    name="municipio"
                                    value={form.municipio}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="Município"
                                    required
                                />
                                <input
                                    name="enderecoCompleto"
                                    value={form.enderecoCompleto}
                                    onChange={handleFieldChange}
                                    type="text"
                                    placeholder="Endereço completo"
                                    required
                                />
                                <select name="status" value={form.status} onChange={handleFieldChange}>
                                    <option value="ATIVO">Ativa</option>
                                    <option value="INATIVO">Inativa</option>
                                </select>
                            </div>

                            <button type="submit">
                                {editingId !== null ? 'Atualizar Escola' : 'Salvar Escola'}
                            </button>
                        </form>
                    </div>

                    <div className="content-card">
                        <div className="section-header">
                            <div>
                                <h2>Escolas cadastradas</h2>
                                <p>Lista local do CRUD para visualização, edição e remoção.</p>
                            </div>
                        </div>

                        {escolas.length > 0 ? (
                            <div className="table-responsive">
                                <table className="data-table">
                                    <thead>
                                        <tr>
                                            <th>Nome</th>
                                            <th>INEP</th>
                                            <th>Município</th>
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
                                                <td>
                                                    <span
                                                        className={`status-pill ${escola.status === 'ATIVO' ? 'status-active' : 'status-inactive'}`}
                                                    >
                                                        {escola.status}
                                                    </span>
                                                </td>
                                                <td>
                                                    <div className="action-group">
                                                        <button
                                                            type="button"
                                                            className="table-action-button"
                                                            onClick={() => handleEdit(escola)}
                                                        >
                                                            Editar
                                                        </button>
                                                        <button
                                                            type="button"
                                                            className="table-action-button danger"
                                                            onClick={() => handleDelete(escola.id)}
                                                        >
                                                            Excluir
                                                        </button>
                                                    </div>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        ) : (
                            <div className="empty-state">
                                <strong>Nenhuma escola cadastrada ainda.</strong>
                                <span>Use o formulário acima para criar o primeiro registro.</span>
                            </div>
                        )}
                    </div>
                </div>
            );
        default:
            return (
                <div className="content-card">
                    <h2>Bem-vindo(a) ao Diário de Classe</h2>
                    <p>Selecione a opção de Escolas no menu lateral para gerenciar os registros.</p>
                </div>
            );
    }
}

export default MainContent;
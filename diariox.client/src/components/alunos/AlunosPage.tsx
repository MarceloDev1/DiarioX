import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { useCrudData } from '../../hooks/useCrudData';
import { apiFetch, readApiError } from '../../utils/api';
import { formatCpf, formatTelefone, formatCep } from '../../utils/formatters';
import { validateCpf } from '../../utils/validators';
import FeedbackMessage from '../ui/FeedbackMessage';
import EmptyState from '../ui/EmptyState';
import StatusPill from '../ui/StatusPill';

type View = 'list' | 'form';

interface Aluno {
    id: number;
    matricula: string;
    nome: string;
    dataNascimento: string;
    sexo: string;
    corRaca: string | null;
    necessidadeEspecial: boolean;
    cpfAluno: string | null;
    certidaoNascimento: string | null;
    responsavelNome1: string;
    responsavelCpf1: string;
    responsavelTelefone1: string;
    responsavelNome2: string | null;
    cep: string;
    enderecoCompleto: string;
    numero: string;
    bairro: string;
    escolaId: number;
    escolaNome: string;
    status: string;
    createdAt: string;
    updatedAt: string | null;
}

interface EscolaOption {
    id: number;
    nome: string;
    status: string;
}

interface AlunoFormState {
    nome: string;
    dataNascimento: string;
    sexo: string;
    corRaca: string;
    necessidadeEspecial: boolean;
    responsavelNome1: string;
    responsavelCpf1: string;
    responsavelTelefone1: string;
    responsavelNome2: string;
    cpfAluno: string;
    certidaoNascimento: string;
    cep: string;
    enderecoCompleto: string;
    numero: string;
    bairro: string;
    escolaId: string;
}

interface AlunoFieldErrors {
    nome: boolean;
    dataNascimento: boolean;
    sexo: boolean;
    responsavelNome1: boolean;
    responsavelCpf1: boolean;
    responsavelTelefone1: boolean;
    cpfAluno: boolean;
    certidaoNascimento: boolean;
    cep: boolean;
    enderecoCompleto: boolean;
    numero: boolean;
    bairro: boolean;
    escolaId: boolean;
}

const sexos = [
    { value: 'MASCULINO', label: 'Masculino' },
    { value: 'FEMININO', label: 'Feminino' },
];

// Categorias padrão do Censo Escolar/IBGE.
const corRacaOpcoes = [
    { value: 'BRANCA', label: 'Branca' },
    { value: 'PRETA', label: 'Preta' },
    { value: 'PARDA', label: 'Parda' },
    { value: 'AMARELA', label: 'Amarela' },
    { value: 'INDIGENA', label: 'Indígena' },
    { value: 'NAO_DECLARADA', label: 'Não declarada' },
];

const emptyForm: AlunoFormState = {
    nome: '',
    dataNascimento: '',
    sexo: '',
    corRaca: '',
    necessidadeEspecial: false,
    responsavelNome1: '',
    responsavelCpf1: '',
    responsavelTelefone1: '',
    responsavelNome2: '',
    cpfAluno: '',
    certidaoNascimento: '',
    cep: '',
    enderecoCompleto: '',
    numero: '',
    bairro: '',
    escolaId: '',
};

const emptyFieldErrors: AlunoFieldErrors = {
    nome: false,
    dataNascimento: false,
    sexo: false,
    responsavelNome1: false,
    responsavelCpf1: false,
    responsavelTelefone1: false,
    cpfAluno: false,
    certidaoNascimento: false,
    cep: false,
    enderecoCompleto: false,
    numero: false,
    bairro: false,
    escolaId: false,
};

function calcularIdade(dataNascimento: string): number {
    if (!dataNascimento) return 0;

    const nascimento = new Date(dataNascimento);
    const hoje = new Date();
    let idade = hoje.getFullYear() - nascimento.getFullYear();
    const aindaNaoFezAniversario =
        hoje.getMonth() < nascimento.getMonth() ||
        (hoje.getMonth() === nascimento.getMonth() && hoje.getDate() < nascimento.getDate());

    if (aindaNaoFezAniversario) idade--;
    return idade;
}

interface AlunosPageProps {
    onEnturmar: (alunoId: number) => void;
}

function AlunosPage({ onEnturmar }: AlunosPageProps) {
    const { items: alunos, isLoading, isSaving, error, load, save, remove } = useCrudData<Aluno>('/api/alunos');

    const [view, setView] = useState<View>('list');
    const [form, setForm] = useState<AlunoFormState>(emptyForm);
    const [fieldErrors, setFieldErrors] = useState<AlunoFieldErrors>(emptyFieldErrors);
    const [localError, setLocalError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);
    const [editingId, setEditingId] = useState<number | null>(null);

    const [statusUpdatingId, setStatusUpdatingId] = useState<number | null>(null);

    const [escolas, setEscolas] = useState<EscolaOption[]>([]);

    useEffect(() => {
        let cancelled = false;

        async function loadEscolas() {
            try {
                const response = await apiFetch('/api/escolas');
                if (cancelled) return;
                if (response.ok) {
                    const data = (await response.json()) as EscolaOption[];
                    setEscolas(data.filter(escola => escola.status === 'ATIVO'));
                }
            } catch {
                if (!cancelled) setLocalError('Falha ao carregar escolas do formulário.');
            }
        }

        void load();
        void loadEscolas();
        return () => { cancelled = true; };
    }, []);

    const handleFieldChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
        const { name, value, type } = event.currentTarget;

        if (type === 'checkbox') {
            const checked = (event.currentTarget as HTMLInputElement).checked;
            setForm(current => ({ ...current, [name]: checked }));
        } else {
            setForm(current => ({ ...current, [name]: value }));
        }

        setFieldErrors(current => ({ ...current, [name]: false }));
        setLocalError(null);
    };

    const idadeAtual = calcularIdade(form.dataNascimento);
    const cpfAlunoObrigatorio = idadeAtual >= 18;

    const validateForm = (): boolean => {
        const responsavelCpfDigits = form.responsavelCpf1.replace(/\D/g, '');
        const cpfAlunoDigits = form.cpfAluno.replace(/\D/g, '');
        const possuiCpfAluno = cpfAlunoDigits.length > 0;

        const nextErrors: AlunoFieldErrors = {
            nome: !form.nome.trim(),
            dataNascimento: !form.dataNascimento,
            sexo: !form.sexo,
            responsavelNome1: !form.responsavelNome1.trim(),
            responsavelCpf1: !validateCpf(responsavelCpfDigits),
            responsavelTelefone1: !form.responsavelTelefone1.trim(),
            cpfAluno: (cpfAlunoObrigatorio && !possuiCpfAluno) || (possuiCpfAluno && !validateCpf(cpfAlunoDigits)),
            certidaoNascimento: !possuiCpfAluno && !form.certidaoNascimento.trim(),
            cep: !form.cep.trim(),
            enderecoCompleto: !form.enderecoCompleto.trim(),
            numero: !form.numero.trim(),
            bairro: !form.bairro.trim(),
            escolaId: !form.escolaId,
        };

        setFieldErrors(nextErrors);
        const hasErrors = Object.values(nextErrors).some(Boolean);

        if (hasErrors) {
            setLocalError('Por favor, preencha todos os campos obrigatórios para prosseguir.');
            return false;
        }

        return true;
    };

    const buildRequestBody = () => ({
        nome: form.nome,
        dataNascimento: form.dataNascimento,
        sexo: form.sexo,
        corRaca: form.corRaca || null,
        necessidadeEspecial: form.necessidadeEspecial,
        responsavelNome1: form.responsavelNome1,
        responsavelCpf1: form.responsavelCpf1.replace(/\D/g, ''),
        responsavelTelefone1: form.responsavelTelefone1.replace(/\D/g, ''),
        responsavelNome2: form.responsavelNome2 || null,
        cpfAluno: form.cpfAluno ? form.cpfAluno.replace(/\D/g, '') : null,
        certidaoNascimento: form.certidaoNascimento || null,
        cep: form.cep.replace(/\D/g, ''),
        enderecoCompleto: form.enderecoCompleto,
        numero: form.numero,
        bairro: form.bairro,
        escolaId: parseInt(form.escolaId, 10),
    });

    const submitAluno = async (enturmar: boolean) => {
        if (!validateForm()) return;

        const result = await save(editingId, buildRequestBody());
        if (result) {
            if (enturmar && !editingId) {
                setForm(emptyForm);
                setEditingId(null);
                setFieldErrors(emptyFieldErrors);
                onEnturmar(result.id);
                return;
            }

            const base = editingId
                ? 'Aluno atualizado com sucesso!'
                : `Aluno cadastrado com sucesso! Matrícula gerada: ${result.matricula}`;
            setSuccessMessage(enturmar ? `${base} Enturmação estará disponível em breve.` : base);
            setView('list');
            setForm(emptyForm);
            setEditingId(null);
            setFieldErrors(emptyFieldErrors);
            setTimeout(() => setSuccessMessage(null), 4000);
        }
    };

    const handleFormSubmit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        void submitAluno(false);
    };

    const handleSalvarEEnturmar = () => {
        void submitAluno(true);
    };

    const handleEditClick = (aluno: Aluno) => {
        setEditingId(aluno.id);
        setForm({
            nome: aluno.nome,
            dataNascimento: aluno.dataNascimento.slice(0, 10),
            sexo: aluno.sexo,
            corRaca: aluno.corRaca ?? '',
            necessidadeEspecial: aluno.necessidadeEspecial,
            responsavelNome1: aluno.responsavelNome1,
            responsavelCpf1: aluno.responsavelCpf1,
            responsavelTelefone1: aluno.responsavelTelefone1,
            responsavelNome2: aluno.responsavelNome2 ?? '',
            cpfAluno: aluno.cpfAluno ?? '',
            certidaoNascimento: aluno.certidaoNascimento ?? '',
            cep: aluno.cep,
            enderecoCompleto: aluno.enderecoCompleto,
            numero: aluno.numero,
            bairro: aluno.bairro,
            escolaId: aluno.escolaId.toString(),
        });
        setView('form');
        setLocalError(null);
        setFieldErrors(emptyFieldErrors);
    };

    const handleDeleteClick = async (id: number) => {
        if (window.confirm('Tem certeza que deseja remover este aluno?')) {
            const success = await remove(id);
            if (success) {
                setSuccessMessage('Aluno removido com sucesso!');
                setTimeout(() => setSuccessMessage(null), 3000);
            }
        }
    };

    const handleToggleStatus = async (aluno: Aluno) => {
        const inativar = aluno.status !== 'INATIVO';
        const confirmMessage = inativar
            ? 'Tem certeza que deseja inativar este aluno? Alunos inativos não podem ser enturmados nem remanejados.'
            : 'Tem certeza que deseja ativar este aluno?';
        if (!window.confirm(confirmMessage)) return;

        setSuccessMessage(null);
        setLocalError(null);
        setStatusUpdatingId(aluno.id);
        try {
            const response = await apiFetch(`/api/alunos/${aluno.id}/status`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ status: inativar ? 'INATIVO' : 'ATIVO' }),
            });
            if (!response.ok) throw new Error(await readApiError(response));

            setSuccessMessage(inativar ? 'Aluno inativado com sucesso!' : 'Aluno ativado com sucesso!');
            setTimeout(() => setSuccessMessage(null), 3000);
            await load();
        } catch (e) {
            setLocalError(e instanceof Error ? e.message : 'Falha ao alterar o status do aluno.');
        } finally {
            setStatusUpdatingId(null);
        }
    };

    const handleNewAluno = () => {
        setEditingId(null);
        setForm(emptyForm);
        setFieldErrors(emptyFieldErrors);
        setLocalError(null);
        setView('form');
    };

    // Alternativa 01: descarta as informações preenchidas sem persistir.
    const handleCancelar = () => {
        setView('list');
        setForm(emptyForm);
        setEditingId(null);
        setLocalError(null);
        setFieldErrors(emptyFieldErrors);
    };

    return (
        <div className="page-container">
            <div className="page-header">
                <h1> Alunos</h1>
            </div>

            <FeedbackMessage message={error} type="error" />
            <FeedbackMessage message={localError} type="error" />
            <FeedbackMessage message={successMessage} type="success" />

            {view === 'list' ? (
                <div className="list-view">
                    <div className="list-header">
                        <button className="btn btn-primary" onClick={handleNewAluno}>
                            ➕ Novo Aluno
                        </button>
                    </div>

                    {isLoading ? (
                        <div className="loading">Carregando alunos...</div>
                    ) : alunos.length === 0 ? (
                        <EmptyState emptyMessage="Nenhum aluno cadastrado. Clique em 'Novo Aluno' para começar." />
                    ) : (
                        <div className="table-container">
                            <table className="data-table">
                                <thead>
                                    <tr>
                                        <th>Nome</th>
                                        <th>Matrícula</th>
                                        <th>CPF do Aluno</th>
                                        <th>Responsável</th>
                                        <th>Telefone</th>
                                        <th>Escola</th>
                                        <th>Status</th>
                                        <th>Ações</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {alunos.map(aluno => (
                                        <tr key={aluno.id}>
                                            <td>{aluno.nome}</td>
                                            <td>{aluno.matricula}</td>
                                            <td className="nowrap-cell">{aluno.cpfAluno ? formatCpf(aluno.cpfAluno) : '—'}</td>
                                            <td>{aluno.responsavelNome1}</td>
                                            <td className="nowrap-cell">{formatTelefone(aluno.responsavelTelefone1)}</td>
                                            <td>{aluno.escolaNome}</td>
                                            <td>
                                                <StatusPill
                                                    status={aluno.status as 'ATIVO' | 'INATIVO' | 'BLOQUEADO' | 'ATIVO_AGUARDANDO_ENTURMACAO'}
                                                    label={aluno.status === 'ATIVO_AGUARDANDO_ENTURMACAO' ? 'Aguardando Enturmação' : undefined}
                                                />
                                            </td>
                                            <td>
                                                <div className="action-group vertical">
                                                    <button type="button" className="table-action-button" onClick={() => handleEditClick(aluno)}>Editar</button>
                                                    <button
                                                        type="button"
                                                        className="table-action-button"
                                                        onClick={() => handleToggleStatus(aluno)}
                                                        disabled={statusUpdatingId === aluno.id}
                                                    >
                                                        {aluno.status === 'INATIVO' ? 'Ativar' : 'Inativar'}
                                                    </button>
                                                    <button type="button" className="table-action-button danger" onClick={() => handleDeleteClick(aluno.id)}>Excluir</button>
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            ) : (
                <div className="form-view">
                    <div className="form-header">
                        <h2>{editingId ? 'Editar Aluno' : 'Novo Aluno'}</h2>
                        <button className="btn btn-secondary" onClick={handleCancelar}>
                            ← Voltar
                        </button>
                    </div>

                    <form onSubmit={handleFormSubmit} className="form-grid">
                        <fieldset className="form-section">
                            <legend>📋 Dados do Aluno</legend>

                            <div className="form-group">
                                <label htmlFor="nome">
                                    Nome Completo <span className="required">*</span>
                                </label>
                                <input
                                    type="text"
                                    id="nome"
                                    name="nome"
                                    value={form.nome}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.nome ? 'input-error' : ''}
                                    disabled={isSaving}
                                />
                                {fieldErrors.nome && <span className="field-error">Nome completo é obrigatório</span>}
                            </div>

                            <div className="form-group">
                                <label htmlFor="dataNascimento">
                                    Data de Nascimento <span className="required">*</span>
                                </label>
                                <input
                                    type="date"
                                    id="dataNascimento"
                                    name="dataNascimento"
                                    value={form.dataNascimento}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.dataNascimento ? 'input-error' : ''}
                                    disabled={isSaving}
                                />
                                {fieldErrors.dataNascimento && (
                                    <span className="field-error">Data de nascimento é obrigatória</span>
                                )}
                            </div>

                            <div className="form-group">
                                <label htmlFor="sexo">
                                    Sexo/Gênero <span className="required">*</span>
                                </label>
                                <select
                                    id="sexo"
                                    name="sexo"
                                    value={form.sexo}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.sexo ? 'input-error' : ''}
                                    disabled={isSaving}
                                >
                                    <option value="">Selecione...</option>
                                    {sexos.map(sexo => (
                                        <option key={sexo.value} value={sexo.value}>
                                            {sexo.label}
                                        </option>
                                    ))}
                                </select>
                                {fieldErrors.sexo && <span className="field-error">Sexo/Gênero é obrigatório</span>}
                            </div>

                            <div className="form-group">
                                <label htmlFor="corRaca">Cor/Raça</label>
                                <select
                                    id="corRaca"
                                    name="corRaca"
                                    value={form.corRaca}
                                    onChange={handleFieldChange}
                                    disabled={isSaving}
                                >
                                    <option value="">Não informado</option>
                                    {corRacaOpcoes.map(opcao => (
                                        <option key={opcao.value} value={opcao.value}>
                                            {opcao.label}
                                        </option>
                                    ))}
                                </select>
                            </div>

                            <div className="form-group">
                                <label className="checkbox-label">
                                    <input
                                        type="checkbox"
                                        name="necessidadeEspecial"
                                        checked={form.necessidadeEspecial}
                                        onChange={handleFieldChange}
                                        disabled={isSaving}
                                    />
                                    <span>Necessidade Especial</span>
                                </label>
                            </div>
                        </fieldset>

                        <fieldset className="form-section">
                            <legend>👪 Dados dos Responsáveis</legend>

                            <div className="form-group">
                                <label htmlFor="responsavelNome1">
                                    Nome do Responsável Legal 1 <span className="required">*</span>
                                </label>
                                <input
                                    type="text"
                                    id="responsavelNome1"
                                    name="responsavelNome1"
                                    value={form.responsavelNome1}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.responsavelNome1 ? 'input-error' : ''}
                                    disabled={isSaving}
                                />
                                {fieldErrors.responsavelNome1 && (
                                    <span className="field-error">Nome do responsável é obrigatório</span>
                                )}
                            </div>

                            <div className="form-group">
                                <label htmlFor="responsavelCpf1">
                                    CPF do Responsável 1 <span className="required">*</span>
                                </label>
                                <input
                                    type="text"
                                    id="responsavelCpf1"
                                    name="responsavelCpf1"
                                    value={formatCpf(form.responsavelCpf1)}
                                    onChange={e => setForm(current => ({ ...current, responsavelCpf1: e.target.value }))}
                                    placeholder="000.000.000-00"
                                    className={fieldErrors.responsavelCpf1 ? 'input-error' : ''}
                                    disabled={isSaving}
                                    maxLength={14}
                                />
                                {fieldErrors.responsavelCpf1 && <span className="field-error">CPF inválido.</span>}
                            </div>

                            <div className="form-group">
                                <label htmlFor="responsavelTelefone1">
                                    Telefone de Contato <span className="required">*</span>
                                </label>
                                <input
                                    type="text"
                                    id="responsavelTelefone1"
                                    name="responsavelTelefone1"
                                    value={formatTelefone(form.responsavelTelefone1)}
                                    onChange={e => setForm(current => ({ ...current, responsavelTelefone1: e.target.value }))}
                                    placeholder="(00) 00000-0000"
                                    className={fieldErrors.responsavelTelefone1 ? 'input-error' : ''}
                                    disabled={isSaving}
                                    maxLength={15}
                                />
                                {fieldErrors.responsavelTelefone1 && (
                                    <span className="field-error">Telefone de contato é obrigatório</span>
                                )}
                            </div>

                            <div className="form-group">
                                <label htmlFor="responsavelNome2">Nome do Responsável 2</label>
                                <input
                                    type="text"
                                    id="responsavelNome2"
                                    name="responsavelNome2"
                                    value={form.responsavelNome2}
                                    onChange={handleFieldChange}
                                    disabled={isSaving}
                                />
                            </div>
                        </fieldset>

                        <fieldset className="form-section">
                            <legend>📄 Documentação/Endereço</legend>

                            <div className="form-group">
                                <label htmlFor="cpfAluno">
                                    CPF do Aluno {cpfAlunoObrigatorio && <span className="required">*</span>}
                                </label>
                                <input
                                    type="text"
                                    id="cpfAluno"
                                    name="cpfAluno"
                                    value={formatCpf(form.cpfAluno)}
                                    onChange={e => setForm(current => ({ ...current, cpfAluno: e.target.value }))}
                                    placeholder="000.000.000-00"
                                    className={fieldErrors.cpfAluno ? 'input-error' : ''}
                                    disabled={isSaving}
                                    maxLength={14}
                                />
                                {fieldErrors.cpfAluno && (
                                    <span className="field-error">
                                        {cpfAlunoObrigatorio ? 'CPF do aluno é obrigatório para maiores de 18 anos.' : 'CPF inválido.'}
                                    </span>
                                )}
                            </div>

                            <div className="form-group">
                                <label htmlFor="certidaoNascimento">
                                    Certidão de Nascimento {!form.cpfAluno && <span className="required">*</span>}
                                </label>
                                <input
                                    type="text"
                                    id="certidaoNascimento"
                                    name="certidaoNascimento"
                                    value={form.certidaoNascimento}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.certidaoNascimento ? 'input-error' : ''}
                                    disabled={isSaving}
                                />
                                {fieldErrors.certidaoNascimento && (
                                    <span className="field-error">Certidão obrigatória quando o aluno não possui CPF</span>
                                )}
                            </div>

                            <div className="form-group">
                                <label htmlFor="cep">
                                    CEP <span className="required">*</span>
                                </label>
                                <input
                                    type="text"
                                    id="cep"
                                    name="cep"
                                    value={formatCep(form.cep)}
                                    onChange={e => setForm(current => ({ ...current, cep: e.target.value }))}
                                    placeholder="00000-000"
                                    className={fieldErrors.cep ? 'input-error' : ''}
                                    disabled={isSaving}
                                    maxLength={9}
                                />
                                {fieldErrors.cep && <span className="field-error">CEP é obrigatório</span>}
                            </div>

                            <div className="form-group">
                                <label htmlFor="enderecoCompleto">
                                    Endereço Completo <span className="required">*</span>
                                </label>
                                <input
                                    type="text"
                                    id="enderecoCompleto"
                                    name="enderecoCompleto"
                                    value={form.enderecoCompleto}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.enderecoCompleto ? 'input-error' : ''}
                                    disabled={isSaving}
                                />
                                {fieldErrors.enderecoCompleto && (
                                    <span className="field-error">Endereço completo é obrigatório</span>
                                )}
                            </div>

                            <div className="form-group">
                                <label htmlFor="numero">
                                    Número <span className="required">*</span>
                                </label>
                                <input
                                    type="text"
                                    id="numero"
                                    name="numero"
                                    value={form.numero}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.numero ? 'input-error' : ''}
                                    disabled={isSaving}
                                />
                                {fieldErrors.numero && <span className="field-error">Número é obrigatório</span>}
                            </div>

                            <div className="form-group">
                                <label htmlFor="bairro">
                                    Bairro <span className="required">*</span>
                                </label>
                                <input
                                    type="text"
                                    id="bairro"
                                    name="bairro"
                                    value={form.bairro}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.bairro ? 'input-error' : ''}
                                    disabled={isSaving}
                                />
                                {fieldErrors.bairro && <span className="field-error">Bairro é obrigatório</span>}
                            </div>

                            <div className="form-group">
                                <label htmlFor="escolaId">
                                    Escola <span className="required">*</span>
                                </label>
                                <select
                                    id="escolaId"
                                    name="escolaId"
                                    value={form.escolaId}
                                    onChange={handleFieldChange}
                                    className={fieldErrors.escolaId ? 'input-error' : ''}
                                    disabled={isSaving}
                                >
                                    <option value="">Selecione...</option>
                                    {escolas.map(escola => (
                                        <option key={escola.id} value={escola.id}>
                                            {escola.nome}
                                        </option>
                                    ))}
                                </select>
                                {fieldErrors.escolaId && <span className="field-error">Escola é obrigatória</span>}
                            </div>
                        </fieldset>

                        <div className="form-actions">
                            <button type="submit" className="btn btn-primary" disabled={isSaving}>
                                {isSaving ? 'Salvando...' : 'Salvar'}
                            </button>
                            {!editingId && (
                                <button
                                    type="button"
                                    className="btn btn-primary"
                                    onClick={handleSalvarEEnturmar}
                                    disabled={isSaving}
                                >
                                    Salvar e Enturmar
                                </button>
                            )}
                            <button type="button" className="btn btn-secondary" onClick={handleCancelar} disabled={isSaving}>
                                Cancelar
                            </button>
                        </div>
                    </form>
                </div>
            )}
        </div>
    );
}

export default AlunosPage;

import { useState, type FormEvent } from 'react';
import './Login.css';

interface LoginProps {
    onLogin: (username: string) => void;
}

type ViewMode = 'login' | 'first-access';
type FirstAccessStep = 'validate' | 'activate' | 'success';

function normalizeCpf(value: string) {
    return value.replace(/\D/g, '');
}

function formatCpf(value: string) {
    const cpf = normalizeCpf(value).slice(0, 11);
    return cpf
        .replace(/(\d{3})(\d)/, '$1.$2')
        .replace(/(\d{3})(\d)/, '$1.$2')
        .replace(/(\d{3})(\d{1,2})$/, '$1-$2');
}

function validateCpf(cpf: string) {
    const onlyDigits = normalizeCpf(cpf);
    if (onlyDigits.length !== 11 || /^(\d)\1+$/.test(onlyDigits)) {
        return false;
    }

    const numbers = onlyDigits.split('').map(Number);
    const calculateDigit = (length: number) => {
        let sum = 0;
        for (let i = 0; i < length; i += 1) {
            sum += numbers[i] * (length + 1 - i);
        }

        const remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    };

    const digit1 = calculateDigit(9);
    const digit2 = calculateDigit(10);
    return numbers[9] === digit1 && numbers[10] === digit2;
}

function validateBirthDate(value: string) {
    if (!value) {
        return false;
    }

    const birthDate = new Date(value);
    if (Number.isNaN(birthDate.getTime())) {
        return false;
    }

    const today = new Date();
    return birthDate < today;
}

function validatePassword(password: string) {
    return /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$/.test(password);
}

function Login({ onLogin }: LoginProps) {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    const [viewMode, setViewMode] = useState<ViewMode>('login');

    const [firstAccessStep, setFirstAccessStep] = useState<FirstAccessStep>('validate');
    const [cpf, setCpf] = useState('');
    const [birthDate, setBirthDate] = useState('');
    const [email, setEmail] = useState('');
    const [emailConfirm, setEmailConfirm] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [newPasswordConfirm, setNewPasswordConfirm] = useState('');
    const [firstAccessError, setFirstAccessError] = useState('');
    const [firstAccessSuccess, setFirstAccessSuccess] = useState('');
    const [firstAccessLoading, setFirstAccessLoading] = useState(false);

    function resetFirstAccessState() {
        setFirstAccessStep('validate');
        setCpf('');
        setBirthDate('');
        setEmail('');
        setEmailConfirm('');
        setNewPassword('');
        setNewPasswordConfirm('');
        setFirstAccessError('');
        setFirstAccessSuccess('');
        setFirstAccessLoading(false);
    }

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();
        setError('');

        if (!username.trim() || !password.trim()) {
            setError('Preencha todos os campos.');
            return;
        }

        setLoading(true);
        try {
            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ login: username, password }),
            });

            if (response.ok) {
                const data: { email?: string } = await response.json();
                onLogin(data.email ?? username);
            } else {
                setError('Usuário ou senha inválidos.');
            }
        } catch {
            setError('Erro ao conectar ao servidor.');
        } finally {
            setLoading(false);
        }
    }

    const handleForgotPassword = () => {
        alert('Funcionalidade de recuperação de senha será implementada.');
        // Redirecionar para página de esqueci minha senha
    };

    const handleFirstAccess = () => {
        setViewMode('first-access');
        resetFirstAccessState();
    };

    const handleBackToLogin = () => {
        setViewMode('login');
        setFirstAccessError('');
        setFirstAccessSuccess('');
    };

    async function handleValidateInstitutionalData(e: FormEvent) {
        e.preventDefault();
        setFirstAccessError('');
        setFirstAccessSuccess('');

        if (!validateCpf(cpf)) {
            setFirstAccessError('Informe um CPF valido.');
            return;
        }

        if (!validateBirthDate(birthDate)) {
            setFirstAccessError('Informe uma data de nascimento valida.');
            return;
        }

        setFirstAccessLoading(true);
        try {
            const payload = {
                cpf: normalizeCpf(cpf),
                birthDate,
            };

            const response = await fetch('/api/auth/first-access/validate', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload),
            });

            if (response.ok || response.status === 404) {
                setFirstAccessStep('activate');
                return;
            }

            setFirstAccessError('Nao foi possivel validar seus dados institucionais.');
        } catch {
            setFirstAccessError('Erro ao conectar ao servidor para validacao.');
        } finally {
            setFirstAccessLoading(false);
        }
    }

    async function handleActivateAccount(e: FormEvent) {
        e.preventDefault();
        setFirstAccessError('');
        setFirstAccessSuccess('');

        const trimmedEmail = email.trim().toLowerCase();
        const trimmedEmailConfirm = emailConfirm.trim().toLowerCase();

        if (!trimmedEmail || !trimmedEmailConfirm) {
            setFirstAccessError('Preencha e confirme o e-mail.');
            return;
        }

        if (trimmedEmail !== trimmedEmailConfirm) {
            setFirstAccessError('Os e-mails informados nao coincidem.');
            return;
        }

        if (!validatePassword(newPassword)) {
            setFirstAccessError(
                'A senha deve ter no minimo 8 caracteres, com letra maiuscula, minuscula, numero e simbolo.',
            );
            return;
        }

        if (newPassword !== newPasswordConfirm) {
            setFirstAccessError('As senhas informadas nao coincidem.');
            return;
        }

        setFirstAccessLoading(true);
        try {
            const payload = {
                cpf: normalizeCpf(cpf),
                birthDate,
                email: trimmedEmail,
                password: newPassword,
            };

            const response = await fetch('/api/auth/first-access/activate', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload),
            });

            if (response.ok || response.status === 404) {
                setFirstAccessStep('success');
                setFirstAccessSuccess('Conta ativada com sucesso! Agora faca seu primeiro login.');
                setTimeout(() => {
                    setViewMode('login');
                }, 1600);
                return;
            }

            setFirstAccessError('Nao foi possivel ativar sua conta. Verifique os dados e tente novamente.');
        } catch {
            setFirstAccessError('Erro ao conectar ao servidor durante a ativacao.');
        } finally {
            setFirstAccessLoading(false);
        }
    }

    return (
        <div className="login-container">
            <div className="login-card">
                <h1 className="login-title">Diário de Classe</h1>
                {viewMode === 'login' && (
                    <>
                        <p className="login-description">
                            Use seu e-mail ou CPF cadastrado para acessar o sistema.
                        </p>

                        <form onSubmit={handleSubmit} className="login-form" noValidate>
                            <div className="form-group">
                                <label htmlFor="username">Usuário (E-mail ou CPF)</label>
                                <input
                                    id="username"
                                    type="text"
                                    autoComplete="username"
                                    placeholder="seu@email.com ou 00000000000"
                                    value={username}
                                    onChange={e => setUsername(e.target.value)}
                                    disabled={loading}
                                />
                            </div>

                            <div className="form-group">
                                <div className="password-header">
                                    <label htmlFor="password">Senha</label>
                                    <button
                                        type="button"
                                        className="forgot-password-link"
                                        onClick={handleForgotPassword}
                                    >
                                        Esqueci minha senha
                                    </button>
                                </div>
                                <input
                                    id="password"
                                    type="password"
                                    autoComplete="current-password"
                                    placeholder="Digite sua senha"
                                    value={password}
                                    onChange={e => setPassword(e.target.value)}
                                    disabled={loading}
                                />
                            </div>

                            {error && <p className="login-error">{error}</p>}

                            <button type="submit" className="login-button" disabled={loading}>
                                {loading ? 'Entrando...' : 'Entrar'}
                            </button>

                            <div className="first-access">
                                <span>E seu primeiro acesso?</span>{' '}
                                <button
                                    type="button"
                                    className="first-access-link"
                                    onClick={handleFirstAccess}
                                >
                                    Primeiro Acesso
                                </button>
                            </div>
                        </form>
                    </>
                )}

                {viewMode === 'first-access' && (
                    <>
                        <p className="login-description">
                            Ative sua conta em duas etapas para realizar o primeiro login.
                        </p>

                        <div className="first-access-progress" aria-label="Progresso do primeiro acesso">
                            <span className={firstAccessStep === 'validate' ? 'active' : ''}>1. Validacao</span>
                            <span className={firstAccessStep === 'activate' ? 'active' : ''}>2. Ativacao</span>
                        </div>

                        {firstAccessStep === 'validate' && (
                            <form onSubmit={handleValidateInstitutionalData} className="login-form" noValidate>
                                <div className="form-group">
                                    <label htmlFor="first-access-cpf">CPF</label>
                                    <input
                                        id="first-access-cpf"
                                        type="text"
                                        inputMode="numeric"
                                        placeholder="000.000.000-00"
                                        value={cpf}
                                        onChange={e => setCpf(formatCpf(e.target.value))}
                                        disabled={firstAccessLoading}
                                    />
                                </div>

                                <div className="form-group">
                                    <label htmlFor="first-access-birth-date">Data de nascimento</label>
                                    <input
                                        id="first-access-birth-date"
                                        type="date"
                                        value={birthDate}
                                        onChange={e => setBirthDate(e.target.value)}
                                        disabled={firstAccessLoading}
                                    />
                                </div>

                                {firstAccessError && <p className="login-error">{firstAccessError}</p>}

                                <button type="submit" className="login-button" disabled={firstAccessLoading}>
                                    {firstAccessLoading ? 'Validando...' : 'Validar dados'}
                                </button>

                                <button
                                    type="button"
                                    className="secondary-button"
                                    onClick={handleBackToLogin}
                                    disabled={firstAccessLoading}
                                >
                                    Voltar para login
                                </button>
                            </form>
                        )}

                        {firstAccessStep === 'activate' && (
                            <form onSubmit={handleActivateAccount} className="login-form" noValidate>
                                <div className="form-group">
                                    <label htmlFor="first-access-email">Confirmacao de e-mail</label>
                                    <input
                                        id="first-access-email"
                                        type="email"
                                        autoComplete="email"
                                        placeholder="seu@email.com"
                                        value={email}
                                        onChange={e => setEmail(e.target.value)}
                                        disabled={firstAccessLoading}
                                    />
                                </div>

                                <div className="form-group">
                                    <label htmlFor="first-access-email-confirm">Repita o e-mail</label>
                                    <input
                                        id="first-access-email-confirm"
                                        type="email"
                                        autoComplete="email"
                                        placeholder="seu@email.com"
                                        value={emailConfirm}
                                        onChange={e => setEmailConfirm(e.target.value)}
                                        disabled={firstAccessLoading}
                                    />
                                </div>

                                <div className="form-group">
                                    <label htmlFor="first-access-password">Nova senha</label>
                                    <input
                                        id="first-access-password"
                                        type="password"
                                        autoComplete="new-password"
                                        placeholder="Crie uma senha forte"
                                        value={newPassword}
                                        onChange={e => setNewPassword(e.target.value)}
                                        disabled={firstAccessLoading}
                                    />
                                </div>

                                <div className="form-group">
                                    <label htmlFor="first-access-password-confirm">Repita a nova senha</label>
                                    <input
                                        id="first-access-password-confirm"
                                        type="password"
                                        autoComplete="new-password"
                                        placeholder="Repita a senha"
                                        value={newPasswordConfirm}
                                        onChange={e => setNewPasswordConfirm(e.target.value)}
                                        disabled={firstAccessLoading}
                                    />
                                </div>

                                {firstAccessError && <p className="login-error">{firstAccessError}</p>}

                                <button type="submit" className="login-button" disabled={firstAccessLoading}>
                                    {firstAccessLoading ? 'Ativando conta...' : 'Ativar conta'}
                                </button>

                                <button
                                    type="button"
                                    className="secondary-button"
                                    onClick={handleBackToLogin}
                                    disabled={firstAccessLoading}
                                >
                                    Cancelar
                                </button>
                            </form>
                        )}

                        {firstAccessStep === 'success' && (
                            <div className="first-access-success" role="status" aria-live="polite">
                                {firstAccessSuccess}
                            </div>
                        )}
                    </>
                )}
            </div>
        </div>
    );
}

export default Login;
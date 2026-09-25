import { useState, type FormEvent } from 'react';
import { FiMail, FiLock, FiEye, FiEyeOff } from 'react-icons/fi';
import { startSession, type LoginResponse, type Session } from '../../utils/api';

interface LoginFormProps {
    onLogin: (session: Session) => void;
    onForgotPassword: () => void;
    onFirstAccess: () => void;
}

function LoginForm({ onLogin, onForgotPassword, onFirstAccess }: LoginFormProps) {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);
    const [showPassword, setShowPassword] = useState(false);

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
                const data = (await response.json()) as LoginResponse;
                onLogin(startSession(data));
            } else {
                const data: { message?: string } = await response.json().catch(() => ({}));
                setError(data.message ?? 'Usuário ou senha inválidos.');
            }
        } catch {
            setError('Erro ao conectar ao servidor.');
        } finally {
            setLoading(false);
        }
    }

    return (
        <>
            <p className="login-description">Use seu e-mail ou CPF cadastrado para acessar o sistema.</p>

            <form onSubmit={handleSubmit} className="login-form" noValidate>
                <div className="form-group">
                    <label htmlFor="username">Usuário (E-mail ou CPF)</label>
                    <div className="input-with-icon">
                        <FiMail className="input-icon" />
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
                </div>

                <div className="form-group">
                    <div className="password-header">
                        <label htmlFor="password">Senha</label>
                        <button type="button" className="forgot-password-link" onClick={onForgotPassword}>
                            Esqueci minha senha
                        </button>
                    </div>
                    <div className="input-with-icon">
                        <FiLock className="input-icon" />
                        <input
                            id="password"
                            type={showPassword ? 'text' : 'password'}
                            autoComplete="current-password"
                            placeholder="Digite sua senha"
                            value={password}
                            onChange={e => setPassword(e.target.value)}
                            disabled={loading}
                        />
                        <button
                            type="button"
                            className="toggle-password-visibility"
                            onClick={() => setShowPassword(show => !show)}
                            tabIndex={-1}
                            aria-label={showPassword ? 'Ocultar senha' : 'Mostrar senha'}
                        >
                            {showPassword ? <FiEyeOff /> : <FiEye />}
                        </button>
                    </div>
                </div>

                {error && <p className="login-error">{error}</p>}

                <button type="submit" className="login-button" disabled={loading}>
                    {loading ? 'Entrando...' : 'Entrar'}
                </button>

                <div className="first-access">
                    <span>É seu primeiro acesso?</span>{' '}
                    <button type="button" className="first-access-link" onClick={onFirstAccess}>
                        Primeiro Acesso
                    </button>
                </div>
            </form>
        </>
    );
}

export default LoginForm;

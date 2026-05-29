import { useState, type FormEvent } from 'react';

interface LoginFormProps {
    onLogin: (username: string) => void;
    onForgotPassword: () => void;
    onFirstAccess: () => void;
}

function LoginForm({ onLogin, onForgotPassword, onFirstAccess }: LoginFormProps) {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

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

    return (
        <>
            <p className="login-description">Use seu e-mail ou CPF cadastrado para acessar o sistema.</p>

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
                        <button type="button" className="forgot-password-link" onClick={onForgotPassword}>
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
                    <button type="button" className="first-access-link" onClick={onFirstAccess}>
                        Primeiro Acesso
                    </button>
                </div>
            </form>
        </>
    );
}

export default LoginForm;

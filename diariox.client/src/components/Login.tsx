import { useState, type FormEvent } from 'react';
import './Login.css';

interface LoginProps {
    onLogin: (username: string) => void;
}

function Login({ onLogin }: LoginProps) {
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

    const handleForgotPassword = () => {
        alert('Funcionalidade de recuperação de senha será implementada.');
        // Redirecionar para página de esqueci minha senha
    };

    const handleFirstAccess = () => {
        alert('Funcionalidade de primeiro acesso será implementada.');
        // Redirecionar para página de cadastro
    };

    return (
        <div className="login-container">
            <div className="login-card">
                <h1 className="login-title">Diário de Classe</h1>
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
                        <span>É seu primeiro acesso?</span>{' '}
                        <button
                            type="button"
                            className="first-access-link"
                            onClick={handleFirstAccess}
                        >
                            Primeiro Acesso
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}

export default Login;
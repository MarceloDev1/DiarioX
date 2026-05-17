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
                body: JSON.stringify({ username, password }),
            });

            if (response.ok) {
                onLogin(username);
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
        <div className="login-container">
            <div className="login-card">
                <h1 className="login-title">DiarioX</h1>
                <p className="login-subtitle">Entre na sua conta</p>

                <form onSubmit={handleSubmit} className="login-form" noValidate>
                    <div className="form-group">
                        <label htmlFor="username">Usuário</label>
                        <input
                            id="username"
                            type="text"
                            autoComplete="username"
                            placeholder="Digite seu usuário"
                            value={username}
                            onChange={e => setUsername(e.target.value)}
                            disabled={loading}
                        />
                    </div>

                    <div className="form-group">
                        <label htmlFor="password">Senha</label>
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
                </form>
            </div>
        </div>
    );
}

export default Login;

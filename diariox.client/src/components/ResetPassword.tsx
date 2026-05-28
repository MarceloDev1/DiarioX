import { useState, type FormEvent } from 'react';
import './Login.css';

interface ResetPasswordProps {
    token: string;
    onSuccess: () => void;
}

function validatePassword(password: string) {
    return /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$/.test(password);
}

function ResetPassword({ token, onSuccess }: ResetPasswordProps) {
    const [password, setPassword] = useState('');
    const [passwordConfirm, setPasswordConfirm] = useState('');
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');
    const [loading, setLoading] = useState(false);

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();
        setError('');

        if (!validatePassword(password)) {
            setError('A senha deve ter no mínimo 8 caracteres, com letra maiúscula, minúscula, número e símbolo.');
            return;
        }

        if (password !== passwordConfirm) {
            setError('As senhas não coincidem.');
            return;
        }

        setLoading(true);
        try {
            const response = await fetch('/api/auth/reset-password', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ token, password }),
            });

            if (response.ok) {
                setSuccess('Senha redefinida com sucesso! Redirecionando para o login...');
                setTimeout(onSuccess, 2000);
                return;
            }

            const data: { message?: string } = await response.json().catch(() => ({}));
            setError(data.message ?? 'Link inválido ou expirado. Solicite um novo link.');
        } catch {
            setError('Erro ao conectar ao servidor.');
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="login-container">
            <div className="login-card">
                <h1 className="login-title">Diário de Classe</h1>
                <p className="login-description">Crie uma nova senha para sua conta.</p>

                {success ? (
                    <div className="recovery-success" role="status" aria-live="polite">
                        {success}
                    </div>
                ) : (
                    <form onSubmit={handleSubmit} className="login-form" noValidate>
                        <div className="form-group">
                            <label htmlFor="reset-password">Nova senha</label>
                            <input
                                id="reset-password"
                                type="password"
                                autoComplete="new-password"
                                placeholder="Crie uma senha forte"
                                value={password}
                                onChange={e => setPassword(e.target.value)}
                                disabled={loading}
                            />
                        </div>

                        <div className="form-group">
                            <label htmlFor="reset-password-confirm">Repita a nova senha</label>
                            <input
                                id="reset-password-confirm"
                                type="password"
                                autoComplete="new-password"
                                placeholder="Repita a senha"
                                value={passwordConfirm}
                                onChange={e => setPasswordConfirm(e.target.value)}
                                disabled={loading}
                            />
                        </div>

                        <p className="recovery-hint">
                            Mínimo 8 caracteres, com maiúscula, minúscula, número e símbolo.
                        </p>

                        {error && <p className="login-error">{error}</p>}

                        <button type="submit" className="login-button" disabled={loading}>
                            {loading ? 'Salvando...' : 'Salvar nova senha'}
                        </button>

                        <button
                            type="button"
                            className="secondary-button"
                            onClick={onSuccess}
                            disabled={loading}
                        >
                            Voltar para login
                        </button>
                    </form>
                )}
            </div>
        </div>
    );
}

export default ResetPassword;

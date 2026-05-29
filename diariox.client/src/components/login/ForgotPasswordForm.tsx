import { useState, type FormEvent } from 'react';

interface ForgotPasswordFormProps {
    onBack: () => void;
}

function ForgotPasswordForm({ onBack }: ForgotPasswordFormProps) {
    const [identifier, setIdentifier] = useState('');
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');
    const [loading, setLoading] = useState(false);

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();
        setError('');
        setSuccess('');

        const value = identifier.trim();
        if (!value) {
            setError('Informe seu e-mail ou CPF para continuar.');
            return;
        }

        setLoading(true);
        try {
            const response = await fetch('/api/auth/forgot-password', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ login: value }),
            });

            if (response.ok) {
                setSuccess('Se o cadastro existir, enviaremos um link de redefinição de senha para o endereço informado.');
                setIdentifier('');
            } else {
                setError('Não foi possível iniciar a recuperação de senha.');
            }
        } catch {
            setError('Erro ao conectar ao servidor.');
        } finally {
            setLoading(false);
        }
    }

    return (
        <>
            <p className="login-description">
                Informe seu e-mail ou CPF para receber as instrucoes de redefinicao de senha.
            </p>

            <form onSubmit={handleSubmit} className="login-form" noValidate>
                <div className="form-group">
                    <label htmlFor="recovery-identifier">E-mail ou CPF</label>
                    <input
                        id="recovery-identifier"
                        type="text"
                        autoComplete="username"
                        placeholder="seu@email.com ou 00000000000"
                        value={identifier}
                        onChange={e => setIdentifier(e.target.value)}
                        disabled={loading}
                    />
                </div>

                <p className="recovery-hint">
                    Por seguranca, a mensagem exibida nao confirma se o cadastro existe.
                </p>

                {error && <p className="login-error">{error}</p>}
                {success && (
                    <div className="recovery-success" role="status" aria-live="polite">
                        {success}
                    </div>
                )}

                <button type="submit" className="login-button" disabled={loading}>
                    {loading ? 'Enviando...' : 'Enviar link de redefinição'}
                </button>

                <button type="button" className="secondary-button" onClick={onBack} disabled={loading}>
                    Voltar para login
                </button>
            </form>
        </>
    );
}

export default ForgotPasswordForm;

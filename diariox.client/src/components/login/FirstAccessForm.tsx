import { useState, type FormEvent } from 'react';
import { validateCpf, validateBirthDate, validatePassword } from '../../utils/validators';
import { formatCpf, normalizeCpf } from '../../utils/formatters';

type FirstAccessStep = 'validate' | 'activate' | 'success';

interface FirstAccessFormProps {
    onBack: () => void;
}

function FirstAccessForm({ onBack }: FirstAccessFormProps) {
    const [step, setStep] = useState<FirstAccessStep>('validate');
    const [cpf, setCpf] = useState('');
    const [birthDate, setBirthDate] = useState('');
    const [email, setEmail] = useState('');
    const [emailConfirm, setEmailConfirm] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [newPasswordConfirm, setNewPasswordConfirm] = useState('');
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');
    const [loading, setLoading] = useState(false);

    async function handleValidate(e: FormEvent) {
        e.preventDefault();
        setError('');

        if (!validateCpf(cpf)) {
            setError('Informe um CPF valido.');
            return;
        }
        if (!validateBirthDate(birthDate)) {
            setError('Informe uma data de nascimento valida.');
            return;
        }

        setLoading(true);
        try {
            const response = await fetch('/api/auth/first-access/validate', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ cpf: normalizeCpf(cpf), birthDate }),
            });

            if (response.ok) {
                setStep('activate');
            } else {
                setError('Nao foi possivel validar seus dados institucionais.');
            }
        } catch {
            setError('Erro ao conectar ao servidor para validacao.');
        } finally {
            setLoading(false);
        }
    }

    async function handleActivate(e: FormEvent) {
        e.preventDefault();
        setError('');

        const trimmedEmail = email.trim().toLowerCase();
        const trimmedEmailConfirm = emailConfirm.trim().toLowerCase();

        if (!trimmedEmail || !trimmedEmailConfirm) {
            setError('Preencha e confirme o e-mail.');
            return;
        }
        if (trimmedEmail !== trimmedEmailConfirm) {
            setError('Os e-mails informados nao coincidem.');
            return;
        }
        if (!validatePassword(newPassword)) {
            setError('A senha deve ter no minimo 8 caracteres, com letra maiuscula, minuscula, numero e simbolo.');
            return;
        }
        if (newPassword !== newPasswordConfirm) {
            setError('As senhas informadas nao coincidem.');
            return;
        }

        setLoading(true);
        try {
            const response = await fetch('/api/auth/first-access/activate', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ cpf: normalizeCpf(cpf), birthDate, email: trimmedEmail, password: newPassword }),
            });

            if (response.ok) {
                setStep('success');
                setSuccess('Conta ativada com sucesso! Agora faca seu primeiro login.');
                setTimeout(onBack, 1600);
            } else {
                setError('Nao foi possivel ativar sua conta. Verifique os dados e tente novamente.');
            }
        } catch {
            setError('Erro ao conectar ao servidor durante a ativacao.');
        } finally {
            setLoading(false);
        }
    }

    return (
        <>
            <p className="login-description">Ative sua conta em duas etapas para realizar o primeiro login.</p>

            <div className="first-access-progress" aria-label="Progresso do primeiro acesso">
                <span className={step === 'validate' ? 'active' : ''}>1. Validacao</span>
                <span className={step === 'activate' ? 'active' : ''}>2. Ativacao</span>
            </div>

            {step === 'validate' && (
                <form onSubmit={handleValidate} className="login-form" noValidate>
                    <div className="form-group">
                        <label htmlFor="first-access-cpf">CPF</label>
                        <input
                            id="first-access-cpf"
                            type="text"
                            inputMode="numeric"
                            placeholder="000.000.000-00"
                            value={cpf}
                            onChange={e => setCpf(formatCpf(e.target.value))}
                            disabled={loading}
                        />
                    </div>
                    <div className="form-group">
                        <label htmlFor="first-access-birth-date">Data de nascimento</label>
                        <input
                            id="first-access-birth-date"
                            type="date"
                            value={birthDate}
                            onChange={e => setBirthDate(e.target.value)}
                            disabled={loading}
                        />
                    </div>
                    {error && <p className="login-error">{error}</p>}
                    <button type="submit" className="login-button" disabled={loading}>
                        {loading ? 'Validando...' : 'Validar dados'}
                    </button>
                    <button type="button" className="secondary-button" onClick={onBack} disabled={loading}>
                        Voltar para login
                    </button>
                </form>
            )}

            {step === 'activate' && (
                <form onSubmit={handleActivate} className="login-form" noValidate>
                    <div className="form-group">
                        <label htmlFor="first-access-email">Confirmacao de e-mail</label>
                        <input
                            id="first-access-email"
                            type="email"
                            autoComplete="email"
                            placeholder="seu@email.com"
                            value={email}
                            onChange={e => setEmail(e.target.value)}
                            disabled={loading}
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
                            disabled={loading}
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
                            disabled={loading}
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
                            disabled={loading}
                        />
                    </div>
                    {error && <p className="login-error">{error}</p>}
                    <button type="submit" className="login-button" disabled={loading}>
                        {loading ? 'Ativando conta...' : 'Ativar conta'}
                    </button>
                    <button type="button" className="secondary-button" onClick={onBack} disabled={loading}>
                        Cancelar
                    </button>
                </form>
            )}

            {step === 'success' && (
                <div className="first-access-success" role="status" aria-live="polite">
                    {success}
                </div>
            )}
        </>
    );
}

export default FirstAccessForm;

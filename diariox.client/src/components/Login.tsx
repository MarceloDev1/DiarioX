import { useEffect, useState } from 'react';
import { FiBookOpen } from 'react-icons/fi';
import './Login.css';
import LoginForm from './login/LoginForm';
import FirstAccessForm from './login/FirstAccessForm';
import ForgotPasswordForm from './login/ForgotPasswordForm';
import type { Session } from '../utils/api';

interface LoginProps {
    onLogin: (session: Session) => void;
    initialViewMode?: ViewMode;
}

type ViewMode = 'login' | 'first-access' | 'forgot-password';

function Login({ onLogin, initialViewMode = 'login' }: LoginProps) {
    const [viewMode, setViewMode] = useState<ViewMode>(initialViewMode);
    const [tenantNome, setTenantNome] = useState<string | null>(null);

    // A instituição vem do subdomínio acessado; no host de administração não há nenhuma.
    useEffect(() => {
        let active = true;
        fetch('/api/tenants/current')
            .then(response => (response.status === 200 ? (response.json() as Promise<{ nome: string }>) : null))
            .then(data => {
                if (active && data) setTenantNome(data.nome);
            })
            .catch(() => {
                // Nome da instituição é apenas informativo na tela de login.
            });
        return () => {
            active = false;
        };
    }, []);

    return (
        <div className="login-container">
            <div className="login-card">
                <div className="login-logo">
                    <FiBookOpen />
                </div>
                <h1 className="login-title">Diário de Classe</h1>
                {tenantNome && <p className="login-tenant">{tenantNome}</p>}
                {viewMode === 'login' && (
                    <LoginForm
                        onLogin={onLogin}
                        onForgotPassword={() => setViewMode('forgot-password')}
                        onFirstAccess={() => setViewMode('first-access')}
                    />
                )}
                {viewMode === 'first-access' && (
                    <FirstAccessForm onBack={() => setViewMode('login')} />
                )}
                {viewMode === 'forgot-password' && (
                    <ForgotPasswordForm onBack={() => setViewMode('login')} />
                )}
            </div>
        </div>
    );
}

export default Login;

import { useState } from 'react';
import './Login.css';
import LoginForm from './login/LoginForm';
import FirstAccessForm from './login/FirstAccessForm';
import ForgotPasswordForm from './login/ForgotPasswordForm';

interface LoginProps {
    onLogin: (username: string) => void;
}

type ViewMode = 'login' | 'first-access' | 'forgot-password';

function Login({ onLogin }: LoginProps) {
    const [viewMode, setViewMode] = useState<ViewMode>('login');

    return (
        <div className="login-container">
            <div className="login-card">
                <h1 className="login-title">Diário de Classe</h1>
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

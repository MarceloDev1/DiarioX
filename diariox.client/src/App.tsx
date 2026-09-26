import { useState, useEffect } from 'react';
import './App.css';
import Login from './components/Login';
import ResetPassword from './components/ResetPassword';
import Sidebar from './components/Sidebar';
import MainContent from './components/MainContent';
import TenantPicker from './components/tenants/TenantPicker';
import InstituicoesPage from './components/tenants/InstituicoesPage';
import { clearSession, restoreSession, saveSession, UNAUTHORIZED_EVENT, type Session } from './utils/api';

function App() {
    // Recarregar a página mantém o usuário logado enquanto o token salvo for válido.
    const [session, setSession] = useState<Session | null>(restoreSession);
    // Administrador global sem instituição selecionada pode abrir a gestão de instituições.
    const [managingTenants, setManagingTenants] = useState(false);
    const [currentPage, setCurrentPage] = useState('home');
    const [initialAlunoId, setInitialAlunoId] = useState<number | null>(null);
    // O link do e-mail de redefinição abre /redefinir-senha?token=...; lido uma vez ao carregar.
    const [resetToken, setResetToken] = useState<string | null>(() =>
        window.location.pathname === '/redefinir-senha'
            ? new URLSearchParams(window.location.search).get('token')
            : null
    );
    const isFirstAccessRoute = window.location.pathname === '/primeiro-acesso';

    // Mantém salvas as mudanças feitas na sessão (ex.: "Trocar instituição").
    useEffect(() => {
        if (session) saveSession(session);
    }, [session]);

    // Token expirado ou inválido em qualquer chamada da API: volta para o login.
    useEffect(() => {
        const handleUnauthorized = () => {
            clearSession();
            setSession(null);
            setCurrentPage('home');
        };
        window.addEventListener(UNAUTHORIZED_EVENT, handleUnauthorized);
        return () => window.removeEventListener(UNAUTHORIZED_EVENT, handleUnauthorized);
    }, []);

    function handleResetSuccess() {
        setResetToken(null);
        window.history.replaceState({}, '', '/');
    }

    function handleNavigate(page: string, alunoId?: number) {
        setCurrentPage(page);
        setInitialAlunoId(alunoId ?? null);
    }

    function handleLogout() {
        setManagingTenants(false);
        clearSession();
        setSession(null);
        setCurrentPage('home');
    }

    function handleChangeTenant() {
        setManagingTenants(false);
        setSession(current => current && { ...current, tenantId: null, tenantNome: null });
        setCurrentPage('home');
    }

    if (resetToken) {
        return <ResetPassword token={resetToken} onSuccess={handleResetSuccess} />;
    }

    if (!session) {
        return <Login onLogin={setSession} initialViewMode={isFirstAccessRoute ? 'first-access' : 'login'} />;
    }

    // Administrador global precisa escolher uma instituição antes de acessar os cadastros.
    if (session.isGlobalAdmin && session.tenantId === null) {
        if (managingTenants) {
            return (
                <main className="main-area">
                    <header className="main-header">
                        <h1>Diário de Classe</h1>
                        <div className="main-header-user">
                            <button type="button" className="logout-button" onClick={() => setManagingTenants(false)}>
                                Escolher instituição
                            </button>
                            <button type="button" className="logout-button" onClick={handleLogout}>
                                Sair
                            </button>
                        </div>
                    </header>
                    <InstituicoesPage />
                </main>
            );
        }

        return <TenantPicker onSelect={setSession} onManage={() => setManagingTenants(true)} onLogout={handleLogout} />;
    }

    return (
        <div className="app-layout">
            <Sidebar onSelectPage={page => handleNavigate(page)} currentPage={currentPage} isGlobalAdmin={session.isGlobalAdmin} />
            <main className="main-area">
                <header className="main-header">
                    <h1>Diário de Classe</h1>
                    <div className="main-header-user">
                        {session.tenantNome && <span className="main-header-tenant">{session.tenantNome}</span>}
                        <p>Olá, <strong>{session.email}</strong>!</p>
                        {session.isGlobalAdmin && (
                            <button type="button" className="logout-button" onClick={handleChangeTenant}>
                                Trocar instituição
                            </button>
                        )}
                        <button type="button" className="logout-button" onClick={handleLogout}>
                            Sair
                        </button>
                    </div>
                </header>
                <MainContent page={currentPage} onNavigate={handleNavigate} initialAlunoId={initialAlunoId} />
            </main>
        </div>
    );
}

export default App;
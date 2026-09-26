import { useState, useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router';
import './App.css';
import Login from './components/Login';
import ResetPassword from './components/ResetPassword';
import Sidebar from './components/Sidebar';
import MainContent from './components/MainContent';
import TenantPicker from './components/tenants/TenantPicker';
import InstituicoesPage from './components/tenants/InstituicoesPage';
import PermissoesProvider from './components/PermissoesProvider';
import FinanceiroPlataformaPage from './components/faturamento/FinanceiroPlataformaPage';
import AvisoFinanceiro from './components/faturamento/AvisoFinanceiro';
import { clearSession, restoreSession, saveSession, UNAUTHORIZED_EVENT, type Session } from './utils/api';
import { caminhoDaPagina, paginaDoCaminho } from './utils/rotas';

function App() {
    // Recarregar a página mantém o usuário logado enquanto o token salvo for válido.
    const [session, setSession] = useState<Session | null>(restoreSession);
    // Administrador global sem instituição selecionada pode abrir a gestão de instituições ou o financeiro.
    const [adminView, setAdminView] = useState<'instituicoes' | 'financeiro' | null>(null);
    // A página aberta vem da URL: recarregar, voltar/avançar e compartilhar links funcionam.
    const location = useLocation();
    const navigate = useNavigate();
    const currentPage = paginaDoCaminho(location.pathname);
    // O link do e-mail de redefinição abre /redefinir-senha?token=...; lido uma vez ao carregar.
    const [resetToken, setResetToken] = useState<string | null>(() =>
        window.location.pathname === '/redefinir-senha'
            ? new URLSearchParams(window.location.search).get('token')
            : null
    );
    const isFirstAccessRoute = location.pathname === '/primeiro-acesso';

    // Mantém salvas as mudanças feitas na sessão (ex.: "Trocar instituição").
    useEffect(() => {
        if (session) saveSession(session);
    }, [session]);

    // Token expirado ou inválido em qualquer chamada da API: volta para o login, mantendo a URL
    // para que o usuário volte à mesma página depois de entrar.
    useEffect(() => {
        const handleUnauthorized = () => {
            clearSession();
            setSession(null);
        };
        window.addEventListener(UNAUTHORIZED_EVENT, handleUnauthorized);
        return () => window.removeEventListener(UNAUTHORIZED_EVENT, handleUnauthorized);
    }, []);

    function handleResetSuccess() {
        setResetToken(null);
        navigate('/', { replace: true });
    }

    function handleNavigate(page: string, alunoId?: number) {
        navigate(caminhoDaPagina(page) + (alunoId ? `?alunoId=${alunoId}` : ''));
    }

    function handleLogout() {
        setAdminView(null);
        clearSession();
        setSession(null);
        navigate('/');
    }

    function handleChangeTenant() {
        setAdminView(null);
        setSession(current => current && { ...current, tenantId: null, tenantNome: null });
        navigate('/');
    }

    if (resetToken) {
        return <ResetPassword token={resetToken} onSuccess={handleResetSuccess} />;
    }

    if (!session) {
        return <Login onLogin={setSession} initialViewMode={isFirstAccessRoute ? 'first-access' : 'login'} />;
    }

    // Administrador global precisa escolher uma instituição antes de acessar os cadastros.
    if (session.isGlobalAdmin && session.tenantId === null) {
        if (adminView) {
            return (
                <main className="main-area">
                    <header className="main-header">
                        <h1>Diário de Classe</h1>
                        <div className="main-header-user">
                            <button type="button" className="logout-button" onClick={() => setAdminView(null)}>
                                Escolher instituição
                            </button>
                            <button
                                type="button"
                                className="logout-button"
                                onClick={() => setAdminView(adminView === 'financeiro' ? 'instituicoes' : 'financeiro')}
                            >
                                {adminView === 'financeiro' ? 'Instituições' : 'Financeiro'}
                            </button>
                            <button type="button" className="logout-button" onClick={handleLogout}>
                                Sair
                            </button>
                        </div>
                    </header>
                    {adminView === 'financeiro' ? <FinanceiroPlataformaPage /> : <InstituicoesPage />}
                </main>
            );
        }

        return (
            <TenantPicker
                onSelect={setSession}
                onManage={() => setAdminView('instituicoes')}
                onFinanceiro={() => setAdminView('financeiro')}
                onLogout={handleLogout}
            />
        );
    }

    return (
        <PermissoesProvider key={session.tenantId}>
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
                    <AvisoFinanceiro onVerFaturas={() => handleNavigate('assinatura')} />
                    <MainContent onNavigate={handleNavigate} />
                </main>
            </div>
        </PermissoesProvider>
    );
}

export default App;
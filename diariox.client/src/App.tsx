import { useState, useEffect } from 'react';
import './App.css';
import Login from './components/Login';
import ResetPassword from './components/ResetPassword';
import Sidebar from './components/Sidebar';
import MainContent from './components/MainContent';

function App() {
    const [currentUser, setCurrentUser] = useState<string | null>(null);
    const [currentPage, setCurrentPage] = useState('home');
    const [initialAlunoId, setInitialAlunoId] = useState<number | null>(null);
    const [resetToken, setResetToken] = useState<string | null>(null);

    useEffect(() => {
        const params = new URLSearchParams(window.location.search);
        const isResetRoute = window.location.pathname === '/redefinir-senha';
        const token = isResetRoute ? params.get('token') : null;
        if (token) setResetToken(token);
    }, []);

    function handleResetSuccess() {
        setResetToken(null);
        window.history.replaceState({}, '', '/');
    }

    function handleNavigate(page: string, alunoId?: number) {
        setCurrentPage(page);
        setInitialAlunoId(alunoId ?? null);
    }

    if (resetToken) {
        return <ResetPassword token={resetToken} onSuccess={handleResetSuccess} />;
    }

    if (!currentUser) {
        return <Login onLogin={setCurrentUser} />;
    }

    return (
        <div className="app-layout">
            <Sidebar onSelectPage={page => handleNavigate(page)} currentPage={currentPage} />
            <main className="main-area">
                <header className="main-header">
                    <h1>Diário de Classe</h1>
                    <p>Olá, <strong>{currentUser}</strong>!</p>
                </header>
                <MainContent page={currentPage} onNavigate={handleNavigate} initialAlunoId={initialAlunoId} />
            </main>
        </div>
    );
}

export default App;
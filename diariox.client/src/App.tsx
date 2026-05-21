import { useState } from 'react';
import './App.css';
import Login from './components/Login';
import Sidebar from './components/Sidebar';
import MainContent from './components/MainContent';

function App() {
    const [currentUser, setCurrentUser] = useState<string | null>(null);
    const [currentPage, setCurrentPage] = useState('home');

    if (!currentUser) {
        return <Login onLogin={setCurrentUser} />;
    }

    return (
        <div className="app-layout">
            <Sidebar onSelectPage={setCurrentPage} currentPage={currentPage} />
            <main className="main-area">
                <header className="main-header">
                    <h1>Diário de Classe</h1>
                    <p>Olá, <strong>{currentUser}</strong>!</p>
                </header>
                <MainContent page={currentPage} />
            </main>
        </div>
    );
}

export default App;
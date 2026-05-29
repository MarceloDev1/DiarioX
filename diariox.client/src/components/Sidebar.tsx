import { useState } from 'react';
import './Sidebar.css';

interface SidebarProps {
    onSelectPage: (page: string) => void;
    currentPage: string;
}

const cadastroSubItems = [
    { id: 'escolas', label: '🏫 Escolas' },
    { id: 'usuarios', label: '👤 Usuários' },
];

function Sidebar({ onSelectPage, currentPage }: SidebarProps) {
    const isCadastroActive = cadastroSubItems.some(item => item.id === currentPage);
    const [cadastroOpen, setCadastroOpen] = useState(isCadastroActive);

    const toggleCadastro = () => setCadastroOpen(open => !open);

    return (
        <aside className="sidebar">
            <div className="sidebar-header">
                <h2>Diário de Classe</h2>
            </div>
            <nav className="sidebar-nav">
                <button
                    className={`sidebar-item ${currentPage === 'home' ? 'active' : ''}`}
                    onClick={() => onSelectPage('home')}
                >
                    🏠 Início
                </button>

                <button
                    className={`sidebar-item sidebar-group ${isCadastroActive ? 'active' : ''}`}
                    onClick={toggleCadastro}
                    aria-expanded={cadastroOpen}
                >
                    📋 Cadastro
                    <span className={`sidebar-chevron ${cadastroOpen ? 'open' : ''}`}>›</span>
                </button>

                {cadastroOpen && (
                    <div className="sidebar-submenu">
                        {cadastroSubItems.map(item => (
                            <button
                                key={item.id}
                                className={`sidebar-item sidebar-subitem ${currentPage === item.id ? 'active' : ''}`}
                                onClick={() => onSelectPage(item.id)}
                            >
                                {item.label}
                            </button>
                        ))}
                    </div>
                )}
            </nav>
            <div className="sidebar-footer">
                <button className="logout-button" onClick={() => window.location.reload()}>
                    🚪 Sair
                </button>
            </div>
        </aside>
    );
}

export default Sidebar;

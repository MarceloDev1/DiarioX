import './Sidebar.css';

interface SidebarProps {
    onSelectPage: (page: string) => void;
    currentPage: string;
}

function Sidebar({ onSelectPage, currentPage }: SidebarProps) {
    const menuItems = [
        { id: 'home', label: '🏠 Início' },
        { id: 'escolas', label: '🏫 Cadastro de Escolas' },
        { id: 'usuarios', label: '👤 Cadastro de Usuários' },
    ];

    return (
        <aside className="sidebar">
            <div className="sidebar-header">
                <h2>Diário de Classe</h2>
            </div>
            <nav className="sidebar-nav">
                {menuItems.map(item => (
                    <button
                        key={item.id}
                        className={`sidebar-item ${currentPage === item.id ? 'active' : ''}`}
                        onClick={() => onSelectPage(item.id)}
                    >
                        {item.label}
                    </button>
                ))}
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
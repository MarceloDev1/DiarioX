import './Sidebar.css';

interface SidebarProps {
    onSelectPage: (page: string) => void;
    currentPage: string;
}

function Sidebar({ onSelectPage, currentPage }: SidebarProps) {
    const menuItems = [
        { id: 'alunos', label: '📘 Cadastro de Alunos' },
        { id: 'professores', label: '👨‍🏫 Cadastro de Professores' },
        { id: 'turmas', label: '🏫 Cadastro de Turmas' },
        { id: 'disciplinas', label: '📚 Cadastro de Disciplinas' },
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
import { useState } from 'react';
import './Sidebar.css';
import { FiHome, FiUsers, FiBriefcase, FiBook, FiLayers, FiAward, FiUserCheck, FiRotateCcw, FiUser } from 'react-icons/fi';
import { MdSchool, MdPeople, MdManageAccounts } from 'react-icons/md';

function LogoIcon() {
    return (
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="18 10 68 84" width="24" height="24" fill="none">
            <rect x="25" y="20" width="30" height="40" rx="2" fill="#A020F0" />
            <rect x="37" y="32" width="30" height="40" rx="2" fill="#9B26E6" stroke="#ffffff" strokeWidth="1" />
            <rect x="49" y="44" width="30" height="40" rx="2" fill="#8A2BE2" stroke="#ffffff" strokeWidth="1" />
        </svg>
    );
}

interface SidebarProps {
    onSelectPage: (page: string) => void;
    currentPage: string;
}

const menuItems = [
    { id: 'home', label: 'Home', icon: FiHome },
    { id: 'cadastro', label: 'Cadastro', icon: FiBriefcase, submenu: [
        { id: 'escolas', label: 'Escolas', icon: MdSchool },
        { id: 'modalidades-ensino', label: 'Modalidades de Ensino', icon: FiBook },
        { id: 'etapas-ensino', label: 'Etapas de Ensino', icon: FiLayers },
        { id: 'anos-letivos', label: 'Anos Letivos', icon: FiAward },
        { id: 'disciplinas', label: 'Disciplinas', icon: FiBook },
        { id: 'turmas', label: 'Turmas', icon: FiUsers },
        { id: 'professores', label: 'Professores', icon: FiUsers },
        { id: 'alocacao-professor', label: 'Alocação de Professor', icon: MdManageAccounts },
    ] },
    { id: 'alunos', label: 'Alunos', icon: MdPeople },
    { id: 'enturmar-aluno', label: 'Enturmar Aluno', icon: FiUserCheck },
    { id: 'remanejar-aluno', label: 'Remanejar Aluno', icon: FiRotateCcw },
    { id: 'usuarios', label: 'Usuários', icon: FiUser },
];

function Sidebar({ onSelectPage, currentPage }: SidebarProps) {
    const cadastroItem = menuItems.find(item => item.id === 'cadastro');
    const isCadastroActive = cadastroItem?.submenu?.some(item => item.id === currentPage);
    const [cadastroOpen, setCadastroOpen] = useState(isCadastroActive);

    const toggleCadastro = () => setCadastroOpen(open => !open);

    return (
        <aside className="sidebar">
            <div className="sidebar-header">
                <div className="sidebar-logo">
                    <LogoIcon />
                    <span>Diário X</span>
                </div>
            </div>
            <nav className="sidebar-nav">
                {menuItems.map(item => {
                    if (item.id === 'cadastro') {
                        const Icon = item.icon;
                        return (
                            <div key={item.id}>
                                <button
                                    className={`sidebar-item sidebar-group ${isCadastroActive ? 'active' : ''}`}
                                    onClick={toggleCadastro}
                                    aria-expanded={cadastroOpen}
                                >
                                    <span className="sidebar-item-content">
                                        <Icon className="sidebar-icon" />
                                        <span>{item.label}</span>
                                    </span>
                                    <span className={`sidebar-chevron ${cadastroOpen ? 'open' : ''}`}>›</span>
                                </button>

                                {cadastroOpen && item.submenu && (
                                    <div className="sidebar-submenu">
                                        {item.submenu.map(subitem => {
                                            const SubIcon = subitem.icon;
                                            return (
                                                <button
                                                    key={subitem.id}
                                                    className={`sidebar-item sidebar-subitem ${currentPage === subitem.id ? 'active' : ''}`}
                                                    onClick={() => onSelectPage(subitem.id)}
                                                >
                                                    <span className="sidebar-item-content">
                                                        <SubIcon className="sidebar-icon" />
                                                        <span>{subitem.label}</span>
                                                    </span>
                                                </button>
                                            );
                                        })}
                                    </div>
                                )}
                            </div>
                        );
                    }

                    const Icon = item.icon;
                    return (
                        <button
                            key={item.id}
                            className={`sidebar-item ${currentPage === item.id ? 'active' : ''}`}
                            onClick={() => onSelectPage(item.id)}
                        >
                            <span className="sidebar-item-content">
                                <Icon className="sidebar-icon" />
                                <span>{item.label}</span>
                            </span>
                        </button>
                    );
                })}
            </nav>
        </aside>
    );
}

export default Sidebar;

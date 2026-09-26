import { useState } from 'react';
import './Sidebar.css';
import type { IconType } from 'react-icons';
import { FiHome, FiUsers, FiBriefcase, FiBook, FiLayers, FiAward, FiUserCheck, FiRotateCcw, FiUser, FiChevronsLeft, FiChevronsRight, FiGlobe, FiSettings, FiShield, FiCheckSquare, FiDollarSign, FiCreditCard } from 'react-icons/fi';
import { MdSchool, MdPeople, MdManageAccounts } from 'react-icons/md';
import { usePermissoes } from '../hooks/usePermissoes';
import { permissaoDaPagina } from '../utils/permissoes';

function LogoIcon() {
    return (
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="18 10 68 84" width="24" height="24" fill="none">
            <rect x="25" y="20" width="30" height="40" rx="2" fill="#A020F0" />
            <rect x="37" y="32" width="30" height="40" rx="2" fill="#9B26E6" stroke="#ffffff" strokeWidth="1" />
            <rect x="49" y="44" width="30" height="40" rx="2" fill="#8A2BE2" stroke="#ffffff" strokeWidth="1" />
        </svg>
    );
}

const COLLAPSED_KEY = 'diariox_sidebar_collapsed';

interface SidebarProps {
    onSelectPage: (page: string) => void;
    currentPage: string;
    isGlobalAdmin?: boolean;
}

interface MenuItem {
    id: string;
    label: string;
    icon: IconType;
    submenu?: MenuItem[];
}

// Itens exclusivos do Administrador global.
const globalAdminItems: MenuItem[] = [
    { id: 'instituicoes', label: 'Instituições', icon: FiGlobe },
    { id: 'financeiro-plataforma', label: 'Financeiro', icon: FiDollarSign },
];

const menuItems: MenuItem[] = [
    { id: 'home', label: 'Home', icon: FiHome },
    { id: 'cadastro', label: 'Cadastro', icon: FiBriefcase, submenu: [
        { id: 'escolas', label: 'Escolas', icon: MdSchool },
        { id: 'modalidades-ensino', label: 'Modalidades de Ensino', icon: FiBook },
        { id: 'etapas-ensino', label: 'Etapas de Ensino', icon: FiLayers },
        { id: 'anos-letivos', label: 'Anos Letivos', icon: FiAward },
        { id: 'disciplinas', label: 'Disciplinas', icon: FiBook },
        { id: 'turmas', label: 'Turmas', icon: FiUsers },
        { id: 'professores', label: 'Professores', icon: FiUsers },
    ] },
    { id: 'alocacao-professor', label: 'Alocação de Professor', icon: MdManageAccounts },
    { id: 'alunos', label: 'Alunos', icon: MdPeople },
    { id: 'enturmar-aluno', label: 'Enturmar Aluno', icon: FiUserCheck },
    { id: 'remanejar-aluno', label: 'Remanejar Aluno', icon: FiRotateCcw },
    { id: 'chamada', label: 'Chamada', icon: FiCheckSquare },
    { id: 'usuarios', label: 'Usuários', icon: FiUser },
    { id: 'configuracoes', label: 'Configurações', icon: FiSettings, submenu: [
        { id: 'permissoes', label: 'Permissões', icon: FiShield },
        { id: 'assinatura', label: 'Assinatura', icon: FiCreditCard },
    ] },
];

function Sidebar({ onSelectPage, currentPage, isGlobalAdmin = false }: SidebarProps) {
    const { can } = usePermissoes();

    // Mostra só as páginas permitidas; grupos sem nenhuma página permitida somem.
    const podeAbrir = (id: string) => !permissaoDaPagina[id] || can(permissaoDaPagina[id]);
    const visibleItems = (isGlobalAdmin ? [...menuItems, ...globalAdminItems] : menuItems)
        .map(item => item.submenu ? { ...item, submenu: item.submenu.filter(sub => podeAbrir(sub.id)) } : item)
        .filter(item => item.submenu ? item.submenu.length > 0 : podeAbrir(item.id));

    const isGroupActive = (item: MenuItem) => item.submenu?.some(sub => sub.id === currentPage) ?? false;
    const [openGroups, setOpenGroups] = useState<Set<string>>(
        () => new Set(menuItems.filter(isGroupActive).map(item => item.id))
    );

    const [collapsed, setCollapsed] = useState(() => {
        try {
            return localStorage.getItem(COLLAPSED_KEY) === 'true';
        } catch {
            return false;
        }
    });

    const toggleGroup = (id: string) => setOpenGroups(current => {
        const next = new Set(current);
        if (next.has(id)) next.delete(id);
        else next.add(id);
        return next;
    });

    const toggleCollapsed = () => {
        setCollapsed(current => {
            const next = !current;
            try {
                localStorage.setItem(COLLAPSED_KEY, String(next));
            } catch {
                // preferência apenas visual; ignora falha de armazenamento
            }
            return next;
        });
    };

    return (
        <aside className={`sidebar ${collapsed ? 'collapsed' : ''}`}>
            <div className="sidebar-header">
                <div className="sidebar-logo">
                    <LogoIcon />
                    <span className="sidebar-label">Diário X</span>
                </div>
                <button
                    type="button"
                    className="sidebar-toggle"
                    onClick={toggleCollapsed}
                    aria-label={collapsed ? 'Expandir menu' : 'Recolher menu'}
                    title={collapsed ? 'Expandir menu' : 'Recolher menu'}
                >
                    {collapsed ? <FiChevronsRight /> : <FiChevronsLeft />}
                </button>
            </div>
            <nav className="sidebar-nav">
                {visibleItems.map(item => {
                    if (item.submenu) {
                        const Icon = item.icon;
                        const isOpen = openGroups.has(item.id);
                        return (
                            <div key={item.id}>
                                <button
                                    className={`sidebar-item sidebar-group ${isGroupActive(item) ? 'active' : ''}`}
                                    onClick={() => toggleGroup(item.id)}
                                    aria-expanded={isOpen}
                                    title={collapsed ? item.label : undefined}
                                >
                                    <span className="sidebar-item-content">
                                        <Icon className="sidebar-icon" />
                                        <span className="sidebar-label">{item.label}</span>
                                    </span>
                                    <span className={`sidebar-chevron ${isOpen ? 'open' : ''}`}>›</span>
                                </button>

                                {isOpen && (
                                    <div className="sidebar-submenu">
                                        {item.submenu.map(subitem => {
                                            const SubIcon = subitem.icon;
                                            return (
                                                <button
                                                    key={subitem.id}
                                                    className={`sidebar-item sidebar-subitem ${currentPage === subitem.id ? 'active' : ''}`}
                                                    onClick={() => onSelectPage(subitem.id)}
                                                    title={collapsed ? subitem.label : undefined}
                                                >
                                                    <span className="sidebar-item-content">
                                                        <SubIcon className="sidebar-icon" />
                                                        <span className="sidebar-label">{subitem.label}</span>
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
                            title={collapsed ? item.label : undefined}
                        >
                            <span className="sidebar-item-content">
                                <Icon className="sidebar-icon" />
                                <span className="sidebar-label">{item.label}</span>
                            </span>
                        </button>
                    );
                })}
            </nav>
        </aside>
    );
}

export default Sidebar;

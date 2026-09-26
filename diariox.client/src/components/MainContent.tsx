import './MainContent.css';
import EscolasPage from './escolas/EscolasPage';
import EtapasEnsinoPage from './etapas-ensino/EtapasEnsinoPage';
import ModalidadesEnsinoPage from './modalidades-ensino/ModalidadesEnsinoPage';
import UsuariosPage from './usuarios/UsuariosPage';
import AnosLetivosPage from './anos-letivos/AnosLetivosPage';
import TurmasPage from './turmas/TurmasPage';
import DisciplinasPage from './disciplinas/DisciplinasPage';
import ProfessoresPage from './professores/ProfessoresPage';
import AlunosPage from './alunos/AlunosPage';
import EnturmarAlunoPage from './alunos/EnturmarAlunoPage';
import ProfessorAlocacoesPage from './professor-alocacoes/ProfessorAlocacoesPage';
import RemanejarAlunoPage from './alunos/RemanejarAlunoPage';
import InstituicoesPage from './tenants/InstituicoesPage';
import PermissoesPage from './configuracoes/PermissoesPage';
import ChamadaPage from './chamada/ChamadaPage';
import AssinaturaPage from './configuracoes/AssinaturaPage';
import FinanceiroPlataformaPage from './faturamento/FinanceiroPlataformaPage';
import { usePermissoes } from '../hooks/usePermissoes';
import { permissaoDaPagina } from '../utils/permissoes';

interface MainContentProps {
    page: string;
    onNavigate: (page: string, alunoId?: number) => void;
    initialAlunoId: number | null;
}

function MainContent({ page, onNavigate, initialAlunoId }: MainContentProps) {
    const { can, isLoading } = usePermissoes();

    const permissao = permissaoDaPagina[page];
    if (permissao && !can(permissao)) {
        return (
            <div className="content-card">
                {isLoading ? <p>Carregando permissões...</p> : (
                    <>
                        <h2>Acesso não permitido</h2>
                        <p>Seu perfil não tem permissão para acessar esta página. Fale com a gerência da instituição.</p>
                    </>
                )}
            </div>
        );
    }

    switch (page) {
        case 'escolas':
            return <EscolasPage />;
        case 'modalidades-ensino':
            return <ModalidadesEnsinoPage />;
        case 'etapas-ensino':
            return <EtapasEnsinoPage />;
        case 'anos-letivos':
            return <AnosLetivosPage />;
        case 'disciplinas':
            return <DisciplinasPage />;
        case 'turmas':
            return <TurmasPage />;
        case 'professores':
            return <ProfessoresPage />;
        case 'alocacao-professor':
            return <ProfessorAlocacoesPage />;
        case 'alunos':
            return <AlunosPage onEnturmar={alunoId => onNavigate('enturmar-aluno', alunoId)} />;
        case 'enturmar-aluno':
            return <EnturmarAlunoPage initialAlunoId={initialAlunoId} />;
        case 'remanejar-aluno':
            return <RemanejarAlunoPage />;
        case 'chamada':
            return <ChamadaPage />;
        case 'usuarios':
            return <UsuariosPage />;
        case 'instituicoes':
            return <InstituicoesPage />;
        case 'permissoes':
            return <PermissoesPage />;
        case 'assinatura':
            return <AssinaturaPage />;
        case 'financeiro-plataforma':
            return <FinanceiroPlataformaPage />;
        default:
            return (
                <div className="content-card">
                    <h2>Bem-vindo(a) ao Diário de Classe</h2>
                    <p>Selecione uma opção no menu lateral para gerenciar os registros.</p>
                </div>
            );
    }
}

export default MainContent;
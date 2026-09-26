import { lazy, Suspense } from 'react';
import { Link, Route, Routes, useLocation, useSearchParams } from 'react-router';
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
import RelatoriosPage from './relatorios/RelatoriosPage';

import { usePermissoes } from '../hooks/usePermissoes';
import { permissaoDaPagina } from '../utils/permissoes';
import { paginaDoCaminho } from '../utils/rotas';

// Carregada sob demanda: traz a biblioteca de gráficos, que só a tela de relatório usa.
const RelatorioPage = lazy(() => import('./relatorios/RelatorioPage'));

interface MainContentProps {
    onNavigate: (page: string, alunoId?: number) => void;
}

function EnturmarAlunoRoute() {
    const [searchParams] = useSearchParams();
    const alunoId = Number(searchParams.get('alunoId')) || null;
    // A key recria a página quando outro aluno é aberto a partir da lista.
    return <EnturmarAlunoPage key={alunoId ?? 'novo'} initialAlunoId={alunoId} />;
}

function MainContent({ onNavigate }: MainContentProps) {
    const { can, isLoading } = usePermissoes();
    const location = useLocation();

    const permissao = permissaoDaPagina[paginaDoCaminho(location.pathname)];
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

    return (
        <Routes>
            <Route path="/escolas" element={<EscolasPage />} />
            <Route path="/modalidades-ensino" element={<ModalidadesEnsinoPage />} />
            <Route path="/etapas-ensino" element={<EtapasEnsinoPage />} />
            <Route path="/anos-letivos" element={<AnosLetivosPage />} />
            <Route path="/disciplinas" element={<DisciplinasPage />} />
            <Route path="/turmas" element={<TurmasPage />} />
            <Route path="/professores" element={<ProfessoresPage />} />
            <Route path="/alocacao-professor" element={<ProfessorAlocacoesPage />} />
            <Route path="/alunos" element={<AlunosPage onEnturmar={alunoId => onNavigate('enturmar-aluno', alunoId)} />} />
            <Route path="/enturmar-aluno" element={<EnturmarAlunoRoute />} />
            <Route path="/remanejar-aluno" element={<RemanejarAlunoPage />} />
            <Route path="/chamada" element={<ChamadaPage />} />
            <Route path="/relatorios" element={<RelatoriosPage />} />
            <Route path="/relatorios/:relatorioId" element={
                <Suspense fallback={<div className="loading">Carregando relatório...</div>}>
                    <RelatorioPage />
                </Suspense>
            } />
            <Route path="/usuarios" element={<UsuariosPage />} />
            <Route path="/instituicoes" element={<InstituicoesPage />} />
            <Route path="/permissoes" element={<PermissoesPage />} />
            <Route path="/assinatura" element={<AssinaturaPage />} />
            <Route path="/financeiro-plataforma" element={<FinanceiroPlataformaPage />} />
            <Route path="/" element={
                <div className="content-card">
                    <h2>Bem-vindo(a) ao Diário de Classe</h2>
                    <p>Selecione uma opção no menu lateral para gerenciar os registros.</p>
                </div>
            } />
            <Route path="*" element={
                <div className="content-card">
                    <h2>Página não encontrada</h2>
                    <p>O endereço acessado não existe. <Link to="/">Voltar para o início</Link>.</p>
                </div>
            } />
        </Routes>
    );
}

export default MainContent;

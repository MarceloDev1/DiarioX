import { lazy, Suspense } from 'react';
import { Link, Route, Routes, useLocation, useNavigate, useSearchParams } from 'react-router';
import './MainContent.css';
import EscolasPage from './escolas/EscolasPage';
import EtapasEnsinoPage from './etapas-ensino/EtapasEnsinoPage';
import ModalidadesEnsinoPage from './modalidades-ensino/ModalidadesEnsinoPage';
import UsuariosPage from './usuarios/UsuariosPage';
import AnosLetivosPage from './anos-letivos/AnosLetivosPage';
import CalendarioLetivoPage from './calendario-letivo/CalendarioLetivoPage';
import TurmasPage from './turmas/TurmasPage';
import HorariosPage from './horarios/HorariosPage';
import DisciplinasPage from './disciplinas/DisciplinasPage';
import ProfessoresPage from './professores/ProfessoresPage';
import AlunosPage from './alunos/AlunosPage';
import EnturmarAlunoPage from './alunos/EnturmarAlunoPage';
import AlunosEnturmadosPage from './alunos/AlunosEnturmadosPage';
import ProfessorAlocacoesPage from './professor-alocacoes/ProfessorAlocacoesPage';
import RemanejarAlunoPage from './alunos/RemanejarAlunoPage';
import DesenturmarAlunoPage from './alunos/DesenturmarAlunoPage';
import TransferirAlunoPage from './alunos/TransferirAlunoPage';
import AlunosTransferidosPage from './alunos/AlunosTransferidosPage';
import InstituicoesPage from './tenants/InstituicoesPage';
import PermissoesPage from './configuracoes/PermissoesPage';
import ChamadaPage from './chamada/ChamadaPage';
import NotasPage from './notas/NotasPage';
import RegrasAvaliacaoPage from './notas/RegrasAvaliacaoPage';
import ConteudoMinistradoPage from './conteudo-ministrado/ConteudoMinistradoPage';
import HabilidadesBnccPage from './habilidades-bncc/HabilidadesBnccPage';
import AssinaturaPage from './configuracoes/AssinaturaPage';
import FinanceiroPlataformaPage from './faturamento/FinanceiroPlataformaPage';
import RelatoriosPage from './relatorios/RelatoriosPage';
import HomePage from './home/HomePage';

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
    const navigate = useNavigate();
    const alunoId = Number(searchParams.get('alunoId')) || null;
    // A key recria a página quando outro aluno é aberto a partir da lista.
    return <EnturmarAlunoPage key={alunoId ?? 'novo'} initialAlunoId={alunoId} onVoltar={() => navigate('/enturmar-aluno')} />;
}

function AlunosEnturmadosRoute() {
    const navigate = useNavigate();
    return <AlunosEnturmadosPage onEnturmar={() => navigate('/enturmar-aluno/novo')} />;
}

function AlunosTransferidosRoute() {
    const navigate = useNavigate();
    return <AlunosTransferidosPage onTransferir={() => navigate('/transferir-aluno/novo')} />;
}

function TransferirAlunoRoute() {
    const navigate = useNavigate();
    return <TransferirAlunoPage onVoltar={() => navigate('/transferir-aluno')} />;
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
            <Route path="/calendario-letivo" element={<CalendarioLetivoPage />} />
            <Route path="/disciplinas" element={<DisciplinasPage />} />
            <Route path="/turmas" element={<TurmasPage />} />
            <Route path="/horarios" element={<HorariosPage />} />
            <Route path="/professores" element={<ProfessoresPage />} />
            <Route path="/alocacao-professor" element={<ProfessorAlocacoesPage />} />
            <Route path="/alunos" element={<AlunosPage onEnturmar={alunoId => onNavigate('enturmar-aluno/novo', alunoId)} />} />
            <Route path="/enturmar-aluno" element={<AlunosEnturmadosRoute />} />
            <Route path="/enturmar-aluno/novo" element={<EnturmarAlunoRoute />} />
            <Route path="/remanejar-aluno" element={<RemanejarAlunoPage />} />
            <Route path="/desenturmar-aluno" element={<DesenturmarAlunoPage />} />
            <Route path="/transferir-aluno" element={<AlunosTransferidosRoute />} />
            <Route path="/transferir-aluno/novo" element={<TransferirAlunoRoute />} />
            <Route path="/chamada" element={<ChamadaPage />} />
            <Route path="/notas" element={<NotasPage />} />
            <Route path="/regras-avaliacao" element={<RegrasAvaliacaoPage />} />
            <Route path="/conteudo-ministrado" element={<ConteudoMinistradoPage />} />
            <Route path="/habilidades-bncc" element={<HabilidadesBnccPage />} />
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
            <Route path="/" element={<HomePage />} />
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

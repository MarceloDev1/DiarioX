import './MainContent.css';
import EscolasPage from './escolas/EscolasPage';
import EtapasEnsinoPage from './etapas-ensino/EtapasEnsinoPage';
import ModalidadesEnsinoPage from './modalidades-ensino/ModalidadesEnsinoPage';
import UsuariosPage from './usuarios/UsuariosPage';
import AnosLetivosPage from './anos-letivos/AnosLetivosPage';

interface MainContentProps {
    page: string;
}

function MainContent({ page }: MainContentProps) {
    switch (page) {
        case 'escolas':
            return <EscolasPage />;
        case 'modalidades-ensino':
            return <ModalidadesEnsinoPage />;
        case 'etapas-ensino':
            return <EtapasEnsinoPage />;
        case 'anos-letivos':
            return <AnosLetivosPage />;
        case 'usuarios':
            return <UsuariosPage />;
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

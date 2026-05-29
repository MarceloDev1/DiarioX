import './MainContent.css';
import EscolasPage from './escolas/EscolasPage';
import UsuariosPage from './usuarios/UsuariosPage';

interface MainContentProps {
    page: string;
}

function MainContent({ page }: MainContentProps) {
    switch (page) {
        case 'escolas':
            return <EscolasPage />;
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

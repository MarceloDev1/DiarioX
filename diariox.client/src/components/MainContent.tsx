import './MainContent.css';

interface MainContentProps {
    page: string;
}

function MainContent({ page }: MainContentProps) {
    switch (page) {
        case 'alunos':
            return (
                <div className="content-card">
                    <h2>Cadastro de Alunos</h2>
                    <form className="cadastro-form">
                        <input type="text" placeholder="Nome completo" />
                        <input type="text" placeholder="CPF" />
                        <input type="email" placeholder="E-mail" />
                        <button type="submit">Salvar Aluno</button>
                    </form>
                </div>
            );
        case 'professores':
            return (
                <div className="content-card">
                    <h2>Cadastro de Professores</h2>
                    <form className="cadastro-form">
                        <input type="text" placeholder="Nome" />
                        <input type="text" placeholder="Disciplina" />
                        <input type="email" placeholder="E-mail" />
                        <button type="submit">Salvar Professor</button>
                    </form>
                </div>
            );
        case 'turmas':
            return (
                <div className="content-card">
                    <h2>Cadastro de Turmas</h2>
                    <form className="cadastro-form">
                        <input type="text" placeholder="Série / Ano" />
                        <input type="text" placeholder="Turno" />
                        <input type="text" placeholder="Capacidade" />
                        <button type="submit">Salvar Turma</button>
                    </form>
                </div>
            );
        case 'disciplinas':
            return (
                <div className="content-card">
                    <h2>Cadastro de Disciplinas</h2>
                    <form className="cadastro-form">
                        <input type="text" placeholder="Nome da disciplina" />
                        <input type="text" placeholder="Carga horária" />
                        <button type="submit">Salvar Disciplina</button>
                    </form>
                </div>
            );
        default:
            return (
                <div className="content-card">
                    <h2>Bem-vindo(a) ao Diário de Classe</h2>
                    <p>Selecione uma opção no menu lateral para realizar cadastros.</p>
                </div>
            );
    }
}

export default MainContent;
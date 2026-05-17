import { useState } from 'react';
import './App.css';
import Login from './components/Login';

function App() {
    const [currentUser, setCurrentUser] = useState<string | null>(null);

    if (!currentUser) {
        return <Login onLogin={setCurrentUser} />;
    }

    return (
        <div>
            <h1 id="homeLabel">Bem-vindo ao DiarioX</h1>
            <p>Login realizado com sucesso.</p>
            <p>Usuário conectado: <strong>{currentUser}</strong></p>
        </div>
    );
}

export default App;
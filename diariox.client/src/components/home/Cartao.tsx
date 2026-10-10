import type { ReactNode } from 'react';

/** Cartão branco da home, com título e ação opcional no cabeçalho. */
function Cartao({ titulo, acao, children, className = '' }: { titulo: string; acao?: ReactNode; children: ReactNode; className?: string }) {
    return (
        <section className={`home-cartao ${className}`}>
            <div className="home-cartao-cabecalho">
                <h2>{titulo}</h2>
                {acao}
            </div>
            {children}
        </section>
    );
}

export default Cartao;

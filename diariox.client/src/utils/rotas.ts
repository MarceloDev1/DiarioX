/**
 * Cada página do menu tem a URL "/<id>" (ex.: /alunos, /relatorios/ocupacao-vagas); a home é "/".
 * O id é o mesmo usado no menu e em permissaoDaPagina.
 */
export function caminhoDaPagina(pagina: string): string {
    return pagina === 'home' ? '/' : `/${pagina}`;
}

/** Id da página a partir da URL: o primeiro segmento do caminho (ou "home"). */
export function paginaDoCaminho(pathname: string): string {
    return pathname.split('/').filter(Boolean)[0] ?? 'home';
}

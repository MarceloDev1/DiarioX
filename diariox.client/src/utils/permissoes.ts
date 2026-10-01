/** Permissão exigida para abrir cada página do menu. Páginas ausentes (ex.: home) são livres. */
export const permissaoDaPagina: Record<string, string> = {
    'escolas': 'escolas.visualizar',
    'modalidades-ensino': 'modalidades-ensino.visualizar',
    'etapas-ensino': 'etapas-ensino.visualizar',
    'anos-letivos': 'anos-letivos.visualizar',
    'disciplinas': 'disciplinas.visualizar',
    'turmas': 'turmas.visualizar',
    'professores': 'professores.visualizar',
    'alocacao-professor': 'alocacao-professor.visualizar',
    'alunos': 'alunos.visualizar',
    // Enturmar, remanejar, desenturmar e transferir alteram o aluno.
    'enturmar-aluno': 'alunos.editar',
    'remanejar-aluno': 'alunos.editar',
    'desenturmar-aluno': 'alunos.editar',
    'transferir-aluno': 'alunos.editar',
    'chamada': 'chamada.visualizar',
    'relatorios': 'relatorios.visualizar',
    'usuarios': 'usuarios.visualizar',
    'permissoes': 'configuracoes.visualizar',
    'assinatura': 'configuracoes.visualizar',
};

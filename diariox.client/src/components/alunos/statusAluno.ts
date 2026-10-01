import type { Status } from '../ui/StatusPill';

export const statusAlunoOpcoes: { value: Status; label: string }[] = [
    { value: 'ATIVO', label: 'Ativo' },
    { value: 'ATIVO_AGUARDANDO_ENTURMACAO', label: 'Aguardando Enturmação' },
    { value: 'NAO_COMPARECEU', label: 'Não Compareceu' },
    { value: 'INATIVO', label: 'Inativo' },
    { value: 'INATIVO_OBITO', label: 'Inativo - Óbito' },
    { value: 'TRANSFERIDO', label: 'Transferido' },
];

/** Rótulo para o StatusPill; ATIVO e INATIVO aparecem pelo próprio código, como nas demais telas. */
export function rotuloStatusAluno(status: string): string | undefined {
    if (status === 'ATIVO' || status === 'INATIVO') return undefined;
    return statusAlunoOpcoes.find(opcao => opcao.value === status)?.label;
}

/** Aluno fora de turma que pode voltar a ser ativado pela tela de Alunos (o transferido, readmitido). O óbito é definitivo (RF013). */
export const podeAtivarAluno = (status: string) => status === 'INATIVO' || status === 'NAO_COMPARECEU' || status === 'TRANSFERIDO';

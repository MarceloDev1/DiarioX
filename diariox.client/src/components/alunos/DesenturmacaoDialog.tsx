import { useState } from 'react';
import ConfirmDialog from '../ui/ConfirmDialog';
import './DesenturmacaoDialog.css';

const MOTIVO_OUTROS = 'OUTROS';
const MAX_OBSERVACAO = 500;

const motivos = [
    { value: 'REESTRUTURACAO_INTERNA', label: 'Reestruturação Interna' },
    { value: 'NAO_COMPARECEU', label: 'Nunca Compareceu / Não Frequentou' },
    { value: 'FALECIMENTO', label: 'Falecimento' },
    { value: 'ERRO_MATRICULA_ENTURMACAO', label: 'Erro de Matrícula/Enturmação' },
    { value: MOTIVO_OUTROS, label: 'Outros' },
];

// RN01: mostra o efeito do motivo antes da confirmação.
const efeitoDoMotivo: Record<string, string> = {
    REESTRUTURACAO_INTERNA: 'O aluno volta para "Aguardando Enturmação" e fica disponível para outra turma.',
    ERRO_MATRICULA_ENTURMACAO: 'O aluno volta para "Aguardando Enturmação" e fica disponível para outra turma.',
    OUTROS: 'O aluno volta para "Aguardando Enturmação" e fica disponível para outra turma.',
    NAO_COMPARECEU: 'O aluno passa para "Não Compareceu" e deixa de contar faltas nesta turma.',
    FALECIMENTO: 'O aluno passa para "Inativo - Óbito" e a matrícula é encerrada definitivamente.',
};

interface DesenturmacaoDialogProps {
    turmaNome: string;
    alunos: { alunoId: number; nome: string }[];
    /** Alternativa 01: todos os alunos da turma. */
    emLote: boolean;
    isLoading: boolean;
    onConfirm: (motivo: string, observacao: string | null) => void;
    /** Precisa ser estável (useCallback): o ConfirmDialog refaz o foco inicial quando ela muda. */
    onCancel: () => void;
}

/**
 * Modal de confirmação da desenturmação (RF013, passos 6 e 7): motivo obrigatório e observação
 * obrigatória para "Outros". Renderize só enquanto estiver aberta, para começar sempre em branco.
 */
function DesenturmacaoDialog({ turmaNome, alunos, emLote, isLoading, onConfirm, onCancel }: DesenturmacaoDialogProps) {
    const [motivo, setMotivo] = useState('');
    const [observacao, setObservacao] = useState('');
    const [erro, setErro] = useState<string | null>(null);

    const confirmar = () => {
        if (!motivo) {
            setErro('Por favor, selecione o motivo da desenturmação para continuar.');
            return;
        }
        if (motivo === MOTIVO_OUTROS && !observacao.trim()) {
            setErro('Informe a observação/justificativa quando o motivo for "Outros".');
            return;
        }
        onConfirm(motivo, observacao.trim() || null);
    };

    const quantidade = alunos.length;

    return (
        <ConfirmDialog
            open
            title={emLote ? 'Desenturmar turma inteira' : quantidade === 1 ? 'Desenturmar aluno' : 'Desenturmar alunos'}
            variant="warning"
            confirmLabel={emLote ? 'Confirmar Desenturmação em Lote' : 'Confirmar Desenturmação'}
            isLoading={isLoading}
            onConfirm={confirmar}
            onCancel={onCancel}
        >
            <p>
                {emLote ? (
                    <>Todos os <strong>{quantidade} alunos</strong> serão retirados da turma</>
                ) : quantidade === 1 ? (
                    <><strong>{alunos[0]?.nome}</strong> será retirado(a) da turma</>
                ) : (
                    <><strong>{quantidade} alunos</strong> serão retirados da turma</>
                )}{' '}
                <strong>{turmaNome}</strong> a partir de hoje. Frequências e notas já lançadas são mantidas.
            </p>
            {!emLote && quantidade > 1 && quantidade <= 5 && (
                <ul className="confirm-dialog-list">
                    {alunos.map(aluno => <li key={aluno.alunoId}>{aluno.nome}</li>)}
                </ul>
            )}

            <div className="desenturmacao-campos">
                <div className="form-field">
                    <label htmlFor="desenturmacao-motivo">
                        Motivo da Desenturmação <span className="required">*</span>
                    </label>
                    <select
                        id="desenturmacao-motivo"
                        value={motivo}
                        disabled={isLoading}
                        aria-invalid={erro !== null && !motivo}
                        onChange={event => {
                            setMotivo(event.target.value);
                            setErro(null);
                        }}
                    >
                        <option value="">Selecione o motivo</option>
                        {motivos.map(opcao => <option key={opcao.value} value={opcao.value}>{opcao.label}</option>)}
                    </select>
                    {motivo && <span className="field-hint">{efeitoDoMotivo[motivo]}</span>}
                </div>

                <div className="form-field">
                    <label htmlFor="desenturmacao-observacao">
                        Observação / Justificativa{' '}
                        {motivo === MOTIVO_OUTROS
                            ? <span className="required">*</span>
                            : <span className="label-optional">(opcional)</span>}
                    </label>
                    <textarea
                        id="desenturmacao-observacao"
                        rows={3}
                        maxLength={MAX_OBSERVACAO}
                        value={observacao}
                        disabled={isLoading}
                        onChange={event => {
                            setObservacao(event.target.value);
                            setErro(null);
                        }}
                    />
                </div>

                {erro && <p className="field-error" role="alert">{erro}</p>}
            </div>
        </ConfirmDialog>
    );
}

export default DesenturmacaoDialog;

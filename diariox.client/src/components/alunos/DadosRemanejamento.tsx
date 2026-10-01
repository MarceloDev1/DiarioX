import { useEffect, useState } from 'react';
import { apiFetch, readApiError } from '../../utils/api';
import { hojeIso } from '../../utils/formatters';

interface TurmaDestino {
    turmaId: number;
    nomeCompleto: string;
    turno: string;
    vagasOfertadas: number;
    vagasDisponiveis: number;
}

export interface DadosRemanejamentoValores {
    dataMovimentacao: string;
    turmaDestinoId: number;
    motivo: string | null;
}

interface DadosRemanejamentoProps {
    turmaOrigemId: number;
    /** Alunos selecionados para o remanejamento. */
    quantidade: number;
    saving: boolean;
    onConfirm: (valores: DadosRemanejamentoValores) => void;
    onCancel: () => void;
}

// Sugestões para o motivo; o campo aceita qualquer texto.
const motivosSugeridos = ['Reorganização de turmas', 'Adequação de turno', 'Solicitação da família', 'Equilíbrio de vagas'];

const plural = (quantidade: number, singular: string, pluralForma: string) =>
    `${quantidade} ${quantidade === 1 ? singular : pluralForma}`;

/**
 * Painel "Dados do Remanejamento": data da movimentação, nova turma (só as da mesma escola, ano letivo e
 * etapa, com vaga na data) e motivo. Use uma key com a turma de origem para recomeçar em branco.
 */
function DadosRemanejamento({ turmaOrigemId, quantidade, saving, onConfirm, onCancel }: DadosRemanejamentoProps) {
    const [dataMovimentacao, setDataMovimentacao] = useState(hojeIso);
    const [turmaDestinoId, setTurmaDestinoId] = useState('');
    const [motivo, setMotivo] = useState('');
    const [destinos, setDestinos] = useState<{ chave: string; turmas: TurmaDestino[] } | null>(null);
    const [erro, setErro] = useState<string | null>(null);

    const chave = `${turmaOrigemId}|${dataMovimentacao}`;

    useEffect(() => {
        if (!dataMovimentacao) return;

        let cancelado = false;
        void apiFetch(`/api/turmas/${turmaOrigemId}/destinos-remanejamento?data=${dataMovimentacao}`)
            .then(async response => {
                if (!response.ok) throw new Error(await readApiError(response));
                const turmas = (await response.json()) as TurmaDestino[];
                if (!cancelado) setDestinos({ chave: `${turmaOrigemId}|${dataMovimentacao}`, turmas });
            })
            .catch(reason => {
                if (!cancelado) setErro(reason instanceof Error ? reason.message : 'Falha ao carregar as turmas de destino.');
            });

        return () => { cancelado = true; };
    }, [turmaOrigemId, dataMovimentacao]);

    // Só valem as turmas consultadas para a data atual; a turma escolhida sai se deixar de ter vaga.
    const turmasDestino = destinos?.chave === chave ? destinos.turmas : null;
    const destino = turmasDestino?.find(turma => String(turma.turmaId) === turmaDestinoId) ?? null;
    const excedeVagas = destino !== null && quantidade > destino.vagasDisponiveis;

    const confirmar = () => {
        if (!dataMovimentacao || !destino) {
            setErro('Por favor, informe a data da movimentação e a nova turma.');
            return;
        }
        if (dataMovimentacao > hojeIso()) {
            setErro('A data da movimentação deve estar entre o início do ano letivo e a data atual.');
            return;
        }
        setErro(null);
        onConfirm({ dataMovimentacao, turmaDestinoId: destino.turmaId, motivo: motivo.trim() || null });
    };

    return (
        <section className="content-card">
            <div className="section-header">
                <div>
                    <h2>Dados do Remanejamento</h2>
                    <p>O aluno deixa a turma atual na véspera da data da movimentação e entra na nova turma nessa data.</p>
                </div>
            </div>

            {erro && <div className="feedback-message feedback-error">{erro}</div>}

            <div className="cadastro-form escola-form form-grid">
                <div className="form-field">
                    <label htmlFor="remanejamento-data">
                        Data da Movimentação <span className="required">*</span>
                    </label>
                    <input
                        id="remanejamento-data"
                        type="date"
                        value={dataMovimentacao}
                        max={hojeIso()}
                        disabled={saving}
                        onChange={event => {
                            setDataMovimentacao(event.target.value);
                            setErro(null);
                        }}
                    />
                </div>

                <div className="form-field">
                    <label htmlFor="remanejamento-destino">
                        Nova Turma (Destino) <span className="required">*</span>
                    </label>
                    <select
                        id="remanejamento-destino"
                        value={destino ? turmaDestinoId : ''}
                        disabled={saving || turmasDestino === null}
                        onChange={event => {
                            setTurmaDestinoId(event.target.value);
                            setErro(null);
                        }}
                    >
                        <option value="">
                            {turmasDestino === null
                                ? 'Carregando turmas...'
                                : turmasDestino.length === 0 ? 'Nenhuma turma da mesma etapa com vaga' : 'Selecione a nova turma'}
                        </option>
                        {turmasDestino?.map(turma => (
                            <option key={turma.turmaId} value={turma.turmaId}>
                                {turma.nomeCompleto} · {plural(turma.vagasDisponiveis, 'vaga', 'vagas')}
                            </option>
                        ))}
                    </select>
                    {excedeVagas && (
                        <span className="field-error">
                            A turma possui {plural(destino.vagasDisponiveis, 'vaga disponível', 'vagas disponíveis')}, mas{' '}
                            {plural(quantidade, 'aluno foi selecionado', 'alunos foram selecionados')}.
                        </span>
                    )}
                </div>

                <div className="form-field form-field-full">
                    <label htmlFor="remanejamento-motivo">
                        Motivo <span className="label-optional">(opcional)</span>
                    </label>
                    <input
                        id="remanejamento-motivo"
                        type="text"
                        list="remanejamento-motivos"
                        maxLength={500}
                        placeholder="Escolha uma sugestão ou digite"
                        value={motivo}
                        disabled={saving}
                        onChange={event => setMotivo(event.target.value)}
                    />
                    <datalist id="remanejamento-motivos">
                        {motivosSugeridos.map(item => <option key={item} value={item} />)}
                    </datalist>
                </div>
            </div>

            <div className="form-actions">
                <button className="primary-button" type="button" disabled={saving || excedeVagas} onClick={confirmar}>
                    {saving
                        ? 'Remanejando...'
                        : quantidade > 1 ? `Confirmar Remanejamento de ${quantidade} alunos` : 'Confirmar Remanejamento'}
                </button>
                <button className="secondary-button cancel-button" type="button" disabled={saving} onClick={onCancel}>
                    Cancelar
                </button>
            </div>
        </section>
    );
}

export default DadosRemanejamento;

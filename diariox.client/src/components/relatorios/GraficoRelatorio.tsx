import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import type { GraficoRelatorio as Grafico } from './tipos';

// Um só tom de azul em dois passos (validado: ordem clara → escura, contraste ≥ 2:1 no fundo branco).
// Série 1 = a parte "cheia" (ex.: enturmados); série 2 = o restante (ex.: vagas disponíveis).
const CORES = ['#2a78d6', '#86b6ef'];
const TEXTO_SECUNDARIO = '#5b6b82';
const GRADE = '#e5ecf5';

const numero = new Intl.NumberFormat('pt-BR');

interface GraficoRelatorioProps {
    grafico: Grafico;
}

/** Barras horizontais: nomes de escola e turma são longos e ficam legíveis no eixo vertical. */
function GraficoRelatorio({ grafico }: GraficoRelatorioProps) {
    const dados = grafico.categorias.map((categoria, i) => {
        const linha: Record<string, string | number> = { categoria };
        grafico.series.forEach((serie, s) => { linha[`s${s}`] = serie.valores[i]; });
        return linha;
    });

    const altura = Math.max(140, grafico.categorias.length * 34 + 40);
    const ultimaSerie = grafico.series.length - 1;

    return (
        <figure className="relatorio-grafico">
            <figcaption>
                <strong>{grafico.titulo}</strong>
                {grafico.series.length > 1 && (
                    <span className="relatorio-legenda">
                        {grafico.series.map((serie, s) => (
                            <span key={serie.nome} className="relatorio-legenda-item">
                                <span className="relatorio-legenda-cor" style={{ background: CORES[s % CORES.length] }} />
                                {serie.nome}
                            </span>
                        ))}
                    </span>
                )}
            </figcaption>
            <ResponsiveContainer width="100%" height={altura}>
                <BarChart data={dados} layout="vertical" margin={{ top: 4, right: 16, bottom: 4, left: 4 }} barCategoryGap="28%">
                    <CartesianGrid horizontal={false} stroke={GRADE} />
                    <XAxis
                        type="number"
                        allowDecimals={false}
                        tick={{ fill: TEXTO_SECUNDARIO, fontSize: 12 }}
                        tickFormatter={v => numero.format(v)}
                        axisLine={false}
                        tickLine={false}
                    />
                    <YAxis
                        type="category"
                        dataKey="categoria"
                        width={190}
                        tick={{ fill: TEXTO_SECUNDARIO, fontSize: 12 }}
                        tickFormatter={(v: string) => (v.length > 28 ? `${v.slice(0, 27)}…` : v)}
                        axisLine={false}
                        tickLine={false}
                    />
                    <Tooltip
                        cursor={{ fill: 'rgba(15, 52, 96, 0.06)' }}
                        formatter={(valor, nome) => [numero.format(Number(valor)), nome]}
                        contentStyle={{ borderRadius: 8, borderColor: GRADE, fontSize: 13 }}
                    />
                    {grafico.series.map((serie, s) => (
                        <Bar
                            key={serie.nome}
                            dataKey={`s${s}`}
                            name={serie.nome}
                            stackId={grafico.empilhado ? 'total' : undefined}
                            fill={CORES[s % CORES.length]}
                            // Linha branca de 2px separa os segmentos; só a ponta da barra é arredondada.
                            stroke="#fff"
                            strokeWidth={2}
                            radius={!grafico.empilhado || s === ultimaSerie ? [0, 4, 4, 0] : 0}
                            maxBarSize={22}
                        />
                    ))}
                </BarChart>
            </ResponsiveContainer>
        </figure>
    );
}

export default GraficoRelatorio;

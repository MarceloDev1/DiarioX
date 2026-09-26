export interface EnderecoCep {
    logradouro: string;
    bairro: string;
    cidade: string;
    uf: string;
}

interface ViaCepResponse {
    logradouro?: string;
    bairro?: string;
    localidade?: string;
    uf?: string;
    erro?: boolean | string;
}

/**
 * Consulta o endereço de um CEP na ViaCEP.
 * Retorna null quando o CEP não existe; lança erro quando o serviço não responde.
 */
export async function buscarEnderecoPorCep(cep: string, signal?: AbortSignal): Promise<EnderecoCep | null> {
    const digits = cep.replace(/\D/g, '');
    if (digits.length !== 8) return null;

    const response = await fetch(`https://viacep.com.br/ws/${digits}/json/`, { signal });
    if (!response.ok) throw new Error(`ViaCEP respondeu ${response.status}`);

    const data = (await response.json()) as ViaCepResponse;
    if (data.erro) return null;

    return {
        logradouro: data.logradouro ?? '',
        bairro: data.bairro ?? '',
        cidade: data.localidade ?? '',
        uf: data.uf ?? '',
    };
}

/** Monta o texto do campo "Endereço completo": logradouro, cidade - UF (omitindo partes vazias). */
export function formatEnderecoCep(endereco: EnderecoCep): string {
    const cidadeUf = [endereco.cidade, endereco.uf].filter(Boolean).join(' - ');
    return [endereco.logradouro, cidadeUf].filter(Boolean).join(', ');
}

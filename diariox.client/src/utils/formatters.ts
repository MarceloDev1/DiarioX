import { normalizeCpf } from './validators';

export { normalizeCpf };

export function formatCpf(value: string) {
    const cpf = normalizeCpf(value).slice(0, 11);
    return cpf
        .replace(/(\d{3})(\d)/, '$1.$2')
        .replace(/(\d{3})(\d)/, '$1.$2')
        .replace(/(\d{3})(\d{1,2})$/, '$1-$2');
}

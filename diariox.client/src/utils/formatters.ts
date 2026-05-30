import { normalizeCnpj, normalizeCpf } from './validators';

export { normalizeCpf, normalizeCnpj };

export function formatCpf(value: string) {
    const cpf = normalizeCpf(value).slice(0, 11);
    return cpf
        .replace(/(\d{3})(\d)/, '$1.$2')
        .replace(/(\d{3})(\d)/, '$1.$2')
        .replace(/(\d{3})(\d{1,2})$/, '$1-$2');
}

export function formatCnpj(value: string) {
    const cnpj = normalizeCnpj(value).slice(0, 14);
    return cnpj
        .replace(/(\d{2})(\d)/, '$1.$2')
        .replace(/(\d{3})(\d)/, '$1.$2')
        .replace(/(\d{3})(\d)/, '$1/$2')
        .replace(/(\d{4})(\d{1,2})$/, '$1-$2');
}

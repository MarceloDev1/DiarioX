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

export function formatTelefone(value: string) {
    const d = value.replace(/\D/g, '').slice(0, 11);
    if (d.length <= 2)  return d.replace(/^(\d{1,2})$/, '($1');
    if (d.length <= 6)  return d.replace(/^(\d{2})(\d{1,4})$/, '($1) $2');
    if (d.length <= 10) return d.replace(/^(\d{2})(\d{4})(\d{0,4})$/, '($1) $2-$3');
    return d.replace(/^(\d{2})(\d{5})(\d{0,4})$/, '($1) $2-$3');
}

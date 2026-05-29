export function normalizeCpf(value: string) {
    return value.replace(/\D/g, '');
}

export function validateCpf(cpf: string) {
    const onlyDigits = normalizeCpf(cpf);
    if (onlyDigits.length !== 11 || /^(\d)\1+$/.test(onlyDigits)) {
        return false;
    }

    const numbers = onlyDigits.split('').map(Number);
    const calculateDigit = (length: number) => {
        let sum = 0;
        for (let i = 0; i < length; i += 1) {
            sum += numbers[i] * (length + 1 - i);
        }
        const remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    };

    const digit1 = calculateDigit(9);
    const digit2 = calculateDigit(10);
    return numbers[9] === digit1 && numbers[10] === digit2;
}

export function validateBirthDate(value: string) {
    if (!value) return false;
    const birthDate = new Date(value);
    if (Number.isNaN(birthDate.getTime())) return false;
    return birthDate < new Date();
}

export function validatePassword(password: string) {
    return /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$/.test(password);
}

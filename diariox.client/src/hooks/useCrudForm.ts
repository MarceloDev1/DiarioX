import { useState, type ChangeEvent } from 'react';

export function useCrudForm<T extends Record<string, unknown>>(emptyForm: T) {
    const [form, setForm] = useState<T>(emptyForm);
    const [editingId, setEditingId] = useState<number | null>(null);

    const handleFieldChange = (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
        const { name, value } = event.target;
        setForm((current) => ({ ...current, [name]: value }));
    };

    const startEdit = (id: number, data: T) => {
        setEditingId(id);
        setForm(data);
    };

    const clear = () => {
        setForm(emptyForm);
        setEditingId(null);
    };

    return { form, setForm, editingId, handleFieldChange, startEdit, clear };
}

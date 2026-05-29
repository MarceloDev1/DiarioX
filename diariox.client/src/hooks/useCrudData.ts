import { useState } from 'react';
import { readApiError } from '../utils/api';

export function useCrudData<T extends { id: number }>(endpoint: string) {
    const [items, setItems] = useState<T[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const load = async () => {
        setIsLoading(true);
        setError(null);
        try {
            const response = await fetch(endpoint);
            if (!response.ok) throw new Error(await readApiError(response));
            const data = (await response.json()) as T[];
            setItems(data);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao carregar dados.');
        } finally {
            setIsLoading(false);
        }
    };

    const save = async (editingId: number | null, body: unknown): Promise<T | null> => {
        setIsSaving(true);
        setError(null);
        try {
            const isEditing = editingId !== null;
            const url = isEditing ? `${endpoint}/${editingId}` : endpoint;
            const response = await fetch(url, {
                method: isEditing ? 'PUT' : 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });
            if (!response.ok) throw new Error(await readApiError(response));
            const item = (await response.json()) as T;
            if (isEditing) {
                setItems((current) => current.map((i) => (i.id === item.id ? item : i)));
            } else {
                setItems((current) => [...current, item]);
            }
            return item;
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao salvar.');
            return null;
        } finally {
            setIsSaving(false);
        }
    };

    const remove = async (id: number): Promise<boolean> => {
        setError(null);
        try {
            const response = await fetch(`${endpoint}/${id}`, { method: 'DELETE' });
            if (!response.ok) throw new Error(await readApiError(response));
            setItems((current) => current.filter((i) => i.id !== id));
            return true;
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Falha ao excluir.');
            return false;
        }
    };

    return { items, isLoading, isSaving, error, load, save, remove };
}

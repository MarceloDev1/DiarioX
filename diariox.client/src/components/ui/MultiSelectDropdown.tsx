import { useEffect, useRef, useState } from 'react';
import './MultiSelectDropdown.css';

interface MultiSelectOption {
    value: string;
    label: string;
}

interface MultiSelectDropdownProps {
    id: string;
    options: MultiSelectOption[];
    selected: string[];
    onChange: (selected: string[]) => void;
    placeholder?: string;
    searchPlaceholder?: string;
    emptyMessage?: string;
    confirmLabel?: string;
    disabled?: boolean;
}

function MultiSelectDropdown({
    id,
    options,
    selected,
    onChange,
    placeholder = 'Selecione',
    searchPlaceholder = 'Pesquisar...',
    emptyMessage = 'Nenhuma opção disponível',
    confirmLabel = 'Confirmar',
    disabled = false,
}: MultiSelectDropdownProps) {
    const [open, setOpen] = useState(false);
    const [draft, setDraft] = useState<string[]>(selected);
    const [search, setSearch] = useState('');
    const containerRef = useRef<HTMLDivElement>(null);
    const searchInputRef = useRef<HTMLInputElement>(null);

    const openMenu = () => {
        setDraft(selected);
        setSearch('');
        setOpen(true);
    };

    useEffect(() => {
        if (!open) return;

        const handlePointerDown = (event: MouseEvent) => {
            if (!containerRef.current?.contains(event.target as Node)) setOpen(false);
        };
        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key === 'Escape') setOpen(false);
        };

        document.addEventListener('mousedown', handlePointerDown);
        document.addEventListener('keydown', handleKeyDown);
        return () => {
            document.removeEventListener('mousedown', handlePointerDown);
            document.removeEventListener('keydown', handleKeyDown);
        };
    }, [open]);

    const toggle = (value: string) => {
        setDraft(current => current.includes(value)
            ? current.filter(item => item !== value)
            : [...current, value]);
    };

    const confirm = () => {
        onChange(draft);
        setOpen(false);
    };

    const term = search.trim().toLowerCase();
    const visibleOptions = options.filter(option => option.label.toLowerCase().includes(term));

    const selectedLabels = options.filter(option => selected.includes(option.value)).map(option => option.label);
    const summary = selectedLabels.length === 0
        ? placeholder
        : selectedLabels.length <= 2
            ? selectedLabels.join(', ')
            : `${selectedLabels.length} selecionadas`;

    return (
        <div className="multiselect" ref={containerRef}>
            <button
                type="button"
                id={id}
                className={`multiselect-trigger${selectedLabels.length === 0 ? ' placeholder' : ''}`}
                aria-haspopup="listbox"
                aria-expanded={open}
                disabled={disabled}
                onClick={() => (open ? setOpen(false) : openMenu())}
            >
                <span className="multiselect-summary">{summary}</span>
                <span className="multiselect-caret" aria-hidden="true">▾</span>
            </button>

            {open && (
                <div className="multiselect-menu">
                    <div className="multiselect-search">
                        <svg viewBox="0 0 20 20" width="18" height="18" aria-hidden="true">
                            <circle cx="8.5" cy="8.5" r="5.5" fill="none" stroke="currentColor" strokeWidth="1.6" />
                            <path d="M13 13l4.5 4.5" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" />
                        </svg>
                        <input
                            type="text"
                            ref={searchInputRef}
                            value={search}
                            onChange={event => setSearch(event.target.value)}
                            onKeyDown={event => { if (event.key === 'Enter') event.preventDefault(); }}
                            placeholder={searchPlaceholder}
                            aria-label={searchPlaceholder}
                            autoFocus
                        />
                        <button
                            type="button"
                            className="multiselect-search-clear"
                            aria-label="Limpar pesquisa"
                            title="Limpar pesquisa"
                            disabled={!search}
                            onClick={() => {
                                setSearch('');
                                searchInputRef.current?.focus();
                            }}
                        >
                            ✕
                        </button>
                    </div>

                    <div className="multiselect-list" role="listbox" aria-multiselectable="true">
                        {visibleOptions.length === 0 ? (
                            <p className="no-options">{options.length === 0 ? emptyMessage : 'Nenhum resultado encontrado'}</p>
                        ) : (
                            visibleOptions.map(option => {
                                const isSelected = draft.includes(option.value);
                                return (
                                    <div
                                        key={option.value}
                                        role="option"
                                        aria-selected={isSelected}
                                        tabIndex={0}
                                        className={`multiselect-option${isSelected ? ' selected' : ''}`}
                                        onClick={() => toggle(option.value)}
                                        onKeyDown={event => {
                                            if (event.key === ' ' || event.key === 'Enter') {
                                                event.preventDefault();
                                                toggle(option.value);
                                            }
                                        }}
                                    >
                                        <span>{option.label}</span>
                                        {isSelected && <span className="multiselect-check" aria-hidden="true">✓</span>}
                                    </div>
                                );
                            })
                        )}
                    </div>

                    <button type="button" className="multiselect-confirm" onClick={confirm}>
                        {confirmLabel}
                    </button>
                </div>
            )}
        </div>
    );
}

export default MultiSelectDropdown;

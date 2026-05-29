interface EmptyStateProps {
    loading?: boolean;
    loadingMessage?: string;
    emptyMessage: string;
    emptySubMessage?: string;
}

function EmptyState({ loading, loadingMessage, emptyMessage, emptySubMessage }: EmptyStateProps) {
    if (loading) {
        return (
            <div className="empty-state">
                <strong>{loadingMessage}</strong>
            </div>
        );
    }
    return (
        <div className="empty-state">
            <strong>{emptyMessage}</strong>
            {emptySubMessage && <span>{emptySubMessage}</span>}
        </div>
    );
}

export default EmptyState;

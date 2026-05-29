interface FeedbackMessageProps {
    message: string | null;
    type?: 'error' | 'success';
}

function FeedbackMessage({ message, type = 'error' }: FeedbackMessageProps) {
    if (!message) return null;
    return <div className={`feedback-message feedback-${type}`}>{message}</div>;
}

export default FeedbackMessage;

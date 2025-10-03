import './Toast.css';

export type ToastIntent = 'info' | 'success' | 'warning' | 'error';

export interface ToastProps {
  title: string;
  description?: string;
  intent?: ToastIntent;
  onDismiss?: () => void;
}

export function Toast({ title, description, intent = 'info', onDismiss }: ToastProps) {
  return (
    <div className={`toast toast--${intent}`} role="status" aria-live="polite">
      <div className="toast__content">
        <p className="toast__title">{title}</p>
        {description ? <p className="toast__description">{description}</p> : null}
      </div>
      {onDismiss ? (
        <button
          type="button"
          className="toast__dismiss"
          onClick={onDismiss}
          aria-label="Dismiss notification"
        >
          ×
        </button>
      ) : null}
    </div>
  );
}

export default Toast;

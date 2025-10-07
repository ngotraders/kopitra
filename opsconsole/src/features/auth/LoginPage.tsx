import type { FormEvent } from 'react';
import { useCallback, useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuthorization } from '../../contexts';
import './LoginPage.css';

interface LoginLocationState {
  from?: string;
}

export function LoginPage() {
  const { signIn, isAuthenticated, isLoading } = useAuthorization();
  const navigate = useNavigate();
  const location = useLocation();
  const [userId, setUserId] = useState('');
  const [email, setEmail] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const destination = (location.state as LoginLocationState | null)?.from ?? '/';

  useEffect(() => {
    if (isAuthenticated) {
      navigate(destination, { replace: true });
    }
  }, [isAuthenticated, destination, navigate]);

  const handleSubmit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (submitting) {
        return;
      }

      const trimmedUserId = userId.trim();
      const trimmedEmail = email.trim();

      if (!trimmedUserId || !trimmedEmail) {
        setError('Enter your operator ID and email to continue.');
        return;
      }

      setSubmitting(true);
      setError(null);
      try {
        await signIn({ userId: trimmedUserId, email: trimmedEmail.toLowerCase() });
        navigate(destination, { replace: true });
      } catch (err) {
        const message =
          err instanceof Error
            ? err.message
            : 'Unable to sign in. Check your credentials and try again.';
        setError(message);
      } finally {
        setSubmitting(false);
      }
    },
    [destination, email, navigate, signIn, submitting, userId],
  );

  return (
    <div className="login-page">
      <section className="login-card" aria-labelledby="login-title">
        <h1 id="login-title">Sign in to Ops Console</h1>
        <p className="login-card__subtitle">
          Authenticate with your operator directory credentials to manage copy trading operations.
        </p>
        <form className="login-form" onSubmit={handleSubmit} noValidate>
          <label className="login-form__label" htmlFor="login-user-id">
            Operator ID
          </label>
          <input
            id="login-user-id"
            name="userId"
            data-testid="login-userId"
            className="login-form__input"
            autoComplete="username"
            value={userId}
            onChange={(event) => setUserId(event.target.value)}
            disabled={submitting || isLoading}
            required
          />

          <label className="login-form__label" htmlFor="login-email">
            Email
          </label>
          <input
            id="login-email"
            name="email"
            data-testid="login-email"
            className="login-form__input"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            disabled={submitting || isLoading}
            required
          />

          {error ? (
            <div role="alert" className="login-form__error" data-testid="login-error">
              {error}
            </div>
          ) : null}

          <button
            type="submit"
            className="login-form__submit"
            disabled={submitting || isLoading}
            data-testid="login-submit"
          >
            {submitting ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
      </section>
    </div>
  );
}

export default LoginPage;

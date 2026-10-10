import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { ApiError, toErrorMessage } from '../../api/client';
import { authApi } from '../../api/auth';
import { writeSession } from '../../api/session';
import { Field } from '../../components/Field';

interface LoginState {
  email: string;
  password: string;
}

const empty: LoginState = { email: '', password: '' };

/** Signs an agent in. Empty fields never reach the API. */
export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [form, setForm] = useState<LoginState>(empty);
  const [errors, setErrors] = useState<Partial<LoginState>>({});
  const [submitError, setSubmitError] = useState<string>();
  const [isSaving, setIsSaving] = useState(false);

  const update = (patch: Partial<LoginState>) => setForm((current) => ({ ...current, ...patch }));

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const nextErrors = validate(form);
    setErrors(nextErrors);
    setSubmitError(undefined);

    if (nextErrors.email || nextErrors.password) {
      return;
    }

    setIsSaving(true);

    try {
      const result = await authApi.login(form.email.trim(), form.password);
      writeSession({
        token: result.token,
        expiresAtUtc: result.expiresAtUtc,
        agent: result.agent,
      });
      navigate(returnPath(location), { replace: true });
    } catch (caught) {
      setSubmitError(
        caught instanceof ApiError
          ? caught.message
          : toErrorMessage(caught, 'Could not sign in.'),
      );
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <main className="login">
      <form className="card form" onSubmit={(event) => void submit(event)} noValidate>
        <h1>Sign in</h1>
        <p className="summary__note">Use the email address of a support agent.</p>

        <Field id="login-email" label="Email" error={errors.email}>
          {(props) => (
            <input
              {...props}
              type="email"
              autoComplete="username"
              value={form.email}
              onChange={(event) => update({ email: event.target.value })}
            />
          )}
        </Field>

        <Field id="login-password" label="Password" error={errors.password}>
          {(props) => (
            <input
              {...props}
              type="password"
              autoComplete="current-password"
              value={form.password}
              onChange={(event) => update({ password: event.target.value })}
            />
          )}
        </Field>

        {submitError && (
          <p className="field__error" role="alert">
            {submitError}
          </p>
        )}

        <button type="submit" className="button button--primary" disabled={isSaving}>
          {isSaving ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </main>
  );
}

function validate(form: LoginState): Partial<LoginState> {
  const errors: Partial<LoginState> = {};

  if (!form.email.trim().includes('@')) {
    errors.email = 'Enter the email address you sign in with.';
  }

  if (form.password.length === 0) {
    errors.password = 'Enter your password.';
  }

  return errors;
}

/** Only paths inside this app are accepted, so a return value cannot leave the site. */
function returnPath(location: { state: unknown; search: string }): string {
  const fromState = (location.state as { from?: string } | null)?.from;
  const fromQuery = new URLSearchParams(location.search).get('return') ?? undefined;
  const candidate = fromState || fromQuery;

  if (!candidate || !candidate.startsWith('/') || candidate.startsWith('//') || candidate.startsWith('/login')) {
    return '/tickets';
  }

  return candidate;
}

import { useEffect, useState } from 'react';
import { ApiError, toErrorMessage } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import { Field } from '../../../components/Field';
import { formatDateTime, humanize } from '../../../lib/format';
import type { TicketDetail, TicketEscalation } from '../../../types/api';

const reasonMin = 5;
const reasonMax = 500;
const actorMax = 100;

interface EscalationPanelProps {
  ticket: TicketDetail;
  onUpdated: (ticket: TicketDetail) => void;
}

/** Records an escalation and shows the history, without reloading the page. */
export function EscalationPanel({ ticket, onUpdated }: EscalationPanelProps) {
  const [reason, setReason] = useState('');
  const [escalatedBy, setEscalatedBy] = useState('');
  const [errors, setErrors] = useState<{ reason?: string; escalatedBy?: string }>({});
  const [submitError, setSubmitError] = useState<string>();
  const [isSaving, setIsSaving] = useState(false);
  const [history, setHistory] = useState<TicketEscalation[]>([]);
  const [historyError, setHistoryError] = useState<string>();

  const canEscalate = ticket.priority !== 'Critical' && ticket.status !== 'Resolved' && ticket.status !== 'Closed';

  useEffect(() => {
    let active = true;

    ticketsApi
      .getEscalations(ticket.id)
      .then((rows) => {
        if (active) {
          setHistory(rows);
        }
      })
      .catch((caught: unknown) => {
        if (active) {
          setHistoryError(toErrorMessage(caught, 'Unable to load escalation history.'));
        }
      });

    return () => {
      active = false;
    };
  }, [ticket.id]);

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const nextErrors = validate(reason, escalatedBy);
    setErrors(nextErrors);
    setSubmitError(undefined);

    if (nextErrors.reason || nextErrors.escalatedBy) {
      return;
    }

    setIsSaving(true);

    try {
      const result = await ticketsApi.escalate(ticket.id, reason.trim(), escalatedBy.trim());
      setReason('');
      setEscalatedBy('');
      setHistory((current) => [result.escalation, ...current.filter((row) => row.id !== result.escalation.id)]);
      onUpdated(result.ticket);
    } catch (caught) {
      setSubmitError(
        caught instanceof ApiError
          ? caught.message
          : toErrorMessage(caught, 'Could not escalate the ticket.'),
      );
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="card">
      <h2>Escalation</h2>

      {!canEscalate && (
        <p className="summary__note">
          {ticket.priority === 'Critical'
            ? 'Critical tickets cannot be escalated.'
            : `${humanize(ticket.status)} tickets cannot be escalated.`}
        </p>
      )}

      <form className="form" onSubmit={(event) => void submit(event)}>
        <Field id="escalate-by" label="Escalated by" error={errors.escalatedBy} hint="Until sign-in, say who you are.">
          {(props) => (
            <input
              {...props}
              value={escalatedBy}
              maxLength={actorMax}
              onChange={(event) => setEscalatedBy(event.target.value)}
            />
          )}
        </Field>

        <Field
          id="escalate-reason"
          label="Reason"
          error={errors.reason}
          hint={`${reasonMin} to ${reasonMax} characters.`}
        >
          {(props) => (
            <textarea
              {...props}
              rows={3}
              value={reason}
              maxLength={reasonMax}
              onChange={(event) => setReason(event.target.value)}
            />
          )}
        </Field>

        {submitError && (
          <p className="field__error" role="alert">
            {submitError}
          </p>
        )}

        <button type="submit" className="button button--primary" disabled={!canEscalate || isSaving}>
          {isSaving ? 'Escalating…' : 'Escalate'}
        </button>
      </form>

      <h3>History</h3>
      {historyError && <p className="field__error">{historyError}</p>}
      {history.length === 0 && !historyError && <p className="summary__note">No escalations yet.</p>}
      {history.length > 0 && (
        <ol className="history">
          {history.map((row) => (
            <li key={row.id}>
              <p>
                <strong>{humanize(row.fromPriority)}</strong> to <strong>{humanize(row.toPriority)}</strong>
                {' · '}
                {row.fromAgent?.fullName ?? 'Unassigned'} to {row.toAgent?.fullName ?? 'Unassigned'}
              </p>
              <p>
                Due {formatDateTime(row.fromDueAtUtc)} to {formatDateTime(row.toDueAtUtc)}
              </p>
              <p>{row.reason}</p>
              <p className="summary__note">
                {row.escalatedBy} · {formatDateTime(row.escalatedAtUtc)}
              </p>
            </li>
          ))}
        </ol>
      )}
    </div>
  );
}

function validate(reason: string, escalatedBy: string): { reason?: string; escalatedBy?: string } {
  const errors: { reason?: string; escalatedBy?: string } = {};
  const trimmedReason = reason.trim();
  const trimmedBy = escalatedBy.trim();

  if (trimmedReason.length < reasonMin || trimmedReason.length > reasonMax) {
    errors.reason = `Enter a reason of ${reasonMin} to ${reasonMax} characters.`;
  }

  if (trimmedBy.length === 0 || trimmedBy.length > actorMax) {
    errors.escalatedBy = `Enter a name of up to ${actorMax} characters.`;
  }

  return errors;
}

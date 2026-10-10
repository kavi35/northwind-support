import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import type { TicketDetail } from '../../../types/api';
import { EscalationPanel } from './EscalationPanel';

vi.mock('../../../api/tickets', () => ({
  ticketsApi: {
    getEscalations: vi.fn(),
    escalate: vi.fn(),
  },
}));

const getEscalations = vi.mocked(ticketsApi.getEscalations);
const escalate = vi.mocked(ticketsApi.escalate);

function ticket(): TicketDetail {
  return {
    id: 12,
    reference: 'TCK-0012',
    title: 'Invoice total does not match the order',
    description: 'The total on the invoice is higher than the order.',
    status: 'InProgress',
    priority: 'High',
    customer: { id: 2, name: 'Fabrikam Inc', email: 'ap@fabrikam.example', phone: null, tier: 'Standard' },
    category: { id: 2, name: 'Billing', requiresSpecialist: true },
    assignedAgent: { id: 4, fullName: 'Sara Lindqvist' },
    createdAtUtc: '2026-09-24T08:12:00Z',
    updatedAtUtc: '2026-09-25T10:03:00Z',
    dueAtUtc: '2026-09-24T16:12:00Z',
    resolvedAtUtc: null,
    slaStatus: 'Breached',
    triage: null,
  };
}

describe('EscalationPanel', () => {
  beforeEach(() => {
    getEscalations.mockResolvedValue([]);
    escalate.mockReset();
  });

  it('does not call the API when the reason is too short', async () => {
    const user = userEvent.setup();
    render(<EscalationPanel ticket={ticket()} onUpdated={vi.fn()} />);

    await screen.findByText('No escalations yet.');
    await user.type(screen.getByLabelText('Reason'), 'no');
    await user.type(screen.getByLabelText('Escalated by'), 'Alex');
    await user.click(screen.getByRole('button', { name: 'Escalate' }));

    expect(escalate).not.toHaveBeenCalled();
    expect(screen.getByText('Enter a reason of 5 to 500 characters.')).toBeInTheDocument();
  });

  it('shows a 409 and leaves the ticket unchanged', async () => {
    const user = userEvent.setup();
    const onUpdated = vi.fn();
    escalate.mockRejectedValue(
      new ApiError(409, 'Ticket TCK-0012 is Critical and cannot be escalated.'),
    );

    render(<EscalationPanel ticket={ticket()} onUpdated={onUpdated} />);

    await screen.findByText('No escalations yet.');
    await user.type(screen.getByLabelText('Reason'), 'The customer is blocked.');
    await user.type(screen.getByLabelText('Escalated by'), 'Alex');
    await user.click(screen.getByRole('button', { name: 'Escalate' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Critical');
    expect(onUpdated).not.toHaveBeenCalled();
    expect(screen.getByLabelText('Reason')).toHaveValue('The customer is blocked.');
  });
});
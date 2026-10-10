import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { SlaStatus } from '../../../types/api';
import { SlaCell } from './SlaCell';

const labels: Record<SlaStatus, string> = {
  WithinSla: 'Within SLA',
  AtRisk: 'At risk',
  Breached: 'Breached',
  Met: 'Met',
  NotApplicable: 'No SLA',
};

describe('SlaCell', () => {
  it('renders every SLA state with its own words', () => {
    render(
      <>
        {(Object.keys(labels) as SlaStatus[]).map((status) => (
          <SlaCell key={status} slaStatus={status} dueAtUtc="2026-10-11T12:00:00Z" />
        ))}
      </>,
    );

    for (const label of Object.values(labels)) {
      expect(screen.getByText(label)).toBeInTheDocument();
    }
  });
});

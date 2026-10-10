import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ticketsApi } from '../../../api/tickets';
import { page, ticketListItem } from '../../../test/fixtures';
import type { TicketFilters } from '../../../types/api';
import { useTicketList } from './useTicketList';

vi.mock('../../../api/tickets', () => ({
  ticketsApi: {
    getTickets: vi.fn(),
  },
}));

const getTickets = vi.mocked(ticketsApi.getTickets);

describe('useTicketList', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    getTickets.mockResolvedValue(page([ticketListItem()]));
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.clearAllMocks();
  });

  it('searches with the text the user typed, once the typing settles', async () => {
    const { result } = renderHook(() => useTicketList());

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });

    expect(getTickets).toHaveBeenCalledTimes(1);

    act(() => {
      result.current.updateFilters({ search: 'invoice' });
    });

    // Still inside the debounce window: no second request yet.
    expect(getTickets).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });

    expect(getTickets).toHaveBeenCalledTimes(2);
    expect(getTickets).toHaveBeenLastCalledWith(
      expect.objectContaining({ search: 'invoice', page: 1 }),
    );

    expect(result.current.isLoading).toBe(false);
    expect(result.current.data?.items).toHaveLength(1);
  });

  it('sends a request as soon as status changes, and still sends that status after search is cleared', async () => {
    const { result } = renderHook(() => useTicketList());

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });
    getTickets.mockClear();

    await act(async () => {
      result.current.updateFilters({ status: 'InProgress' });
    });

    expect(getTickets).toHaveBeenCalledTimes(1);
    expect(getTickets).toHaveBeenLastCalledWith(
      expect.objectContaining({ status: 'InProgress', search: '', page: 1 }),
    );

    act(() => {
      result.current.updateFilters({ search: 'printer' });
    });

    // Inside the debounce window the status request is not repeated for each keystroke.
    expect(getTickets).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });

    expect(getTickets).toHaveBeenCalledTimes(2);
    expect(getTickets).toHaveBeenLastCalledWith(
      expect.objectContaining({ status: 'InProgress', search: 'printer', page: 1 }),
    );

    act(() => {
      result.current.updateFilters({ search: '' });
    });

    expect(getTickets).toHaveBeenCalledTimes(2);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });

    expect(getTickets).toHaveBeenCalledTimes(3);
    expect(getTickets).toHaveBeenLastCalledWith(
      expect.objectContaining({ status: 'InProgress', search: '', page: 1 }),
    );
  });

  it('sends a filter added later, such as sla status, without special wiring', async () => {
    const { result } = renderHook(() => useTicketList());

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });
    getTickets.mockClear();

    await act(async () => {
      result.current.updateFilters({ slaStatus: 'AtRisk' } as Partial<TicketFilters>);
    });

    expect(getTickets).toHaveBeenCalledTimes(1);
    expect(getTickets).toHaveBeenLastCalledWith(expect.objectContaining({ slaStatus: 'AtRisk' }));
  });

  it('clears a settled search immediately, along with the other filters', async () => {
    const { result } = renderHook(() => useTicketList());

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });

    await act(async () => {
      result.current.updateFilters({ status: 'InProgress', search: 'printer' });
    });

    await act(async () => {
      await vi.advanceTimersByTimeAsync(300);
    });
    getTickets.mockClear();

    await act(async () => {
      result.current.resetFilters();
    });

    expect(getTickets).toHaveBeenCalledTimes(1);
    const cleared = getTickets.mock.calls.at(-1)?.[0];
    expect(cleared?.search).toBe('');
    expect(cleared?.status).toBeUndefined();
    expect(cleared?.page).toBe(1);
  });
});

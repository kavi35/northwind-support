import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import type { PagedResult, TicketFilters, TicketListItem } from '../../../types/api';

const DEBOUNCE_MS = 300;

export const emptyFilters: TicketFilters = {
  search: '',
  status: undefined,
  priority: undefined,
  categoryId: undefined,
  customerId: undefined,
  assignedAgentId: undefined,
  unassignedOnly: false,
  slaStatus: undefined,
  page: 1,
  pageSize: 20,
  sortBy: 'createdAtUtc',
  sortDirection: 'desc',
};

interface State {
  isLoading: boolean;
  data?: PagedResult<TicketListItem>;
  error?: string;
}

/**
 * Owns the ticket list's filters and the data they produce.
 *
 * Search is debounced so typing does not send a request per keystroke. Every other field is
 * sent immediately. The request is keyed from the whole filter object, so a field added later
 * (such as slaStatus) is included without a change in this hook.
 */
export function useTicketList() {
  const [filters, setFilters] = useState<TicketFilters>(emptyFilters);
  const [debouncedSearch, setDebouncedSearch] = useState(filters.search);
  const [state, setState] = useState<State>({ isLoading: true });
  const requestSeq = useRef(0);

  useEffect(() => {
    if (filters.search === debouncedSearch) {
      return;
    }

    const handle = window.setTimeout(() => setDebouncedSearch(filters.search), DEBOUNCE_MS);

    return () => window.clearTimeout(handle);
  }, [filters.search, debouncedSearch]);

  // Search is the debounced value. Every other property, including ones added later, is current.
  const requestFilters: TicketFilters = { ...filters, search: debouncedSearch };
  const requestKey = JSON.stringify(requestFilters);

  const load = useCallback(async () => {
    const requested = JSON.parse(requestKey) as TicketFilters;
    const seq = ++requestSeq.current;
    setState((current) => ({ ...current, isLoading: true, error: undefined }));

    try {
      const data = await ticketsApi.getTickets(requested);
      if (seq !== requestSeq.current) {
        return;
      }

      setState({ isLoading: false, data, error: undefined });
    } catch (error) {
      if (seq !== requestSeq.current) {
        return;
      }

      const message = error instanceof ApiError ? error.message : 'Unable to load tickets.';
      setState((current) => ({ ...current, isLoading: false, error: message }));
    }
  }, [requestKey]);

  useEffect(() => {
    // Deferred so this effect does not set state synchronously. The microtask still runs
    // before the next paint, so a dropdown change is not held for the search debounce.
    queueMicrotask(() => {
      void load();
    });
  }, [load]);

  const updateFilters = (patch: Partial<TicketFilters>) =>
    setFilters((current) => ({ ...current, ...patch, page: patch.page ?? 1 }));

  const resetFilters = () => {
    setDebouncedSearch(emptyFilters.search);
    setFilters(emptyFilters);
  };

  return { filters, updateFilters, resetFilters, reload: load, ...state };
}

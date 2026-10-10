import { useSyncExternalStore } from 'react';
import { readSession, subscribeSession, type Session } from '../../api/session';

/** The current session. Updates when the agent signs in or out. */
export function useSession(): Session | null {
  return useSyncExternalStore(subscribeSession, readSession, readSession);
}

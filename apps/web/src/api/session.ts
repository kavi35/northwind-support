export interface SignedInAgent {
  id: number;
  fullName: string;
  email: string;
}

export interface Session {
  token: string;
  expiresAtUtc: string;
  agent: SignedInAgent;
}

const storageKey = 'support-desk.session';

let snapshot: Session | null = readStored();
const listeners = new Set<() => void>();

/** The signed-in agent, or null. The same object until the session changes. */
export function readSession(): Session | null {
  return snapshot;
}

export function writeSession(session: Session): void {
  localStorage.setItem(storageKey, JSON.stringify(session));
  snapshot = session;
  emit();
}

export function clearSession(): void {
  localStorage.removeItem(storageKey);
  snapshot = null;
  emit();
}

export function subscribeSession(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

/** Sends an unauthenticated visitor to login, remembering the page they asked for. */
export function redirectToLogin(): void {
  const here = `${window.location.pathname}${window.location.search}`;

  if (here.startsWith('/login')) {
    return;
  }

  window.location.assign(`/login?return=${encodeURIComponent(here)}`);
}

function emit(): void {
  listeners.forEach((listener) => listener());
}

function readStored(): Session | null {
  const raw = localStorage.getItem(storageKey);

  if (!raw) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as Session;

    if (!parsed.token || !parsed.agent?.fullName) {
      return null;
    }

    return parsed;
  } catch {
    return null;
  }
}

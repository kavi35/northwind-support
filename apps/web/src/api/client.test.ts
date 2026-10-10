import { afterEach, describe, expect, it, vi } from 'vitest';
import { request } from './client';
import { clearSession, writeSession } from './session';

describe('request', () => {
  afterEach(() => {
    clearSession();
    vi.unstubAllGlobals();
  });

  it('attaches the stored token to the call', async () => {
    writeSession({
      token: 'signed-token',
      expiresAtUtc: '2026-10-11T18:00:00Z',
      agent: { id: 1, fullName: 'Alex Turner', email: 'alex.turner@northwind-support.example' },
    });

    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ items: [] }),
    });
    vi.stubGlobal('fetch', fetchMock);

    await request('/tickets');

    const headers = fetchMock.mock.calls[0][1].headers as Headers;
    expect(headers.get('Authorization')).toBe('Bearer signed-token');
  });
});

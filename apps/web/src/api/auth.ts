import type { Session } from './session';
import { request } from './client';

export interface LoginResult {
  token: string;
  expiresAtUtc: string;
  agent: Session['agent'];
}

export const authApi = {
  login: (email: string, password: string) =>
    request<LoginResult>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
};

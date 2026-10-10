import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { beforeEach, describe, expect, it } from 'vitest';
import { clearSession } from '../../api/session';
import { RequireAuth } from './RequireAuth';

describe('RequireAuth', () => {
  beforeEach(() => {
    clearSession();
  });

  it('sends a visitor who is not signed in to the login page', () => {
    render(
      <MemoryRouter initialEntries={['/tickets/12']}>
        <Routes>
          <Route element={<RequireAuth />}>
            <Route path="/tickets/:id" element={<p>Ticket</p>} />
          </Route>
          <Route path="/login" element={<h1>Sign in</h1>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument();
    expect(screen.queryByText('Ticket')).not.toBeInTheDocument();
  });
});

import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useSession } from './useSession';

/** Sends a visitor who is not signed in to the login page, and remembers where they were going. */
export function RequireAuth() {
  const session = useSession();
  const location = useLocation();

  if (!session) {
    const from = `${location.pathname}${location.search}`;

    return <Navigate to="/login" replace state={{ from }} />;
  }

  return <Outlet />;
}

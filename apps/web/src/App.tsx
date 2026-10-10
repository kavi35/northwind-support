import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { clearSession } from './api/session';
import { useSession } from './features/auth/useSession';

/** Application shell: the header, the navigation, and wherever the router puts the page. */
export function App() {
  const session = useSession();
  const navigate = useNavigate();

  const logout = () => {
    clearSession();
    navigate('/login', { replace: true });
  };

  return (
    <div className="app">
      <header className="app__header">
        <Link className="app__brand" to="/tickets">
          Northwind Support
        </Link>

        <nav className="app__nav" aria-label="Main">
          <NavLink to="/tickets">Tickets</NavLink>
          <NavLink to="/customers">Customers</NavLink>
        </nav>

        <div className="app__session">
          <span>{session?.agent.fullName}</span>
          <button type="button" className="button" onClick={logout}>
            Log out
          </button>
        </div>
      </header>

      <main className="app__main">
        <Outlet />
      </main>
    </div>
  );
}

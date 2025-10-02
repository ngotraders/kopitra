import { Link } from 'react-router-dom';
import './NotFound.css';

export interface NotFoundProps {
  message?: string;
}

export function NotFound({ message = 'The requested view could not be located.' }: NotFoundProps) {
  return (
    <section className="not-found" aria-label="Not found">
      <h1>Page not found</h1>
      <p>{message}</p>
      <Link to="/dashboard/activity">Return to dashboard</Link>
    </section>
  );
}

export default NotFound;

import './Header.css';

export interface HeaderProps {
  environment: 'Production' | 'Sandbox';
  userName: string;
  onSignOut?: () => void;
}

export function Header({ environment, userName, onSignOut }: HeaderProps) {
  return (
    <div className="header">
      <div>
        <span className="header__title">TradeAgentEA Console</span>
        <span className={`header__badge header__badge--${environment.toLowerCase()}`}>
          {environment}
        </span>
      </div>
      <div className="header__actions">
        <span className="header__user" aria-label="signed in user">
          {userName}
        </span>
        <button type="button" className="header__button" onClick={onSignOut}>
          Sign out
        </button>
      </div>
    </div>
  );
}

export default Header;

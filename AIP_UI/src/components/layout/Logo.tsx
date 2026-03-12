import { Link } from 'react-router-dom';

interface LogoProps {
  onClick?: () => void;
  className?: string;
}

export const Logo = ({ onClick, className = '' }: LogoProps) => {
  return (
    <Link to="/" className={`flex items-center gap-2 ${className}`} onClick={onClick}>
      <img src="/favicon.ico" alt="Crime Portal Logo" className="h-6 w-6" />
      <span className="font-semibold text-lg">Crime Portal</span>
    </Link>
  );
}; 
import { Link } from "react-router-dom";
import { LOGO_SIZES } from "@/constants/header";

interface LogoProps {
  variant: keyof typeof LOGO_SIZES;
  className?: string;
  containerClassName?: string;
}

export const Logo = ({ variant, className = "", containerClassName = "" }: LogoProps) => (
  <Link to="/" className="flex items-center">
    <div className={`relative flex justify-center ${containerClassName}`}>
      <img 
        src="/HOEnbg.png" 
        alt="Heart of England Co-operative"
        className={`${LOGO_SIZES[variant]} w-auto ${className}`}
      />
    </div>
  </Link>
); 
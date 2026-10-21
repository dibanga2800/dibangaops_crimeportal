import { Link } from "react-router-dom";
import { LOGO_SIZES } from "@/constants/header";

interface LogoProps {
  variant: keyof typeof LOGO_SIZES;
  className?: string;
  containerClassName?: string;
}

export const Logo = ({ variant, className = "", containerClassName = "" }: LogoProps) => (
  <Link to="/" className="flex shrink-0 items-center rounded-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring">
    <div className={`relative flex justify-center ${containerClassName}`}>
      <img 
        src="/HOEnbg.png" 
        alt="Heart of England Co-operative"
        className={`${LOGO_SIZES[variant]} w-auto dark:rounded dark:bg-white dark:px-1.5 dark:py-0.5 ${className}`}
      />
    </div>
  </Link>
); 
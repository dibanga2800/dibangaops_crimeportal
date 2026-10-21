import { Search } from "lucide-react";
import { Input } from "@/components/ui/input";
import { COMMON_CLASSES } from "@/constants/header";

interface SearchInputProps {
  autoFocus?: boolean;
  className?: string;
}

export const SearchInput = ({ autoFocus = false, className = "" }: SearchInputProps) => (
  <div className={`relative w-full ${className}`}>
    <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
    <Input 
      type="search" 
      placeholder="Search incidents, stores, people…"
      aria-label="Search" 
      className={COMMON_CLASSES.searchInput}
      autoFocus={autoFocus}
    />
  </div>
); 
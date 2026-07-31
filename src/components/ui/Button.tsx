import type { ButtonHTMLAttributes, ReactNode } from "react";

type Variant = "primary" | "secondary" | "danger" | "ghost";

const variantClasses: Record<Variant, string> = {
  primary: "bg-room-accent text-room-bg hover:bg-emerald-400 focus-visible:outline-room-accent",
  secondary: "bg-room-panelAlt text-room-text border border-room-border hover:border-room-accent/60",
  danger: "bg-red-600 text-white hover:bg-red-500 focus-visible:outline-red-500",
  ghost: "bg-transparent text-room-text hover:bg-room-panelAlt border border-transparent hover:border-room-border",
};

type Props = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: Variant;
  children: ReactNode;
};

export function Button({ variant = "primary", className = "", children, ...rest }: Props) {
  return (
    <button
      className={`inline-flex items-center justify-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:opacity-50 ${variantClasses[variant]} ${className}`}
      {...rest}
    >
      {children}
    </button>
  );
}

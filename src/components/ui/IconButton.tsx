import type { ButtonHTMLAttributes, ReactNode } from "react";

type Props = ButtonHTMLAttributes<HTMLButtonElement> & {
  active?: boolean;
  danger?: boolean;
  label: string;
  icon: ReactNode;
};

export function IconButton({ active = true, danger = false, label, icon, className = "", ...rest }: Props) {
  const tone = danger
    ? "bg-red-600 hover:bg-red-500 text-white"
    : active
      ? "bg-room-panelAlt hover:bg-room-border text-room-text"
      : "bg-red-500/15 hover:bg-red-500/25 text-red-400";
  return (
    <button
      title={label}
      aria-label={label}
      className={`flex h-12 w-12 items-center justify-center rounded-full border border-room-border transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-room-accent ${tone} ${className}`}
      {...rest}
    >
      {icon}
    </button>
  );
}

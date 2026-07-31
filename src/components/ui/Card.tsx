import type { HTMLAttributes, ReactNode } from "react";

type Props = HTMLAttributes<HTMLDivElement> & { children: ReactNode };

export function Card({ className = "", children, ...rest }: Props) {
  return (
    <div
      className={`rounded-xl border border-room-border bg-room-panel p-5 shadow-sm ${className}`}
      {...rest}
    >
      {children}
    </div>
  );
}

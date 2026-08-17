import { Spinner } from "@/components/ui/spinner";
import { cn } from "@/lib/utils";

type Props = {
  label?: string;
  className?: string;
};

/** Consistent full-block loading state - used anywhere a page/section is waiting on data. */
export function LoadingBlock({ label = "กำลังโหลด...", className }: Props) {
  return (
    <div className={cn("flex items-center justify-center gap-2 py-10 text-sm text-muted-foreground", className)}>
      <Spinner />
      <span>{label}</span>
    </div>
  );
}

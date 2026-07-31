import { Spinner } from "@/components/ui/Spinner";

type Props = {
  label?: string;
  className?: string;
};

/** Consistent full-block loading state - used anywhere a page/section is waiting on data. */
export function LoadingBlock({ label = "กำลังโหลด...", className = "" }: Props) {
  return (
    <div className={`flex items-center justify-center gap-2 py-10 text-sm text-room-muted ${className}`}>
      <Spinner className="h-4 w-4" />
      <span>{label}</span>
    </div>
  );
}

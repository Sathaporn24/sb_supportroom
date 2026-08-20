import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { cn } from "@/lib/utils";

type Props = {
  columns: number;
  rows?: number;
  className?: string;
};

const DEFAULT_ROWS = 5;

/**
 * Table-shaped loading state for pages waiting on a list/table that already knows its own column
 * count - pass the same `columns` as the real table so the skeleton doesn't jump in width once
 * data arrives.
 */
export function TableSkeleton({ columns, rows = DEFAULT_ROWS, className }: Props) {
  return (
    <div className={cn("overflow-hidden rounded-xl border", className)}>
      <Table>
        <TableHeader>
          <TableRow>
            {Array.from({ length: columns }).map((_, index) => (
              <TableHead key={index} className="px-4">
                <Skeleton className="h-3 w-16" />
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {Array.from({ length: rows }).map((_, rowIndex) => (
            <TableRow key={rowIndex}>
              {Array.from({ length: columns }).map((_, colIndex) => (
                <TableCell key={colIndex} className="px-4 py-3">
                  <Skeleton className="h-4 w-full" />
                </TableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

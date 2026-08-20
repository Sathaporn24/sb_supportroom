import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

type Props = {
  count?: number;
};

const DEFAULT_COUNT = 3;

/**
 * Card-shaped loading state for pages that render a vertical list of Cards rather than a table
 * (e.g. flagged items, conflicts) - mirrors the title/body/footer-action shape those cards use.
 */
export function CardListSkeleton({ count = DEFAULT_COUNT }: Props) {
  return (
    <div className="flex flex-col gap-3">
      {Array.from({ length: count }).map((_, index) => (
        <Card key={index} size="sm">
          <CardContent className="flex flex-col gap-3">
            <Skeleton className="h-4 w-2/3" />
            <Skeleton className="h-4 w-full" />
            <div className="flex items-center justify-between">
              <Skeleton className="h-3 w-24" />
              <Skeleton className="h-8 w-20" />
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

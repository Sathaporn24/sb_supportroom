import { CheckCircle2Icon, CircleIcon } from "lucide-react";
import { AdminLink } from "@/components/admin/AdminLink";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyTitle } from "@/components/ui/empty";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

type ChecklistStepProps = {
  done: boolean;
  title: string;
  description: string;
  action?: { href: string; label: string; disabled?: boolean };
};

function ChecklistStep({ done, title, description, action }: ChecklistStepProps) {
  return (
    <div className={cn("flex items-center gap-3 rounded-lg border p-3 text-left", done && "bg-muted/40")}>
      {done ? (
        <CheckCircle2Icon className="size-5 shrink-0 text-primary" />
      ) : (
        <CircleIcon className="size-5 shrink-0 text-muted-foreground" />
      )}
      <div className="flex-1">
        <p className="text-sm font-medium">{title}</p>
        <p className="text-xs text-muted-foreground">{description}</p>
      </div>
      {action &&
        (action.disabled ? (
          <span
            aria-disabled
            className={cn(buttonVariants({ variant: "outline", size: "sm" }), "pointer-events-none opacity-50")}
          >
            {action.label}
          </span>
        ) : (
          <AdminLink href={action.href} className={buttonVariants({ variant: "outline", size: "sm" })}>
            {action.label}
          </AdminLink>
        ))}
    </div>
  );
}

/** Shown instead of an empty training-links table - walks a brand-new company through the three
 * things that have to exist before a link is worth creating. */
export function OnboardingChecklist({ hasLesson }: { hasLesson: boolean }) {
  return (
    <Empty className="border">
      <EmptyHeader>
        <EmptyTitle>เริ่มต้นใช้งาน</EmptyTitle>
        <EmptyDescription>ทำ 3 ขั้นตอนนี้ให้ครบก่อนสร้างลิงก์การเรียนแรก</EmptyDescription>
      </EmptyHeader>
      <EmptyContent className="max-w-md gap-2">
        <ChecklistStep
          done
          title="หมวดความรู้"
          description="ระบบเตรียมหมวดเริ่มต้นไว้ให้อัตโนมัติแล้ว"
        />
        <ChecklistStep
          done={hasLesson}
          title="เพิ่มบทเรียนแรก"
          description="ตั้งค่าบทเรียนจาก Google Slides หรือ PDF"
          action={{ href: "/admin/lessons/new", label: "เพิ่มบทเรียน" }}
        />
        <ChecklistStep
          done={false}
          title="สร้างลิงก์การเรียนแรก"
          description={hasLesson ? "สร้างลิงก์เพื่อส่งให้ผู้เรียน" : "ต้องมีบทเรียนอย่างน้อย 1 บทก่อน"}
          action={{ href: "/admin/links/new", label: "สร้างลิงก์การเรียน", disabled: !hasLesson }}
        />
      </EmptyContent>
    </Empty>
  );
}

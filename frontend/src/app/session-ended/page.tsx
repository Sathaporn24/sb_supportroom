import { Card } from "@/components/ui/Card";

// No link back to /admin here - this page is reached by any teacher on a public, unauthenticated
// link, and /admin has no auth of its own (see CLAUDE.md). Linking to it from a public-facing
// page handed every teacher a one-click path into the full CS dashboard (every session's teacher
// name/school, chat transcripts, and the "Reset Demo Data" button).
export default function SessionEndedPage() {
  return (
    <main className="flex min-h-screen items-center justify-center p-6">
      <Card className="max-w-md text-center">
        <h1 className="text-xl font-semibold text-room-text">ขอบคุณค่ะ</h1>
        <p className="mt-3 text-sm text-room-muted">
          การสอนใช้งานระบบในห้องนี้สิ้นสุดแล้ว หากมีคำถามเพิ่มเติม สามารถติดต่อทีม CS ได้เลยค่ะ
        </p>
      </Card>
    </main>
  );
}

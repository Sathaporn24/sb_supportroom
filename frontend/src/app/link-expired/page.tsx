import { Card } from "@/components/ui/Card";

// No link back to /admin here - same reasoning as session-ended/page.tsx: this is a public,
// unauthenticated page, and /admin has no auth of its own.
export default function LinkExpiredPage() {
  return (
    <main className="flex min-h-screen items-center justify-center p-6">
      <Card className="max-w-md text-center">
        <h1 className="text-xl font-semibold text-room-text">ลิงก์นี้หมดอายุหรือไม่สามารถใช้งานได้</h1>
        <p className="mt-3 text-sm text-room-muted">
          กรุณาติดต่อทีม CS เพื่อขอลิงก์เข้าห้องสอนใหม่อีกครั้งค่ะ
        </p>
      </Card>
    </main>
  );
}

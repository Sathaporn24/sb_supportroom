import Link from "next/link";
import { Card } from "@/components/ui/Card";

export default function LinkExpiredPage() {
  return (
    <main className="flex min-h-screen items-center justify-center p-6">
      <Card className="max-w-md text-center">
        <h1 className="text-xl font-semibold text-room-text">ลิงก์นี้หมดอายุหรือถูกใช้งานแล้ว</h1>
        <p className="mt-3 text-sm text-room-muted">
          กรุณาติดต่อทีม CS เพื่อขอลิงก์เข้าห้องสอนใหม่อีกครั้งค่ะ
        </p>
        <Link
          href="/admin"
          className="mt-6 inline-block rounded-lg bg-room-accent px-4 py-2 text-sm font-medium text-room-bg hover:bg-emerald-400"
        >
          กลับหน้า Admin
        </Link>
      </Card>
    </main>
  );
}

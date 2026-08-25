import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

// No link back to /admin here - same reasoning as session-ended/page.tsx: this is a public,
// unauthenticated page, and /admin has no auth of its own.
export default function LinkExpiredPage() {
  return (
    <main className="flex min-h-[100dvh] items-center justify-center p-6">
      <Card className="max-w-md text-center">
        <CardHeader>
          <CardTitle className="text-xl">ลิงก์นี้หมดอายุหรือไม่สามารถใช้งานได้</CardTitle>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground">
          กรุณาติดต่อทีม CS เพื่อขอลิงก์เข้าห้องสอนใหม่อีกครั้งค่ะ
        </CardContent>
      </Card>
    </main>
  );
}

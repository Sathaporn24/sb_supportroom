import Link from "next/link";
import { DocumentUploadList } from "@/components/admin/DocumentUploadList";

export default function DocumentsLibraryPage() {
  return (
    <main className="mx-auto max-w-3xl space-y-6 p-6">
      <div>
        <Link href="/admin" className="text-xs text-room-muted hover:text-room-text">
          ← กลับหน้า Admin
        </Link>
        <h1 className="mt-1 text-xl font-semibold text-room-text">คลังเอกสาร (ใช้ได้ทุกบทเรียน)</h1>
        <p className="mt-1 text-sm text-room-muted">
          เอกสารในหน้านี้จะถูกใช้อ้างอิงตอบคำถามได้ในทุกบทเรียน — เหมาะกับเอกสารกลางที่ลูกค้ามีอยู่แล้ว เช่น
          ราคาสินค้า ตารางเรียน หรือคำถามที่พบบ่อย ไม่ต้องผูกกับบทเรียนใดบทเรียนหนึ่ง
        </p>
      </div>

      <DocumentUploadList />
    </main>
  );
}

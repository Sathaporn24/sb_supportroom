import { AdminLink } from "@/components/admin/AdminLink";
import { DocumentUploadList } from "@/components/admin/DocumentUploadList";

export default function DocumentsLibraryPage() {
  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
      <div>
        <AdminLink href="/admin" className="text-xs text-muted-foreground hover:text-foreground">
          ← กลับหน้า Admin
        </AdminLink>
        <h1 className="mt-1 text-xl font-semibold">คลังเอกสาร (ใช้ได้ทุกบทเรียน)</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          เอกสารในหน้านี้จะถูกใช้อ้างอิงตอบคำถามได้ในทุกบทเรียน — เหมาะกับเอกสารกลางที่ลูกค้ามีอยู่แล้ว เช่น
          ราคาสินค้า ตารางเรียน หรือคำถามที่พบบ่อย ไม่ต้องผูกกับบทเรียนใดบทเรียนหนึ่ง
        </p>
      </div>

      <DocumentUploadList />
    </main>
  );
}

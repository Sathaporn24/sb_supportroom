import { AdminLink } from "@/components/admin/AdminLink";
import { DeletedDocumentsList } from "@/components/admin/DeletedDocumentsList";
import { DocumentUploadList } from "@/components/admin/DocumentUploadList";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

export default function DocumentsLibraryPage() {
  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-6 p-6">
      <div>
        <AdminLink href="/admin" className="text-xs text-muted-foreground hover:text-foreground">
          ← กลับหน้า Admin
        </AdminLink>
        <h1 className="mt-1 text-xl font-semibold">คลังเอกสาร</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          อัปโหลดเอกสารให้ตอบได้ทั้งบริษัท หรือจำกัดไว้เฉพาะหมวดใดหมวดหนึ่งก็ได้ — เอกสารที่ผูกกับบทเรียนใดบทเรียน
          หนึ่งโดยเฉพาะให้อัปโหลดที่หน้าแก้ไขบทเรียนนั้นแทน
        </p>
      </div>

      <Tabs defaultValue="active">
        <TabsList>
          <TabsTrigger value="active">เอกสารทั้งหมด</TabsTrigger>
          <TabsTrigger value="deleted">กู้คืนเอกสารที่ถูกลบ</TabsTrigger>
        </TabsList>
        <TabsContent value="active">
          <DocumentUploadList />
        </TabsContent>
        <TabsContent value="deleted">
          <DeletedDocumentsList />
        </TabsContent>
      </Tabs>
    </main>
  );
}

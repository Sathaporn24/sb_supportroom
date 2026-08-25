import { DeletedDocumentsList } from "@/components/admin/DeletedDocumentsList";
import { DocumentUploadList } from "@/components/admin/DocumentUploadList";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

export default function DocumentsLibraryPage() {
  return (
    <main className="flex w-full flex-col gap-6 p-6">
      <div>
        <h1 className="text-xl font-semibold text-primary">คลังเอกสาร</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          อัปโหลดเอกสารให้ตอบได้ทั้งบริษัท หรือจำกัดไว้เฉพาะหมวดใดหมวดหนึ่งก็ได้
        </p>
      </div>

      <Tabs defaultValue="active">
        <TabsList>
          <TabsTrigger value="active" data-testid="documents-active-tab">
            เอกสารทั้งหมด
          </TabsTrigger>
          <TabsTrigger value="deleted" data-testid="documents-deleted-tab">
            กู้คืนเอกสารที่ถูกลบ
          </TabsTrigger>
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

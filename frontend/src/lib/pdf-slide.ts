// NR-4/EX-3 - `slideObjectId` ("pdf-page-N", N is 1-based) is the only thing that reliably maps
// a slide to its real file page number once pages can be excluded (R4.7). `index`/`lessonIndex`
// renumber the *visible* pages and must never be used to derive the image page number - see
// design.md EX-3's "ห้ามคำนวณเลขหน้าของไฟล์จาก Index/ตำแหน่งใน array".
const PDF_PAGE_OBJECT_ID_PREFIX = "pdf-page-";

/** Parses the real 1-based file page number out of a `pdf-page-N` slideObjectId. */
export function getPdfFilePageNumber(slideObjectId: string): number {
  return Number(slideObjectId.slice(PDF_PAGE_OBJECT_ID_PREFIX.length));
}

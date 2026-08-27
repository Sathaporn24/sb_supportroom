-- LT-24 preflight for MG-L1 (AddLessonTrashLifecycle)
--
-- Must run (and its result recorded) BEFORE generating/applying MG-L1 in any environment,
-- including a future production apply. LT-24: today there is no delete endpoint and
-- LessonConfig.IsDelete's setter is init-only, so this is expected to return 0 rows on a
-- database that has never run Module L's archive flow. If it does not, STOP - do not apply
-- the migration, and do not guess that these rows are trash and backfill a purge job for them.
-- Inspect every returned row by hand instead.
--
-- Run: docker exec <postgres-container> psql -U <user> -d <db> -f preflight-lt24-lesson-trash.sql
-- or:  psql "$POSTGRES_CONNECTION_STRING" -f preflight-lt24-lesson-trash.sql

SELECT
    "Id",
    "CompanyId",
    "Slug",
    "Title",
    "IsDelete",
    "DeletedAt",
    "DeleteBy"
FROM "LessonConfig"
WHERE "IsDelete" = TRUE;

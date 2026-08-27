CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804185633_InitialCreate') THEN
    CREATE TABLE "LessonConfig" (
        "Id" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "Slug" text NOT NULL,
        "Title" text NOT NULL,
        "Description" text,
        "SlidesSourceUrl" text NOT NULL,
        "PresentationId" text,
        "SlidesEmbedUrl" text,
        "IntroWaitMs" integer NOT NULL,
        "BreathPauseMs" integer NOT NULL,
        "FinalQuestionWaitMs" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "SlideConfigs" jsonb,
        CONSTRAINT "PK_LessonConfig" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804185633_InitialCreate') THEN
    CREATE TABLE "SessionQuestion" (
        "Id" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "SessionId" text NOT NULL,
        "SlideObjectId" text,
        "Transcript" text,
        "Answer" text,
        "AnswerStatus" text NOT NULL,
        CONSTRAINT "PK_SessionQuestion" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804185633_InitialCreate') THEN
    CREATE TABLE "TrainingSession" (
        "Id" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "Token" text NOT NULL,
        "LessonId" text NOT NULL,
        "LessonSlug" text NOT NULL,
        "TeacherName" text,
        "SchoolName" text,
        "Status" text NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "StartedAt" timestamp with time zone,
        "EndedAt" timestamp with time zone,
        "CompletedAllSlides" boolean NOT NULL,
        "LastSlideObjectId" text,
        CONSTRAINT "PK_TrainingSession" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804185633_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_LessonConfig_Slug" ON "LessonConfig" ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804185633_InitialCreate') THEN
    CREATE INDEX "IX_SessionQuestion_SessionId" ON "SessionQuestion" ("SessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804185633_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_TrainingSession_Token" ON "TrainingSession" ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260804185633_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260804185633_InitialCreate', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260806150540_AddSessionSummary') THEN
    CREATE TABLE "SessionSummary" (
        "Id" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "SessionId" text NOT NULL,
        "CompletedAllSlides" boolean NOT NULL,
        "LastSlideObjectId" text,
        "UnansweredPoints" text[] NOT NULL,
        CONSTRAINT "PK_SessionSummary" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260806150540_AddSessionSummary') THEN
    CREATE UNIQUE INDEX "IX_SessionSummary_SessionId" ON "SessionSummary" ("SessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260806150540_AddSessionSummary') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260806150540_AddSessionSummary', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260806162114_AddChatMessage') THEN
    CREATE TABLE "ChatMessage" (
        "Id" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "SessionId" text NOT NULL,
        "SenderRole" text NOT NULL,
        "SenderName" text,
        "Text" text NOT NULL,
        CONSTRAINT "PK_ChatMessage" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260806162114_AddChatMessage') THEN
    CREATE INDEX "IX_ChatMessage_SessionId" ON "ChatMessage" ("SessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260806162114_AddChatMessage') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260806162114_AddChatMessage', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260807030953_AddDocumentResource') THEN
    CREATE TABLE "DocumentResource" (
        "Id" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "LessonId" text,
        "FileName" text NOT NULL,
        "ContentType" text NOT NULL,
        "SizeBytes" bigint NOT NULL,
        "ObsBucket" text NOT NULL,
        "ObsKey" text NOT NULL,
        "IndexingStatus" text NOT NULL,
        "IndexedChunkCount" integer NOT NULL,
        CONSTRAINT "PK_DocumentResource" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260807030953_AddDocumentResource') THEN
    CREATE INDEX "IX_DocumentResource_LessonId" ON "DocumentResource" ("LessonId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260807030953_AddDocumentResource') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260807030953_AddDocumentResource', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260807205425_AddLessonPdfSource') THEN
    ALTER TABLE "LessonConfig" ADD "ContentSourceType" text NOT NULL DEFAULT 'google_slides';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260807205425_AddLessonPdfSource') THEN
    ALTER TABLE "LessonConfig" ADD "PdfDocumentResourceId" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260807205425_AddLessonPdfSource') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260807205425_AddLessonPdfSource', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    DROP INDEX "IX_LessonConfig_Slug";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    ALTER TABLE "TrainingSession" RENAME COLUMN "TeacherName" TO "RecipientName";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    ALTER TABLE "TrainingSession" RENAME COLUMN "SchoolName" TO "RecipientOrgName";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    ALTER TABLE "TrainingSession" ADD "CompanyId" text NOT NULL DEFAULT 'default';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    ALTER TABLE "SessionSummary" ADD "CompanyId" text NOT NULL DEFAULT 'default';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    ALTER TABLE "SessionQuestion" ADD "CompanyId" text NOT NULL DEFAULT 'default';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    ALTER TABLE "LessonConfig" ADD "CompanyId" text NOT NULL DEFAULT 'default';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    ALTER TABLE "DocumentResource" ADD "CompanyId" text NOT NULL DEFAULT 'default';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    ALTER TABLE "ChatMessage" ADD "CompanyId" text NOT NULL DEFAULT 'default';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    CREATE INDEX "IX_TrainingSession_CompanyId" ON "TrainingSession" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    CREATE INDEX "IX_SessionSummary_CompanyId" ON "SessionSummary" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    CREATE INDEX "IX_SessionQuestion_CompanyId" ON "SessionQuestion" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    CREATE UNIQUE INDEX "IX_LessonConfig_CompanyId_Slug" ON "LessonConfig" ("CompanyId", "Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    CREATE INDEX "IX_DocumentResource_CompanyId" ON "DocumentResource" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    CREATE INDEX "IX_ChatMessage_CompanyId" ON "ChatMessage" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811045733_AddCompanyId') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260811045733_AddCompanyId', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811064348_RenameChatSenderRoles') THEN
    UPDATE "ChatMessage" SET "SenderRole" = 'recipient' WHERE "SenderRole" = 'teacher';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811064348_RenameChatSenderRoles') THEN
    UPDATE "ChatMessage" SET "SenderRole" = 'agent' WHERE "SenderRole" = 'cs';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260811064348_RenameChatSenderRoles') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260811064348_RenameChatSenderRoles', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    CREATE TABLE "Company" (
        "Id" text NOT NULL,
        "Name" text NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        CONSTRAINT "PK_Company" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    CREATE INDEX "IX_Company_IsActive" ON "Company" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    INSERT INTO "Company" ("Id", "Name", "IsActive", "CreateDate", "IsDelete")
    SELECT DISTINCT c, c, true, now(), false
    FROM (
        SELECT "CompanyId" AS c FROM "TrainingSession"
        UNION SELECT "CompanyId" FROM "LessonConfig"
        UNION SELECT "CompanyId" FROM "SessionQuestion"
        UNION SELECT "CompanyId" FROM "ChatMessage"
        UNION SELECT "CompanyId" FROM "DocumentResource"
    ) AS used
    WHERE c IS NOT NULL AND c <> '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    CREATE TABLE "AdminUser" (
        "Id" text NOT NULL,
        "CompanyId" text,
        "Role" text NOT NULL,
        "Email" text NOT NULL,
        "PasswordHash" text,
        "DisplayName" text NOT NULL,
        "IsActive" boolean NOT NULL,
        "LastLoginAt" timestamp with time zone,
        "MustChangePassword" boolean NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        CONSTRAINT "PK_AdminUser" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    CREATE INDEX "IX_AdminUser_CompanyId" ON "AdminUser" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    CREATE UNIQUE INDEX "IX_AdminUser_Email" ON "AdminUser" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    CREATE TABLE "LearningSession" (
        "Id" text NOT NULL,
        "CompanyId" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "TrainingLinkId" text NOT NULL,
        "LearnerKey" text NOT NULL,
        "RecipientName" text NOT NULL,
        "Status" text NOT NULL,
        "StartedAt" timestamp with time zone NOT NULL,
        "EndedAt" timestamp with time zone,
        "LastActivityAt" timestamp with time zone NOT NULL,
        "LastSlideObjectId" text,
        "LastSlideIndex" integer NOT NULL,
        "CompletedAllSlides" boolean NOT NULL,
        CONSTRAINT "PK_LearningSession" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    CREATE INDEX "IX_LearningSession_CompanyId" ON "LearningSession" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    CREATE INDEX "IX_LearningSession_TrainingLinkId_LearnerKey" ON "LearningSession" ("TrainingLinkId", "LearnerKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    INSERT INTO "LearningSession" (
        "Id", "CompanyId", "CreateBy", "CreateDate", "UpdateBy", "UpdateDate",
        "DeleteBy", "IsDelete", "DeletedAt",
        "TrainingLinkId", "LearnerKey", "RecipientName", "Status",
        "StartedAt", "EndedAt", "LastActivityAt",
        "LastSlideObjectId", "LastSlideIndex", "CompletedAllSlides")
    SELECT
        'learning_' || ts."Id",
        ts."CompanyId", ts."CreateBy", ts."CreateDate", ts."UpdateBy", ts."UpdateDate",
        ts."DeleteBy", ts."IsDelete", ts."DeletedAt",
        ts."Id",
        'legacy-' || ts."Id",
        COALESCE(NULLIF(ts."RecipientName", ''), 'ผู้เรียน'),
        CASE WHEN ts."Status" = 'ENDED' THEN 'ENDED' ELSE 'IN_PROGRESS' END,
        COALESCE(ts."StartedAt", ts."CreateDate"),
        ts."EndedAt",
        COALESCE(ts."EndedAt", ts."StartedAt", ts."CreateDate"),
        ts."LastSlideObjectId",
        0,
        ts."CompletedAllSlides"
    FROM "TrainingSession" ts
    WHERE ts."StartedAt" IS NOT NULL
       OR EXISTS (SELECT 1 FROM "SessionQuestion" q WHERE q."SessionId" = ts."Id")
       OR EXISTS (SELECT 1 FROM "ChatMessage" m WHERE m."SessionId" = ts."Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    UPDATE "SessionQuestion" q
    SET "SessionId" = 'learning_' || q."SessionId"
    WHERE EXISTS (
        SELECT 1 FROM "LearningSession" ls
        WHERE ls."Id" = 'learning_' || q."SessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    UPDATE "ChatMessage" m
    SET "SessionId" = 'learning_' || m."SessionId"
    WHERE EXISTS (
        SELECT 1 FROM "LearningSession" ls
        WHERE ls."Id" = 'learning_' || m."SessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    DROP INDEX "IX_TrainingSession_Token";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    DROP INDEX "IX_TrainingSession_CompanyId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    ALTER TABLE "TrainingSession" RENAME TO "TrainingLink";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    ALTER TABLE "TrainingLink" RENAME CONSTRAINT "PK_TrainingSession" TO "PK_TrainingLink";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    ALTER TABLE "TrainingLink" DROP COLUMN "RecipientName";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    ALTER TABLE "TrainingLink" DROP COLUMN "Status";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    ALTER TABLE "TrainingLink" DROP COLUMN "StartedAt";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    ALTER TABLE "TrainingLink" DROP COLUMN "EndedAt";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    ALTER TABLE "TrainingLink" DROP COLUMN "CompletedAllSlides";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    ALTER TABLE "TrainingLink" DROP COLUMN "LastSlideObjectId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    ALTER TABLE "TrainingLink" ADD "MaxAttendees" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    CREATE UNIQUE INDEX "IX_TrainingLink_Token" ON "TrainingLink" ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    CREATE INDEX "IX_TrainingLink_CompanyId" ON "TrainingLink" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    ALTER TABLE "SessionQuestion" ADD "ReviewResult" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    ALTER TABLE "SessionQuestion" ADD "ReviewNote" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    ALTER TABLE "SessionQuestion" ADD "ReviewedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    DROP TABLE "SessionSummary";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260813140603_SplitLinkAndAddAuth') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260813140603_SplitLinkAndAddAuth', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818155126_AddTotalSlideCount') THEN
    ALTER TABLE "LearningSession" ALTER COLUMN "LastSlideIndex" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818155126_AddTotalSlideCount') THEN
    ALTER TABLE "LearningSession" ADD "TotalSlideCount" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818155126_AddTotalSlideCount') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260818155126_AddTotalSlideCount', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    CREATE TABLE "KnowledgeCategory" (
        "Id" text NOT NULL,
        "CompanyId" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "ParentId" text,
        "Level" integer NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "SortOrder" integer NOT NULL,
        "IsSystemDefault" boolean NOT NULL,
        CONSTRAINT "PK_KnowledgeCategory" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    ALTER TABLE "LessonConfig" ADD "CategoryId" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    ALTER TABLE "DocumentResource" ADD "ScopeType" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    ALTER TABLE "DocumentResource" ADD "ScopeId" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    ALTER TABLE "DocumentResource" ADD "FailureReason" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    INSERT INTO "KnowledgeCategory" ("Id", "CompanyId", "CreateDate", "IsDelete", "ParentId", "Level", "Name", "SortOrder", "IsSystemDefault")
    SELECT 'kbcat-backfill-parent-' || md5("CompanyId"), "CompanyId", now(), false, null, 1, 'ยังไม่จัดหมวด', 9999, true FROM (SELECT "CompanyId" FROM "LessonConfig" UNION SELECT "CompanyId" FROM "DocumentResource") companies ON CONFLICT ("Id") DO NOTHING;
    INSERT INTO "KnowledgeCategory" ("Id", "CompanyId", "CreateDate", "IsDelete", "ParentId", "Level", "Name", "SortOrder", "IsSystemDefault")
    SELECT 'kbcat-backfill-child-' || md5("CompanyId"), "CompanyId", now(), false, 'kbcat-backfill-parent-' || md5("CompanyId"), 2, 'ยังไม่จัดหมวด', 9999, true FROM (SELECT "CompanyId" FROM "LessonConfig" UNION SELECT "CompanyId" FROM "DocumentResource") companies ON CONFLICT ("Id") DO NOTHING;
    UPDATE "LessonConfig" SET "CategoryId" = 'kbcat-backfill-child-' || md5("CompanyId") WHERE "CategoryId" IS NULL;
    UPDATE "DocumentResource" SET "ScopeType" = CASE WHEN "LessonId" IS NULL THEN 'company' ELSE 'lesson' END, "ScopeId" = "LessonId" WHERE "ScopeType" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    ALTER TABLE "LessonConfig" ALTER COLUMN "CategoryId" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    ALTER TABLE "DocumentResource" ALTER COLUMN "ScopeType" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    DROP INDEX "IX_DocumentResource_CompanyId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    DROP INDEX "IX_DocumentResource_LessonId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    ALTER TABLE "DocumentResource" DROP COLUMN "LessonId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    CREATE INDEX "IX_LessonConfig_CategoryId" ON "LessonConfig" ("CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    CREATE INDEX "IX_DocumentResource_CompanyId_ScopeType_ScopeId" ON "DocumentResource" ("CompanyId", "ScopeType", "ScopeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    CREATE INDEX "IX_KnowledgeCategory_CompanyId" ON "KnowledgeCategory" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    CREATE INDEX "IX_KnowledgeCategory_CompanyId_ParentId_SortOrder" ON "KnowledgeCategory" ("CompanyId", "ParentId", "SortOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819082956_AddKnowledgeTaxonomyAndScope') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260819082956_AddKnowledgeTaxonomyAndScope', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819122301_AddDurableIndexingJobs') THEN
    CREATE TABLE "BackgroundJob" (
        "Id" text NOT NULL,
        "CompanyId" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "JobType" text NOT NULL,
        "TargetId" text NOT NULL,
        "PayloadJson" text,
        "Status" text NOT NULL,
        "AttemptCount" integer NOT NULL,
        "NextAttemptAt" timestamp with time zone NOT NULL,
        "StartedAt" timestamp with time zone,
        "FinishedAt" timestamp with time zone,
        "LastErrorCode" text,
        "LastErrorDetail" text,
        CONSTRAINT "PK_BackgroundJob" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819122301_AddDurableIndexingJobs') THEN
    CREATE INDEX "IX_BackgroundJob_CompanyId_JobType_TargetId" ON "BackgroundJob" ("CompanyId", "JobType", "TargetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819122301_AddDurableIndexingJobs') THEN
    CREATE INDEX "IX_BackgroundJob_Status_NextAttemptAt" ON "BackgroundJob" ("Status", "NextAttemptAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819122301_AddDurableIndexingJobs') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260819122301_AddDurableIndexingJobs', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819124738_AddDocumentChunks') THEN
    CREATE TABLE "DocumentChunk" (
        "Id" text NOT NULL,
        "CompanyId" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "DocumentId" text NOT NULL,
        "ChunkKey" text NOT NULL,
        "VectorId" text NOT NULL,
        "NamespaceKey" text NOT NULL,
        "SeqNo" integer NOT NULL,
        "Text" text NOT NULL,
        "CharCount" integer NOT NULL,
        "HasSuspectCharacters" boolean NOT NULL,
        CONSTRAINT "PK_DocumentChunk" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819124738_AddDocumentChunks') THEN
    CREATE INDEX "IX_DocumentChunk_CompanyId" ON "DocumentChunk" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819124738_AddDocumentChunks') THEN
    CREATE INDEX "IX_DocumentChunk_DocumentId_SeqNo" ON "DocumentChunk" ("DocumentId", "SeqNo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819124738_AddDocumentChunks') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260819124738_AddDocumentChunks', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819130857_AddLessonSlideNarrations') THEN
    CREATE TABLE "LessonSlideNarration" (
        "Id" text NOT NULL,
        "CompanyId" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "LessonId" text NOT NULL,
        "SlideObjectId" text NOT NULL,
        "NarrationText" text NOT NULL,
        CONSTRAINT "PK_LessonSlideNarration" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819130857_AddLessonSlideNarrations') THEN
    CREATE INDEX "IX_LessonSlideNarration_CompanyId" ON "LessonSlideNarration" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819130857_AddLessonSlideNarrations') THEN
    CREATE INDEX "IX_LessonSlideNarration_LessonId_SlideObjectId" ON "LessonSlideNarration" ("LessonId", "SlideObjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819130857_AddLessonSlideNarrations') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260819130857_AddLessonSlideNarrations', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819134222_AddKnowledgeQnA') THEN
    CREATE TABLE "KnowledgeQnA" (
        "Id" text NOT NULL,
        "CompanyId" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "Question" text NOT NULL,
        "Answer" text NOT NULL,
        "ScopeType" text NOT NULL,
        "ScopeId" text,
        "VectorId" text NOT NULL,
        "IndexedNamespaceKey" text,
        "IndexingStatus" text NOT NULL,
        "FailureReason" text,
        CONSTRAINT "PK_KnowledgeQnA" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819134222_AddKnowledgeQnA') THEN
    CREATE TABLE "KnowledgeQnAConflict" (
        "Id" text NOT NULL,
        "CompanyId" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "QnAId" text NOT NULL,
        "SessionQuestionId" text,
        "ConflictingSourceLabel" text NOT NULL,
        "ModelNote" text,
        "ResolvedAt" timestamp with time zone,
        "ResolvedBy" text,
        CONSTRAINT "PK_KnowledgeQnAConflict" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819134222_AddKnowledgeQnA') THEN
    CREATE TABLE "KnowledgeQnASource" (
        "Id" text NOT NULL,
        "CompanyId" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "QnAId" text NOT NULL,
        "SessionQuestionId" text NOT NULL,
        CONSTRAINT "PK_KnowledgeQnASource" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819134222_AddKnowledgeQnA') THEN
    CREATE INDEX "IX_SessionQuestion_CompanyId_AnswerStatus" ON "SessionQuestion" ("CompanyId", "AnswerStatus");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819134222_AddKnowledgeQnA') THEN
    CREATE INDEX "IX_SessionQuestion_CompanyId_ReviewResult" ON "SessionQuestion" ("CompanyId", "ReviewResult");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819134222_AddKnowledgeQnA') THEN
    CREATE INDEX "IX_KnowledgeQnA_CompanyId_ScopeType_ScopeId" ON "KnowledgeQnA" ("CompanyId", "ScopeType", "ScopeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819134222_AddKnowledgeQnA') THEN
    CREATE INDEX "IX_KnowledgeQnAConflict_CompanyId_ResolvedAt" ON "KnowledgeQnAConflict" ("CompanyId", "ResolvedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819134222_AddKnowledgeQnA') THEN
    CREATE INDEX "IX_KnowledgeQnAConflict_QnAId" ON "KnowledgeQnAConflict" ("QnAId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819134222_AddKnowledgeQnA') THEN
    CREATE INDEX "IX_KnowledgeQnASource_CompanyId_SessionQuestionId" ON "KnowledgeQnASource" ("CompanyId", "SessionQuestionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819134222_AddKnowledgeQnA') THEN
    CREATE INDEX "IX_KnowledgeQnASource_QnAId" ON "KnowledgeQnASource" ("QnAId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260819134222_AddKnowledgeQnA') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260819134222_AddKnowledgeQnA', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821054948_BackfillMissingDefaultCategoryChain') THEN
    INSERT INTO "KnowledgeCategory" (
        "Id", "CompanyId", "CreateBy", "CreateDate", "UpdateBy", "UpdateDate",
        "DeleteBy", "IsDelete", "DeletedAt", "ParentId", "Level", "Name",
        "Description", "SortOrder", "IsSystemDefault")
    SELECT
        'kbcat-company-admin-parent-' || md5(company."Id"),
        company."Id",
        NULL,
        now(),
        NULL,
        NULL,
        NULL,
        false,
        NULL,
        NULL,
        1,
        'ยังไม่จัดหมวด',
        NULL,
        9999,
        true
    FROM "Company" company
    WHERE (
        SELECT COUNT(*)
        FROM "KnowledgeCategory" leaf
        WHERE leaf."CompanyId" = company."Id"
          AND leaf."IsSystemDefault" = true
          AND leaf."Level" = 2
    ) <= 1
      AND NOT EXISTS (
        SELECT 1
        FROM "KnowledgeCategory" parent
        WHERE parent."CompanyId" = company."Id"
          AND parent."IsSystemDefault" = true
          AND parent."Level" = 1
    );

    UPDATE "KnowledgeCategory" leaf
    SET "ParentId" = 'kbcat-company-admin-parent-' || md5(leaf."CompanyId")
    WHERE leaf."IsSystemDefault" = true
      AND leaf."Level" = 2
      AND (
        SELECT COUNT(*)
        FROM "KnowledgeCategory" sibling
        WHERE sibling."CompanyId" = leaf."CompanyId"
          AND sibling."IsSystemDefault" = true
          AND sibling."Level" = 2
      ) = 1
      AND EXISTS (
        SELECT 1
        FROM "KnowledgeCategory" parent
        WHERE parent."Id" = 'kbcat-company-admin-parent-' || md5(leaf."CompanyId")
          AND parent."CompanyId" = leaf."CompanyId"
          AND parent."IsSystemDefault" = true
          AND parent."Level" = 1
    );

    INSERT INTO "KnowledgeCategory" (
        "Id", "CompanyId", "CreateBy", "CreateDate", "UpdateBy", "UpdateDate",
        "DeleteBy", "IsDelete", "DeletedAt", "ParentId", "Level", "Name",
        "Description", "SortOrder", "IsSystemDefault")
    SELECT
        'kbcat-company-admin-leaf-' || md5(company."Id"),
        company."Id",
        NULL,
        parent."CreateDate",
        NULL,
        NULL,
        NULL,
        false,
        NULL,
        parent."Id",
        2,
        'ยังไม่จัดหมวด',
        NULL,
        9999,
        true
    FROM "Company" company
    JOIN LATERAL (
        SELECT candidate."Id", candidate."CreateDate"
        FROM "KnowledgeCategory" candidate
        WHERE candidate."CompanyId" = company."Id"
          AND candidate."IsSystemDefault" = true
          AND candidate."Level" = 1
        ORDER BY candidate."Id"
        LIMIT 1
    ) parent ON true
    WHERE NOT EXISTS (
        SELECT 1
        FROM "KnowledgeCategory" leaf
        WHERE leaf."CompanyId" = company."Id"
          AND leaf."IsSystemDefault" = true
          AND leaf."Level" = 2
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821054948_BackfillMissingDefaultCategoryChain') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260821054948_BackfillMissingDefaultCategoryChain', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821142529_CorrectDefaultCategoryChainLeafCreateDate') THEN
    UPDATE "KnowledgeCategory" leaf
    SET "CreateDate" = now()
    FROM "KnowledgeCategory" parent
    WHERE leaf."Id" = 'kbcat-company-admin-leaf-' || md5(leaf."CompanyId")
      AND leaf."CompanyId" = parent."CompanyId"
      AND leaf."ParentId" = parent."Id"
      AND leaf."IsSystemDefault" = true
      AND leaf."Level" = 2
      AND parent."IsSystemDefault" = true
      AND parent."Level" = 1
      AND parent."Id" <> 'kbcat-company-admin-parent-' || md5(parent."CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260821142529_CorrectDefaultCategoryChainLeafCreateDate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260821142529_CorrectDefaultCategoryChainLeafCreateDate', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822074306_AddCompanyLessonPacingDefaults') THEN
    ALTER TABLE "LessonConfig" ALTER COLUMN "IntroWaitMs" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822074306_AddCompanyLessonPacingDefaults') THEN
    ALTER TABLE "LessonConfig" ALTER COLUMN "FinalQuestionWaitMs" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822074306_AddCompanyLessonPacingDefaults') THEN
    ALTER TABLE "LessonConfig" ALTER COLUMN "BreathPauseMs" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822074306_AddCompanyLessonPacingDefaults') THEN
    ALTER TABLE "Company" ADD "DefaultBreathPauseMs" integer NOT NULL DEFAULT 500;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822074306_AddCompanyLessonPacingDefaults') THEN
    ALTER TABLE "Company" ADD "DefaultFinalQuestionWaitMs" integer NOT NULL DEFAULT 5000;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822074306_AddCompanyLessonPacingDefaults') THEN
    ALTER TABLE "Company" ADD "DefaultIntroWaitMs" integer NOT NULL DEFAULT 5000;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822074306_AddCompanyLessonPacingDefaults') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260822074306_AddCompanyLessonPacingDefaults', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822143217_RemoveLessonConfigPacingOverrides') THEN
    ALTER TABLE "LessonConfig" DROP COLUMN "IntroWaitMs";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822143217_RemoveLessonConfigPacingOverrides') THEN
    ALTER TABLE "LessonConfig" DROP COLUMN "BreathPauseMs";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822143217_RemoveLessonConfigPacingOverrides') THEN
    ALTER TABLE "LessonConfig" DROP COLUMN "FinalQuestionWaitMs";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260822143217_RemoveLessonConfigPacingOverrides') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260822143217_RemoveLessonConfigPacingOverrides', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823151420_RemoveChatMessageAndAddQuestionSource') THEN
    ALTER TABLE "SessionQuestion" ADD "Source" text NOT NULL DEFAULT 'voice';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823151420_RemoveChatMessageAndAddQuestionSource') THEN
    ALTER TABLE "SessionQuestion" ALTER COLUMN "Source" DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823151420_RemoveChatMessageAndAddQuestionSource') THEN
    DROP TABLE "ChatMessage";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260823151420_RemoveChatMessageAndAddQuestionSource') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260823151420_RemoveChatMessageAndAddQuestionSource', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260825121033_AddDocumentContentHash') THEN
    ALTER TABLE "DocumentResource" ADD "ContentHash" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260825121033_AddDocumentContentHash') THEN
    CREATE INDEX "IX_DocumentResource_CompanyId_ContentHash" ON "DocumentResource" ("CompanyId", "ContentHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260825121033_AddDocumentContentHash') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260825121033_AddDocumentContentHash', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260826072010_AddLessonExcludedSlides') THEN
    CREATE TABLE "LessonExcludedSlide" (
        "Id" text NOT NULL,
        "CompanyId" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "LessonId" text NOT NULL,
        "SlideObjectId" text NOT NULL,
        CONSTRAINT "PK_LessonExcludedSlide" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260826072010_AddLessonExcludedSlides') THEN
    CREATE INDEX "IX_LessonExcludedSlide_CompanyId" ON "LessonExcludedSlide" ("CompanyId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260826072010_AddLessonExcludedSlides') THEN
    CREATE INDEX "IX_LessonExcludedSlide_LessonId_SlideObjectId" ON "LessonExcludedSlide" ("LessonId", "SlideObjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260826072010_AddLessonExcludedSlides') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260826072010_AddLessonExcludedSlides', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260826151755_AddLessonTrashLifecycle') THEN
    ALTER TABLE "LessonConfig" ADD "PurgeJobId" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260826151755_AddLessonTrashLifecycle') THEN
    ALTER TABLE "LessonConfig" ADD "PurgeStartedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260826151755_AddLessonTrashLifecycle') THEN
    CREATE TABLE "SessionQuestionReviewExclusion" (
        "Id" text NOT NULL,
        "CompanyId" text NOT NULL,
        "CreateBy" text,
        "CreateDate" timestamp with time zone NOT NULL,
        "UpdateBy" text,
        "UpdateDate" timestamp with time zone,
        "DeleteBy" text,
        "IsDelete" boolean NOT NULL,
        "DeletedAt" timestamp with time zone,
        "SessionQuestionId" text NOT NULL,
        "LessonId" text NOT NULL,
        "Reason" text NOT NULL,
        CONSTRAINT "PK_SessionQuestionReviewExclusion" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260826151755_AddLessonTrashLifecycle') THEN
    CREATE INDEX "IX_LessonConfig_CompanyId_IsDelete_DeletedAt" ON "LessonConfig" ("CompanyId", "IsDelete", "DeletedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260826151755_AddLessonTrashLifecycle') THEN
    CREATE INDEX "IX_SessionQuestionReviewExclusion_CompanyId_LessonId" ON "SessionQuestionReviewExclusion" ("CompanyId", "LessonId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260826151755_AddLessonTrashLifecycle') THEN
    CREATE UNIQUE INDEX "IX_SessionQuestionReviewExclusion_CompanyId_SessionQuestionId" ON "SessionQuestionReviewExclusion" ("CompanyId", "SessionQuestionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260826151755_AddLessonTrashLifecycle') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260826151755_AddLessonTrashLifecycle', '10.0.10');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260827181826_AddAuditLog') THEN
    CREATE TABLE "AuditLog" (
        "Id" text NOT NULL,
        "CompanyId" text,
        "ActorUserId" text NOT NULL,
        "Action" text NOT NULL,
        "EntityName" text NOT NULL,
        "EntityId" text NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AuditLog" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260827181826_AddAuditLog') THEN
    CREATE INDEX "IX_AuditLog_ActorUserId" ON "AuditLog" ("ActorUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260827181826_AddAuditLog') THEN
    CREATE INDEX "IX_AuditLog_CompanyId_OccurredAt" ON "AuditLog" ("CompanyId", "OccurredAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260827181826_AddAuditLog') THEN
    CREATE INDEX "IX_AuditLog_EntityName_EntityId" ON "AuditLog" ("EntityName", "EntityId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260827181826_AddAuditLog') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260827181826_AddAuditLog', '10.0.10');
    END IF;
END $EF$;
COMMIT;


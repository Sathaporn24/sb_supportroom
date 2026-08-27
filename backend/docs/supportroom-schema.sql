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
    "DefaultIntroWaitMs" integer NOT NULL,
    "DefaultBreathPauseMs" integer NOT NULL,
    "DefaultFinalQuestionWaitMs" integer NOT NULL,
    CONSTRAINT "PK_Company" PRIMARY KEY ("Id")
);


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


CREATE TABLE "DocumentResource" (
    "Id" text NOT NULL,
    "CompanyId" text NOT NULL,
    "CreateBy" text,
    "CreateDate" timestamp with time zone NOT NULL,
    "UpdateBy" text,
    "UpdateDate" timestamp with time zone,
    "DeleteBy" text,
    "IsDelete" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "ScopeType" text NOT NULL,
    "ScopeId" text,
    "FileName" text NOT NULL,
    "ContentType" text NOT NULL,
    "SizeBytes" bigint NOT NULL,
    "ObsBucket" text NOT NULL,
    "ObsKey" text NOT NULL,
    "IndexingStatus" text NOT NULL,
    "IndexedChunkCount" integer NOT NULL,
    "FailureReason" text,
    "ContentHash" text,
    CONSTRAINT "PK_DocumentResource" PRIMARY KEY ("Id")
);


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
    "LastSlideIndex" integer,
    "TotalSlideCount" integer,
    "CompletedAllSlides" boolean NOT NULL,
    CONSTRAINT "PK_LearningSession" PRIMARY KEY ("Id")
);


CREATE TABLE "LessonConfig" (
    "Id" text NOT NULL,
    "CompanyId" text NOT NULL,
    "CreateBy" text,
    "CreateDate" timestamp with time zone NOT NULL,
    "UpdateBy" text,
    "UpdateDate" timestamp with time zone,
    "DeleteBy" text,
    "IsDelete" boolean NOT NULL,
    "DeletedAt" timestamp with time zone,
    "PurgeJobId" text,
    "PurgeStartedAt" timestamp with time zone,
    "Slug" text NOT NULL,
    "CategoryId" text NOT NULL,
    "Title" text NOT NULL,
    "Description" text,
    "SlidesSourceUrl" text NOT NULL,
    "PresentationId" text,
    "SlidesEmbedUrl" text,
    "ContentSourceType" text NOT NULL,
    "PdfDocumentResourceId" text,
    "IsActive" boolean NOT NULL,
    "SlideConfigs" jsonb,
    CONSTRAINT "PK_LessonConfig" PRIMARY KEY ("Id")
);


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


CREATE TABLE "SessionQuestion" (
    "Id" text NOT NULL,
    "CompanyId" text NOT NULL,
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
    "Source" text NOT NULL,
    "ReviewResult" text,
    "ReviewNote" text,
    "ReviewedAt" timestamp with time zone,
    CONSTRAINT "PK_SessionQuestion" PRIMARY KEY ("Id")
);


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


CREATE TABLE "TrainingLink" (
    "Id" text NOT NULL,
    "CompanyId" text NOT NULL,
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
    "RecipientOrgName" text,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "MaxAttendees" integer,
    CONSTRAINT "PK_TrainingLink" PRIMARY KEY ("Id")
);


CREATE INDEX "IX_AdminUser_CompanyId" ON "AdminUser" ("CompanyId");


CREATE UNIQUE INDEX "IX_AdminUser_Email" ON "AdminUser" ("Email");


CREATE INDEX "IX_BackgroundJob_CompanyId_JobType_TargetId" ON "BackgroundJob" ("CompanyId", "JobType", "TargetId");


CREATE INDEX "IX_BackgroundJob_Status_NextAttemptAt" ON "BackgroundJob" ("Status", "NextAttemptAt");


CREATE INDEX "IX_Company_IsActive" ON "Company" ("IsActive");


CREATE INDEX "IX_DocumentChunk_CompanyId" ON "DocumentChunk" ("CompanyId");


CREATE INDEX "IX_DocumentChunk_DocumentId_SeqNo" ON "DocumentChunk" ("DocumentId", "SeqNo");


CREATE INDEX "IX_DocumentResource_CompanyId_ContentHash" ON "DocumentResource" ("CompanyId", "ContentHash");


CREATE INDEX "IX_DocumentResource_CompanyId_ScopeType_ScopeId" ON "DocumentResource" ("CompanyId", "ScopeType", "ScopeId");


CREATE INDEX "IX_KnowledgeCategory_CompanyId" ON "KnowledgeCategory" ("CompanyId");


CREATE INDEX "IX_KnowledgeCategory_CompanyId_ParentId_SortOrder" ON "KnowledgeCategory" ("CompanyId", "ParentId", "SortOrder");


CREATE INDEX "IX_KnowledgeQnA_CompanyId_ScopeType_ScopeId" ON "KnowledgeQnA" ("CompanyId", "ScopeType", "ScopeId");


CREATE INDEX "IX_KnowledgeQnAConflict_CompanyId_ResolvedAt" ON "KnowledgeQnAConflict" ("CompanyId", "ResolvedAt");


CREATE INDEX "IX_KnowledgeQnAConflict_QnAId" ON "KnowledgeQnAConflict" ("QnAId");


CREATE INDEX "IX_KnowledgeQnASource_CompanyId_SessionQuestionId" ON "KnowledgeQnASource" ("CompanyId", "SessionQuestionId");


CREATE INDEX "IX_KnowledgeQnASource_QnAId" ON "KnowledgeQnASource" ("QnAId");


CREATE INDEX "IX_LearningSession_CompanyId" ON "LearningSession" ("CompanyId");


CREATE INDEX "IX_LearningSession_TrainingLinkId_LearnerKey" ON "LearningSession" ("TrainingLinkId", "LearnerKey");


CREATE INDEX "IX_LessonConfig_CategoryId" ON "LessonConfig" ("CategoryId");


CREATE INDEX "IX_LessonConfig_CompanyId_IsDelete_DeletedAt" ON "LessonConfig" ("CompanyId", "IsDelete", "DeletedAt");


CREATE UNIQUE INDEX "IX_LessonConfig_CompanyId_Slug" ON "LessonConfig" ("CompanyId", "Slug");


CREATE INDEX "IX_LessonExcludedSlide_CompanyId" ON "LessonExcludedSlide" ("CompanyId");


CREATE INDEX "IX_LessonExcludedSlide_LessonId_SlideObjectId" ON "LessonExcludedSlide" ("LessonId", "SlideObjectId");


CREATE INDEX "IX_LessonSlideNarration_CompanyId" ON "LessonSlideNarration" ("CompanyId");


CREATE INDEX "IX_LessonSlideNarration_LessonId_SlideObjectId" ON "LessonSlideNarration" ("LessonId", "SlideObjectId");


CREATE INDEX "IX_SessionQuestion_CompanyId" ON "SessionQuestion" ("CompanyId");


CREATE INDEX "IX_SessionQuestion_CompanyId_AnswerStatus" ON "SessionQuestion" ("CompanyId", "AnswerStatus");


CREATE INDEX "IX_SessionQuestion_CompanyId_ReviewResult" ON "SessionQuestion" ("CompanyId", "ReviewResult");


CREATE INDEX "IX_SessionQuestion_SessionId" ON "SessionQuestion" ("SessionId");


CREATE INDEX "IX_SessionQuestionReviewExclusion_CompanyId_LessonId" ON "SessionQuestionReviewExclusion" ("CompanyId", "LessonId");


CREATE UNIQUE INDEX "IX_SessionQuestionReviewExclusion_CompanyId_SessionQuestionId" ON "SessionQuestionReviewExclusion" ("CompanyId", "SessionQuestionId");


CREATE INDEX "IX_TrainingLink_CompanyId" ON "TrainingLink" ("CompanyId");


CREATE UNIQUE INDEX "IX_TrainingLink_Token" ON "TrainingLink" ("Token");



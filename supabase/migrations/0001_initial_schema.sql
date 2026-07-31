-- SupportRoom AI — initial schema for LessonConfig / Session / Question / Result.
-- Not applied yet in this phase - see docs/SUPABASE_SETUP_AND_SCHEMA.md for how to run
-- this against a real Supabase project. Google Slides remains the source of truth for
-- teaching content; these tables only hold admin-set config + usage history.
--
-- Access model: only the Next.js server (service role key) talks to these tables via
-- Route Handlers / repositories. RLS is enabled with no anon/authenticated policies, so
-- direct client access (anon/authenticated keys) is denied by default. The service role
-- key bypasses RLS entirely, which is how the app reads/writes in every phase.

create extension if not exists "pgcrypto";

-- ---------------------------------------------------------------------------
-- updated_at trigger helper
-- ---------------------------------------------------------------------------
create or replace function set_updated_at()
returns trigger as $$
begin
  new.updated_at = now();
  return new;
end;
$$ language plpgsql;

-- ---------------------------------------------------------------------------
-- lessons
-- ---------------------------------------------------------------------------
create table if not exists lessons (
  id uuid primary key default gen_random_uuid(),
  slug text not null unique,
  title text not null,
  description text,
  slides_source_url text not null default '',
  presentation_id text,
  slides_embed_url text,
  intro_wait_ms integer not null default 5000 check (intro_wait_ms >= 0),
  breath_pause_ms integer not null default 1000 check (breath_pause_ms >= 0),
  final_question_wait_ms integer not null default 5000 check (final_question_wait_ms >= 0),
  is_active boolean not null default false,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create trigger lessons_set_updated_at
  before update on lessons
  for each row execute function set_updated_at();

-- ---------------------------------------------------------------------------
-- lesson_slide_configs — admin-set per-slide metadata (video duration only;
-- speaker notes/images/video themselves live in Google Slides, never copied here)
-- ---------------------------------------------------------------------------
create table if not exists lesson_slide_configs (
  id uuid primary key default gen_random_uuid(),
  lesson_id uuid not null references lessons(id) on delete cascade,
  slide_object_id text not null,
  slide_index integer not null check (slide_index >= 0),
  video_duration_ms integer check (video_duration_ms is null or video_duration_ms >= 0),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  unique (lesson_id, slide_object_id)
);

create index if not exists lesson_slide_configs_lesson_order_idx
  on lesson_slide_configs (lesson_id, slide_index);

create trigger lesson_slide_configs_set_updated_at
  before update on lesson_slide_configs
  for each row execute function set_updated_at();

-- ---------------------------------------------------------------------------
-- training_sessions
-- ---------------------------------------------------------------------------
create table if not exists training_sessions (
  id uuid primary key default gen_random_uuid(),
  token uuid not null unique default gen_random_uuid(),
  lesson_id uuid not null references lessons(id),
  lesson_slug text not null,
  teacher_name text,
  school_name text,
  status text not null default 'NOT_STARTED'
    check (status in ('NOT_STARTED', 'IN_PROGRESS', 'ENDED', 'EXPIRED')),
  expires_at timestamptz not null,
  started_at timestamptz,
  ended_at timestamptz,
  completed_all_slides boolean not null default false,
  last_slide_object_id text,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create index if not exists training_sessions_created_at_idx on training_sessions (created_at desc);
create index if not exists training_sessions_lesson_id_idx on training_sessions (lesson_id);

create trigger training_sessions_set_updated_at
  before update on training_sessions
  for each row execute function set_updated_at();

-- ---------------------------------------------------------------------------
-- session_questions — one row per Push-to-Talk question actually asked
-- ---------------------------------------------------------------------------
create table if not exists session_questions (
  id uuid primary key default gen_random_uuid(),
  session_id uuid not null references training_sessions(id) on delete cascade,
  slide_object_id text,
  transcript text,
  answer text,
  answer_status text not null
    check (answer_status in ('answered', 'not_found', 'out_of_scope', 'no_speech', 'transcription_failed')),
  created_at timestamptz not null default now()
);

create index if not exists session_questions_session_created_idx
  on session_questions (session_id, created_at);

-- ---------------------------------------------------------------------------
-- session_results — snapshot written once when a session ends
-- ---------------------------------------------------------------------------
create table if not exists session_results (
  id uuid primary key default gen_random_uuid(),
  session_id uuid not null unique references training_sessions(id) on delete cascade,
  completed_all_slides boolean not null,
  last_slide_object_id text,
  questions_summary jsonb,
  unanswered_points jsonb,
  created_at timestamptz not null default now()
);

-- ---------------------------------------------------------------------------
-- Row Level Security — deny-by-default. Only the service role (used exclusively by
-- Route Handlers, never shipped to the browser) can read/write. No anon/authenticated
-- policies are defined because CS has no login in this phase and teachers never talk
-- to Supabase directly.
-- ---------------------------------------------------------------------------
alter table lessons enable row level security;
alter table lesson_slide_configs enable row level security;
alter table training_sessions enable row level security;
alter table session_questions enable row level security;
alter table session_results enable row level security;

-- ---------------------------------------------------------------------------
-- Seed data — mirrors src/providers/data/mock/seed.ts so switching
-- DATA_PROVIDER=supabase starts from the same three demo topics.
-- ---------------------------------------------------------------------------
insert into lessons (slug, title, description, slides_source_url, is_active)
values
  ('login-mobile', 'วิธีการ Login (mobile)', 'สอนขั้นตอนเข้าสู่ระบบผ่านแอปพลิเคชันมือถือ', '', false),
  ('login-web', 'วิธีการ Login (Web)', null, '', false),
  ('forgot-password', 'ลืมรหัสผ่าน', null, '', false)
on conflict (slug) do nothing;

-- After seeding, set each lesson's slidesSourceUrl via the Admin UI, click
-- "ตรวจสอบ/Sync Slides", then tick "เปิดใช้งานบทเรียนนี้" and save to activate it.

# 05 — Ma'lumotlar bazasi sxemasi (PostgreSQL 16)

## 1. Umumiy konvensiyalar

| Qoida | Qiymat |
|-------|--------|
| Jadval nomi | `snake_case`, ko'plik: `schools`, `assessment_tests` |
| Ustun nomi | `snake_case`: `created_at`, `school_id` |
| PK | `uuid`, `gen_random_uuid()` (pgcrypto) |
| Vaqt | `timestamptz` — **hamma joyda UTC** |
| Sana | `date` (tug'ilgan sana) |
| Pul | `numeric(10,6)` (AI narxi) |
| Erkin struktura | `jsonb` (ball, AI javob, bayroq) |
| Enum | `smallint` + C# enum (DB enum turi ishlatilmaydi — migratsiya osonligi uchun) |
| Naming EF'da | `UseSnakeCaseNamingConvention()` (EFCore.NamingConventions) |

Kengaytmalar: `pgcrypto` (uuid), `pg_trgm` (ism bo'yicha qidiruv), `unaccent` (ixtiyoriy).

---

## 2. DDL

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- ============ SCHOOLS ============
CREATE TABLE schools (
    id                        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name                      varchar(200) NOT NULL,
    region                    varchar(100) NOT NULL,
    district                  varchar(100) NOT NULL,
    school_number             varchar(20),
    contact_person            varchar(150),
    contact_phone             varchar(20),
    slug                      varchar(80)  NOT NULL,
    access_token              varchar(64)  NOT NULL,
    access_code               varchar(6),
    entry_code                varchar(8),                          -- 2026-09-07: maktab kodi; kind=1 da doim, kind=2 da NULL
    daily_registration_limit  int          NOT NULL DEFAULT 500,
    kind                      smallint     NOT NULL,               -- P47: 1 School, 2 PublicSpace (DEFAULT yo'q)
    is_active                 boolean      NOT NULL DEFAULT true,
    show_result_to_student    boolean      NOT NULL DEFAULT false, -- P47: ilgari global App:ShowResultToStudent
    notes                     varchar(1000),
    is_deleted                boolean      NOT NULL DEFAULT false,
    deleted_at                timestamptz,
    created_at                timestamptz  NOT NULL DEFAULT now(),
    updated_at                timestamptz  NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_schools_slug        ON schools(slug) WHERE is_deleted = false;
CREATE UNIQUE INDEX ux_schools_token       ON schools(access_token);
-- 2026-09-07: maktab kodi maktabni aniqlaydi — unikal; NULL (ommaviy makon) kirmaydi, is_deleted ga qaramaydi.
CREATE UNIQUE INDEX ux_schools_entry_code  ON schools(entry_code) WHERE entry_code IS NOT NULL;
CREATE INDEX        ix_schools_region_dist ON schools(region, district);
CREATE INDEX        ix_schools_name_trgm   ON schools USING gin (name gin_trgm_ops);
-- P47: bazada AYNAN BITTA ommaviy makon (poyga holatiga qarshi yagona haqiqiy himoya).
CREATE UNIQUE INDEX ux_schools_public_space ON schools(kind) WHERE kind = 2 AND is_deleted = false;

-- ============ TEST CATALOG ============
CREATE TABLE test_definitions (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code               varchar(20)  NOT NULL,
    name_uz            varchar(150) NOT NULL,
    description_uz     text,
    version            int          NOT NULL DEFAULT 1,
    display_order      int          NOT NULL,
    question_count     int          NOT NULL,
    estimated_minutes  int          NOT NULL,
    shuffle_questions  boolean      NOT NULL DEFAULT false,
    page_size          int          NOT NULL DEFAULT 10,
    is_active          boolean      NOT NULL DEFAULT true,
    kind               smallint     NOT NULL DEFAULT 1,   -- 1 Standard, 2 Custom
    is_system          boolean      NOT NULL DEFAULT false,
    scoring_strategy   varchar(20)  NOT NULL,             -- MBTI16|BIG5|RIASEC|ACTIVITY|SUM
    status             smallint     NOT NULL DEFAULT 2,   -- 1 Draft, 2 Published, 3 Archived
    created_by_admin_user_id uuid,
    published_at       timestamptz,
    created_at         timestamptz  NOT NULL DEFAULT now(),
    updated_at         timestamptz  NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_test_definitions_code ON test_definitions(code);
CREATE INDEX ix_test_definitions_active ON test_definitions(is_active, display_order)
    WHERE status = 2;

CREATE TABLE test_scales (
    id                        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    test_definition_id        uuid NOT NULL REFERENCES test_definitions(id) ON DELETE CASCADE,
    code                      varchar(10)  NOT NULL,
    name_uz                   varchar(120) NOT NULL,
    description_uz            text,
    display_order             int          NOT NULL,
    interpretation_bands_json jsonb        NOT NULL DEFAULT '[]'
);
CREATE UNIQUE INDEX ux_test_scales ON test_scales(test_definition_id, code);

CREATE TABLE questions (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    test_definition_id  uuid NOT NULL REFERENCES test_definitions(id) ON DELETE CASCADE,
    code                varchar(20)  NOT NULL,
    display_order       int          NOT NULL,
    text_uz             text         NOT NULL,
    text_ru             text,
    text_en             text,
    question_type       smallint     NOT NULL,   -- 1 Likert5, 2 Likert7, 3 Binary, 4 SingleChoice, 5 ForcedChoice
    scale               varchar(10)  NOT NULL,   -- EI, SN, TF, JP, O, C, E, A, N, R, I, ART, SOC, ENT, CONV, MOT, SELF, SOCA, ENG
    scale_direction     smallint     NOT NULL DEFAULT 1,  -- +1 | -1
    weight              numeric(4,2) NOT NULL DEFAULT 1.0,
    is_required         boolean      NOT NULL DEFAULT true,
    is_active           boolean      NOT NULL DEFAULT true,
    is_system           boolean      NOT NULL DEFAULT false
);
CREATE UNIQUE INDEX ux_questions_code       ON questions(code);
CREATE INDEX        ix_questions_test_order ON questions(test_definition_id, display_order);

CREATE TABLE answer_options (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    question_id    uuid NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    text_uz        text NOT NULL,
    value          int  NOT NULL,
    scale          varchar(10),
    display_order  int  NOT NULL
);
CREATE INDEX ix_answer_options_question ON answer_options(question_id, display_order);

CREATE TABLE type_catalog (
    code                   varchar(4) PRIMARY KEY,      -- INTJ ...
    name_uz                varchar(80)  NOT NULL,
    short_description_uz   varchar(300) NOT NULL,
    long_description_uz    text         NOT NULL,
    strengths_json         jsonb        NOT NULL DEFAULT '[]',
    growth_areas_json      jsonb        NOT NULL DEFAULT '[]',
    career_hints_json      jsonb        NOT NULL DEFAULT '[]'
);

CREATE TABLE career_map (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    holland_code             varchar(2)   NOT NULL,
    field_name_uz            varchar(150) NOT NULL,
    description_uz           text,
    example_professions_json jsonb        NOT NULL DEFAULT '[]',
    relevance_order          int          NOT NULL DEFAULT 1
);
CREATE INDEX ix_career_map_code ON career_map(holland_code, relevance_order);

-- ============ STUDENTS ============
CREATE TABLE students (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    school_id                   uuid NOT NULL REFERENCES schools(id),
    public_user_id              uuid REFERENCES public_users(id),  -- P47: maktab oqimida NULL
    full_name                   varchar(200) NOT NULL,
    normalized_name             varchar(200) NOT NULL,
    birth_date                  date         NOT NULL,
    gender                      smallint     NOT NULL DEFAULT 0,
    grade                       int          NOT NULL CHECK (grade BETWEEN 0 AND 11), -- P47: 0 = sinf yo'q (kattalar)
    class_letter                varchar(2),
    phone                       varchar(20)  NOT NULL,
    parent_phone                varchar(20),
    email                       varchar(150),
    consent_given_at            timestamptz  NOT NULL,
    consent_version             varchar(30),                        -- P47: qabul qilingan rozilik matni versiyasi
    parental_consent            boolean      NOT NULL DEFAULT false,-- P47: ota-ona/vasiy roziligi
    -- snapshot
    last_personality_type       varchar(4),
    last_maturity_index         numeric(5,2),
    last_activity_index         numeric(5,2),
    last_activity_level         smallint,
    last_holland_code           varchar(3),
    needs_attention             boolean      NOT NULL DEFAULT false,
    last_assessment_at          timestamptz,
    completed_assessment_count  int          NOT NULL DEFAULT 0,
    is_deleted                  boolean      NOT NULL DEFAULT false,
    deleted_at                  timestamptz,
    created_at                  timestamptz  NOT NULL DEFAULT now(),
    updated_at                  timestamptz  NOT NULL DEFAULT now()
);
-- P47: filtrga `public_user_id IS NULL` qo'shildi — F.I.Sh.+tug'ilgan sana unikalligi FAQAT
-- maktab oqimiga tegishli; ommaviy makonda identifikator Telegram akkaunti.
CREATE UNIQUE INDEX ux_students_identity ON students(school_id, normalized_name, birth_date)
    WHERE is_deleted = false AND public_user_id IS NULL;
-- P47: bitta akkauntga bitta o'quvchi profili (90 kunlik oyna shu profil bo'yicha ishlaydi).
CREATE UNIQUE INDEX ux_students_public_user ON students(public_user_id)
    WHERE public_user_id IS NOT NULL AND is_deleted = false;
CREATE INDEX ix_students_school_grade ON students(school_id, grade);
CREATE INDEX ix_students_name_trgm    ON students USING gin (full_name gin_trgm_ops);
CREATE INDEX ix_students_attention    ON students(needs_attention) WHERE needs_attention = true;
CREATE INDEX ix_students_last_at      ON students(last_assessment_at DESC NULLS LAST);

-- ============ ASSESSMENTS ============
CREATE TABLE assessments (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id              uuid NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    school_id               uuid NOT NULL REFERENCES schools(id),
    session_token           varchar(64)  NOT NULL,               -- ⚠️ ochiq matn, P47 2-bosqichida o'chiriladi
    session_token_hash      varchar(64)  NOT NULL,               -- P47: SHA-256 hex (DEFAULT yo'q)
    status                  smallint     NOT NULL DEFAULT 0,
    language_code           varchar(5)   NOT NULL DEFAULT 'uz',
    started_at              timestamptz  NOT NULL DEFAULT now(),
    completed_at            timestamptz,
    expires_at              timestamptz  NOT NULL,
    reliability_score       numeric(5,2),
    reliability_flag        smallint,
    total_duration_seconds  int,
    ip_hash                 varchar(64),
    user_agent              varchar(300),
    is_deleted              boolean      NOT NULL DEFAULT false,
    created_at              timestamptz  NOT NULL DEFAULT now(),
    updated_at              timestamptz  NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_assessments_token      ON assessments(session_token);
CREATE UNIQUE INDEX ux_assessments_token_hash ON assessments(session_token_hash);
CREATE INDEX ix_assessments_student        ON assessments(student_id, started_at DESC);
CREATE INDEX ix_assessments_school_status  ON assessments(school_id, status);
CREATE INDEX ix_assessments_status_started ON assessments(status, started_at DESC);

CREATE TABLE assessment_tests (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    assessment_id       uuid NOT NULL REFERENCES assessments(id) ON DELETE CASCADE,
    test_definition_id  uuid NOT NULL REFERENCES test_definitions(id),
    status              smallint NOT NULL DEFAULT 0,
    display_order       int      NOT NULL,
    answered_count      int      NOT NULL DEFAULT 0,
    total_count         int      NOT NULL,
    question_order_json jsonb,
    started_at          timestamptz,
    completed_at        timestamptz
);
CREATE UNIQUE INDEX ux_assessment_tests ON assessment_tests(assessment_id, test_definition_id);

CREATE TABLE answers (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    assessment_test_id  uuid NOT NULL REFERENCES assessment_tests(id) ON DELETE CASCADE,
    question_id         uuid NOT NULL REFERENCES questions(id),
    raw_value           int  NOT NULL,
    selected_option_id  uuid REFERENCES answer_options(id),
    duration_ms         int  NOT NULL DEFAULT 0,
    revision_count      int  NOT NULL DEFAULT 0,
    answered_at         timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_answers_test_question ON answers(assessment_test_id, question_id);

CREATE TABLE test_results (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    assessment_test_id     uuid NOT NULL REFERENCES assessment_tests(id) ON DELETE CASCADE,
    assessment_id          uuid NOT NULL REFERENCES assessments(id) ON DELETE CASCADE,
    test_code              varchar(20) NOT NULL,
    result_code            varchar(20),
    raw_scores_json        jsonb NOT NULL,
    normalized_scores_json jsonb NOT NULL,
    levels_json            jsonb NOT NULL DEFAULT '{}',
    composite_index        numeric(5,2),
    flags_json             jsonb NOT NULL DEFAULT '[]',
    scoring_version        int   NOT NULL DEFAULT 1,
    test_version           int   NOT NULL DEFAULT 1,
    computed_at            timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_test_results_test ON test_results(assessment_test_id);
CREATE INDEX ix_test_results_assessment  ON test_results(assessment_id);
CREATE INDEX ix_test_results_code        ON test_results(test_code, result_code);

-- ============ AI ============
CREATE TABLE ai_provider_configs (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    provider           smallint     NOT NULL,   -- 1 Gemini, 2 OpenAi, 3 Anthropic
    display_name       varchar(80)  NOT NULL,
    api_key_encrypted  text,
    model              varchar(80)  NOT NULL,
    base_url           varchar(200),
    max_output_tokens  int          NOT NULL DEFAULT 4096,
    temperature        numeric(3,2) NOT NULL DEFAULT 0.4,
    is_default         boolean      NOT NULL DEFAULT false,
    is_active          boolean      NOT NULL DEFAULT false,
    fallback_order     int          NOT NULL DEFAULT 100,
    last_checked_at    timestamptz,
    last_check_status  varchar(200),
    created_at         timestamptz  NOT NULL DEFAULT now(),
    updated_at         timestamptz  NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_ai_provider_default ON ai_provider_configs(is_default) WHERE is_default = true;
CREATE UNIQUE INDEX ux_ai_provider_kind    ON ai_provider_configs(provider);

CREATE TABLE ai_analyses (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    assessment_id           uuid NOT NULL REFERENCES assessments(id) ON DELETE CASCADE,
    provider                smallint NOT NULL,
    model                   varchar(80)  NOT NULL,
    prompt_version          varchar(20)  NOT NULL,
    status                  smallint     NOT NULL DEFAULT 0,
    request_payload_json    jsonb,
    response_json           jsonb,
    summary                 text,
    personality_portrait    text,
    strengths_json          jsonb,
    growth_areas_json       jsonb,
    recommendations_json    jsonb,
    career_suggestions_json jsonb,
    teacher_notes           text,
    parent_notes            text,
    attention_flags_json    jsonb,
    input_tokens            int,
    output_tokens           int,
    estimated_cost_usd      numeric(10,6),
    duration_ms             int,
    error_message           varchar(2000),
    attempt_number          int      NOT NULL DEFAULT 1,
    is_current              boolean  NOT NULL DEFAULT false,
    created_at              timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_ai_analyses_assessment ON ai_analyses(assessment_id, created_at DESC);
CREATE UNIQUE INDEX ux_ai_analyses_current ON ai_analyses(assessment_id) WHERE is_current = true;

CREATE TABLE prompt_templates (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    key         varchar(50)  NOT NULL,      -- 'full_analysis'
    version     varchar(20)  NOT NULL,      -- 'v1.0'
    system_text text NOT NULL,
    user_text   text NOT NULL,
    json_schema jsonb NOT NULL,
    is_active   boolean NOT NULL DEFAULT false,
    created_at  timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_prompt_templates ON prompt_templates(key, version);

-- ============ IDENTITY & AUDIT ============
CREATE TABLE admin_users (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    username              varchar(60)  NOT NULL,
    email                 varchar(150) NOT NULL,
    full_name             varchar(150),
    password_hash         varchar(300) NOT NULL,
    role                  smallint     NOT NULL DEFAULT 1,
    is_active             boolean      NOT NULL DEFAULT true,
    totp_secret_encrypted text,
    totp_enabled          boolean      NOT NULL DEFAULT false,
    failed_login_count    int          NOT NULL DEFAULT 0,
    locked_until          timestamptz,
    last_login_at         timestamptz,
    created_at            timestamptz  NOT NULL DEFAULT now(),
    updated_at            timestamptz  NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_admin_users_username ON admin_users(lower(username));
CREATE UNIQUE INDEX ux_admin_users_email    ON admin_users(lower(email));

CREATE TABLE refresh_tokens (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    admin_user_id     uuid NOT NULL REFERENCES admin_users(id) ON DELETE CASCADE,
    token_hash        varchar(128) NOT NULL,
    expires_at        timestamptz  NOT NULL,
    revoked_at        timestamptz,
    created_by_ip_hash varchar(64),
    created_at        timestamptz  NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_refresh_tokens_hash ON refresh_tokens(token_hash);

CREATE TABLE audit_logs (
    id             bigserial PRIMARY KEY,
    admin_user_id  uuid REFERENCES admin_users(id),
    action         varchar(80) NOT NULL,
    entity_type    varchar(60),
    entity_id      uuid,
    before_json    jsonb,
    after_json     jsonb,
    ip_hash        varchar(64),
    user_agent     varchar(300),
    created_at     timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_audit_logs_created ON audit_logs(created_at DESC);
CREATE INDEX ix_audit_logs_entity  ON audit_logs(entity_type, entity_id);

-- ============ RATE LIMIT ============
CREATE TABLE registration_counters (
    school_id  uuid NOT NULL REFERENCES schools(id) ON DELETE CASCADE,
    date_utc   date NOT NULL,
    count      int  NOT NULL DEFAULT 0,
    PRIMARY KEY (school_id, date_utc)
);
```

---

### P34 da qo'shilgan — dastur modeli (migratsiyalar `AddAssessmentPrograms`, `RequireAssessmentProgramId`)

```sql
-- Dastur = nomlangan, tartiblangan test to'plami. O'quvchi kirishda DASTURNI tanlaydi.
create table assessment_programs (
  id                     uuid primary key,
  code                   varchar(50)  not null,
  name_uz                varchar(200) not null,
  description_uz         varchar(1000) null,
  kind                   smallint     not null,  -- 1 System, 2 Custom
  visibility             smallint     not null,  -- 1 Public, 2 Assigned
  status                 smallint     not null,  -- 1 Draft, 2 Published, 3 Archived
  is_active              boolean      not null default true,
  is_system              boolean      not null default false,
  display_order          int          not null,
  created_by_admin_user_id uuid       null references admin_users(id),
  created_at             timestamptz  not null,
  updated_at             timestamptz  not null
);
create unique index ux_assessment_programs_code on assessment_programs(code);
-- 2026-09-06: `status` va `is_active` — SAQLASH maydonlari. Admin API va UI ular o'rniga
-- BITTA hosila holatni ko'radi (`ProgramState`: Draft · Active · Paused · Archived,
-- `docs/04` 2.13). Domen invarianti: `status = 3` (Archived) bo'lsa `is_active` DOIM `false`;
-- eski, buzilgan qatorlar seed bosqichida idempotent tuzatiladi
-- (`DbSeeder.ReconcileProgramStatesAsync`) — migratsiya QO'SHILMAGAN.

create table program_tests (
  program_id         uuid not null references assessment_programs(id) on delete cascade,
  test_definition_id uuid not null references test_definitions(id) on delete restrict,
  display_order      int  not null,
  primary key (program_id, test_definition_id)
);

-- Faqat visibility = Assigned dasturlar uchun.
create table school_programs (
  school_id  uuid not null references schools(id) on delete cascade,
  program_id uuid not null references assessment_programs(id) on delete cascade,
  primary key (school_id, program_id)
);

alter table test_definitions add column scoring_mode smallint not null default 1; -- 1 Scored, 2 Survey
alter table assessments     add column program_id   uuid     not null references assessment_programs(id);
```

> **Migratsiya tartibi muhim.** `program_id` avval `nullable` qo'shiladi; ikkinchi migratsiya
> `SET NOT NULL` dan **oldin** o'z ichida: (a) `PERSONALITY_PROFILE` tizim dasturini
> **deterministik ID** (`00000000-0000-0000-0000-000000000001`) bilan yaratadi,
> (b) mavjud tizim testlarini `program_tests` ga bog'laydi, (c) `program_id IS NULL` qatorlarni
> backfill qiladi. Backfill'ni seeder'ga qoldirish **xato**: `--migrate` barcha migratsiyalarni
> bitta chaqiruvda bajaradi, `--seed` esa faqat undan keyin ishlaydi — natijada mavjud
> sessiyalar nol-GUID ga bog'lanib FK cheklovini buzardi (P34 QA topilmasi).

### P-dashboard da qo'shilgan — havola ochilishi hisoblagichi

```sql
-- Voronkaning eng yuqori bo'g'ini: maktab havolasi necha marta ochilgani.
-- Shaxsiy ma'lumot saqlanmaydi — faqat kunlik son (IP/qurilma yo'q).
create table school_link_views (
  school_id uuid not null references schools(id) on delete cascade,
  date_utc  date not null,
  count     int  not null default 0,
  primary key (school_id, date_utc)
);
```

> Hisoblagich `registration_counters` bilan bir xil atomik naqshda oshiriladi
> (`INSERT ... ON CONFLICT DO UPDATE`). **Ma'lum cheklov:** takroriy sahifa ochish va
> botlar sonni oshiradi — MVP uchun qabul qilingan.

### P13 da qo'shilgan (migratsiya `AddAuditLogsAndTotpSupport`)

```sql
-- 2FA zaxira kodlari: har biri alohida qator, xeshlangan, bir martalik.
-- Hujjatda dastlab yo'q edi — TOTP zaxira kodlarini xom holda saqlash mumkin emasligi uchun
-- alohida jadval kerak bo'ldi (parol bilan bir xil `IPasswordHasher` ishlatiladi).
create table admin_totp_backup_codes (
  id              uuid primary key,
  admin_user_id   uuid not null references admin_users(id) on delete cascade,
  code_hash       text not null,
  used_at         timestamptz null,
  created_at      timestamptz not null
);
create index ix_admin_totp_backup_codes_admin on admin_totp_backup_codes(admin_user_id);

-- EF Core FK ustunlari uchun avtomatik indeks qo'shadi; `audit_logs` da ham shunday:
create index ix_audit_logs_admin_user_id on audit_logs(admin_user_id);

-- TOTP kodini qayta ishlatishga qarshi himoya: oxirgi ishlatilgan vaqt qadami.
alter table admin_users add column totp_last_used_step bigint null;

-- Optimistik konkurentlik tokeni. ⚠️ DOIMIY `DEFAULT` BO'LMASLIGI SHART: qiymat har
-- yangilanishda yangi `Guid` bilan almashadi (`AppDbContext.SaveChanges`), shu sabab
-- ustun darajasidagi doimiy default token vazifasini o'ldiradi (qiymatsiz INSERT jimgina
-- bir xil qiymat yozardi). `20260902070721_AddAuditLogsAndTotpSupport` EF Core
-- `defaultValue:` orqali `'000…0'::uuid` doimiy default qoldirgan edi —
-- `20260902194217_DropConcurrencyStampDefault` uni olib tashlab, nol-GUID qolgan
-- qatorlarni `gen_random_uuid()` bilan tiklaydi.
alter table admin_users add column concurrency_stamp uuid not null;  -- DEFAULT yo'q!
```

### P47 da qo'shilgan — ommaviy makon va ommaviy foydalanuvchilar (migratsiya `AddPublicSpaceAndPublicUsers`)

```sql
-- ============ PUBLIC USERS ============
CREATE TABLE public_users (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    telegram_id    bigint,                       -- yaratishda majburiy; o'chirishda NULL (anonimlashtirish)
    username       varchar(64),
    first_name     varchar(100),
    last_name      varchar(100),
    photo_url      varchar(500),
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now(),
    last_login_at  timestamptz NOT NULL,
    deleted_at     timestamptz                   -- yumshoq o'chirish (is_deleted ustuni YO'Q)
);
CREATE UNIQUE INDEX ux_public_users_telegram ON public_users(telegram_id) WHERE telegram_id IS NOT NULL;
CREATE INDEX        ix_public_users_created  ON public_users(created_at DESC);

CREATE TABLE public_refresh_tokens (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    public_user_id     uuid NOT NULL REFERENCES public_users(id) ON DELETE CASCADE,
    token_hash         varchar(128) NOT NULL,    -- xom token HECH QACHON saqlanmaydi
    expires_at         timestamptz  NOT NULL,
    revoked_at         timestamptz,
    created_by_ip_hash varchar(64),
    created_at         timestamptz  NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_public_refresh_tokens_hash ON public_refresh_tokens(token_hash);
CREATE INDEX        ix_public_refresh_tokens_user ON public_refresh_tokens(public_user_id);
```

**Uch qadamli backfill (`DEFAULT` tuzog'idan qochish uchun — quyidagi qoidaga qarang):**

```sql
-- schools.kind
alter table schools add column kind smallint null;
update schools set kind = 1 where kind is null;      -- mavjud yozuvlar — haqiqiy maktab
alter table schools alter column kind set not null;

-- assessments.session_token_hash: mavjud FAOL sessiyalar buzilmaydi (xesh ochiq
-- matnli tokendan bazaning O'ZIDA hisoblanadi; formula `Domain.Common.TokenHash.Compute`
-- bilan bayt-ma-bayt bir xil).
alter table assessments add column session_token_hash varchar(64) null;
update assessments set session_token_hash = encode(sha256(convert_to(session_token, 'UTF8')), 'hex')
    where session_token_hash is null;
alter table assessments alter column session_token_hash set not null;
```

> **Ikki bosqichli destruktiv o'zgarish (`CLAUDE.md` 7-qoida):** ochiq matnli `session_token`
> ustuni SHU migratsiyada o'chirilMAYDI — hozircha `SessionTokenAuthenticationHandler` va
> `StartSession` rezyume yo'li aynan shu ustunga tayanadi. 2-bosqich: o'qish yo'li
> `session_token_hash` ga ko'chiriladi (rezyumeda token ROTATSIYA qilinadi —
> `Assessment.RotateSessionToken`, chunki xeshdan xom token qayta tiklanmaydi), so'ng alohida
> migratsiya `session_token` va `ux_assessments_token` ni tashlaydi.

**Ommaviy makon yozuvi migratsiyada emas, seederda:** `DbSeeder.SeedPublicSpaceAsync`,
deterministik `id = 00000000-0000-0000-0000-000000000002`, `slug = 'ommaviy'`,
`kind = 2`, `show_result_to_student = true`, `access_token` seed vaqtida tasodifiy
generatsiya qilinadi (kodda qattiq yozilgan sir yo'q — `CLAUDE.md` 4-qoida). Idempotent:
mavjud yozuv umuman o'zgartirilmaydi (token qayta generatsiya qilinsa mavjud ommaviy
havolalar o'lardi).

---

### P46 da qo'shilgan (migratsiya `AddPendingTotpEnrollment`)

```sql
-- 2FA o'rnatishning tasdiqlash bosqichi: sir avval KUTISH holatiga yoziladi va faqat
-- ilovadagi kod tekshirilgach `totp_secret_encrypted` ga ko'chadi (`docs/08` 2-bo'lim).
-- Faqat qo'shimcha, nullable ustunlar — destruktiv o'zgarish yo'q.
alter table admin_users add column pending_totp_secret_encrypted text null;
alter table admin_users add column pending_totp_created_at timestamptz null;
```

> **Qoida (`defaultValue` tuzog'i):** mavjud jadvalga `NOT NULL` ustun qo'shganda EF Core
> `migrationBuilder.AddColumn(..., defaultValue: X)` yozadi va buni **bir martalik backfill
> emas, ustunning DOIMIY `DEFAULT`i** sifatida chiqaradi. Bu `bool`/`int`/enum bayroqlari
> uchun to'g'ri (modelda ham `HasDefaultValue` bor). Lekin qiymati **har qator uchun noyob**
> bo'lishi kerak bo'lgan ustun (identifikator, token, vaqt tamg'asi) uchun noto'g'ri —
> u yerda backfill xom SQL bilan (`UPDATE ... SET x = gen_random_uuid()`) bajarilib,
> `DEFAULT` qoldirilmasligi kerak. Buni `tests/StudentRoadMap.Migrations.Tests`
> (`MigratsiyaSxemasi_ModeldanQurilganSxemaBilanBirXil`) avtomatik ushlaydi.

---

## 3. Enum ↔ raqam mosligi (kodda ham shu)

| Enum | Qiymatlar |
|------|-----------|
| `AssessmentStatus` | 0 Draft, 1 InProgress, 2 Completed, 3 Analyzing, 4 Analyzed, 5 AnalysisFailed, 6 Abandoned |
| `TestStatus` | 0 NotStarted, 1 InProgress, 2 Completed (`assessment_tests.status`) |
| `Gender` | 0 Unspecified, 1 Male, 2 Female |
| `QuestionType` | 1 Likert5, 2 Likert7, 3 Binary, 4 SingleChoice, 5 ForcedChoice, **6 ShortText, 7 LongText, 8 MultiChoice, 9 Phone** (6–9 — P52, faqat `Survey` rejimida) |
| `VisibilityMatch` | 1 All, 2 Any — `visibility_rule` jsonb ichida SATR sifatida (`"All"`), raqam emas |
| `VisibilityOperator` | 1 Equals, 2 NotEquals, 3 AnyOf, 4 NoneOf, 5 ContainsAny, 6 ContainsAll, 7 Answered, 8 NotAnswered — jsonb ichida SATR sifatida |
| `TestKind` | 1 Standard (ilmiy metodika), 2 Custom (superadmin anketasi) |
| `TestDefinitionStatus` | 1 Draft, 2 Published, 3 Archived (`test_definitions.status`) |
| `ReliabilityFlag` | 1 Reliable, 2 Questionable, 3 Unreliable |
| `ActivityLevel` | 1 Passive, 2 LowActive, 3 Moderate, 4 Active, 5 HighlyActive |
| `AiProvider` | 1 Gemini, 2 OpenAi, 3 Anthropic |
| `AiAnalysisStatus` | 0 Pending, 1 Running, 2 Succeeded, 3 Failed |
| `AdminRole` | 1 SuperAdmin, 2 SchoolAdmin (v2), 3 Psychologist (v2) |
| `SchoolKind` | 1 School (maktab havolasi oqimi), 2 PublicSpace (ommaviy makon) — `schools.kind` |
| `PublicUserDeletionReason` | 1 NoLongerNeeded, 2 NotUseful, 3 PrivacyConcern, 4 CreatedByMistake, 5 Other — `public_users.deletion_reason` (2026-09-08) |

---

### 2026-09-07 da qo'shilgan — maktab kodi (migratsiya `AddSchoolEntryCode`)

`schools.entry_code varchar(8)` (nullable) + `ux_schools_entry_code`. **Ikki bosqichli, buzilmaydigan:**
ustun nullable qo'shiladi → mavjud `kind = 1` maktablar (o'chirilganlar ham) bazaning o'zida
unikal kod bilan to'ldiriladi (`DO $$ ... $$` bloki: pgcrypto `gen_random_bytes`, har bayt uchun
rejection sampling `< 248` → `% 31`, alifbo `ABCDEFGHJKMNPQRSTUVWXYZ23456789`, to'qnashishda
qayta urinish) → indeks. `SET NOT NULL` QILINMAYDI — ommaviy makon (`kind = 2`) `NULL` qoladi;
"`kind = 1` uchun doim bor" invariantini domen kafolatlaydi. `EntryCodeMigrationTests`.

### 2026-09-08 da qo'shilgan — o'chirish sababi (migratsiya `AddPublicUserDeletionReason`)

Egasining qarori: "ma'lumotimni o'chiring" so'ralganda sabab olinadi va superadminga
ko'rinadigan holda saqlanadi.

```sql
alter table public_users add column deletion_reason smallint null;
alter table public_users add column deletion_comment varchar(500) null;
```

Faqat qo'shimcha, nullable ustunlar — destruktiv o'zgarish yo'q, backfill shart emas (mavjud
o'chirilgan yozuvlarda ikkalasi `NULL` qoladi — eski sabab noma'lum, buzib ko'rsatilmaydi).
Anonimlashtirish (`telegram_id`/`username`/`first_name`/`last_name`/`photo_url` → `NULL`)
bu ikki ustunga TEGMAYDI (`PublicUser.MarkDeleted`).

### P52 da qo'shilgan — tarmoqlanuvchi so'rovnoma (migratsiya `AddBranchingSurvey`)

To'liq shartnoma — `docs/18-tarmoqlanuvchi-sorovnoma.md` §3. Faqat qo'shish va kengaytirish:
destruktiv qadam yo'q, shu sabab bir bosqichda bajarildi (`docs/05` §4 siyosati).

```sql
-- Yangi jadval: anketa bo'limlari (1-BO'LIM, 2-A, 2-B, 2-C, 3-BO'LIM …)
CREATE TABLE question_sections (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    test_definition_id uuid NOT NULL REFERENCES test_definitions(id) ON DELETE CASCADE,
    code               varchar(20)  NOT NULL,
    title_uz           varchar(200) NOT NULL,
    description_uz     varchar(1000),
    display_order      int          NOT NULL,
    visibility_rule    jsonb                        -- ko'rsatish sharti; NULL = shartsiz
);
CREATE UNIQUE INDEX ux_question_sections_test_code  ON question_sections (test_definition_id, code);
CREATE INDEX        ix_question_sections_test_order ON question_sections (test_definition_id, display_order);

-- questions: bo'lim, shart va turga xos sozlamalar
ALTER TABLE questions
    ADD COLUMN section_id      uuid REFERENCES question_sections(id) ON DELETE SET NULL,
    ADD COLUMN visibility_rule jsonb,
    ADD COLUMN placeholder     varchar(200),
    ADD COLUMN input_pattern   varchar(200),        -- regex (ShortText/Phone)
    ADD COLUMN max_length      int,
    ADD COLUMN min_selections  int,                 -- MultiChoice
    ADD COLUMN max_selections  int;
CREATE INDEX ix_questions_section ON questions (section_id, display_order);

-- answers: uch xil javob shakli
ALTER TABLE answers
    ALTER COLUMN raw_value DROP NOT NULL,           -- matn/ko'p tanlovda NULL
    ADD COLUMN text_value      text,
    ADD COLUMN selected_values jsonb;               -- int[]

-- Aynan BITTA shakl to'ldirilgan bo'lishi shart (domen invariantining DB egizagi)
ALTER TABLE answers ADD CONSTRAINT ck_answers_shape CHECK (
    (raw_value IS NOT NULL)::int
  + (text_value IS NOT NULL)::int
  + (selected_values IS NOT NULL)::int = 1
);
```

> `ck_answers_shape` SQLite sinov muhitida (`EnsureCreated`) `::int` sintaksisini
> tushunmaydi — u yerda mantiqan bir xil `CASE WHEN` ekvivalenti ishlatiladi
> (`AppDbContext.OnModelCreating`, provayder bo'yicha shoxlanish). Postgres modeliga
> ta'sir qilmaydi.

**`visibility_rule` jsonb shakli** (enumlar — satr, camelCase):

```json
{ "match": "All",
  "conditions": [ { "questionCode": "Q1_6", "operator": "Equals", "values": [1] } ] }
```

Shartda savol **kodi** ishlatiladi, ID emas (`docs/18` B-5) — jsonb o'qiladigan bo'lib
qoladi va import/eksport aylanmasi buzilmaydi. Shart faqat `display_order` kichikroq
savolga havola qila oladi (B-4), bu nashr validatsiyasida `VISIBILITY_FORWARD_REFERENCE`
bilan qulflangan.

---

## 4. Migratsiya siyosati

1. Har o'zgarish — **EF Core migration**, qo'lda SQL yozilmaydi (yuqoridagi DDL — mos yozuvlar manbai).
   `dotnet ef migrations add <Nom> -p src/StudentRoadMap.Infrastructure -s src/StudentRoadMap.Api`
2. Migratsiya nomi ma'noli: `AddReliabilityScoreToAssessment`.
3. **Destruktiv** o'zgarish (ustun o'chirish/tip almashtirish) alohida migratsiyada, avval
   yangi ustun qo'shiladi → ma'lumot ko'chiriladi → keyingi relizda eskisi o'chiriladi.
4. Seed ma'lumot (test bankiga tegishli) — migratsiyada emas, `DbSeeder` da idempotent
   (`Code` bo'yicha upsert), start-upda `SEED_ON_STARTUP=true` bo'lsa ishlaydi.
   Seed'dan kelgan 4 metodika va ularning savollari `is_system = true` bilan belgilanadi —
   superadmin ularni o'chira olmaydi va shkalasini o'zgartira olmaydi (BR-8).
   Superadmin yaratgan anketalar `kind = 2`, `is_system = false` va seeder ularga tegmaydi.
5. Production'da `Database.Migrate()` avtomatik emas — alohida `migrate` konteyner/step.

---

## 5. Ishlash bo'yicha eslatmalar

- Admin ro'yxatlarida `students` jadvalidagi **snapshot** ustunlar ishlatiladi — JOIN kerak emas.
- `answers` eng katta jadval (190 qator × sessiya). Kerak bo'lsa `assessment_test_id` bo'yicha
  `BRIN` yoki oylik partitsiya (v2).
- `jsonb` ustunlarga GIN indeks faqat real ehtiyoj bo'lsa qo'shiladi (masalan tip bo'yicha
  filtr — buning o'rniga `test_results.result_code` ustuni ishlatiladi).
- Dashboard statistikasi 60 soniyaga keshlanadi (`IMemoryCache`), og'ir `GROUP BY` har so'rovda emas.
- Ulanish: `Npgsql`, `Pooling=true;Maximum Pool Size=50`, `CommandTimeout=30`.

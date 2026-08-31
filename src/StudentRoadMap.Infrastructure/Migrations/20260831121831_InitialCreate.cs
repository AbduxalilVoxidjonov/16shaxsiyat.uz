using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRoadMap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    username = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    role = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    totp_secret_encrypted = table.Column<string>(type: "text", nullable: true),
                    totp_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    failed_login_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    email_lower = table.Column<string>(type: "text", nullable: true, computedColumnSql: "lower(email)", stored: true),
                    username_lower = table.Column<string>(type: "text", nullable: true, computedColumnSql: "lower(username)", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_provider_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    provider = table.Column<short>(type: "smallint", nullable: false),
                    display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    api_key_encrypted = table.Column<string>(type: "text", nullable: true),
                    model = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    base_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    max_output_tokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 4096),
                    temperature = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0.4m),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    fallback_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    last_checked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_check_status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_provider_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "career_map",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    holland_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    field_name_uz = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description_uz = table.Column<string>(type: "text", nullable: true),
                    example_professions_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'"),
                    relevance_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_career_map", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prompt_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    system_text = table.Column<string>(type: "text", nullable: false),
                    user_text = table.Column<string>(type: "text", nullable: false),
                    json_schema = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prompt_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "schools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    school_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    contact_person = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    access_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    access_code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    daily_registration_limit = table.Column<int>(type: "integer", nullable: false, defaultValue: 500),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schools", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "test_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name_uz = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description_uz = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    estimated_minutes = table.Column<int>(type: "integer", nullable: false),
                    shuffle_questions = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    page_size = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    kind = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    scoring_strategy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)2),
                    created_by_admin_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "type_catalog",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    name_uz = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    short_description_uz = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    long_description_uz = table.Column<string>(type: "text", nullable: false),
                    strengths_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'"),
                    growth_areas_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'"),
                    career_hints_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_type_catalog", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_ip_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_admin_users_admin_user_id",
                        column: x => x.admin_user_id,
                        principalTable: "admin_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    gender = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    grade = table.Column<int>(type: "integer", nullable: false),
                    class_letter = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    parent_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    consent_given_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_personality_type = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    last_maturity_index = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    last_activity_index = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    last_activity_level = table.Column<short>(type: "smallint", nullable: true),
                    last_holland_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    needs_attention = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_assessment_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_assessment_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_students", x => x.id);
                    table.CheckConstraint("ck_students_grade", "grade BETWEEN 1 AND 11");
                    table.ForeignKey(
                        name: "fk_students_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    test_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    text_uz = table.Column<string>(type: "text", nullable: false),
                    text_ru = table.Column<string>(type: "text", nullable: true),
                    text_en = table.Column<string>(type: "text", nullable: true),
                    question_type = table.Column<short>(type: "smallint", nullable: false),
                    scale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    scale_direction = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    weight = table.Column<decimal>(type: "numeric(4,2)", nullable: false, defaultValue: 1.0m),
                    is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_questions", x => x.id);
                    table.ForeignKey(
                        name: "fk_questions_test_definitions_test_definition_id",
                        column: x => x.test_definition_id,
                        principalTable: "test_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    language_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "uz"),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reliability_score = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    reliability_flag = table.Column<short>(type: "smallint", nullable: true),
                    ip_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    total_duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessments", x => x.id);
                    table.ForeignKey(
                        name: "fk_assessments_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_assessments_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "answer_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text_uz = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false),
                    scale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_answer_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_answer_options_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ai_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<short>(type: "smallint", nullable: false),
                    model = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    prompt_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    request_payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    response_json = table.Column<string>(type: "jsonb", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    personality_portrait = table.Column<string>(type: "text", nullable: true),
                    strengths_json = table.Column<string>(type: "jsonb", nullable: true),
                    growth_areas_json = table.Column<string>(type: "jsonb", nullable: true),
                    recommendations_json = table.Column<string>(type: "jsonb", nullable: true),
                    career_suggestions_json = table.Column<string>(type: "jsonb", nullable: true),
                    teacher_notes = table.Column<string>(type: "text", nullable: true),
                    parent_notes = table.Column<string>(type: "text", nullable: true),
                    attention_flags_json = table.Column<string>(type: "jsonb", nullable: true),
                    input_tokens = table.Column<int>(type: "integer", nullable: true),
                    output_tokens = table.Column<int>(type: "integer", nullable: true),
                    estimated_cost_usd = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    attempt_number = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_current = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_analyses", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_analyses_assessments_assessment_id",
                        column: x => x.assessment_id,
                        principalTable: "assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assessment_tests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    answered_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_count = table.Column<int>(type: "integer", nullable: false),
                    question_order_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessment_tests", x => x.id);
                    table.ForeignKey(
                        name: "fk_assessment_tests_assessments_assessment_id",
                        column: x => x.assessment_id,
                        principalTable: "assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_assessment_tests_test_definitions_test_definition_id",
                        column: x => x.test_definition_id,
                        principalTable: "test_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assessment_test_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raw_value = table.Column<int>(type: "integer", nullable: false),
                    selected_option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    revision_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_answers", x => x.id);
                    table.ForeignKey(
                        name: "fk_answers_answer_options_selected_option_id",
                        column: x => x.selected_option_id,
                        principalTable: "answer_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_answers_assessment_tests_assessment_test_id",
                        column: x => x.assessment_test_id,
                        principalTable: "assessment_tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_answers_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "test_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    assessment_test_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    test_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    result_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    raw_scores_json = table.Column<string>(type: "jsonb", nullable: false),
                    normalized_scores_json = table.Column<string>(type: "jsonb", nullable: false),
                    levels_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'"),
                    composite_index = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    flags_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'"),
                    scoring_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    test_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_results_assessment_tests_assessment_test_id",
                        column: x => x.assessment_test_id,
                        principalTable: "assessment_tests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_test_results_assessments_assessment_id",
                        column: x => x.assessment_id,
                        principalTable: "assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_admin_users_email",
                table: "admin_users",
                column: "email_lower",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_admin_users_username",
                table: "admin_users",
                column: "username_lower",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_analyses_assessment",
                table: "ai_analyses",
                columns: new[] { "assessment_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_ai_analyses_current",
                table: "ai_analyses",
                column: "assessment_id",
                unique: true,
                filter: "is_current = true");

            migrationBuilder.CreateIndex(
                name: "ux_ai_provider_default",
                table: "ai_provider_configs",
                column: "is_default",
                unique: true,
                filter: "is_default = true");

            migrationBuilder.CreateIndex(
                name: "ux_ai_provider_kind",
                table: "ai_provider_configs",
                column: "provider",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_answer_options_question",
                table: "answer_options",
                columns: new[] { "question_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_answers_question_id",
                table: "answers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_answers_selected_option_id",
                table: "answers",
                column: "selected_option_id");

            migrationBuilder.CreateIndex(
                name: "ux_answers_test_question",
                table: "answers",
                columns: new[] { "assessment_test_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assessment_tests_test_definition_id",
                table: "assessment_tests",
                column: "test_definition_id");

            migrationBuilder.CreateIndex(
                name: "ux_assessment_tests",
                table: "assessment_tests",
                columns: new[] { "assessment_id", "test_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assessments_school_status",
                table: "assessments",
                columns: new[] { "school_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_assessments_status_started",
                table: "assessments",
                columns: new[] { "status", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_assessments_student",
                table: "assessments",
                columns: new[] { "student_id", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_assessments_token",
                table: "assessments",
                column: "session_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_career_map_code",
                table: "career_map",
                columns: new[] { "holland_code", "relevance_order" });

            migrationBuilder.CreateIndex(
                name: "ux_prompt_templates",
                table: "prompt_templates",
                columns: new[] { "key", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_questions_test_order",
                table: "questions",
                columns: new[] { "test_definition_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ux_questions_code",
                table: "questions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_admin_user_id",
                table: "refresh_tokens",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_schools_name_trgm",
                table: "schools",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_schools_region_dist",
                table: "schools",
                columns: new[] { "region", "district" });

            migrationBuilder.CreateIndex(
                name: "ux_schools_slug",
                table: "schools",
                column: "slug",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ux_schools_token",
                table: "schools",
                column: "access_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_students_attention",
                table: "students",
                column: "needs_attention",
                filter: "needs_attention = true");

            // Xom SQL — istisno (`docs/05` §5 talabi: `NULLS LAST`, hech qachon test
            // topshirmagan o'quvchi "so'nggi faollik" ro'yxati OXIRIDA qolsin). EF Core fluent
            // API'da index NULLS tartibini sozlash imkoni yo'q (`docs/05` §4 "qo'lda SQL
            // migratsiya yozilmaydi" qoidasiga PM ruxsati bilan shu bitta band uchun istisno).
            migrationBuilder.Sql(
                "CREATE INDEX ix_students_last_at ON students (last_assessment_at DESC NULLS LAST);");

            migrationBuilder.CreateIndex(
                name: "ix_students_name_trgm",
                table: "students",
                column: "full_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_students_school_grade",
                table: "students",
                columns: new[] { "school_id", "grade" });

            migrationBuilder.CreateIndex(
                name: "ux_students_identity",
                table: "students",
                columns: new[] { "school_id", "normalized_name", "birth_date" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_test_definitions_active",
                table: "test_definitions",
                columns: new[] { "is_active", "display_order" },
                filter: "status = 2");

            migrationBuilder.CreateIndex(
                name: "ux_test_definitions_code",
                table: "test_definitions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_test_results_assessment",
                table: "test_results",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "ix_test_results_code",
                table: "test_results",
                columns: new[] { "test_code", "result_code" });

            migrationBuilder.CreateIndex(
                name: "ux_test_results_test",
                table: "test_results",
                column: "assessment_test_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_analyses");

            migrationBuilder.DropTable(
                name: "ai_provider_configs");

            migrationBuilder.DropTable(
                name: "answers");

            migrationBuilder.DropTable(
                name: "career_map");

            migrationBuilder.DropTable(
                name: "prompt_templates");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "test_results");

            migrationBuilder.DropTable(
                name: "type_catalog");

            migrationBuilder.DropTable(
                name: "answer_options");

            migrationBuilder.DropTable(
                name: "admin_users");

            migrationBuilder.DropTable(
                name: "assessment_tests");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "assessments");

            migrationBuilder.DropTable(
                name: "test_definitions");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "schools");
        }
    }
}

#!/usr/bin/env bash
# scripts/restore-test.sh — zaxiradan tiklanishni SINAYDI (docs/13 §7: "Tiklanmagan
# backup — backup emas"). Har chorakda ishga tushiriladi (yoki CI'da qo'lda).
#
# XAVFSIZ: ishlab turgan `studentroadmap` bazasiga TEGMAYDI. Zaxira alohida, vaqtinchalik
# bazaga (`srm_restore_test`) tiklanadi, qatorlar soni tekshiriladi, so'ng o'sha vaqtinchalik
# baza o'chiriladi. Shu bilan haqiqiy tiklanish yo'li (`pg_restore`) sinovdan o'tkaziladi,
# lekin production ma'lumoti xavf ostida qolmaydi.
#
# Ishlatilishi:
#   ./scripts/restore-test.sh                       # ./backups/ dagi ENG YANGI faylni sinaydi
#   ./scripts/restore-test.sh backups/srm_2026-09-02_030000.dump
#
# --- Haqiqiy falokatdan keyingi tiklash (qo'lda, bu skript BUNI qilmaydi) -----------------
#   docker compose up -d db
#   docker compose exec -T db dropdb -U srm studentroadmap      # ESKI ma'lumot o'chadi!
#   docker compose exec -T db createdb -U srm studentroadmap
#   docker compose exec -T db pg_restore -U srm -d studentroadmap --no-owner < backups/srm_XXXX.dump
#   docker compose up -d api   # api --migrate SHART EMAS: pg_restore sxema+ma'lumotni birga tiklaydi
# -------------------------------------------------------------------------------------------

set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

BACKUP_DIR="${BACKUP_DIR:-./backups}"
DB_SERVICE="${DB_SERVICE:-db}"
DB_USER="${DB_USER:-srm}"
TEST_DB="${TEST_DB:-srm_restore_test}"

BACKUP_FILE="${1:-}"
if [ -z "$BACKUP_FILE" ]; then
  BACKUP_FILE="$(find "$BACKUP_DIR" -name 'srm_*.dump' -type f | sort | tail -n1)"
fi
if [ -z "$BACKUP_FILE" ] || [ ! -f "$BACKUP_FILE" ]; then
  echo "XATO: sinaladigan zaxira fayli topilmadi (${BACKUP_DIR} bo'sh?). Avval ./scripts/backup.sh ishga tushiring." >&2
  exit 1
fi

if ! docker compose ps --status running --services 2>/dev/null | grep -qx "$DB_SERVICE"; then
  echo "XATO: '$DB_SERVICE' servisi ishlamayapti (docker compose up -d db)." >&2
  exit 1
fi

echo "Sinov bazasi: ${TEST_DB}  ·  Zaxira fayli: ${BACKUP_FILE}"

cleanup() {
  docker compose exec -T "$DB_SERVICE" dropdb -U "$DB_USER" --if-exists "$TEST_DB" >/dev/null 2>&1 || true
}
trap cleanup EXIT

# Oldingi muvaffaqiyatsiz urinishdan qolgan bo'lsa — tozalab boshlaymiz.
docker compose exec -T "$DB_SERVICE" dropdb -U "$DB_USER" --if-exists "$TEST_DB"
docker compose exec -T "$DB_SERVICE" createdb -U "$DB_USER" "$TEST_DB"

echo "Tiklanmoqda..."
docker compose exec -T "$DB_SERVICE" pg_restore -U "$DB_USER" -d "$TEST_DB" --no-owner < "$BACKUP_FILE"

echo "Tekshirilmoqda (jadval soni va asosiy jadvallardagi qatorlar)..."
TABLE_COUNT=$(docker compose exec -T "$DB_SERVICE" psql -U "$DB_USER" -d "$TEST_DB" -tAc \
  "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public';" | tr -d '[:space:]')
MIGRATIONS_ROW=$(docker compose exec -T "$DB_SERVICE" psql -U "$DB_USER" -d "$TEST_DB" -tAc \
  "SELECT count(*) FROM \"__EFMigrationsHistory\";" | tr -d '[:space:]')

echo "  jadvallar: ${TABLE_COUNT}  ·  qo'llangan migratsiyalar: ${MIGRATIONS_ROW}"

if [ "$TABLE_COUNT" -lt 1 ] || [ "$MIGRATIONS_ROW" -lt 1 ]; then
  echo "FAIL: tiklangan bazada kutilgan jadval/migratsiya tarixi topilmadi." >&2
  exit 1
fi

echo "PASS: zaxira '${BACKUP_FILE}' muvaffaqiyatli tiklandi va tekshirildi (${TEST_DB} avtomatik o'chiriladi)."

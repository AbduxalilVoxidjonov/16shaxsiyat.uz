#!/usr/bin/env bash
# scripts/backup.sh — PostgreSQL to'liq zaxira nusxasi (docs/13-deploy-va-infratuzilma.md §7).
#
# `db` konteyneri ichidagi `pg_dump` orqali ishlaydi — host mashinada Postgres mijoz
# vositalari o'rnatilgan bo'lishi shart emas. Natija maxsus (`-Fc`) formatda saqlanadi —
# faqat shu format `pg_restore` bilan tanlab tiklashga (jadval/sxema bo'yicha) imkon beradi.
#
# Ishlatilishi:
#   ./scripts/backup.sh                # standart: ./backups/ papkaga
#   BACKUP_DIR=/mnt/backups ./scripts/backup.sh
#
# Cron namunasi (har kuni 03:00, UTC+5 — docs/13 §7):
#   0 3 * * * cd /opt/16shaxsiyat && ./scripts/backup.sh >> /var/log/srm-backup.log 2>&1
#
# Eslatma: bu skript **serverda**, `docker compose` ishlayotgan katalogda ishga tushiriladi.
# Sirlar bu yerda YO'Q — `db` konteyneri ichida allaqachon `.env` orqali sozlangan.

set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

BACKUP_DIR="${BACKUP_DIR:-./backups}"
RETENTION_DAYS="${RETENTION_DAYS:-30}"
DB_SERVICE="${DB_SERVICE:-db}"
DB_USER="${DB_USER:-srm}"
DB_NAME="${DB_NAME:-studentroadmap}"
STAMP="$(date +%F_%H%M%S)"
OUT_FILE="${BACKUP_DIR}/srm_${STAMP}.dump"

mkdir -p "$BACKUP_DIR"

if ! docker compose ps --status running --services 2>/dev/null | grep -qx "$DB_SERVICE"; then
  echo "XATO: '$DB_SERVICE' servisi ishlamayapti (docker compose up -d db)." >&2
  exit 1
fi

echo "Zaxira olinmoqda: ${DB_NAME} → ${OUT_FILE}"
# `-T`: pseudo-TTY yo'q — natija to'g'ridan-to'g'ri stdout'ga, faylga buzilmasdan yoziladi.
docker compose exec -T "$DB_SERVICE" pg_dump -Fc -U "$DB_USER" "$DB_NAME" > "$OUT_FILE"

SIZE=$(du -h "$OUT_FILE" | cut -f1)
echo "Tayyor: ${OUT_FILE} (${SIZE})"

# Eskirgan zaxiralarni tozalash (docs/13 §7: 30 kun).
DELETED=$(find "$BACKUP_DIR" -name 'srm_*.dump' -mtime "+${RETENTION_DAYS}" -print -delete | wc -l | tr -d ' ')
if [ "$DELETED" != "0" ]; then
  echo "${DELETED} ta eskirgan zaxira (> ${RETENTION_DAYS} kun) o'chirildi."
fi

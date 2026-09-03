using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentRoadMap.Infrastructure.Migrations
{
    /// <summary>
    /// `admin_users.concurrency_stamp` ustunidan DOIMIY `DEFAULT`ni olib tashlaydi va
    /// nol-GUID qolgan qatorlarni haqiqiy (noyob) qiymat bilan tiklaydi.
    ///
    /// SABAB (migratsiya sinov qatlami topgan, `docs/13` §8.1 "Ochiq qarz"):
    /// `20260902070721_AddAuditLogsAndTotpSupport` mavjud jadvalga `NOT NULL uuid` ustun
    /// qo'shganda EF Core `defaultValue: new Guid("000…0")` yozgan. EF buni BIR MARTALIK
    /// backfill deb emas, ustunning DOIMIY `DEFAULT`i sifatida chiqargan. Oqibati ikkita:
    ///   1) `concurrency_stamp` — optimistik konkurentlik tokeni; qiymatsiz `INSERT`
    ///      (xom SQL, boshqa migratsiya, qo'lda tuzatish) jimgina nol-GUID yozadi va token
    ///      o'z vazifasini bajarmaydi — bir vaqtda ikki tahrir bir-birini bosib ketishi mumkin.
    ///      `EnsureCreated()` bilan qurilgan sinov bazasida bu DEFAULT yo'q edi, shu sabab
    ///      testlar buni HECH QACHON ko'rmasdi.
    ///   2) Migratsiya paytida mavjud barcha admin qatorlari BIR XIL stamp oldi.
    ///
    /// DESTRUKTIV EMAS: `DROP DEFAULT` ma'lumot yo'qotmaydi; ustun turi, `NOT NULL` holati
    /// va mavjud (nol bo'lmagan) qiymatlar tegilmaydi. Modelda (`AdminUserConfiguration`)
    /// bu ustun uchun default umuman e'lon qilinmagan — ya'ni migratsiya sxemani MODELGA
    /// yaqinlashtiradi, undan uzoqlashtirmaydi (`has-pending-model-changes` bo'sh qoladi).
    /// </summary>
    public partial class DropConcurrencyStampDefault : Migration
    {
        private const string ZeroGuid = "00000000-0000-0000-0000-000000000000";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Avval DEFAULT olib tashlanadi — shundan keyin qiymatsiz `INSERT` 23502
            //    (not-null violation) bilan RAD ETILADI, ya'ni xato jimgina o'tib ketmaydi.
            migrationBuilder.Sql("""
                ALTER TABLE admin_users ALTER COLUMN concurrency_stamp DROP DEFAULT;
                """);

            // 2) Keyin default tufayli bir xil (nol) stamp olgan mavjud qatorlar tiklanadi.
            //    `gen_random_uuid()` — volatil funksiya, HAR QATOR uchun qayta hisoblanadi,
            //    ya'ni har bir admin NOYOB token oladi. Idempotent: `WHERE` sharti tufayli
            //    qayta ishga tushirilsa 0 qatorga tegadi va allaqachon to'g'ri bo'lgan
            //    stamp'larni buzmaydi.
            migrationBuilder.Sql($"""
                UPDATE admin_users
                SET concurrency_stamp = gen_random_uuid()
                WHERE concurrency_stamp = '{ZeroGuid}'::uuid;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Faqat sxema qadami qaytariladi (`Up`ning aynan teskarisi). Tiklangan stamp
            // qiymatlari ATAYLAB nol-GUIDga qaytarilmaydi — bu ma'lumotni (va konkurentlik
            // himoyasini) yo'qotish bo'lardi; boshqa migratsiyalar `Down()`ida ham shu tamoyil.
            migrationBuilder.Sql($"""
                ALTER TABLE admin_users
                ALTER COLUMN concurrency_stamp SET DEFAULT '{ZeroGuid}'::uuid;
                """);
        }
    }
}

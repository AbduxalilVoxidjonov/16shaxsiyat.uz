namespace StudentRoadMap.Migrations.Tests.Infrastructure;

/// <summary>
/// Docker talab qiladigan test. Docker topilmasa — test ANIQ SABAB bilan `Skip` bo'ladi
/// (jimgina "yashil o'tish" EMAS, `docs/12` §5.1). `SRM_REQUIRE_DOCKER=1` bo'lsa
/// (CI) `Skip` umuman qo'yilmaydi — test ishga tushadi va tushunarli xato bilan yiqiladi.
///
/// Qo'shimcha paketsiz (`Xunit.SkippableFact` kerak emas): `Skip` xususiyati kashfiyot
/// (discovery) paytida, konstruktorda o'rnatiladi.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!DockerEnvironment.IsAvailable && !DockerEnvironment.IsRequired)
        {
            Skip = "Docker topilmadi — migratsiya sinovi o'tkazib yuborildi. Sabab: "
                + DockerEnvironment.UnavailableReason
                + " | Docker'ni ishga tushiring yoki CI kabi "
                + DockerEnvironment.RequireEnvironmentVariable
                + "=1 bilan majburiy rejimda ishga tushiring (`docs/12` §5.1).";
        }
    }
}

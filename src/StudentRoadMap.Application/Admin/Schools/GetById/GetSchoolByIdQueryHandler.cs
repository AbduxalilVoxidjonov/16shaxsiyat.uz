using MediatR;
using StudentRoadMap.Application.Admin.Schools.LinkHealth;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.Admin.Schools.GetById;

/// <summary>`docs/07` 3.1-bo'lim: "Batafsil + statistika (o'quvchi soni, yakunlangan sessiyalar)". Read-only.</summary>
internal sealed class GetSchoolByIdQueryHandler : IRequestHandler<GetSchoolByIdQuery, Result<AdminSchoolDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;
    private readonly IAppSettings _appSettings;
    private readonly IQrCodeGenerator _qrCodeGenerator;

    public GetSchoolByIdQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor, IAppSettings appSettings, IQrCodeGenerator qrCodeGenerator)
    {
        _context = context;
        _executor = executor;
        _appSettings = appSettings;
        _qrCodeGenerator = qrCodeGenerator;
    }

    public async Task<Result<AdminSchoolDetailDto>> Handle(GetSchoolByIdQuery request, CancellationToken cancellationToken)
    {
        var school = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.Schools).SchoolsOnly().Where(s => s.Id == request.Id),
            cancellationToken).ConfigureAwait(false);

        if (school is null)
        {
            return Result.Failure<AdminSchoolDetailDto>(new Error(ProblemCodes.NotFound, "Maktab topilmadi."));
        }

        var stats = await SchoolMapping.ComputeStatsAsync(_context, _executor, school.Id, cancellationToken).ConfigureAwait(false);

        var linkHealth = await SchoolLinkHealthEvaluator.EvaluateOneAsync(_context, _executor, school.Id, cancellationToken).ConfigureAwait(false);


        return Result.Success(SchoolMapping.ToDetailDto(school, _appSettings, _qrCodeGenerator, stats, linkHealth));
    }
}

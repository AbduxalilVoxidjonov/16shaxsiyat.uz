using MediatR;
using StudentRoadMap.Application.Common.Interfaces;
using StudentRoadMap.Application.Common.Models;
using StudentRoadMap.Application.PublicUsers.TelegramLogin;
using StudentRoadMap.Domain.Common;

namespace StudentRoadMap.Application.PublicUsers.GetProfile;

internal sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, Result<PublicUserDto>>
{
    private readonly IAppDbContext _context;
    private readonly IAsyncQueryExecutor _executor;

    public GetMyProfileQueryHandler(IAppDbContext context, IAsyncQueryExecutor executor)
    {
        _context = context;
        _executor = executor;
    }

    public async Task<Result<PublicUserDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        // Global filtr (`DeletedAt == null`) o'chirilgan akkauntni YASHIRADI — access token
        // hali amal qilayotgan bo'lsa ham (30 daqiqa) profil ochilmaydi.
        var user = await _executor.FirstOrDefaultAsync(
            _context.AsNoTracking(_context.PublicUsers).Where(u => u.Id == request.PublicUserId),
            cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            return Result.Failure<PublicUserDto>(new Error(ProblemCodes.PublicUserDeleted, "Bu akkaunt mavjud emas."));
        }

        return Result.Success(new PublicUserDto(
            user.Id,
            user.Username,
            user.FirstName,
            user.LastName,
            user.PhotoUrl,
            user.CreatedAt,
            user.LastLoginAt));
    }
}

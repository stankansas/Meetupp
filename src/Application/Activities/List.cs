using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Activities;

public static class List {

  internal class Handler : IRequestHandler<Query, PaginatedList<ActivityDto>> {
    private readonly IAppDbContext dbContext;
    private readonly ILogger<Handler> logger;
    private readonly IMapper mapper;
    private readonly IIdentityService currUserService;

    public Handler(IAppDbContext dbContext, IMapper mapper, IIdentityService currUserService, ILogger<Handler> logger) {
      this.dbContext = dbContext;
      this.currUserService = currUserService;
      this.mapper = mapper;
      this.logger = logger;
    }

    public async Task<PaginatedList<ActivityDto>> Handle(Query request, CancellationToken ct) {
      var queryable = dbContext.Activities
        .AsNoTracking()
        .Include(x => x.Comments).ThenInclude(x => x.CreatedBy).ThenInclude(x => x.Photos)
        .Include(x => x.UserActivities).ThenInclude(x => x.AppUser).ThenInclude(x => x.Photos)
        .Where(x => x.Date >= request.StartDate)
        .OrderBy(x => x.Date)
        .AsQueryable();

      if (request.IsGoing && !request.IsHost) {
        queryable = queryable.Where(x => x.UserActivities.Any(a => a.AppUser.Id == currUserService.GetCurrUserId()));
      }

      if (request.IsHost && !request.IsGoing) {
        queryable = queryable.Where(x => x.UserActivities.Any(a => a.AppUser.Id == currUserService.GetCurrUserId() && a.IsHost));
      }

      var loggedInUser = await currUserService.GetCurrUserProfileAsync(ct);

      return await queryable
        .ProjectTo<ActivityDto>(mapper.ConfigurationProvider, new { currUser = loggedInUser })
        .PaginatedListAsync(request.Offset, request.Limit);
    }

    public async Task<List<ActivityDto>> Handle111(Query request, CancellationToken ct) {
      // try {
      //   request.ToString();

      //   for (var i = 0; i < 5; i++) {
      //     ct.ThrowIfCancellationRequested();
      //     await Task.Delay(1000, ct);
      //     logger.LogInformation($"Task {i} has completed");
      //   }
      // } catch (Exception ex) when (ex is TaskCanceledException) {
      //   logger.LogInformation("Task was cancelled.");
      // }
      await Task.CompletedTask;

      return mapper.Map<List<Activity>, List<ActivityDto>>(null);
    }
  }

  public sealed record Query(
    int Limit,
    int Offset,
    bool IsGoing,
    bool IsHost,
    DateTime StartDate) : IRequest<PaginatedList<ActivityDto>>;

}
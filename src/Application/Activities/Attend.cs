using Application.Auth;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;
using static Application.Errors.RestException;

namespace Application.Activities;

public static class Attend {
  public class Command : IRequest {
    public Guid Id { get; set; }
  }

  public class Handler : IRequestHandler<Command> {
    private readonly DataContext dbContext;
    private readonly ICurrUserService currUserService;

    public Handler(DataContext dbContext, ICurrUserService currUserService) {
      this.dbContext = dbContext;
      this.currUserService = currUserService;
    }

    public async Task<Unit> Handle(Command request, CancellationToken ct) {
      var activity = await dbContext.Activities.SingleOrDefaultAsync(x => x.Id == request.Id, ct);
      ThrowIfNotFound(activity, new { Activity = "Not found" });

      var attendance = await dbContext.UserActivities
        .SingleOrDefaultAsync(x => x.ActivityId == activity.Id && x.AppUserId == currUserService.UserId, ct);
      ThrowIfBadRequest(attendance != null, new { Attendance = "Already attending this activity" });

      attendance = UserActivity.Create(currUserService.UserId, activity.Id);

      dbContext.UserActivities.Add(attendance);

      var success = await dbContext.SaveChangesAsync(ct) > 0;

      if (success) return Unit.Value;

      throw new Exception("Problem saving attendance");
    }
  }
}
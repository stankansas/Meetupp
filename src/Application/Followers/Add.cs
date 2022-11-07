using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Application.Auth;
using Application.Errors;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Followers;

public class Add {
  public class Command : IRequest {
    public string Username { get; set; }
  }

  public class Handler : IRequestHandler<Command> {
    private readonly DataContext dbContext;
    private readonly IUserAccessor userAccessor;
    public Handler(DataContext dbContext, IUserAccessor userAccessor) {
      this.userAccessor = userAccessor;
      this.dbContext = dbContext;
    }

    public async Task<Unit> Handle(Command request, CancellationToken ct) {
      var observer = await dbContext.Users.SingleOrDefaultAsync(x => x.UserName == userAccessor.GetCurrentUsername(), ct);

      var target = await dbContext.Users.SingleOrDefaultAsync(x => x.UserName == request.Username, ct);

      if (target == null)
        throw new RestException(HttpStatusCode.NotFound, new { User = "Not found" });

      var following = await dbContext.Followings.SingleOrDefaultAsync(x => x.ObserverId == observer.Id && x.TargetId == target.Id, ct);

      if (following != null)
        throw new RestException(HttpStatusCode.BadRequest, new { User = "You are already following this user" });

      if (following == null) {
        following = new UserFollowing {
          Observer = observer,
          Target = target
        };

        dbContext.Followings.Add(following);
      }

      var success = await dbContext.SaveChangesAsync(ct) > 0;

      if (success) return Unit.Value;

      throw new Exception("Problem saving followX");
    }
  }
}
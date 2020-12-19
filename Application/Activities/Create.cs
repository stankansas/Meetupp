using System;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using MediatR;
using Persistence;

namespace Application.Activities
{
    public class Create
    {
        public class Command : IRequest<Guid>
        {
            public Guid Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public DateTime Date { get; set; }
            public string City { get; set; }
            public string Venue { get; set; }
        }

        public class Handler : IRequestHandler<Command, Guid>
        {
            private readonly FacebukDbContext _context;
            public Handler(FacebukDbContext context)
            {
                _context = context;
            }

            public async Task<Guid> Handle(Command request, CancellationToken cancellationToken)
            {
                var activity = new Activity
                {
                    Id = request.Id,
                    Title = request.Title,
                    Description = request.Description,
                    Category = request.Category,
                    Date = request.Date,
                    City = request.City,
                    Venue = request.Venue
                };
                
                //Dont use AddSync
                _context.Activities.Add(activity);
                var success = await _context.SaveChangesAsync() > 0;

                if (success) return activity.Id;

                throw new Exception("Problem Adding changes");
            }
        }
    }
}
using FluentValidation;
using NoteTakingApp.Core.Models;
using NoteTakingApp.Core.Extensions;
using NoteTakingApp.Core.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using NoteTakingApp.Core.DomainEvents;

namespace NoteTakingApp.API.Features.Tags
{
    public class SaveTagCommand
    {
        public class Validator: AbstractValidator<Request> {
            public Validator()
            {
                RuleFor(request => request.Tag.TagId).NotNull();
            }
        }

        public class Request : IRequest<Response> {
            public TagApiModel Tag { get; set; }
        }

        public class Response
        {			
            public int TagId { get; set; }
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            private readonly IAppDbContext _context;

            public Handler(IAppDbContext context) => _context = context;

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                var tag = await _context.Tags.FindAsync(request.Tag.TagId);

                if (tag == null) _context.Tags.Add(tag = new Tag());

                tag.Name = request.Tag.Name;

                tag.Slug = request.Tag.Name.GenerateSlug();

                tag.RaiseDomainEvent(new TagSavedDomainEvent(tag));

                await _context.SaveChangesAsync(cancellationToken);

                return new Response() { TagId = tag.TagId };
            }
        }
    }
}

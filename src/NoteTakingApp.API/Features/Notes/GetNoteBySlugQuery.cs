using NoteTakingApp.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NoteTakingApp.API.Features.Notes
{
    public class GetNoteBySlugQuery
    {
        public class Request : IRequest<Response> {
            public string Slug { get; set; }
        }

        public class Response
        {
            public NoteApiModel Note { get; set; }
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            private readonly IAppDbContext _context;

            public Handler(IAppDbContext context) => _context = context;

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
                => new Response()
                {
                    Note = NoteApiModel.FromNote(await _context.Notes
                        .Include(x => x.NoteTags)
                        .ThenInclude(nt => nt.Tag)                        
                        .Where(x => x.Slug == request.Slug)
                        .SingleAsync(cancellationToken))
                };
        }
    }
}

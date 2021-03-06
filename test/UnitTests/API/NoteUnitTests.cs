using NoteTakingApp.API.Features.Notes;
using NoteTakingApp.API.Features.Tags;
using NoteTakingApp.Core.Models;
using NoteTakingApp.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NoteTakingApp.Core.DomainEvents;

namespace UnitTests.API
{
    public class NoteUnitTests
    {     
        [Fact]
        public async Task ShouldHandleSaveNoteCommandRequest()
        {

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "ShouldHandleSaveNoteCommandRequest")
                .Options;

            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Publish(It.IsAny<NoteSavedDomainEvent>(), It.IsAny<CancellationToken>()))
                .Verifiable();

            using (var context = new AppDbContext(options, mediator.Object))
            {
                var handler = new SaveNoteCommand.Handler(context);

                var tag = new Tag();
                tag.Update("Angular");
                context.Tags.Add(tag);

                context.SaveChanges();

                var response = await handler.Handle(new SaveNoteCommand.Request()
                {
                    Note = new NoteApiModel()
                    {
                        Title = "Quinntyne",
                        Tags = new List<TagApiModel>() { new TagApiModel() { TagId = 1 } }
                    }
                }, default(CancellationToken));

                Assert.Equal(1, response.NoteId);
            }
        }

        [Fact]
        public async Task ShouldHandleGetNoteByIdQueryRequest()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "ShouldHandleGetNoteByIdQueryRequest")
                .Options;

            using (var context = new AppDbContext(options))
            {
                context.Notes.Add(new Note()
                {
                    NoteId = 1,
                    Title = "Quinntyne"
                });

                context.SaveChanges();

                var handler = new GetNoteByIdQuery.Handler(context);

                var response = await handler.Handle(new GetNoteByIdQuery.Request()
                {
                    NoteId = 1
                }, default(CancellationToken));

                Assert.Equal("Quinntyne", response.Note.Title);
            }
        }

        [Fact]
        public async Task ShouldHandleGetNoteBySlugQueryRequest()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "ShouldHandleGetNoteBySlugQueryRequest")
                .Options;

            using (var context = new AppDbContext(options))
            {
                var tag = new Tag();

                tag.Update("Angular");

                context.Tags.Add(tag);

                context.Notes.Add(new Note()
                {
                    NoteId = 1,
                    Title = "Quinntyne",
                    Slug = "quinntyne",
                    NoteTags = new List<NoteTag>() {
                        new NoteTag() { TagId = tag.TagId }
                    }
                });

                context.SaveChanges();

                var handler = new GetNoteBySlugQuery.Handler(context);

                var response = await handler.Handle(new GetNoteBySlugQuery.Request()
                {
                    Slug = "quinntyne"
                }, default(CancellationToken));

                Assert.Equal("Quinntyne", response.Note.Title);
                Assert.Single(response.Note.Tags);
            }
        }

        [Fact]
        public async Task ShouldHandleGetNotesQueryRequest()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "ShouldHandleGetNotesQueryRequest")
                .Options;

            using (var context = new AppDbContext(options))
            {
                context.Notes.Add(new NoteTakingApp.Core.Models.Note()
                {
                    NoteId = 1,
                    Title = "Quinntyne"                    
                });

                context.SaveChanges();

                var handler = new GetNotesQuery.Handler(context);

                var response = await handler.Handle(new GetNotesQuery.Request(), default(CancellationToken));

                Assert.Single(response.Notes);
            }
        }

        [Fact]
        public async Task ShouldHandleRemoveNoteCommandRequest()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Publish(It.IsAny<NoteRemovedDomainEvent>(), It.IsAny<CancellationToken>()))
                .Verifiable();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "ShouldHandleRemoveNoteCommandRequest")
                .Options;

            using (var context = new AppDbContext(options, mediator.Object))
            {
                context.Notes.Add(new Note()
                {
                    NoteId = 1,
                    Title = "Quinntyne",
                });

                context.SaveChanges();

                var handler = new RemoveNoteCommand.Handler(context);

                await handler.Handle(new RemoveNoteCommand.Request()
                {
                    NoteId =  1 
                }, default(CancellationToken));

                Assert.Equal(0, context.Notes.Count());
            }
        }

        [Fact]
        public async Task ShouldHandleUpdateNoteCommandRequest()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "ShouldHandleUpdateNoteCommandRequest")
                .Options;

            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Publish(It.IsAny<NoteSavedDomainEvent>(), It.IsAny<CancellationToken>()))
                .Verifiable();

            using (var context = new AppDbContext(options, mediator.Object))
            {
                context.Notes.Add(new Note()
                {
                    NoteId = 1,
                    Title = "Quinntyne"
                });

                context.SaveChanges();

                var handler = new SaveNoteCommand.Handler(context);

                var response = await handler.Handle(new SaveNoteCommand.Request()
                {
                    Note = new NoteApiModel()
                    {
                        NoteId = 1,
                        Title = "Quinntyne"
                    }
                }, default(CancellationToken));

                Assert.Equal(1, response.NoteId);
                Assert.Equal("Quinntyne", context.Notes.Single(x => x.NoteId == 1).Title);
            }
        }
    }
}

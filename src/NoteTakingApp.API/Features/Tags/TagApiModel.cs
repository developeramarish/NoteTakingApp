using NoteTakingApp.API.Features.Notes;
using NoteTakingApp.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace NoteTakingApp.API.Features.Tags
{
    public class TagApiModel
    {        
        public int TagId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public ICollection<NoteApiModel> Notes { get; set; } = new HashSet<NoteApiModel>();
        public static TagApiModel FromTag(Tag tag)
            => new TagApiModel
            {
                TagId = tag.TagId,
                Name = tag.Name,
                Slug = tag.Slug,
                Notes = tag.NoteTags.Select(x => NoteApiModel.FromNote(x.Note, false)).ToList()                
            };
    }
}

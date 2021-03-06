using Orchard.ContentManagement.MetaData.Models;
using Orchard.DependencyInjection;
using Orchard.DisplayManagement.Handlers;

namespace Orchard.ContentTypes.Editors
{
    public interface IContentTypeDefinitionDisplayDriver : IDisplayDriver<ContentTypeDefinition, BuildDisplayContext, BuildEditorContext, UpdateTypeEditorContext>, IDependency
    {
    }
}
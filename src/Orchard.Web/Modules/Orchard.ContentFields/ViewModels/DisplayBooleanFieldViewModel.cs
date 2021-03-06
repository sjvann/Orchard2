﻿using Orchard.ContentFields.Fields;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData.Models;

namespace Orchard.ContentFields.ViewModels
{
    public class DisplayBooleanFieldViewModel
    {
        public BooleanField Field { get; set; }
        public ContentPart Part { get; set; }
        public ContentPartFieldDefinition PartFieldDefinition { get; set; }
    }
}

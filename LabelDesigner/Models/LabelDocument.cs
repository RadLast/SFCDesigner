using LabelDesigner.Models.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelDesigner.Models
{
    public class LabelDocument
    {
        public Metadata Metadata { get; set; } = new Metadata();
        //public LabelDefinition LabelDefinition { get; set; } = new LabelDefinition();
        public List<LabelBase> Elements { get; set; } = new List<LabelBase>();
    }
}

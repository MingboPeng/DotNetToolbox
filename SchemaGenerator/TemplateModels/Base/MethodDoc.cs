using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateModels.Base
{
    public class MethodDoc
    {
        public string Summary { get; set; }
        public Dictionary<string, string> Params { get; set; } = new Dictionary<string, string>();
        public string Returns { get; set; }
    }

}

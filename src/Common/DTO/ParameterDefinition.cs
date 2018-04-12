using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.HpcAcm.Common.Dto
{
    public class ParameterDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public object DefaultValue { get; set; }
        public string[] Options { get; set; }
    }
}

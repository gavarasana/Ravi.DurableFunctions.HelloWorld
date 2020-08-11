using System;
using System.Collections.Generic;

namespace Ravi.DurableFunctions.HelloWorld.Dtos
{
    public class FormatVideoRequest
    {
        public string Location { get; set; }
        public int BitRate { get; set; }
    }
}

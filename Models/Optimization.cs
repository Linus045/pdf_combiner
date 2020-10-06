using System.ComponentModel;

namespace PDF_Combiner.Models
{
    public enum Optimization {
        [Description("Don't Optimize")]
        None,
        [Description("Optimize Speed (bigger file)")]
        OptimizeSpeed,
        [Description("Optimize Size (compressed file)")]
        OptimizeSize
    }
}
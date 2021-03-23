using System;
using System.Collections.Generic;
using System.Text;

namespace InitProperties.Reflection
{
    /// <summary>
    /// Types annotated with this attribute will verify that init properties are initialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
    public sealed class VerifiesInitPropertiesAttribute : Attribute
    {
        public VerifiesInitPropertiesAttribute()
        {
        }
    }
}

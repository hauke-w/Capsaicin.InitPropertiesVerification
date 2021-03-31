using Capsaicin.InitPropertiesVerification;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Example
{
    [VerifiesInitProperties]
    partial class Class2 : Class1
    {
        [Required]
        public object? Property3 { get; init; }

        public override string ToString()
        {
            // call VerifyIsInitializedOnce or VerifyIsInitialized to get rid of the null warnings
            VerifyIsInitializedOnce();
            return "Property1: " + MyProperty.Value.ToString() // no warning
                + ", Property2: " + MyProperty2.Value.ToString() // null warning
                + ", Property3: " + Property3.ToString(); // no warning
        }
    }
}
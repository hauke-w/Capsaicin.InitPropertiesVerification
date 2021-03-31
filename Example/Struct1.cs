using Capsaicin.InitPropertiesVerification;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Example
{
    [VerifiesInitProperties]
    partial struct Struct1
    {
        [Required]
        public int? MyProperty { get; init; }

        public int? MyProperty2 { get; init; }

        public int MyProperty3 { get; set; }

        public override string ToString()
        {
            // call VerifyIsInitialized to get rid of the null warnings
            // Structs do not have VerifyIsInitializedOnce.
            VerifyIsInitialized();
            return MyProperty.Value // no null warning on MyProperty because it it is verified in VerifyIsInitializedOnce.
                + " x " 
                + MyProperty2.Value; // null warning on MyProperty2 because it is not marked as required.
        }
    }
}

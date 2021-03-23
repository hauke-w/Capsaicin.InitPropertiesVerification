﻿using InitProperties.Reflection;
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
    partial class Class1
    {
        [Required]
        public int? MyProperty { get; init; }

        public int? MyProperty2 { get; init; }

        public override string ToString()
        {
            VerifyIsInitializedOnce();
            return MyProperty.Value + " x " + MyProperty2.Value; // null warning on MyProperty2 because it is not marked as required.
        }
    }
}

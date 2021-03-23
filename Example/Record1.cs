using InitProperties.Reflection;
using System.ComponentModel.DataAnnotations;

namespace Example
{
    [VerifiesInitProperties]
    partial record Record1
    {
        [Required]
        public object? MyProperty { get; init; }

        public int? MyProperty2 { get; init; }

        public int MyProperty3 { get; set; }

        public override string ToString()
        {
            VerifyIsInitializedOnce();
            return MyProperty // no null warning on MyProperty because it it is verified in VerifyIsInitializedOnce.
                + " x "
                + MyProperty2.Value; // null warning on MyProperty2 because it is not marked as required.
        }
    }
}

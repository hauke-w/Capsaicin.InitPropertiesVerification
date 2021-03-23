using System;
using System.Diagnostics;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var a = new Class1
            {
                MyProperty = 1,
                MyProperty2 = 2
            };
            Console.WriteLine(a.ToString());

            var b = new Class1
            {
                // required property MyProperty not initialized!
                MyProperty2 = 3
            };       

            try
            {
                // will throw InvalidOperationException
                Console.WriteLine(b.ToString());
            }
            catch (InvalidOperationException e)
            {
                Debug.Assert(e.Message == $"Property '{nameof(Class1.MyProperty)}' is not initialized.");
            }

            // Class2 is sub class of Class1 and defines an additional property
            var c = new Class2
            {
                MyProperty = 5,
                MyProperty2 = 6,
                Property3 = "property3 value"
            };
            Console.WriteLine(c.ToString());

            var d = new Class1
            {
                MyProperty = 4
                // MyProperty2 not initialized but it is not marked as required
            };
            // will throw NullReferenceException
            Console.WriteLine(d.ToString());
        }
    }
}

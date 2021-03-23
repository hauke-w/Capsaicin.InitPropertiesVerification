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

            var b = new Class1
            {
                // required property MyProperty not initialized!
                MyProperty2 = 3
            };

            var c = new Class1
            {
                MyProperty = 4
                // MyProperty2 not initialized but it is not marked as required
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

            // will throw NullReferenceException
            Console.WriteLine(c.ToString());
        }
    }
}

using Common.Models;
using System;
using System.IO;


class DisposableTest
{
    static void Main()
    {
        TestNormalDisposal();
        TestExceptionDuringOperation();
        TestMultipleDispose();
    }

    static void TestNormalDisposal()
    {
        Console.WriteLine("=== Test 1: Normal Disposal ===");

        string testFile = "C:\\Users\\Dimitrije\\Documents\\GitHub\\vp_projekat\\VP_Baterija\\Common\\Files\\test.csv";

       // Test writer
        using (var writer = new EisCsvWriter(testFile))
        {
            writer.WriteLine("FrequencyHz,R_ohm,X_ohm,V,T_degC,Range_ohm,RowIndex");
            writer.WriteEisSample(new EisSample
            {
                FrequencyHz = 1000,
                R_ohm = 0.5,
                X_ohm = 0.3,
                V = 3.7,
                T_degC = 25,
                Range_ohm = 1.0,
                RowIndex = 1
            });
        } // Automatic disposal here

        // Test reader
        using (var reader = new EisCsvReader(testFile))
        {
            Console.WriteLine("File contents:");
            while (!reader.EndOfStream)
            {
                Console.WriteLine(reader.ReadLine());
            }
        } // Automatic disposal here

        File.Delete(testFile);
        Console.WriteLine("Normal disposal test completed.\n");
    }

    static void TestExceptionDuringOperation()
    {
        Console.WriteLine("=== Test 2: Exception During Operation ===");

        string testFile = "test_exception.csv";

        try
        {
            using (var writer = new EisCsvWriter(testFile))
            {
                writer.WriteLine("Header line");

                // Simulate an exception
                throw new InvalidOperationException("Simulated network interruption!");

                writer.WriteLine("This should never be written");
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Exception caught: {ex.Message}");
            Console.WriteLine("Resources should still be properly disposed.");
        }

        // Verify file was created and resources were disposed
        if (File.Exists(testFile))
        {
            Console.WriteLine("File was created before exception.");
            File.Delete(testFile);
        }

        Console.WriteLine("Exception handling test completed.\n");
    }

    static void TestMultipleDispose()
    {
        Console.WriteLine("=== Test 3: Multiple Dispose Calls ===");

        string testFile = "test_multiple.csv";

        var writer = new EisCsvWriter(testFile);
        writer.WriteLine("Test line");

        // Call dispose multiple times - should not throw exceptions
        writer.Dispose();
        writer.Dispose();
        writer.Dispose();

        Console.WriteLine("Multiple dispose calls handled safely.");

        // Try to use after disposal - should throw ObjectDisposedException
        try
        {
            writer.WriteLine("This should fail");
        }
        catch (ObjectDisposedException)
        {
            Console.WriteLine("ObjectDisposedException correctly thrown after disposal.");
        }

        File.Delete(testFile);
        Console.WriteLine("Multiple disposal test completed.\n");
    }
}
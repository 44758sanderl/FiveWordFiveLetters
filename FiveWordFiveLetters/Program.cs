namespace FiveWordFiveLetters;

using System.IO;

internal class Program

{
    static void Main(string[] args)
    {
        StreamReader sr = new StreamReader("PerfectList.txt");
        while (sr.EndOfStream == false)
        {
            String line = sr.ReadLine();
            Console.WriteLine(line);
        }
        

    }
}

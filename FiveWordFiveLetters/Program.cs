namespace FiveWordFiveLetters;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

internal class Program
{
    private const int WordsPerSolution = 5;

    private record Word(string Text, uint Mask);

    private static long combinationsTested = 0;
    private static long solutionsFound = 0;

    static void Main(string[] args)
    {
        const string inputFile = "word_list_alpha.txt";

        Console.WriteLine("Loading words...");

        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"ERROR: Could not find '{inputFile}'.");
            return;
        }

        List<Word> words = LoadWords(inputFile);

        Console.WriteLine($"Usable words: {words.Count}");
        Console.WriteLine();

        if (words.Count < WordsPerSolution)
        {
            Console.WriteLine("Not enough usable words to create a solution.");
            return;
        }

        Console.WriteLine("Searching...");
        Console.WriteLine();

        Parallel.For(
            0,
            words.Count,
            i =>
            {
                Word[] selected = new Word[WordsPerSolution];

                Word firstWord = words[i];
                selected[0] = firstWord;

                Search(
                    words,
                    selected,
                    depth: 1,
                    startIndex: i + 1,
                    usedLetters: firstWord.Mask
                );
            });

        Console.WriteLine();
        Console.WriteLine($"Usable words:        {words.Count:N0}");
        Console.WriteLine($"Combinations tested: {Interlocked.Read(ref combinationsTested):N0}");
        Console.WriteLine($"Solutions found:     {Interlocked.Read(ref solutionsFound):N0}");
    }

    static List<Word> LoadWords(string filename)
    {
        var words = new List<Word>();
        var usedMasks = new HashSet<uint>();

        foreach (string? line in File.ReadLines(filename))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string word = line.Trim().ToLowerInvariant();

            if (word.Length != 5)
                continue;

            uint mask = GetMask(word);

            if (mask == 0)
                continue;

            if (!usedMasks.Add(mask))
                continue;

            words.Add(new Word(word, mask));
        }

        return words;
    }

    static uint GetMask(string word)
    {
        uint mask = 0;

        foreach (char c in word)
        {
            if (c < 'a' || c > 'z')
                return 0;

            uint bit = 1u << (c - 'a');


            if ((mask & bit) != 0)
                return 0;

            mask |= bit;
        }

        return mask;
    }

    static void Search(
        List<Word> words,
        Word[] selected,
        int depth,
        int startIndex,
        uint usedLetters)
    {
        if (depth == WordsPerSolution)
        {
            Interlocked.Increment(ref solutionsFound);
            return;
        }

        int remainingWords = WordsPerSolution - depth;

        int usedLetterCount = BitCount(usedLetters);

        int remainingLetters = 26 - usedLetterCount;

        if (remainingLetters < remainingWords * 5)
            return;

        for (int i = startIndex; i < words.Count; i++)
        {
            Word word = words[i];

            if ((usedLetters & word.Mask) != 0)
                continue;

            Interlocked.Increment(ref combinationsTested);

            selected[depth] = word;

            Search(
                words,
                selected,
                depth + 1,
                i + 1,
                usedLetters | word.Mask
            );
        }
    }

    static int BitCount(uint value)
    {
        int count = 0;

        while (value != 0)
        {
            value &= value - 1;
            count++;
        }

        return count;
    }
}

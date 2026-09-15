using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GUI
{
    public partial class MainWindow : Window
    {
        private string selectedFilePath = string.Empty;
        private const int WordsPerSolution = 5;

        private record Word(string Text, uint Mask);

        private long combinationsTested = 0;
        private long solutionsFound = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnChoose_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;
                BtnStart.IsEnabled = true;
                BtnRunWithoutThreads.IsEnabled = true;
                MessageBox.Show($"Selected file: {openFileDialog.SafeFileName}", "File Loaded");
            }
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            BtnStart.IsEnabled = false;
            BtnRunWithoutThreads.IsEnabled = false;
            BtnChoose.IsEnabled = false;

            combinationsTested = 0;
            solutionsFound = 0;
            MyProgressBar.Value = 0;

            List<Word> words = LoadWords(selectedFilePath);

            if (words.Count < WordsPerSolution)
            {
                MessageBox.Show("Not enough usable words to create a solution.", "Error");
                BtnChoose.IsEnabled = true;
                BtnStart.IsEnabled = true;
                BtnRunWithoutThreads.IsEnabled = true;
                return;
            }

            var progressReporter = new Progress<int>(percent => MyProgressBar.Value = percent);

            await Task.Run(() => RunParallelSearch(words, progressReporter));

            MessageBox.Show($"Search finished!\n\nCombinations tested: {combinationsTested:N0}\nSolutions found: {solutionsFound:N0}", "Done!");

            BtnChoose.IsEnabled = true;
            BtnStart.IsEnabled = true;
            BtnRunWithoutThreads.IsEnabled = true;
        }

        private void RunParallelSearch(List<Word> words, IProgress<int> progress)
        {
            int totalWords = words.Count;
            int completedWords = 0;

            Parallel.For(0, totalWords, i =>
            {
                Word[] selected = new Word[WordsPerSolution];
                Word firstWord = words[i];
                selected[0] = firstWord;

                long localCombinations = 0;

                Search(words, selected, 1, i + 1, firstWord.Mask, ref localCombinations, true);

                Interlocked.Add(ref combinationsTested, localCombinations);

                int currentCompleted = Interlocked.Increment(ref completedWords);
                int percentComplete = (int)((double)currentCompleted / totalWords * 100);

                progress.Report(percentComplete);
            });
        }

        private async void BtnRunWithoutThreads_Click(object sender, RoutedEventArgs e)
        {
            BtnStart.IsEnabled = false;
            BtnRunWithoutThreads.IsEnabled = false;
            BtnChoose.IsEnabled = false;

            combinationsTested = 0;
            solutionsFound = 0;
            MyProgressBar.Value = 0;

            List<Word> words = LoadWords(selectedFilePath);

            if (words.Count < WordsPerSolution)
            {
                MessageBox.Show("Not enough usable words to create a solution.", "Error");
                BtnChoose.IsEnabled = true;
                BtnStart.IsEnabled = true;
                BtnRunWithoutThreads.IsEnabled = true;
                return;
            }

            var progressReporter = new Progress<int>(percent => MyProgressBar.Value = percent);

            await Task.Run(() => RunNonThreadedSearch(words, progressReporter));

            MessageBox.Show($"Search finished!\n\nCombinations tested: {combinationsTested:N0}\nSolutions found: {solutionsFound:N0}", "Done!");

            BtnChoose.IsEnabled = true;
            BtnStart.IsEnabled = true;
            BtnRunWithoutThreads.IsEnabled = true;
        }

        private void RunNonThreadedSearch(List<Word> words, IProgress<int> progress)
        {
            int totalWords = words.Count;

            for (int i = 0; i < totalWords; i++)
            {
                Word[] selected = new Word[WordsPerSolution];
                Word firstWord = words[i];
                selected[0] = firstWord;

                long localCombinations = 0;

                Search(words, selected, 1, i + 1, firstWord.Mask, ref localCombinations, false);

                combinationsTested += localCombinations;

                int percentComplete = (int)((double)(i + 1) / totalWords * 100);
                progress.Report(percentComplete);
            }
        }

        private void Search(
            List<Word> words,
            Word[] selected,
            int depth,
            int startIndex,
            uint usedLetters,
            ref long localCombinations,
            bool threaded)
        {
            if (depth == WordsPerSolution)
            {
                if (threaded)
                    Interlocked.Increment(ref solutionsFound);
                else
                    solutionsFound++;

                return;
            }

            int remainingWords = WordsPerSolution - depth;
            int remainingLetters = 26 - BitCount(usedLetters);

            if (remainingLetters < remainingWords * 5)
                return;

            for (int i = startIndex; i < words.Count; i++)
            {
                Word word = words[i];

                if ((usedLetters & word.Mask) != 0)
                    continue;

                localCombinations++;
                selected[depth] = word;

                Search(
                    words,
                    selected,
                    depth + 1,
                    i + 1,
                    usedLetters | word.Mask,
                    ref localCombinations,
                    threaded);
            }
        }

        private List<Word> LoadWords(string filename)
        {
            var words = new List<Word>();
            var usedMasks = new HashSet<uint>();

            foreach (string line in File.ReadLines(filename))
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

        private uint GetMask(string word)
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

        private int BitCount(uint value)
        {
            int count = 0;

            while (value != 0)
            {
                value &= value - 1;
                count++;
            }

            return count;
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            MyProgressBar.Value = 0;
            combinationsTested = 0;
            solutionsFound = 0;
        }
    }
}

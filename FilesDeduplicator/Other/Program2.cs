using System.Drawing;
using Puzzle;
using Shipwreck.Phash;
using Shipwreck.Phash.Bitmaps;

namespace FilesDeduplicator.Other;

internal class Program2
{
    static void Main2(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var files = Directory.GetFiles(@"D:\Projects\FilesDeduplicator\TestFiles\Images", "*", SearchOption.AllDirectories).ToList();

        Console.WriteLine();
        Console.WriteLine("Phash");
        var phashes = files.ToDictionary(x => x, CalculatePhash);
        var resultPhash = new List<Tuple<float, string, string>>();
        var combinations = GetPairs(files);
        foreach (var combination in combinations)
        {
            var hash1 = phashes[combination.Item1];
            var hash2 = phashes[combination.Item2];

            var score = ImagePhash.GetCrossCorrelation(hash1, hash2);
            resultPhash.Add(Tuple.Create(score, combination.Item1, combination.Item2));
        }

        foreach (var tuple in resultPhash.OrderByDescending(x => x.Item1))
            Console.WriteLine($"{tuple.Item1}: {Path.GetFileName(tuple.Item2)} - {Path.GetFileName(tuple.Item3)}");


        Console.WriteLine();
        Console.WriteLine("Puzzles");
        var puzzles = files.ToDictionary(x => x, CalculatePuzzle);
        var resultPuzzle = new List<Tuple<SignatureSimilarity, string, string>>();
        foreach (var combination in combinations)
        {
            var puzzle1 = puzzles[combination.Item1];
            var puzzle2 = puzzles[combination.Item2];

            var score = puzzle1.CompareTo(puzzle2);
            resultPuzzle.Add(Tuple.Create(score, combination.Item1, combination.Item2));
        }
        foreach (var tuple in resultPuzzle.OrderByDescending(x => x.Item1))
            Console.WriteLine($"{tuple.Item1}: {Path.GetFileName(tuple.Item2)} - {Path.GetFileName(tuple.Item3)}");
    }

    public static IEnumerable<Tuple<T, T>> GetPairs<T>(List<T> list)
    {
        var count = 0;
        foreach (var l1 in list)
        {
            count++;
            foreach (var l2 in list.Skip(count))
            {
                yield return Tuple.Create(l1, l2);
            }
        }
    }

    private static LuminosityLevel[] CalculatePuzzle(string file)
    {
        SignatureGenerator gen = new SignatureGenerator();
        using var image = SixLabors.ImageSharp.Image.Load(file);
        return gen.GenerateSignature(image).ToArray();
    }

    private static Digest CalculatePhash(string file)
    {
        using var bitmap = (Bitmap)Image.FromFile(file);
        var hash = ImagePhash.ComputeDigest(bitmap.ToLuminanceImage(), 3.5f, 1f);
        return hash;
    }
}
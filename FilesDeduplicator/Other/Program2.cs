using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Quality;
using OpenCvSharp.XFeatures2D;
using Puzzle;
using Shipwreck.Phash;
using Shipwreck.Phash.Bitmaps;
using Size = OpenCvSharp.Size;

namespace FilesDeduplicator.Other;

internal class Program2
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        var files = Directory.GetFiles(@"D:\Projects\FilesDeduplicator\TestFiles\Images", "*", SearchOption.AllDirectories).ToList();
        var combinations = GetPairs(files).ToList();

        Console.WriteLine();
        Console.WriteLine("OpenCV SSMI");
        foreach (var combination in combinations)
        {
            using var image1 = new Mat(combination.Item1, ImreadModes.Grayscale);
            //image1.ConvertTo(image1, MatType.CV_8UC3);

            using var image2 = new Mat(combination.Item2, ImreadModes.Grayscale);
            //image2.ConvertTo(image2, MatType.CV_8UC3);
            
            var targetWidth = 640; // Set your desired width
            var targetHeight = 480; // Set your desired height

            Cv2.Resize(image1, image1, new Size(targetWidth, targetHeight));
            Cv2.Resize(image2, image2, new Size(targetWidth, targetHeight));

            // Compute SSIM
            using var ssim = QualitySSIM.Create(image1);
            var ssimValue = ssim.Compute(image2);
            var similarityThreshold = 0.5; // Adjust as needed
            var isSimilar = ssimValue.ToDouble() > similarityThreshold;

            var msg = $"{isSimilar} - {ssimValue}: {Path.GetFileName(combination.Item1)} - {Path.GetFileName(combination.Item2)}";
            Console.WriteLine(msg);

            //Console.WriteLine($"Similarity Score: {similarityScore}");

            // You can set a threshold to determine if the images are similar

            //if (similarityScore < similarityThreshold)
            //    Console.WriteLine("Images are similar.");
            //else
            //    Console.WriteLine("Images are not similar.");

            //using var fsWrite = new FileStorage(@"D:\Projects\FilesDeduplicator\TestFiles\descriptors.yml", FileStorage.Modes.Write);
            //fsWrite.Write("descriptors", descriptors1);
        }

        Console.WriteLine();
        Console.WriteLine("OpenCV SURF");
        var surf = SURF.Create(2000); // Adjust the threshold as needed
        using var matcher = new BFMatcher(NormTypes.L2);
        foreach (var combination in combinations)
        {
            using var image1 = new Mat(combination.Item1, ImreadModes.ReducedColor2);
            //image1.ConvertTo(image1, MatType.CV_8UC3);

            using var image2 = new Mat(combination.Item2, ImreadModes.ReducedColor2);
            //image2.ConvertTo(image2, MatType.CV_8UC3);
            
            // Detect keypoints and compute descriptors for both images
            KeyPoint[] keypoints1, keypoints2;
            using var descriptors1 = new Mat();
            using var descriptors2 = new Mat();
            surf.DetectAndCompute(image1, null, out keypoints1, descriptors1);
            surf.DetectAndCompute(image2, null, out keypoints2, descriptors2);

            // Create a descriptor matcher (e.g., Brute-Force or FLANN)
            //var size = descriptors1.DataEnd - descriptors1.DataStart;
            var matches = matcher.Match(descriptors1, descriptors2);

            // Calculate similarity score (you can customize this based on your needs)
            double similarityScore = matches.Average(m => m.Distance);

            var similarityThreshold = 0.5; // Adjust as needed
            var isSimilar = similarityScore < similarityThreshold;
            var msg = $"{isSimilar} - {similarityScore}: {Path.GetFileName(combination.Item1)} - {Path.GetFileName(combination.Item2)}";
            Console.WriteLine(msg);

            //Console.WriteLine($"Similarity Score: {similarityScore}");

            // You can set a threshold to determine if the images are similar

            //if (similarityScore < similarityThreshold)
            //    Console.WriteLine("Images are similar.");
            //else
            //    Console.WriteLine("Images are not similar.");

            //using var fsWrite = new FileStorage(@"D:\Projects\FilesDeduplicator\TestFiles\descriptors.yml", FileStorage.Modes.Write);
            //fsWrite.Write("descriptors", descriptors1);
        }


        Console.WriteLine();
        Console.WriteLine("Phash");
        var phashes = files.ToDictionary(x => x, CalculatePhash);
        var resultPhash = new List<Tuple<float, string, string>>();
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
        var gen = new SignatureGenerator();
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
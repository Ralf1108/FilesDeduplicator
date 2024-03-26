using FilesDeduplicator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using OpenCvSharp.Quality;
using RelationalFileSystem.Entities;
using RelationalFileSystem.Extensions;
using File = RelationalFileSystem.Entities.File;

namespace FilesDeduplicator.Domain.Plugins;

// https://docs.opencv.org/3.4/d5/dc4/tutorial_video_input_psnr_ssim.html
class OpenCVDuplicatePlugin : IDuplicatePlugin
{
    public string Identifier => "OpenCV";
    public FileAttributeKey Key => new(Guid.Parse("D36634CC-C48E-4AD1-A5DD-46D55AAE19CD"));

    public FileDuplicateAttribute Calculate(FilePath filePath)
    {
        // Load the images (replace with your image file paths)
        using var imageA = Cv2.ImRead("path/to/imageA.jpg", ImreadModes.Grayscale);
        using var imageB = Cv2.ImRead("path/to/imageB.jpg", ImreadModes.Grayscale);

        // Compute SSIM
        var ssim = QualitySSIM.Create(imageA);
        var ssimValue = ssim.Compute(imageB);

        Console.WriteLine(ssim);
        return new FileDuplicateAttribute(Key, ssimValue.ToString());
    }

    public async Task<List<File>> GetCalculationCandidates(IQueryable<File> query)
    {
        return await query
            .IncludeFolderNames()
            .ToListAsync();
    }

    public async Task<List<List<FileId>>> FindDuplicates(IQueryable<FileAttribute> query)
    {
        return new List<List<FileId>>();
    }
}
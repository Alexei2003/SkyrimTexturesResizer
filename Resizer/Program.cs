using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;

using SixLabors.ImageSharp.Processing;

class TextureDownscaler
{
    static void Main()
    {
        Console.WriteLine("Откуда");
        Console.WriteLine("1.Папка модов");
        Console.WriteLine("2.Папка мода");
        var action = Console.ReadLine();

        string[] dirArr = [];
        switch (action)
        {
            case "1":
                dirArr = Directory.GetDirectories("D:\\Content\\NEFARAM\\mods");
                break;
            case "2":
                Console.WriteLine("Путь");
                dirArr = [Console.ReadLine() ?? ""];
                break;
        }

        const string OutputDir = "Out";
        const float MaxSize = 512;

        Directory.CreateDirectory(OutputDir);

        var decoder = new BcDecoder
        {
            OutputOptions = { }
        };
        var encoder = new BcEncoder
        {
            OutputOptions = { GenerateMipMaps = true, FileFormat = OutputFileFormat.Dds, Quality = CompressionQuality.Balanced }
        };

        int index = 0;
        int count = 0;
        foreach (var dir in dirArr)
        {
            Parallel.ForEach(Directory.GetFiles(dir, "*.dds", SearchOption.AllDirectories), new ParallelOptions(), file =>
            {
                Console.WriteLine($"{++count}");
                if (!IsTechnicalMap(file))
                {
                    try
                    {
                        string relPath = Path.GetRelativePath(dir, file);
                        string outPath = Path.Combine(OutputDir, relPath);

                        if (!File.Exists(outPath))
                        {
                            using var fsIn = File.OpenRead(file);
                            using var image = BCnDecoderExtensions.DecodeToImageRgba32(decoder, fsIn);

                            var maxSize = int.Max(image.Width, image.Height);
                            if (maxSize > MaxSize)
                            {
                                var width = image.Width;
                                var height = image.Height;
                                var delta = MaxSize / maxSize;
                                int newWidth = int.Max(1, (int)(image.Width * delta));
                                int newHeight = int.Max(1, (int)(image.Height * delta));
                                image.Mutate(x => x.Resize(newWidth, newHeight));

                                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                                encoder.OutputOptions.Format = CompressionFormat.Bc7;
                                using var fsOut = File.Create(outPath);
                                BCnEncoderExtensions.EncodeToStream(encoder, image, fsOut);

                                Console.WriteLine($"Ready {index}/{dirArr.Length} - {file} : {width}x{height} -> {image.Width}x{image.Height}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка: {file} — {ex.Message}");
                    }
                }
            });
            index++;
        }

        Console.WriteLine("Готово!");
        Console.ReadLine();
    }

    // Простая догадка формата по имени файла
    static bool IsTechnicalMap(string file)
    {
        string name = Path.GetFileName(file).ToLowerInvariant();
        return name.Contains("_n.") || name.Contains("_normal.") ||
               name.Contains("_h.") || name.Contains("_height.") ||
               name.Contains("_r.") || name.Contains("_rough.") ||
               name.Contains("_m.") || name.Contains("_metal.") ||
               name.Contains("_ao.") || name.Contains("_mask.");
    }
}

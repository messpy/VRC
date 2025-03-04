using System;
using System.Drawing;
using System.IO;

namespace FlipImage
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: FlipImage.exe <image_file>");
                return;
            }

            string imagePath = args[0];

            try
            {
                using (Bitmap bmp = new Bitmap(imagePath))
                {
                    // 左右反転：RotateNoneFlipX
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);

                    string outputPath = GenerateOutputFileName(imagePath);
                    bmp.Save(outputPath);
                    Console.WriteLine("Flipped image saved as: " + outputPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing image: " + ex.Message);
            }
        }

        private static string GenerateOutputFileName(string imagePath)
        {
            string directory = Path.GetDirectoryName(imagePath);
            string fileName = Path.GetFileNameWithoutExtension(imagePath);
            string extension = Path.GetExtension(imagePath);
            return Path.Combine(directory, fileName + "_flipped" + extension);
        }
    }
}

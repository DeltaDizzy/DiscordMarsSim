using System.Reflection;

namespace DiscordMarsSim
{
    internal class ImageManager
    {
        static readonly HttpClient client = new();
        static async Task DownloadImages(string[] urls)
        {
            foreach (var path in urls)
            {
                string name = new(Path.GetFileName(path).TakeWhile(c => c != '?').ToArray());
                var response = await client.GetAsync(path);
                FileInfo image = new(GetImagePath().FullName + Path.DirectorySeparatorChar + name);
                //if (File.Exists(image.FullName))
                FileStream fs = new(image.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                await response.Content.CopyToAsync(fs);

            }
        }

        static FileStream LoadImage(string name)
        {
            return new(GetRootPath() + $"\\Images\\{name}", FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        static string GetRootPath()
        {
            DirectoryInfo path = new(Path.GetDirectoryName($"{Assembly.GetExecutingAssembly().Location}")!);
            if (!path.EnumerateDirectories().Where(dir => dir.Name.Contains("Images")).Any())
            {
                path.CreateSubdirectory("Images");
            }
            return path.FullName;
        }
        static DirectoryInfo GetImagePath()
        {
            return new DirectoryInfo(GetRootPath()).CreateSubdirectory("Images");
        }
    }
}

using Raylib_cs;
using static Raylib_cs.Raylib;

namespace PointMasses.Textures;

public class TextureManager
{
    private readonly IDictionary<string, Texture2D> _textures;
    private readonly string _resourcesPath;

    public TextureManager()
    {
        #if DEBUG
            Console.WriteLine("Point-masses is running in DEBUG mode");
            _resourcesPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + "/res/";
        #else
            Console.WriteLine("Point-masses is running in RELEASE mode");
            _resourcesPath = Directory.GetCurrentDirectory() + "/res/";
        #endif
        _textures = new Dictionary<string, Texture2D>();
    }

    ~TextureManager()
    {
        foreach (var texture in _textures.Values)
        {
            UnloadTexture(texture);
        }
        Console.WriteLine("Unloaded textures");
    }

    public void LoadTexture(string fileName)
    {
        Image loadedImage = LoadImage(_resourcesPath + fileName);
        _textures.Add(fileName, LoadTextureFromImage(loadedImage));
        UnloadImage(loadedImage);
    }

    public Texture2D GetTexture(string fileName)
    {;
        if (_textures.TryGetValue(fileName, out Texture2D texture))
        {
            return texture;
        }
        throw new KeyNotFoundException(fileName);
    }
}
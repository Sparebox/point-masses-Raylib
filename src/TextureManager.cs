using Raylib_cs;
using static Raylib_cs.Raylib;
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Textures;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public class TextureManager
{
    public Texture2D _centerOfMassIcon;

    public TextureManager()
    {
        string resourcesPath;
        #if DEBUG
            Console.WriteLine("Point-masses is running in DEBUG mode");
            resourcesPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + "/res/";
        #else
            Console.WriteLine("Point-masses is running in RELEASE mode");
            resourcesPath = Directory.GetCurrentDirectory() + "/res/";
        #endif
        Image centerOfMassIcon = LoadImage(resourcesPath + "center_of_mass_icon.png");
        _centerOfMassIcon = LoadTextureFromImage(centerOfMassIcon);
        UnloadImage(centerOfMassIcon);
    }

    ~TextureManager()
    {
        UnloadTexture(_centerOfMassIcon);
    }
}
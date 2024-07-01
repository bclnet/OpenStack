namespace OpenStack.Graphics
{
    public interface IObjectManager<Object, Material, Texture>
    {
        Object CreateObject(string path, out object tag);
        void PreloadObject(string path);
    }
}
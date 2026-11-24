using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.UI;

public class DesktopPhotoPicker : MonoBehaviour
{
    [SerializeField] private RawImage target;

    public void PickImage()
    {
        var filters = new[] { new ExtensionFilter("Images", "png", "jpg", "jpeg") };

        StandaloneFileBrowser.OpenFilePanelAsync("Select a photo", "", filters, false, paths =>
        {
            if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

            byte[] bytes = File.ReadAllBytes(paths[0]);
            var tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes))
                target.texture = tex;
        });
    }
}
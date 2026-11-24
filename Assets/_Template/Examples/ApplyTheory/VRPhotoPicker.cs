using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class VRPhotoBrowser : MonoBehaviour
{
    [SerializeField] private Transform gridParent;   // has a GridLayoutGroup
    [SerializeField] private Button thumbnailPrefab; // Button with a RawImage child
    [SerializeField] private RawImage preview;

    void OnEnable()
    {
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        // or Application.persistentDataPath for photos the app manages itself

        foreach (string file in Directory.GetFiles(folder, "*.*"))
        {
            string ext = Path.GetExtension(file).ToLower();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png") continue;

            var tex = new Texture2D(2, 2);
            if (!tex.LoadImage(File.ReadAllBytes(file))) continue;

            var btn = Instantiate(thumbnailPrefab, gridParent);
            btn.GetComponentInChildren<RawImage>().texture = tex;
            btn.onClick.AddListener(() => preview.texture = tex);
        }
    }
}
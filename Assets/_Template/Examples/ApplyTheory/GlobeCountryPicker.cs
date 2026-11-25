using UnityEngine;

[System.Serializable]
public class MaskCountry
{
    public string name;
    public Color32 maskColor;     // Farbe dieses Landes in der Maske
    public Texture2D[] photos;
}

public class GlobeCountryPicker : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Texture2D maskTexture;
    [SerializeField] private VRPhotoBrowser photoBrowser;
    [SerializeField] private MaskCountry[] countries;
    [SerializeField] private int tolerance = 6;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleRay(cam.ScreenPointToRay(Input.mousePosition));
    }

    // Kann auch vom VR-Controller aufgerufen werden
    public void HandleRay(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;
        if (hit.collider.gameObject != gameObject) return;

        Vector2 uv = hit.textureCoord;
        int x = Mathf.Clamp((int)(uv.x * maskTexture.width), 0, maskTexture.width - 1);
        int y = Mathf.Clamp((int)(uv.y * maskTexture.height), 0, maskTexture.height - 1);
        Color32 c = maskTexture.GetPixel(x, y);

        Debug.Log($"UV {uv}, Pixel {x},{y}, Farbe {c}");

        if (countries.Length > 0)
            photoBrowser.ShowPhotos(countries[0].photos);
    }

    bool Close(Color32 a, Color32 b) =>
        Mathf.Abs(a.r - b.r) <= tolerance &&
        Mathf.Abs(a.g - b.g) <= tolerance &&
        Mathf.Abs(a.b - b.b) <= tolerance;
}
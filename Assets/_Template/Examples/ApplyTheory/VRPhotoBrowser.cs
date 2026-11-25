using UnityEngine;
using UnityEngine.UI;

public class VRPhotoBrowser : MonoBehaviour
{
    [SerializeField] private Transform gridParent;
    [SerializeField] private Button thumbnailPrefab;
    [SerializeField] private RawImage preview;

    private Texture2D[] current = new Texture2D[0];
    private int index;

    public void ShowPhotos(Texture2D[] photos)
    {
        foreach (Transform child in gridParent) Destroy(child.gameObject);

        current = photos;
        for (int i = 0; i < photos.Length; i++)
        {
            var btn = Instantiate(thumbnailPrefab, gridParent);
            btn.GetComponentInChildren<RawImage>().texture = photos[i];
            int idx = i; // wichtig für die Lambda-Funktion
            btn.onClick.AddListener(() => ShowIndex(idx));
        }

        index = 0;
        if (photos.Length > 0) ShowIndex(0);
        gameObject.SetActive(true);
    }

    public void ShowIndex(int i)
    {
        if (current.Length == 0) return;
        index = (i + current.Length) % current.Length; // springt am Ende herum
        preview.texture = current[index];
    }

    public void Next() => ShowIndex(index + 1);
    public void Previous() => ShowIndex(index - 1);

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) Next();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) Previous();
    }
}
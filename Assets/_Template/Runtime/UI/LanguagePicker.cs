using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// The entrance picker: shows itself only while the visitor has not chosen yet.
    /// </summary>
    public class LanguagePicker : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        private void Start()
        {
            if (root == null) root = gameObject;
            root.SetActive(!LabLanguage.Chosen);
        }
    }
}

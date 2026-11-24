using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Tags a Grabbable so that only matching SnapZones accept it
    /// (e.g. key "red" fits only in zones whose acceptedKey is "red").
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public class SnapKey : MonoBehaviour
    {
        [SerializeField] private string key = "";
        public string Key { get => key; set => key = value; }
    }
}

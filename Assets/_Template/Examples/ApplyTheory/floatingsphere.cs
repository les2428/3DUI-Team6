using UnityEngine;

public class floatingsphere : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.1f;  // how high it bobs (meters)
    [SerializeField] private float speed = 1f;        // bobs per second-ish
    [SerializeField] private float rotateSpeed = 20f; // optional slow spin (0 = off)

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * speed * Mathf.PI * 2f) * amplitude;
        transform.position = startPos + Vector3.up * offset;

        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }
}
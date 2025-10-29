using UnityEngine;
public class SimpleFollow2D : MonoBehaviour
{
    public Transform target;
    public float zOffset = -10f;
    void LateUpdate()
    {
        if (!target) return;
        var p = target.position;
        transform.position = new Vector3(p.x, p.y, zOffset);
    }
}
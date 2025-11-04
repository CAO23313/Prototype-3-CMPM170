using UnityEngine;
public class SimpleFollow2D : MonoBehaviour
{
    public Transform target;
    public float zOffset = -10f;
    void LateUpdate()
    {
        if (!target) return;
        var p = target.position;
        p.x = Mathf.Clamp(p.x, -30f, 50f);
        p.y = Mathf.Clamp(p.y, -20f, 20f);
        transform.position = new Vector3(p.x, p.y, zOffset);
    }
}
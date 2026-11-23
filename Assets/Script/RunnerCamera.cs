using UnityEngine;


public class RunnerCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 4f, -7f);
    public float sideFollowSpeed = 8f;    // how quickly it follows lane changes
    public float heightFollowSpeed = 4f;  // lower = less bounce on jumps
    public float lookDownAngle = 15f;

    void LateUpdate()
    {
        if (!target) return;

        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, target.position.x + offset.x, sideFollowSpeed * Time.deltaTime);
        pos.y = Mathf.Lerp(pos.y, target.position.y + offset.y, heightFollowSpeed * Time.deltaTime);
        pos.z = target.position.z + offset.z; // locked forward so it never falls behind

        transform.position = pos;
        transform.rotation = Quaternion.Euler(lookDownAngle, 0f, 0f);
    }
}
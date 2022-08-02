using UnityEngine;

public class TriggerPoint : MonoBehaviour
{

    public Vector2 vel;
    public int power;
    public bool enter = false;
    public Vector3 savePos;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "circle") {
            enter = true;
        }
    }
}

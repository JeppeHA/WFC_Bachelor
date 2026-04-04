using UnityEngine;

public class Transition : MonoBehaviour
{
    public MapNode ownerNode;
    public int direction; // 0=West, 1=East, 2=South, 3=North

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
    }
}
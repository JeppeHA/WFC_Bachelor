using System;
using System.Collections;
using UnityEngine;

public class Transition : MonoBehaviour
{
    public MapNode ownerNode;
    public int direction; // 0=West, 1=East, 2=South, 3=North
    public bool switchRoom = false;
    public bool switchActivated = false;
    [SerializeField] private float tick;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        MapGraph mapGraph = FindObjectOfType<MapGraph>();
        mapGraph.RequestTransition(ownerNode, direction);
    }

    private void Update()
    {
        if (switchActivated)
        {
            switchActivated = false;
            StartCoroutine(checkForSwitchRoom());
        }
    }

    private IEnumerator checkForSwitchRoom()
    {
        if (switchRoom)
        {
            MapGraph mapGraph = FindObjectOfType<MapGraph>();
            mapGraph.RequestTransition(ownerNode, direction);
        }
        yield return new WaitForSeconds(tick);
        if(switchRoom)
            StartCoroutine(checkForSwitchRoom());
    }
}
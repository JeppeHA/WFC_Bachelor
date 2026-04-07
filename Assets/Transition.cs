using System;
using System.Collections;
using UnityEngine;

public class Transition : MonoBehaviour
{
    public MapNode ownerNode;
    public int direction; // 0=West, 1=East, 2=South, 3=North
    [SerializeField]
    private bool switchingRoom = false;
    [SerializeField]
    private GameObject player;

    private void Start()
    {
        player = GameObject.FindWithTag("Player");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || switchingRoom) return; 
        //DoTransition();
    }

    private void DoTransition()
    {
        player.transform.position = Vector3.zero;
        switchingRoom = true;
        int opposite = (direction + 2) % 4;
        ownerNode.neighbors[direction].transitions[opposite].SetSwitch(true);

        MapGraph mapGraph = FindObjectOfType<MapGraph>();
        Vector3 spawnPosition = ownerNode.neighbors[direction].transitions[opposite].transform.position;
    
        Debug.Log("Before RequestTransition: " + player.transform.position);
        mapGraph.RequestTransition(ownerNode, direction);
        Debug.Log("After RequestTransition: " + player.transform.position);
        Debug.Log("New position: " + spawnPosition);
        player.transform.position = spawnPosition;
        Debug.Log("After teleport: " + player.transform.position);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        switchingRoom = false;
    }

    public void SetSwitch(bool state)
    {
        switchingRoom = state;
    }

    /*
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
    */
}
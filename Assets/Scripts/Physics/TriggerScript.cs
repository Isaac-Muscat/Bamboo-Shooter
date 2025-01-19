using System;
using UnityEngine;
using UnityEngine.Events;

public class TriggerScript : MonoBehaviour
{
    public UnityEvent<GameObject, Vector2> triggerEnter;
    public UnityEvent triggerExit;

    private void OnTriggerEnter(Collider other)
    {
        Vector3 closestP = other.ClosestPoint(transform.position);
        triggerEnter.Invoke(other.gameObject, new Vector2(closestP.x, closestP.z));
    }

    private void OnTriggerExit(Collider other)
    {
        triggerExit.Invoke();
    }
}

using System.Collections.Generic;
using UnityEngine;

public class InteractionDetector : MonoBehaviour
{
    private readonly List<MonoBehaviour> interactablesInRange = new List<MonoBehaviour>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryRegister(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        MonoBehaviour interactable = other.GetComponent<IInteractable>() as MonoBehaviour;
        if (interactable == null)
        {
            interactable = other.GetComponentInParent<IInteractable>() as MonoBehaviour;
        }

        if (interactable != null)
        {
            interactablesInRange.Remove(interactable);
        }
    }

    public IInteractable GetClosestInteractable(Vector3 fromPosition)
    {
        CleanupDestroyedReferences();

        IInteractable closest = null;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < interactablesInRange.Count; i++)
        {
            MonoBehaviour candidate = interactablesInRange[i];
            if (candidate == null)
            {
                continue;
            }

            float sqrDistance = (candidate.transform.position - fromPosition).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closest = candidate as IInteractable;
            }
        }

        return closest;
    }

    private void TryRegister(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        MonoBehaviour interactable = other.GetComponent<IInteractable>() as MonoBehaviour;
        if (interactable == null)
        {
            interactable = other.GetComponentInParent<IInteractable>() as MonoBehaviour;
        }

        if (interactable != null && !interactablesInRange.Contains(interactable))
        {
            interactablesInRange.Add(interactable);
        }
    }

    private void CleanupDestroyedReferences()
    {
        for (int i = interactablesInRange.Count - 1; i >= 0; i--)
        {
            if (interactablesInRange[i] == null)
            {
                interactablesInRange.RemoveAt(i);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailFollow : MonoBehaviour
{
    public Transform target;
    private TrailRenderer trailRenderer;
    public float width;

    void Start()
    {
        trailRenderer = GetComponent<TrailRenderer>();
        trailRenderer.startWidth = width;
        trailRenderer.endWidth = width;
    }

    void Update()
    {
        if (target != null)
        {
            transform.position = target.position;
        }
        else
        {
            Debug.LogWarning("Target not set for TrailFollow script.");
        }
    }

    
}

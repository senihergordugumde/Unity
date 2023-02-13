using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCam : MonoBehaviour
{
    [SerializeField] GameObject followThis;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = followThis.transform.position + new Vector3(0,0,-10);
    }
}

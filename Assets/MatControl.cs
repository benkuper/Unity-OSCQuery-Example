using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatControl : MonoBehaviour
{
    public Color mainColor;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        GetComponent<Renderer>().material.color = mainColor;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatControl : MonoBehaviour
{
    public Color mainColor;

    public enum ColorMode { Albedo, Emission }
    public ColorMode colorMode;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        GetComponent<Renderer>().material.color = mainColor;
    }

    public void launchSomething()
    {
        mainColor = Color.yellow;
    }

    public void launchWithDefaultParameters(bool white = true)
    {
        mainColor = white?Color.white:Color.red;
    }

    public void launch2(bool white = false)
    {
        mainColor = white ? Color.white : Color.red;
    }
}

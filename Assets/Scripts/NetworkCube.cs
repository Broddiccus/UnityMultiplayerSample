using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkCube : MonoBehaviour
{
    public string id = string.Empty;

    public void Setup(string _id)
    {
        id = _id;
    }

    public void ChangeColor(float r, float g, float b)
    {
        this.gameObject.GetComponent<Renderer>().material.color = new Color(r,g,b,1.0f);
    }
}

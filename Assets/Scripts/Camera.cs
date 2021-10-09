using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Camera : MonoBehaviour
{
    // == REFERENZEN
    public Player player;

    // == ATTRIBUTE
    public float lag = 2f;
    public float xOffset = 0f;
    public float yOffset = 0f;

    public float softCutoffSlope = 3;
    public float softCutoffThreshold = 4;


    private void Awake()
    {
        this.player = GameObject.Find("Player").GetComponent<Player>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 position = new Vector3(this.player.transform.position.x,
                                       this.player.transform.position.y,
                                       this.transform.position.z);
        
        Vector3 distanceVector = new Vector3((this.xOffset + this.player.transform.position.x - this.transform.position.x),
                                             (this.yOffset + this.player.transform.position.y - this.transform.position.y),
                                             0);
        float distance = distanceVector.magnitude;


        Vector3 translateVector = distanceVector * this.lag * (float)(this.softCutoffSlope * Math.Exp((1/this.softCutoffSlope) * (distance - this.softCutoffThreshold)) + 1);


        if (distance > 0.5) 
        {
            this.transform.Translate(translateVector * Time.deltaTime);
        }
    }
}

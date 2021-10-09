using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Camera : MonoBehaviour
{
    // == REFERENZEN
    public Entity subject;

    // == ATTRIBUTE
    public float lag = 2f;
    public float xOffset = 0f;
    public float yOffset = 0f;

    public float softCutoffSlope = 3;
    public float softCutoffThreshold = 4;

    public bool isFollowing = true;


    private void Awake()
    {
        this.subject = GameObject.Find("Player").GetComponent<Player>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (this.isFollowing)
        {
            Vector3 position = new Vector3(this.subject.transform.position.x,
                                           this.subject.transform.position.y,
                                           this.transform.position.z);
        
            Vector3 distanceVector = new Vector3((this.xOffset + this.subject.transform.position.x - this.transform.position.x),
                                                (this.yOffset + this.subject.transform.position.y - this.transform.position.y),
                                                0);
            float distance = distanceVector.magnitude;


            Vector3 translateVector = distanceVector * this.lag;//* (float)(this.softCutoffSlope * Math.Exp((1/this.softCutoffSlope) * (distance - this.softCutoffThreshold)) + 1);


            if (distance > 0.5) 
            {
                this.transform.Translate(translateVector * Time.deltaTime);
            }
        }
    }

    public void Enable()
    {
        this.isFollowing = true;
    }

    public void Disable()
    {
        this.isFollowing = false;
    }

    public void SetSubject(Entity subject)
    {
        this.subject = subject;
    }
}

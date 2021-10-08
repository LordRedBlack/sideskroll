using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{
    // == REFERENZEN
    public Player player;

    // == ATTRIBUTE
    public float lag = 2f;
    public float xOffset = 0f;
    public float yOffset = 0f;


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
        Vector3 translateVector = new Vector3((this.xOffset + this.player.transform.position.x - this.transform.position.x) * this.lag,
                                              (this.yOffset + this.player.transform.position.y - this.transform.position.y) * this.lag,
                                              0);
        this.transform.Translate(translateVector * Time.deltaTime);
        
    }
}

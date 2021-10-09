using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hook : MonoBehaviour
{

    // == REFERENCES
    public Rigidbody2D rb;
    public Player player;
    public LineRenderer lr;

    // == ATTRIBUTES
    public float maxDistance = 10;
    public float maxTime = 1.5f;

    public float time = 0;
    public bool isHooked = false;

    void Awake()
    {
        this.rb = this.GetComponent<Rigidbody2D>();
        this.lr = this.GetComponent<LineRenderer>();

        this.lr.positionCount = 2;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 distanceVector = new Vector2(this.transform.position.x - this.player.transform.position.x,
                                             this.transform.position.y - this.player.transform.position.y);
        Vector2 forceVector = distanceVector / distanceVector.magnitude;

        if (this.isHooked == true)
        {
            this.player.lockVelocity = true;
            this.player.rb.AddForce(forceVector * 1, ForceMode2D.Force);
        }
        
        this.time += Time.deltaTime;
        
        if (this.time > this.maxTime)
        {
            this.Delete();
        }

        this.lr.SetPosition(0, this.transform.position);
        this.lr.SetPosition(1, this.player.transform.position);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Hook")
        {
            this.Attach();
        }
        else if (collision.gameObject.tag == "Player")
        {
            // 
        } else
        {
            this.Delete();
        }
    }

    public void Attach()
    {
        this.rb.bodyType = RigidbodyType2D.Static;
        this.rb.velocity = new Vector2(0, 0);

        this.isHooked = true;
        this.player.isHooked = true;
    }

    public void Delete()
    {
        this.player.lockVelocity = false;
        this.player.ReleaseHook();
        Destroy(this.gameObject);
    }
}

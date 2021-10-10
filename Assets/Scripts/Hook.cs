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

    public float hookForce = 1.3f;

    public float minDistance = 1f;

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

        // - Hook delete conditions

        // Der Hook existiert nur eine bestimmte Zeit
        this.time += Time.deltaTime;

        if (this.time > this.maxTime)
        {
            this.Delete();
        }

        // Der Hook kann nur so kurz werden
        if (this.isHooked && distanceVector.magnitude < this.minDistance)
        {
            this.Delete();
        }

        if (this.isHooked == true)
        {
            this.player.lockVelocity = true;
            this.player.rb.AddForce(forceVector * this.hookForce, ForceMode2D.Force);
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
        this.rb.velocity = new Vector2(0, 0);
        this.rb.bodyType = RigidbodyType2D.Static;

        this.isHooked = true;
        this.player.isHooked = true;
    }

    public void Delete()
    {
        this.player.ReleaseHook();
        Destroy(this.gameObject);
    }
}

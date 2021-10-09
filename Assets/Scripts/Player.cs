using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // == REFERENZEN

    public Rigidbody2D rb;
    public Animator anim;
    public SpriteRenderer sr;
    public ParticleSystem jumpPS;
    //!!!!
    public ParticleSystem walkPS;
    //!!!!
    // == ATTRIBUTE

    // Die Geschwindigkeit welche der Charakter on default hat
    public float speed = 10f;
    
    // !!!
    // Wir brauchen einen boolean der togglet ob man die Geschwindigkeit modifizieren kann oder nicht
    // weil sobald man aus dem Walljump rauskommt wird die x Geschwind. überschrieben.
    // Am besten togglen zu wie viel Prozent man die Geschw. bestimmen kann.
    
    public float jumpForce = 100f;
    
    // Die Richtung in welche der Charakter zeigt.
    // 1 bedeuetet der Charakter zeigt nach rechts
    // -1 dementsprechend nach links
    public int direction = 1;

    public bool inAir = false;
    public bool isWallsliding = false;

    public bool jumpAvailable = false;
    public bool walljumpAvailable = false;

    // possible states
    // - idle
    // - walk
    // - jump
    // - wallslide
    public string state = "idle";

    // Start is called before the first frame update
    void Start()
    {
        this.rb = this.GetComponent<Rigidbody2D>();
        this.anim = this.GetComponent<Animator>();
        this.sr = this.GetComponent<SpriteRenderer>();

        GameObject jumpPsGO = this.transform.GetChild(0).gameObject as GameObject;
        this.jumpPS = jumpPsGO.GetComponent<ParticleSystem>();

        GameObject walkPsGO = this.transform.GetChild(1).gameObject as GameObject;
        this.walkPS = walkPsGO.GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // float horizontal = Input.GetAxis("Horizontal") * Time.deltaTime * speed;
        // float vertical = Input.GetAxis("Vertical") * Time.deltaTime * speed;

        float velocity = this.speed * Input.GetAxis("Horizontal");
        if (this.isWallsliding == true)
        {
            velocity = velocity * 0.1f;
        }
        Vector2 velocityVector = new Vector2(velocity, this.rb.velocity.y);
        this.rb.velocity = velocityVector;


        if (Input.GetAxis("Jump") == 1 && this.inAir == false)
        {
            Debug.Log("Jump!");
            Vector2 jumpVector = new Vector2(0, this.jumpForce);
            this.rb.AddForce(jumpVector, ForceMode2D.Impulse);
            this.inAir = true;
            jumpPS.Play();
        }

        if (Input.GetAxis("Jump") == 1 && this.isWallsliding && this.walljumpAvailable)
        {
            Vector2 jumpVector = new Vector2(0.4f * this.direction, 0.6f) * this.jumpForce * 1.3f;
            this.rb.AddForce(jumpVector, ForceMode2D.Impulse);
            this.walljumpAvailable = false;
        }


        if (this.isWallsliding == true)
        {
            this.state = "wallsliding";
        }
        if (this.isWallsliding == false && this.inAir == true)
        {
            this.state = "jump";
        }
        else if (this.inAir == false && velocity == 0)
        {
            this.state = "idle";
        }
        else if (this.inAir == false && velocity != 0)
        {
            this.state = "walk";
            walkPS.Play();
        }

        if (velocity > 0)
        {
            this.direction = 1;
        } else
        {
            this.direction = -1;
        }

        if (this.isWallsliding)
        {
            this.direction *= -1;
        }

        if (inAir != true && velocity != 0)
        {
            walkPS.Play();
        }

        this.UpdateAnimation();
    }

    public void UpdateAnimation()
    {
        if (this.state == "idle")
        {
            this.anim.Play("player_idle");
        }
        else if (this.state == "walk")
        {
            this.anim.Play("player_walk");
        } 
        else if (this.state == "jump")
        {
            this.anim.Play("player_jump");
        }
        else if (this.state == "wallsliding")
        {
            this.anim.Play("player_wallslide");
        }

        if (this.direction == 1)
        {
            this.sr.flipX = false;
        } 
        else if (this.direction == -1) 
        {
            this.sr.flipX = true;
        }
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            this.inAir = false;
            this.jumpAvailable = true;
            this.walljumpAvailable = true;
        }
    }

    public void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Wallslide")
        {
            this.isWallsliding = true;
        }
    }

    public void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Wallslide")
        {
            this.isWallsliding = false;
            Debug.Log("Exit wall");
        }
    }
}

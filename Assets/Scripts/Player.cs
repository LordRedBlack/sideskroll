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
    // weil sobald man aus dem Walljump rauskommt wird die x Geschwind. �berschrieben.
    // Am besten togglen zu wie viel Prozent man die Geschw. bestimmen kann.
    public bool lockVelocity = false;
    
    public float jumpForce = 100f;
    
    // Die Richtung in welche der Charakter zeigt.
    // 1 bedeuetet der Charakter zeigt nach rechts
    // -1 dementsprechend nach links
    public int direction = 1;

    public bool inAir = false;
    public bool isWallsliding = false;

    public bool jumpAvailable = false;
    public bool walljumpAvailable = false;
    
    public bool lockWalljump = false;
    public int maxWalljumps = 2;
    public int availableWalljumps = 0;

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

        if (this.lockVelocity == false) 
        {
            Vector2 velocityVector = new Vector2(velocity, this.rb.velocity.y);
            this.rb.velocity = velocityVector;
        }


        if (Input.GetAxis("Jump") == 1 && this.inAir == false)
        {
            Vector2 jumpVector = new Vector2(0, this.jumpForce);
            this.rb.AddForce(jumpVector, ForceMode2D.Impulse);
            this.inAir = true;
            jumpPS.Play();
        }

        // ~ Process Walljump
        if (Input.GetAxis("Jump") == 1 && 
            this.isWallsliding && 
            this.availableWalljumps > 0 &&
            this.lockWalljump == false)
        {
            this.StartCoroutine(this.Walljump());
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

    public IEnumerator Walljump()
    {
        // Diese Flag verhindert dass rechts und links Steuerungseingaben die velocity überschreiben
        this.lockVelocity = true;

        // Der Jump Vector wird als Kraftstoß auf den Rigidbody ausgeführt. Ein Walljump hat sowohl eine y als auch eine kleine 
        // x Komponente.     
        Vector2 jumpVector = new Vector2(0.3f * this.direction, 0.7f) * this.jumpForce * 1.5f;
        this.rb.AddForce(jumpVector, ForceMode2D.Impulse);
        // Damit wird einer der verfügbaren Walljumps verbraucht. Ausßerdem müssen wir locken damit nicht mehrer jumps auf einmal verbraucht 
        // werden.
        this.availableWalljumps -= 1;
        this.lockWalljump = true;
        // Jump Partikel effekt
        this.jumpPS.Play();

        // Für eine bestimmte Zeit wird die Eingabe blockiert, damit die x Geschwindigkeit des Jumps nicht 
        // überschrieben wird. Danach sind die Controls wieder voll freigegeben.
        yield return new WaitForSeconds(0.3f);
        this.lockVelocity = false;
    }

    public void ResetWalljumps()
    {
        this.availableWalljumps = this.maxWalljumps;
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
            
            // Wenn wir einen "richtigen" Boden wieder berühren füllen sich unsere Walljumps auf
            this.ResetWalljumps();
        }

        // Wenn wir wieder eine Wall berühren wird der walljump wieder freigeschalten. Der Walljump wird blockiert sobald ein 
        // neuer walljump getriggert wird. Diese Mechanik brauchen wir in Verbindung mit multiple walljumps damit nicht aus Versehen meherer 
        // jump Vektoren auf einmal aktiviert werden und alle jumps in einem "super jump" verbraucht werden.
        if (collision.gameObject.tag == "Wallslide")
        {
            this.lockWalljump = false;
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
        }
    }
}

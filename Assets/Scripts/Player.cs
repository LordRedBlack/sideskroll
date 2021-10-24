using System.IO;
// using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum PlayerState
{
    Idle,
    Walking,
    Falling,
    Wallslide,
    Hooked,
}

/*
TODO: Base the wallJump Vector on the Wall normal vector?

Das ist die main Klasse welche so ziemlich alles was mit dem Player zu tun hat managed. Das beinhalteted momentan vor allem folgendes:
- Das Movement
- State management und darauf basierend die Animationen

Folgende Dinge müssen beachtet werden wenn dieses Component genutzt werden soll:
- 
*/

public class Player : Entity
{
    // == REFERENZEN

    // -- internal
    public Rigidbody2D rb;
    public Animator anim;
    public SpriteRenderer sr;
    public ParticleSystem jumpPS;
    public ParticleSystem walkPS;

    // -- external 
    public GameState gs;

    public GameObject hookPrefab;
    public Hook hook;

    // == ATTRIBUTE

    // Die Geschwindigkeit welche der Charakter on default hat
    [SerializeField] public float speed = 40f;


    protected Vector3 velocity = Vector3.zero;

    public float walkForce = 20f;
    public float airForce = 5f;
    
    // Wir brauchen einen boolean der togglet ob man die Geschwindigkeit modifizieren kann oder nicht
    // weil sobald man aus dem Walljump rauskommt wird die x Geschwind. �berschrieben.
    // Am besten togglen zu wie viel Prozent man die Geschw. bestimmen kann.
    public bool lockVelocity = false;
    
    public float jumpForce = 400f;
    
    // Die Richtung in welche der Charakter zeigt.
    // 1 bedeuetet der Charakter zeigt nach rechts
    // -1 dementsprechend nach links
    public int direction = 1;

    public bool inAir = false;
    public bool isWallsliding = false;

    public bool jumpAvailable = false;
    public bool walljumpAvailable = false;
    
    public bool lockJump = false;
    public int maxWalljumps = 2;
    public int availableWalljumps = 0;

    public int availableHooks = 1;
    public bool isHooked = false;
    public bool hooked = false;

    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public LayerMask wallslideLayer;
    [SerializeField] public Transform groundCheck;
    [SerializeField] public Transform ceilingCheck;
    
    [SerializeField] public bool grounded = false;
    [SerializeField] public bool wallsliding = false;

    [Range(0f, .3f)] [SerializeField] public float movementSmoothing = 0.05f;
    [Range(0f, 1f)] [SerializeField] public float airSpeedModifier = 0.3f;

    [SerializeField] public Vector2 wallslideNormalVector = Vector2.zero;

    // Das ist die stärke der Kraft mit der wir beim Wallsliden an die Wand gedrückt werden. Das ist einer der Punkte welche bestimmen wie stark man an 
    // der Wand haftet. klar, je stärker diese Kraft desto eher haftet man an der Wand. Der andere Aspekt ist "friction" vom Wand Material. Je größer 
    // das ist desto eher haftet man auch. Im Normalfall sollte man die friction der Wand nutzen um das einzustellen und diesen Wert konstant halten.
    [SerializeField] public float wallslideNormalForce = 60f;

    [SerializeField] public float swingCooldown = 0f;

    // possible states
    // - idle
    // - walk
    // - jump
    // - wallslide
    public PlayerState state = PlayerState.Idle;

    // == CONSTANTEN

    // Der Radius um den "GroundCheck" GameObject des Players, welcher abgesucht werden soll nach Collisions mit "Ground" Layer Objekten
    // Wenn eine solche Collision registriert wird, dann wird der jump zurückgesetzt.
    public const float groundedRadius = .2f;

    public const float wallslideRadius = 0.4f;

    // Dieser Wert gibt die Grenze der x Geschwindigkeit an, ab der man "idle" und "walking" animation switched. Sollte ein kleiner Wert ungleich null sein!
    const float idleVelocityThreshold = 0.1f;

    const float gravityScale = 3;


    // Awake ist die Methode in der man die automatische Referenz Bildung von anderen Components und GameObjects vornehmen sollte!
    void Awake()
    {
        this.rb = this.GetComponent<Rigidbody2D>();
        this.anim = this.GetComponent<Animator>();
        this.sr = this.GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        GameObject jumpPsGO = this.transform.GetChild(0).gameObject as GameObject;
        this.jumpPS = jumpPsGO.GetComponent<ParticleSystem>();

        GameObject walkPsGO = this.transform.GetChild(1).gameObject as GameObject;
        this.walkPS = walkPsGO.GetComponent<ParticleSystem>();

        GameObject goGameState = GameObject.Find("GameState") as GameObject;
        this.gs = goGameState.GetComponent<GameState>();
    }

    // Update is called once per frame
    // - Ich glaube FixedUpdate sollte man wirklich nur für movement und physics benutzen. Sowas wie die Animation updates sollte ich dann eher in die 
    //   normale Update Methode packen?
    void FixedUpdate()
    {
        // ~ HOOKED CONTROLS
        // Wenn wir hooked sind brauchen wir komplett andere controls. Wenn man weiterhin die velocity the RB modifiziert dann kommen das ganz komische 
        // Effekte heraus. Während wir hooked sind dürfen wir eigenltich nur über Kräfte navigieren!
        if (this.hooked) 
        {
            // Hier hatte ich zuerst ausprobiert über kurze Kraft Impulse die Steuerung zu machen. Die hätten dann einen Cooldown und dann könnte man so alle 
            // halbe Sekunde oder so mit einem kleinen Ruck navigieren. Das hat sich schrecklich angefühlt. Eine kleine konstante Kraft ist deutlich besser 
            // vom feeling her!
            this.rb.AddForce(new Vector2(Input.GetAxisRaw("Horizontal") * 2, 0), ForceMode2D.Force);
        }
        else 
        {
            // ~ GROUNDED CHECK
            // Diese Section gestohlen von https://github.com/Brackeys/2D-Character-Controller/blob/master/CharacterController2D.cs
            this.grounded = false; // Wir gehen davon aus dass wir nicht grounded sind bis wir tatsächlich eine Koll. finden die das Gegenteil beweist!
            Collider2D[] colliders = Physics2D.OverlapCircleAll(this.groundCheck.position, groundedRadius, this.groundLayer);
            for (int i = 0; i < colliders.Length; i++) 
            {
                if (colliders[i].gameObject != this.gameObject) // Kollision mit Teilen von uns selbst passiert immer und wollen wir ignorieren
                {
                    this.grounded = true;
                    // An dieser Stelle können wir noch zusätzliche Sachen passieren lassen wenn grounded auf true gesetzt wird. Vielleicht iwan sowas 
                    // wie ein Partikeleffekt wenn man auf dem Boden aufkommt?
                    break;
                }
            }

            // ~ WALLSLIDING CHECK
            // Ich weiß das ist ziemlich ineffektiv, aber für den Moment ist es übersichtlicher das zu trennen
            this.wallsliding = false; 
            colliders = Physics2D.OverlapCircleAll(this.transform.position, wallslideRadius, this.wallslideLayer);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject != this.gameObject)
                {
                    // Wenn man nahe an einer Wand dran ist. Was in diese condition der Fall ist, dann soll man so ein bisschen daran "snappen". Die Idee hier ist 
                    // Im "wallslide" state eine konstante kleine Kraft in die Richtung der Wand auszuüben, dass man in bisschen an ihr slided. Dazu müssen wir 
                    // der Methode eben diesen Punkt übergeben.
                    Vector2 closestPoint = colliders[i].ClosestPoint(this.transform.position);
                    Vector2 normalVector = closestPoint - (Vector2) this.transform.position;
                    this.wallslideNormalVector = normalVector.normalized;
                    this.wallsliding = true;
                    break;
                }
            }

            // ~ HORIZONTAL MOVEMENT
            // - Dieses "SmoothDamp" ist genau das was ich gebraucht habe! Diese Funktion tut smooth über die Zeit einen Vektor an einen anderen annähern. Das erzeugt nicht 
            //   den gleichen Bug wenn man velocity einfach hart überschreibt. Andere Kräfte können jetzt immernoch auf den RB einwirken.
            // - Ich weiß nicht was dieses zusätzliche "ref ..." da drinnen macht?
            // - GetAxisRaw glaube ich returned einen float im Gegensatz zum normalen GetAxis welches einen binary int (1 oder -1) gibt. Mit raw hat man glaube ich die Unterstüzung auch 
            //   für controller mit analog sticks.
            float move = this.speed * Input.GetAxisRaw("Horizontal");
            if (this.grounded == false) { move *= this.airSpeedModifier; }
            Vector3 targetVelocity = new Vector2(move * Time.fixedDeltaTime, this.rb.velocity.y);
            this.rb.velocity = Vector3.SmoothDamp(this.rb.velocity, targetVelocity, ref this.velocity, this.movementSmoothing);
            
            // ~ JUMPING
            // - Ich hab herausgefunden dass man die Inputs eigentlich in der Update Methode verarbeitet und dann über eine button-pressed-flag an die 
            //   FixedUpdate Methode weitergibt, aber ich mache es einfach so und unterbinde das double trigger problem mit der lock Flag welche über eine sub routine 
            //   versetzt zurückgesetzt wird.
            if (Input.GetAxis("Jump") == 1 && 
                this.grounded == true && 
                this.lockJump == false)
            {
                StartCoroutine(this.Jump());
            }

            // Walljump
            if (Input.GetAxis("Jump") == 1 && 
                this.wallsliding && 
                this.availableWalljumps > 0 &&
                this.lockJump == false)
            {
                StartCoroutine(this.Walljump());
            }
        }

        // ~ PROCESS WALLSLIDING
        if (this.wallsliding == true) 
        {
            // Wenn die wallsliding flag auf true gesetzt ist, dann bedeutet das in der Nähe wurde eine wallslide Fläche erkannt. Außerdem wissen wir dass in 
            // this.wallslideNormalVector derjenige Vector liegt welcher genau zwischen dem Mittelpunkt des Spielers und dem nähesten Punkt der Wallslide Fläche 
            // verläuft. Jezt wollen wir das der Spieler an diese Fläche "snappt" und auch kleben bleibt wenn man nicht aktiv in die Richtung steuert. 
            // dass erreichen wir durch eine konstante Kraft in die Richtung der Fläche.
            this.state = PlayerState.Wallslide;
            this.rb.AddForce(this.wallslideNormalVector * this.wallslideNormalForce, ForceMode2D.Force);

            // Wenn wir wallsliden, dann können wir unsere Blickrichtung daraus ermitteln auf welcher Seite die Wand ist. Wir wollen dann natürlich genau in die 
            // andere Richtung schauen.
            if (this.wallslideNormalVector.x > 0) 
            {
                this.direction = -1;
            } 
            else 
            {
                this.direction = 1;
            }
        } 
        else 
        {
            // Wenn der Spieler nicht wallslided, dann muss er entweder auf dem Boden oder in der Luft sein.
            if (this.grounded) 
            {
                if (Math.Abs(this.rb.velocity.x) < idleVelocityThreshold) {
                this.state = PlayerState.Idle;
                }
                else
                {
                    this.state = PlayerState.Walking;
                }
            }
            else
            {
                this.state = PlayerState.Falling;
            }

            // Für sowohl Boden als auch in der Luft gelten die gleichen Regeln für die Direction. Wir stellen ein dass der Spieler 
            // in die Richtung orientiert ist, in die seine x Geschwindigkeit zeigt.
            if (this.rb.velocity.x >= idleVelocityThreshold) 
            {
                this.direction = 1;
            }
            else if (this.rb.velocity.x <= -idleVelocityThreshold)
            {
                this.direction = -1;
            } 
        }

        // ~ Process Shooting
        if (Input.GetAxis("Fire1") == 1 &&
            this.availableHooks > 0 && 
            this.hooked == false) 
        {
            StartCoroutine(this.ShootHook());
        }

        if (Input.GetAxis("Fire1") == 1 &&
            this.hooked == true)
        {
            this.hook.Delete();
        }

        this.UpdateAnimation();
    }

    public void EnterWallslide(Vector2 wallVector)
    {
        // Der Punkt den wir hier übergeben bekommen ist nur der nähste Punkt an der Wand Punkt. Wir wollen jetzt den Normalenvektor mit dem Player.
        // Also ein Vektor zwischen diesem Punkt an der Wand und dem Mittelpunkt der Wand.
        // diesen Vektor können wir dann benutzen um den Spieler mit einer leichten Kraft konstant an die Wand zu drücken
        Vector2 normalVector = wallVector - (Vector2) this.transform.position;
        this.rb.AddForce(normalVector.normalized * 100f, ForceMode2D.Force);

        this.state = PlayerState.Wallslide;
    }

    public IEnumerator ShootHook()
    {
        Vector3 mousePosition = UnityEngine.Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;
        Vector2 mouseVector = new Vector2(mousePosition.x, mousePosition.y);
        //Instantiate(this.hookPrefab, mousePosition, Quaternion.identity);

        Vector2 directionVector = new Vector2(mousePosition.x - this.transform.position.x,
                                              mousePosition.y - this.transform.position.y);
        
        GameObject goHook = Instantiate(this.hookPrefab, this.transform.position, Quaternion.identity) as GameObject;
        
        Hook hook = goHook.GetComponent<Hook>();
        hook.player = this;

        this.hook = hook;
        hook.rb.AddForce(directionVector * 0.4f, ForceMode2D.Impulse);
        this.availableHooks -= 1;
        Physics2D.IgnoreCollision(hook.GetComponent<CircleCollider2D>(), this.GetComponent<CircleCollider2D>());

        yield return new WaitForSeconds(0.1f);
    }

    public void ReleaseHook()
    {
        this.StartCoroutine(this.DetachHook());
    }

    public IEnumerator DetachHook()
    {
        this.rb.bodyType = RigidbodyType2D.Dynamic;
        this.hooked = false;

        float tMax = 0.8f;
        float t = 0f;
        while (t < tMax)
        {

            this.rb.gravityScale = Mathf.Lerp(-gravityScale * 2, gravityScale, t / tMax);
            t += Time.fixedDeltaTime;
            yield return null;
        }

        this.rb.gravityScale = gravityScale;
        this.availableHooks += 1;
    }

    public IEnumerator Jump()
    {
        Vector2 jumpVector = new Vector2(0, this.jumpForce);

        this.rb.AddForce(jumpVector, ForceMode2D.Impulse);
        this.grounded = false;
        this.lockJump = true;

        jumpPS.Play();

        yield return new WaitForSeconds(0.2f);
        
        this.lockJump = false;
    }


    public IEnumerator Walljump()
    {
        // Damit wird einer der verfügbaren Walljumps verbraucht. Ausßerdem müssen wir locken damit nicht mehrer jumps auf einmal verbraucht 
        // werden.
        this.availableWalljumps -= 1;
        this.lockJump = true;

        // Der Jump Vector wird als Kraftstoß auf den Rigidbody ausgeführt. Ein Walljump hat sowohl eine y als auch eine kleine 
        // x Komponente.     
        Vector2 jumpVector = new Vector2(this.direction, 1) * this.jumpForce * 1.2f;
        this.rb.AddForce(jumpVector, ForceMode2D.Impulse);

        // Jump Partikel effekt
        this.jumpPS.Play();

        // Ok, also folgendes Problem: Wann man ander Wand klebt dann drückt der Spieler aktiv die Richtungstaste welche an die Wand zeigt. Wenn man dann durch den Kraftimpuls von der 
        // Wand weggeschleudert wird und dann die air controls direkt wieder funktionieren, dann klatscht man sehr schnell wieder zurück an die Wand.
        // Deshalb machen wir hier folgendes: Über einen kurzen Zeitraum unterdrücken wir die air controls ein bisschen -> damit fühlt sich das ganze sehr viel smoother an.
        float t = 0f;
        float tMax = 0.6f;
        while (t < tMax) 
        {
            this.airSpeedModifier = Mathf.Lerp(0f, 0.7f, t / tMax);
            t += Time.deltaTime;
            yield return null;
        }

        // Am Ende geben wir den Sprung wieder frei und setzen die air controls zurück.
        this.airSpeedModifier = 1f;
        this.lockJump = false;
    }

    public void ResetWalljumps()
    {
        this.availableWalljumps = this.maxWalljumps;
    }

    public void UpdateAnimation()
    {
        if (this.state == PlayerState.Idle)
        {
            this.anim.Play("player_idle");
        }
        else if (this.state == PlayerState.Walking)
        {
            this.anim.Play("player_walk");
        } 
        else if (this.state == PlayerState.Falling)
        {
            this.anim.Play("player_jump");
        }
        else if (this.state == PlayerState.Wallslide)
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
            //this.lockWalljump = false;
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
        if (collision.gameObject.tag == "Ground")
        {
            this.inAir = true;
        }

        if (collision.gameObject.tag == "Wallslide")
        {
            this.isWallsliding = false;
        }
    }
}

using System;
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
    
    // Das ist die Zeit in Sekunden nach welcher der Hook verschwindet, also dessen Lebensdauer. Der Hook an sich ist ja schon mächtig und deshalb braucht man 
    // irgendeine Limitation mit der man challanges aufbauen kann. Ich denke eben die Dauer ist ganz gut. Das zwingt den Spieler schnell zu handeln.
    [SerializeField] public float maxTime = 5f;

    // Dieses Feld enthält die Zeit, welche der Hook *tatsächlich* schon existiert. Dieser Counter wird bis zur max time zählen und dann wird getriggert 
    // dass der hook sich selbst löscht.
    [SerializeField] public float time = 0;

    // Die Lebensdauer des Hooks soll die entscheidende Schwierigkeit bei der Verwendung sein. Dazu muss man dem Spieler aber irgendwie eine visuelle Indication 
    // geben wann diese Lebensdauer um ist. Dazu habe ich mir überlegt, dass das "Seil" des hooks (= Der line renderer) über die Lebensdauer seine farbe ändert.
    // Dazu diese Status variable. Über die Lebensdauer wird sie von 0 -> 1 ge-lerpt.
    [SerializeField] public float shade = 0f;

    // Insgesamt funktioniert der Hook, oder genauer gesagt das Seil des hooks, wie eine physikalische Feder. Oder ich denke eine bessere Beschreibung ist ein 
    // Gummiband. Dieses Band hat eine gewisse Grundlänge zu der es zurückkehren will. Diese Variable hier gibt diese Grundlänge an. Im Mittel wird sich das Gummi 
    // auf diese Länge einpendeln
    [SerializeField] public float baseLength = 3f;

    // Diese maximale Länge gilt nur für das schießen des Hooks! Wir wollen natürlich dass der Hook eine begrenzte range hat. Das ist diese Variable. Wenn der hook 
    // diese Länge überschreitet und noch nirgends haften geblieben ist, dann wird er gelöscht.
    [SerializeField] public float maxLength = 10f;

    // Diese Variable hat zu jedem Moment die *tatsächliche* Länge des Seils, gemessen als der direkte Abstand zwischen dem Spieler und dem Hook Projektil.
    [SerializeField] public float length = 0f;

    // Wie gesagt soll der Hook ein Gummiband sein und ein Gummiband ist genauer gesagt eine Feder. Eine Feder funktioniert so: Sie übt eine Kraft in Richtung der 
    // Gleichgewichtslage (Grundzustand) aus. Diese Kraft ist proportional zur Auslenkung. D.h. je weiter man eine Feder ausseinanderzieht desto höher die Kraft. 
    // der lineare Proportionalitätsfaktor heißt Federkonstante und ist dieser Wert hier.
    [SerializeField] public float springConstant = 15f;

    // Dieser Wert heißt "Dämpfungskonstante". Streng genommen kommt sowas in einer echten Feder nicht vor, aber ich habe gemerkt dass die Feder EXTREM große 
    // Auslenkungen erreichen kann. Deshalb macht es Sinn das zu dämpfen. Eine solche Dämpfungskonstante erzeugt eine Kraft *entgegen* des Grundzustands proportional 
    // zur momentanten *Geschwindigkeit*
    [SerializeField] public float dampeningConstant = 5f;

    // Dies ist ebenfalls eine vorsichtsmaßnahme gegen zu große Schwingungen. Wir limitieren die maximale Kraft die von der Feder ausgeübt werden kann.
    [SerializeField] public float maxForce = 300f;

    
    [SerializeField] public LayerMask hookLayer = 8;

    // Das ist eine Flag, die anzeigt ob der hook schon irgendwo kleben geblieben ist.
    [SerializeField] public bool attached = false;

    // "length" ist die absolute Länge. Aber stretch ist die Auslenkung aus der Ruhelage. Also die Differenz von length und base length. Also wie viel länger ist 
    // der hook als er eigentlich sein will?
    public float stretch = 0f;
    // Diese Variable speichert den Wert von stretch aus der vorherigen Berechnung. Das ist eine Methode für Arme um die Änderungsgeschwindigkeit zu schätzen. Diese 
    // Änderungsgeschwindigkeit brauchen wir für die Dämpfung welche ja proportional dazu ist.
    public float prevStretch = 0f;


    // CONSTANTS
    public float hookRadius = 0.1f;


    void Awake()
    {
        this.rb = this.GetComponent<Rigidbody2D>();
        this.lr = this.GetComponent<LineRenderer>();
        this.lr.material = new Material(Shader.Find("Sprites/Default"));

        this.lr.positionCount = 2;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector2 distanceVector = new Vector2(this.transform.position.x - this.player.transform.position.x,
                                             this.transform.position.y - this.player.transform.position.y);
        this.length = distanceVector.magnitude;

        Vector2 forceVector = distanceVector / distanceVector.magnitude;

        // ~ ATTACH CHECK
        // Hier prüfen wir ob der Hook mit einer "hookbaren" Oberflächer kollidiert. Diesen Check müssen wir aber nur so lange machen wie das noch nicht 
        // der Fall ist. Also sobald wir einmal hooked sind bleibt das auch so
        if (this.attached == false) 
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(this.transform.position, hookRadius, this.hookLayer);
            for (int i = 0; i < colliders.Length; i++) 
            {
                if (colliders[i].gameObject != this.gameObject) // Kollision mit Teilen von uns selbst passiert immer und wollen wir ignorieren
                {
                    this.Attach();
                    break;
                }
            }


            if (this.length > this.maxLength) 
            {
                this.Delete();
            }
        }

        // ~ SPRING PHYSICS IMPLEMENTATION
        // Also der Hook soll im Grunde wie einer Feder funktionieren. Diese Feder hat eine bestimmte Länge, wenn sie auf dieser Länge gestrechted wird oder sogar 
        // kürzer ist, dann übt sie keine Kraft aus. Wenn die Feder allerdings Länger gestrechted wird als diese Länge, dann übt sie eine Kraft aus, welche proporional 
        // zur Auslenkung ist.
        this.prevStretch = this.stretch;
        this.stretch = this.length - this.baseLength;
        if (stretch > 0 && this.attached == true) 
        {
            // 
            float force = stretch * this.springConstant + this.dampeningConstant * (this.stretch - this.prevStretch) * 20;
            force = force < this.maxForce ? (force > 0 ? force : 0)  : this.maxForce;

            this.player.rb.AddForce(distanceVector.normalized * force, ForceMode2D.Force);
        }


        // ~ UPDATE LINE RENDERING
        // Zuerst wollen wir die Farbe updaten. Der Zustandswert für den Farbton ergibt sich aus einem Lerp über die verstrichene Zeit. Das ist ein Wert zwischen 0 und 1 weil er 
        // als prozent wert für RGB Farbcode benutzt werden soll. Die tatsächliche Farbe ändert sich von Weiß zu rot und wird währenddessen auch immer durchsichtiger
        this.shade = Mathf.Lerp(0, 1, this.time / this.maxTime);
        Color color = new Color(1, 1 - this.shade, 1 - this.shade, 1 - this.shade);
        this.lr.SetColors(color, color);

        // Dann müssen wir auch den LineRenderer selber updaten, also Startpunkt auf den Spieler setzen und Endpunkt an den Hook, damit es auch weiterhin so aussieht, dass die beiden 
        // durch ein "seil" verbunden wären.
        this.lr.SetPosition(0, this.transform.position);
        this.lr.SetPosition(1, this.player.transform.position);
    
        // Klar wir müssen die Zeit updaten. Wichtig, da wir in FixedUpdate sind müssen wir hier fixedDeltaTime nehmen.
        // Wenn die Zeit die Lebensdauer überschreitet soll der Hook aufhören zu existieren.
        this.time += Time.fixedDeltaTime;
        if (this.time > this.maxTime)
        {
            this.Delete();
        }

    }

    public void Attach()
    {
        // Zuerst mal setzen wir unsere eigene Flag. Das verhindert dass wir in Zukunft weiter auf Collisionen mit möglichen hookbaren 
        // Oberflächen prüfen. Wenn wir einmal hooked sind brauchen wir das ja nicht.
        this.attached = true;
        // Dann müssen wir auch eine entprechende flag im Player setzen, damit dort im Code die entsprechenden Konsequenzen angewandt werden können.
        // Zum Beispiel werden die Controls leicht geändert während man hooked ist.
        this.player.hooked = true;

        // Die Wichtigste Änderung ist, dass sich der Hook danach nicht mehr bewegen darf!
        this.rb.velocity = new Vector2(0, 0);
        this.rb.bodyType = RigidbodyType2D.Static;
    }

    public void Delete()
    {
        this.player.ReleaseHook();
        Destroy(this.gameObject);
    }
}

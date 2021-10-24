using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 Das Grundgerüst dieses Skripts basiert auf diesem Video von Brackeys: https://www.youtube.com/watch?v=dwcT-Dch0bA


*/
public class PlayerMovement : MonoBehaviour
{

    [SerializeField] public CharacterController2D controller;

    [SerializeField] public float runSpeed = 80f;
    [SerializeField] public float horizontalMove = 0f;
    [SerializeField] public bool jump = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.horizontalMove = Input.GetAxisRaw("Horizontal") * this.runSpeed;

        if (Input.GetButtonDown("Jump"))
        {
            this.jump = true;
        }
    }

    void FixedUpdate()
    {
        // Das random "false" da drin ist momentan ein Platzhalter für eine Crouch funktion, welche der Controller auch unterstützt.
        this.controller.Move(horizontalMove * Time.fixedDeltaTime, false, this.jump);
        jump = false;
    }
}

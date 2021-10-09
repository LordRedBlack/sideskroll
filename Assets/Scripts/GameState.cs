using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    // == REFERENCES

    public GameObject goPlayer;
    public Player player;

    // Start is called before the first frame update
    void Start()
    {
        this.goPlayer = GameObject.Find("Player");
        this.player = this.goPlayer.GetComponent<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

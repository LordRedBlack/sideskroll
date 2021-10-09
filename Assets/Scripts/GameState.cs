using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState instance;

    // == REFERENCES
    public GameOverScreen GameOverScreen;

    public GameObject goPlayer;
    public Player player;

    public GameObject goCamera;
    public Camera cam;

    public Transform respawnPoint;
    public GameObject playerPrefab;


    public void GameOver()
    {
        GameOverScreen.Setup();
    }
    private void Awake()
    {
        instance = this;
        
    }

    public void Respawn ()
    {
        Instantiate(playerPrefab, respawnPoint.position, Quaternion.identity);
    }

    // Start is called before the first frame update
    void Start()
    {
        this.goPlayer = GameObject.Find("Player");
        this.player = this.goPlayer.GetComponent<Player>();

        GameObject goCamera = GameObject.Find("Main Camera") as GameObject;
        this.cam = goCamera.GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

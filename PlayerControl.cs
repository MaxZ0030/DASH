using EZCameraShake;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerControl : MonoBehaviour
{
    [Header("Dash Attributes")]
    public float speed = 6.0f;
    public float rechargeTime = 4.0f;
    public int playerDashCounter = 1;
    public int maxDashes;

    [Header("Global Variables")]
    public int score = 0;
    public int lives = 3;
    public float scoreMultiplier = 0.9f;
    public bool invincible;
    public int invincibleTime;

    [Header("References")]
    public Rigidbody2D rb;
    public GameObject[] particles;
    public SpriteRenderer spriteRenderer;
    public Camera MainCamera;
    public Animator animator;
    public static bool paused;
    private bool addMultiplier = false;
    private Boolean dash;
    private float objectHeight;
    private float objectWidth;
    private Vector2 screenBounds;
    private Vector2 pauseArea = new Vector2(8.1f, 4.2f);
    private CircleCollider2D hitbox;


    // Start is called before the first frame update
    public virtual void Start()
    {
        //Obtain basic character data
        MainCamera = Camera.main;
        screenBounds = MainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, MainCamera.transform.position.z));
        objectWidth = transform.GetComponent<SpriteRenderer>().bounds.extents.x; //extents = size of width / 2
        objectHeight = transform.GetComponent<SpriteRenderer>().bounds.extents.y; //extents = size of height / 2
        rb = this.GetComponent<Rigidbody2D>();
        spriteRenderer = this.GetComponent<SpriteRenderer>();
        maxDashes = 1;
        hitbox = this.GetComponent<CircleCollider2D>();
        invincible = false;
        animator = this.GetComponent<Animator>();
        paused = false;
    }

    // Update is called once per frame
    public virtual void Update()
    {

        //Make a speed check and change hitbox accordingly based on speed
        if (rb.velocity.magnitude > Vector2.one.magnitude)
        {
            if (hitbox.radius != 0.6f)
            {
                hitbox.radius = 0.6f;
            }
            animator.SetBool("isDashing", true);
            //Time.timeScale = 1.0f;

        }
        else
        {
            if (hitbox.radius != 0.4f)
            {
                hitbox.radius = 0.4f;
            }
            animator.SetBool("isDashing", false);
            rb.velocity = Vector2.zero;

            //Time.timeScale = 0.3f;
        }


        
        
        //Check if game is paused
        if (!UISystem.paused)
        {

            //If the screen is clicked/touched, then check for dash
            //For touch controls
            //Input.touchCount > 0
            if (Input.GetMouseButtonDown(0))
            {
                //Touch touch = Input.GetTouch(0);

                Vector2 touch = Input.mousePosition;
                //replace touch to touch.position
                Vector2 touchPosition = MainCamera.ScreenToWorldPoint(touch);
                Debug.Log(touchPosition);
                if (!(touchPosition.x >= pauseArea.x && touchPosition.y >= pauseArea.y))
                {
                    Vector2 dir = touchPosition - (Vector2)this.transform.position;
                    this.transform.right = dir;
                    //touch.phase == TouchPhase.Ended && playerDashCounter > 0
                    if (Input.GetMouseButtonDown(0) && playerDashCounter > 0)
                    {
                        dash = true;
                        if (addMultiplier)
                        {
                            scoreMultiplier = 0.9f;
                        }
                        addMultiplier = true;
                        playerDashCounter--;
                        if (playerDashCounter < 0)
                        {
                            playerDashCounter = 0;
                        }
                        if (playerDashCounter == 0)
                        {
                            StartCoroutine(Recharge());
                        }
                    }
                    
                }
            }

        }
    }

    //Called in order to reset dash
    private IEnumerator Recharge()
    {

        yield return new WaitForSeconds(rechargeTime);
        if (playerDashCounter < maxDashes)
        {
            playerDashCounter = maxDashes;
        }

    }

    //Ensures player isn't immediately killed upon respawning
    private IEnumerator SpawnProtection()
    {
        
    }

    //Lets the player know when they are invulnerable via sprite blinking
    public IEnumerator ConstBlink()
    {
     

    }


    //Temporary powerup for infinite dashes
    public IEnumerator InfiniteDashes(int dashtime)
    {
        
    }

    //Check for collision detection when a player hits an object
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Crude but effective way of telling what object has collided with
        Powerup powerup = collision.GetComponent<Powerup>();
        Enemy enemy = collision.GetComponent<Enemy>();
        Bullet bullet = collision.GetComponent<Bullet>();
        DestroyExplos explosion = collision.GetComponent<DestroyExplos>();
        if (powerup != null)
        {
            playerDashCounter++;
            Debug.Log("Collided");
            powerup.activatePowerup();
        }

        else if ((enemy != null || bullet != null || explosion != null) && rb.velocity.magnitude > Vector2.one.magnitude + 1)
        {
            //If it's an enemy
            if (enemy != null)
            {
                CameraShaker.Instance.ShakeOnce(2.0f, 10.0f, 0.2f, 0.2f);
                enemy.destroy();
                playerDashCounter++;
                if (addMultiplier)
                {
                    addMultiplier = false;
                    scoreMultiplier += 0.1f;
                    Math.Round(scoreMultiplier, 1);
                }
                Instantiate(particles[0], transform.position, Quaternion.identity);
                score += (int)(100 * scoreMultiplier);
                StopCoroutine(Recharge());
            }

            //If it's a bullet
            else if (bullet != null)
            {
                score += (int)(50 * scoreMultiplier);

                Instantiate(particles[0], transform.position, Quaternion.identity);
                bullet.destroy();
            }

            //If it's a damaging explosion
            else
            {
                Instantiate(particles[1], transform.position, Quaternion.identity);
            }

            Mathf.Clamp(playerDashCounter, 0, maxDashes);
        }
        else if ((enemy != null || bullet != null || explosion != null) && rb.velocity.magnitude <= Vector2.one.magnitude + 1 && !invincible)
        {
            scoreMultiplier = 0.9f;
            this.GetComponent<SpriteRenderer>().enabled = false;
            UnityEngine.Debug.LogWarning("player died");

            Instantiate(particles[2], transform.position, Quaternion.identity);
            StartCoroutine(SpawnProtection());
            StartCoroutine(ConstBlink());

            if (--lives < 0)
            {
                GameObject audio = GameObject.Find("AudioManager");
                audio.GetComponent<AudioManager>().StopPlaying("Theme1");
                audio.GetComponent<AudioManager>().Play("MainTheme");
                UISystem.paused = false;
                SceneManager.LoadScene("MainMenu");
            }

            GameObject[] to_delete = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject temp in to_delete)
            {
                temp.GetComponent<Enemy>().destroy();
            }
            to_delete = GameObject.FindGameObjectsWithTag("Bullet");
            foreach (GameObject temp in to_delete)
            {
                temp.GetComponent<Bullet>().destroy();
            }

            transform.position = Vector2.zero;
        }

    }

    //Call screenWrap in LateUpdate so it doesn't interfere with collisions
    public virtual void LateUpdate()
    {
        screenWrap();
    }

    //Ensure the player is in bounds of the screen
    private void screenWrap()
    {
        Vector3 viewPos = transform.position;
        if (viewPos.x > screenBounds.x - objectWidth)
        {
            viewPos.x = screenBounds.x * -1 + objectWidth + 0.5f;

        }
        if (viewPos.y > screenBounds.y - objectHeight)
        {
            viewPos.y = screenBounds.y * -1 + objectHeight;
        }
        if (viewPos.x < screenBounds.x * -1 + objectWidth)
        {

            viewPos.x = screenBounds.x - objectWidth - 0.5f;
        }
        if (viewPos.y < screenBounds.y * -1 + objectHeight)
        {
            viewPos.y = screenBounds.y - objectHeight;
        }

        transform.position = viewPos;

    }

    //Add a brief force to the player, giving it the effect of dashing
    public virtual void playerDash()
    {
        if (dash)
        {

            dash = false;
            FindObjectOfType<AudioManager>().Play("Dash");
            rb.velocity = new Vector2(0, 0);
            rb.AddForce(transform.right * speed, ForceMode2D.Impulse);
        }

    }


}

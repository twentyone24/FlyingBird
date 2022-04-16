using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SkyFlightGame
{
	/// <summary>
	/// This script controls the game, starting it, following game progress, and finishing it with game over.
	/// </summary>
	public class SFGGameController : MonoBehaviour 
	{
        [Tooltip("The player object that moves in the scene")]
        public SFGPlayerControls playerObject;
        public static SFGPlayerControls currentPlayerObject = null;

        [Tooltip("The camera object that follows the player object")]
        public Transform cameraObject;

        [Tooltip("The lives object that will display the number of lives the player has")]
        public Text livesText;

        [Tooltip("The ring that the player must pass through")]
        public SFGRing ringObject;

        [Tooltip("The forward distance we push the ring after the player passes through it")]
        public float ringForwardOffset = 40;

        [Tooltip("The random side distance we set the ring after the player passes through it")]
        public float ringSideOffset = 20;

        [Tooltip("The random height we set the ring after the player must pass through")]
        public float ringHeight = 8;

        [Tooltip("A list of sections that will keep appearing as the player moves forward")]
        public Transform[] groundSections;
        internal int sectionIndex = 0;

        [Tooltip("The size of a single section. This is used to calculate how much we move each section forward")]
        public float sectionSize = 100;

		// Are we using the mouse now?
		internal bool usingMouse = false;
        
		//The score of the game. Score is earned by passing through rings
		internal float score = 1;
		
		[Tooltip("The text object that displays the score, assigned from the scene")]
		internal float highScore = 0;
        
		[Tooltip("How long to wait before starting gameplay")]
		public float startDelay = 1;

		// Is the game over?
		internal bool  isGameOver = false;

		[Tooltip("The level of the main menu that can be loaded after the game ends")]
		public string mainMenuLevelName = "SFGMenu";
		
		[Tooltip("The keyboard/gamepad button that will restart the game after game over")]
		public string confirmButton = "Submit";
		
		[Tooltip("The keyboard/gamepad button that pauses the game")]
		public string pauseButton = "Cancel";
		internal bool  isPaused = false;

		[Tooltip("Various canvases for the UI, assign them from the scene")]
		public Transform gameCanvas;
		public Transform pauseCanvas;
		public Transform gameOverCanvas;

		// A general use index
		internal int index = 0;

        [Tooltip("Turn on tilt controls which turn the player based on the tilt of the mobile device")]
        public bool tiltControls = false;
        public float tiltCutoff = 0.5f;
        public float tiltOffset = 0.7f;

		[Tooltip("Turn on tilt controls which turn the player based on the tilt of the mobile device")]
		public bool joystickControls = false;
		internal Transform joystick;
		internal Transform joystickDirection;

		void Awake()
		{
            Time.timeScale = 1;

			// Activate the pause canvas early on, so it can detect info about sound volume state
			if ( pauseCanvas )    pauseCanvas.gameObject.SetActive(true);

            if (currentPlayerObject == null )
            {
                // Give the player control after a delay
                playerObject.Invoke("GetControl", startDelay);
            }
            else
            {
                AssignCurrentPlayer(currentPlayerObject);
            }
        }

		/// <summary>
		/// Start is only called once in the lifetime of the behaviour.
		/// The difference between Awake and Start is that Start is only called if the script instance is enabled.
		/// This allows you to delay any initialization code, until it is really needed.
		/// Awake is always called before any Start functions.
		/// This allows you to order initialization of scripts
		/// </summary>
		void Start()
		{
			//Hide the game over and pause screens
			if ( gameOverCanvas )    gameOverCanvas.gameObject.SetActive(false);
			if ( pauseCanvas )    pauseCanvas.gameObject.SetActive(false);

            // Update the lives text of the player
            if (livesText) livesText.text = playerObject.lives.ToString();

            //Get the highscore for the player
            highScore = PlayerPrefs.GetFloat(SceneManager.GetActiveScene().name + "HighScore", 0);

			// Assign the joystick objects if they exist
			if (gameCanvas.Find("Joystick")) joystick = gameCanvas.Find("Joystick");
			if (gameCanvas.Find("JoystickDirection")) joystickDirection = gameCanvas.Find("JoystickDirection");

			if ( joystick && joystickDirection )
            {
				joystick.gameObject.SetActive(false);
				joystickDirection.gameObject.SetActive(false);
			}

			// If are not on a mobile device, hide the mobile controls
			if ( !Application.isMobilePlatform && gameCanvas.Find("ButtonMobileControls") ) gameCanvas.Find("ButtonMobileControls").gameObject.SetActive(false);
		}

        /// <summary>
        /// Assigns the current player object, ususally from a character selector script
        /// </summary>
        /// <param name="player"></param>
        public void AssignCurrentPlayer( SFGPlayerControls player )
        {
            // If we have a character selected, assign it as the current player
            currentPlayerObject = player;

            // Remove the previous player
            Destroy(playerObject.gameObject);

            // Create the new player object and place it in the scene
            playerObject = Instantiate(currentPlayerObject, playerObject.transform.position, playerObject.transform.rotation);

            // Give the player control after a delay
            playerObject.Invoke("GetControl", startDelay);
        }

        /// <summary>
        /// Creates a new ring by moving it forwards and to the sides, and changing the number inside
        /// </summary>
        public void CreateRing()
        {
            // Either move the ring randomly to the right or to the left
            if ( Random.value > 0.5f )
            {
                ringObject.targetPosition = new Vector3(Random.Range(0.2f, 1) * ringSideOffset, Random.Range(ringHeight, ringHeight), ringObject.transform.position.z + ringForwardOffset + playerObject.moveSpeed);
            }
            else
            {
                ringObject.targetPosition = new Vector3(Random.Range(-0.2f, -1) * ringSideOffset, Random.Range(ringHeight, ringHeight), ringObject.transform.position.z + ringForwardOffset + playerObject.moveSpeed);
            }

            // Add to the score
            score++;

            ringObject.ringCount++;

            // Set the score text value
            ringObject.transform.Find("RingNumber/Text").GetComponent<Text>().text = score.ToString();

            if ( ringObject.delaySpeedBoost > 0 )
            {
                ringObject.delaySpeedBoost--;
            }
            else if( ringObject.ringCount % ringObject.speedBoostEvery == 0 )
            {
                // Increase the movement speed of the player
                playerObject.moveSpeed += ringObject.speedBoost;
            }
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        void  Update()
		{
			// Delay the start of the game
			if ( startDelay > 0 )
			{
				startDelay -= Time.deltaTime;
			}
			else
			{
                //If the game is over, listen for the Restart and MainMenu buttons
                if ( isGameOver == true )
				{
					//The jump button restarts the game
					if ( Input.GetButtonDown(confirmButton) )
					{
						Restart();
					}
					
					//The pause button goes to the main menu
					if ( Input.GetButtonDown(pauseButton) )
					{
						MainMenu();
					}
				}
				else
				{
                    if (playerObject)
                    {
                        // Move the ground sections to appear in front of the player as it moves forward
                        if (sectionIndex < groundSections.Length - 1 && playerObject.transform.position.z > groundSections[sectionIndex + 1].position.z)
                        {
                            groundSections[sectionIndex].position += Vector3.forward * sectionSize * groundSections.Length;

                            sectionIndex++;
                        }
                        else if (sectionIndex == groundSections.Length - 1 && playerObject.transform.position.z > groundSections[0].position.z)
                        {
                            groundSections[sectionIndex].position += Vector3.forward * sectionSize * groundSections.Length;

                            sectionIndex = 0;
                        }

                        float horizontalLimit = Mathf.Clamp(playerObject.transform.position.x, -ringSideOffset, ringSideOffset);

                        float verticalLimit = Mathf.Clamp(playerObject.transform.position.y, 1f, playerObject.heightLimit);

                        playerObject.transform.position = new Vector3(horizontalLimit, verticalLimit, playerObject.transform.position.z);
                    }

                    //Toggle pause/unpause in the game
                    if ( Input.GetButtonDown(pauseButton) )
					{
						if ( isPaused == true )    Unpause();
						else    Pause(true);
					}

					// If we move the mouse in any direction, then mouse controls take effect
					if ( Input.GetAxisRaw("Mouse X") != 0 || Input.GetAxisRaw("Mouse Y") != 0 || Input.GetMouseButtonDown(0) || Input.touchCount > 0 )    usingMouse = true;

					// If we press gamepad or keyboard arrows, then mouse controls are turned off
					if ( Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0 )    usingMouse = false;

                    if (playerObject)
                    {
                        if (Application.isMobilePlatform && tiltControls == true)
                        {
                            Vector3 accel = Input.acceleration;

                            // filter the jerky acceleration in the variable accel:
                            accel = Vector3.Lerp(accel, Input.acceleration, tiltCutoff * Time.deltaTime * playerObject.turnSpeed);

                            // map accel -Y and X to game X and Y directions:
                            playerObject.turnDirection = new Vector3(accel.x, -accel.y - tiltOffset, 0);

                            // limit dir vector to magnitude 1:
                            if (playerObject.turnDirection.sqrMagnitude > 1) playerObject.turnDirection.Normalize();

                            //transform.eulerAngles = dir * 50;

                            // Rotate the player based on turn direction which is controlled by the mouse/keyboard/gamepad/tilt/etc
                            //playerObject.transform.localEulerAngles = new Vector3(playerObject.turnDirection.y * -playerObject.turnAngle, 0, playerObject.turnDirection.x * -playerObject.turnAngle);

                            //playerObject.transform.localEulerAngles = playerObject.turnDirection * playerObject.turnAngle;
                        }
                        else if (usingMouse == true)
                        {
							if (joystickControls == true)
                            {
								if (Input.GetButtonDown("Fire1") || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
								{
									playerObject.rightStickPosition = Input.mousePosition;

									if ( joystick && joystickDirection)
									{
										joystick.gameObject.SetActive(true);
										joystickDirection.gameObject.SetActive(true);
									}
								}

								// Turn the player based on the position of the finger on the screen
								//if (Input.GetButton("Fire1")) playerObject.turnDirection = Vector2.Lerp(playerObject.turnDirection, new Vector2((Input.mousePosition.x - Screen.width * 0.5f) / Screen.width * 2, (Input.mousePosition.y - Screen.height * 0.3f) / Screen.height * 1.4f), Time.deltaTime * playerObject.turnSpeed * 0.3f);
								if (Input.GetButton("Fire1")) playerObject.turnDirection = Vector2.MoveTowards(playerObject.turnDirection, new Vector2((Input.mousePosition.x - playerObject.rightStickPosition.x) * 0.1f, (Input.mousePosition.y - playerObject.rightStickPosition.y) * 0.1f), Time.deltaTime * playerObject.turnSpeed * 0.05f);
								else playerObject.turnDirection = Vector2.Lerp(playerObject.turnDirection, Vector2.zero, Time.deltaTime * playerObject.turnSpeed * 0.1f);

								joystickDirection.position = Input.mousePosition;

								// Limit the turn direction to a magnitude of 1
								playerObject.turnDirection = Vector2.ClampMagnitude(playerObject.turnDirection, 1);

								// Limit the position of the camera
								if (playerObject.transform.position.y > playerObject.heightLimit)
								{
									playerObject.turnDirection = Vector2.Lerp(playerObject.turnDirection, new Vector2(playerObject.turnDirection.x, 0), Time.deltaTime * playerObject.turnSpeed * 0.2f);
								}

								if (Input.GetButtonUp("Fire1") || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
								{
									if (joystick && joystickDirection)
									{
										joystick.gameObject.SetActive(false);
										joystickDirection.gameObject.SetActive(false);
									}
								}
							}
							else // Automatic direction controls with mouse ( no need to tap and hold to change direction )
                            {
								playerObject.turnDirection = Vector2.Lerp(playerObject.turnDirection, new Vector2((Input.mousePosition.x - Screen.width * 0.5f) / Screen.width * 2, (Input.mousePosition.y - Screen.height * 0.5f) / Screen.height * 2), Time.deltaTime * playerObject.turnSpeed);
							}

						}
                        else
                        {
                            playerObject.turnDirection = Vector2.Lerp(playerObject.turnDirection, new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), Time.deltaTime * playerObject.turnSpeed * 0.1f);
                        }
                    }
				}
			}
		}
        
        
		
		/// <summary>
		/// Pause the game, and shows the pause menu
		/// </summary>
		/// <param name="showMenu">If set to <c>true</c> show menu.</param>
		public void  Pause( bool showMenu )
		{
			isPaused = true;
			
			//Set timescale to 0, preventing anything from moving
			Time.timeScale = 0;
			
			//Show the pause screen and hide the game screen
			if ( showMenu == true )
			{
				if ( pauseCanvas )    pauseCanvas.gameObject.SetActive(true);
				if ( gameCanvas )    gameCanvas.gameObject.SetActive(false);
			}
		}
		
		/// <summary>
		/// Resume the game
		/// </summary>
		public void  Unpause()
		{
			isPaused = false;
			
			//Set timescale back to the current game speed
			Time.timeScale = 1;
			
			//Hide the pause screen and show the game screen
			if ( pauseCanvas )    pauseCanvas.gameObject.SetActive(false);
			if ( gameCanvas )    gameCanvas.gameObject.SetActive(true);
		}

		/// <summary>
		/// Runs the game over event and shows the game over screen
		/// </summary>
		IEnumerator GameOver(float delay)
		{
			isGameOver = true;
            
			yield return new WaitForSeconds(delay);
			
			//Remove the pause and game screens
			if ( pauseCanvas )    Destroy(pauseCanvas.gameObject);
			if ( gameCanvas )    Destroy(gameCanvas.gameObject);
			
			//Show the game over screen
			if ( gameOverCanvas )    
			{
				//Show the game over screen
				gameOverCanvas.gameObject.SetActive(true);
				
				//Write the score text
				gameOverCanvas.Find("TextScore").GetComponent<Text>().text = "SCORE " + score.ToString();
				
				//Check if we got a high score
				if ( score > highScore )    
				{
					highScore = score;
					
					//Register the new high score
					#if UNITY_5_3 || UNITY_5_3_OR_NEWER
					PlayerPrefs.SetFloat(SceneManager.GetActiveScene().name + "HighScore", score);
					#else
					PlayerPrefs.SetFloat(Application.loadedLevelName + "HighScore", score);
					#endif
				}
				
				//Write the high sscore text
				gameOverCanvas.Find("TextHighScore").GetComponent<Text>().text = "HIGH SCORE " + highScore.ToString();
			}
		}
		
		/// <summary>
		/// Restart the current level
		/// </summary>
		void  Restart()
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		}
		
		/// <summary>
		/// Restart the current level
		/// </summary>
		void  MainMenu()
		{
			SceneManager.LoadScene(mainMenuLevelName);
		}

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;

            Gizmos.DrawLine(new Vector3(-ringSideOffset, 0, 0), new Vector3(-ringSideOffset, ringHeight, 0));
            Gizmos.DrawLine(new Vector3(ringSideOffset, 0, 0), new Vector3(ringSideOffset, ringHeight, 0));
            Gizmos.DrawLine(new Vector3(-ringSideOffset, 0, 0), new Vector3(ringSideOffset, 0, 0));
            Gizmos.DrawLine(new Vector3(-ringSideOffset, ringHeight, 0), new Vector3(ringSideOffset, ringHeight, 0));
        }
    }
}
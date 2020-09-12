using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(LineRenderer))]
public class GameSystem : MonoBehaviour
{
  // Enums for clarity
  public enum GameState { Paused, Setup, Running, Over };

  public GameState gameState;
  public static GameSystem instance;

  // References to existing objects and prefabs to use
  public TextMesh statusText;
  public TextMesh livesText;
  public TextMesh scoreText;
  public GameObject projectilePrefab;
  public GameObject enemyPrefab;
  public GameObject playerBoundaryPrefab;
  public GameObject boundaryPrefab;
  public GameObject groundPrefab;

  // Game objects and variables
  ARRaycastManager arRaycastManager;
  ARPlaneManager arPlaneManager;

  // Indicator Data
  Vector3 placementPos;
  Quaternion placementRot;
  bool placementValid = false;
  public GameObject placementIndicator;

  // Crosshair Indicator
  Vector3 crosshairOffset = new Vector3(0f, 0f, 0.15f);
  public GameObject crosshairPrefab;

  // Boundary Points
  LineRenderer line;
  GameObject leftBoundary;
  GameObject rightBoundary;
  GameObject playerBoundaryPlane;
  GameObject groundPlane;
  Vector3 boundaryMidpoint;
  float boundaryDistance;
  Vector3 boundaryDirection;
  Vector3 boundaryOrtho;
  int numOfSetBoundaries = 0;

  // Enemy Spawn properties
  double enemySpawnRate = 0.03f;
  float enemyDefaultDistance = 1.0f;

  // Others
  System.Random RNG;

  // Gameplay Variables
  public int numOfLives = 3;
  public int numOfPoints = 0;

  // Find specific scripts and initialize variables
  void Start()
  {
    arRaycastManager = FindObjectOfType<ARRaycastManager>();

    arPlaneManager = FindObjectOfType<ARPlaneManager>();
    arPlaneManager.enabled = false;

    gameState = GameState.Paused;
    RNG = new System.Random();

    if (instance == null)
      instance = this;

    // Any other set up you may need for your application

    // Create placement indicator prefab w/ appropriate size and visibility
    placementIndicator.SetActive(false);

    // Hide score and lives text
    scoreText.gameObject.SetActive(false);
    livesText.gameObject.SetActive(false);

    // Setup line
    line = GetComponent<LineRenderer>();
    line.enabled = false;
    line.positionCount = 0;

    // Create crosshair
    crosshairPrefab.transform.SetParent(FindObjectOfType<Camera>().transform);
    crosshairPrefab.transform.position = crosshairOffset;
    crosshairPrefab.SetActive(false);
  }

  // Based off current GameState, handle accordingly
  void Update()
  {
    switch (gameState)
    {
      // Paused state (default): Wait for input to start game
      default:
      case GameState.Paused:
        // Check for a screen tap and set the game state for allowing user to set up the scene
        if (CheckForInput()) {
          gameState = GameState.Setup;
          statusText.gameObject.SetActive(false);
        }
        return;

      // Setup state: Enable plane detection and set positions in real world space
      case GameState.Setup:
        // Enables the plane manager
        if (!arPlaneManager.enabled)
          arPlaneManager.enabled = true;

        // Call your update AR Raycast funciton
        UpdatePlacementPose();
        // Set up your game scene here

        if (placementValid && CheckForInput()) {
          if (numOfSetBoundaries == 0) {
            leftBoundary = Instantiate(boundaryPrefab, placementPos, placementRot);
            leftBoundary.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            numOfSetBoundaries++;
          } else if (numOfSetBoundaries == 1) {
            rightBoundary = Instantiate(boundaryPrefab, placementPos, placementRot);
            rightBoundary.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            numOfSetBoundaries++;
          }
        }

        // CONTINUE TO RUN STATE after boundaries are set
        if (numOfSetBoundaries > 1) {
          // render line boundary
          line.positionCount = 2;
          line.SetPosition(0, leftBoundary.transform.position);
          line.SetPosition(1, rightBoundary.transform.position);
          line.enabled = true;

          // Calculate boundary properties (for ease later)
          boundaryMidpoint = (leftBoundary.transform.position + rightBoundary.transform.position) / 2.0f;
          boundaryDistance =  (rightBoundary.transform.position - leftBoundary.transform.position).magnitude;
          boundaryDirection = (rightBoundary.transform.position - leftBoundary.transform.position) / boundaryDistance;
          boundaryOrtho = Vector3.Cross(boundaryDirection, Vector3.up);

          // Set up boundary
          playerBoundaryPlane = Instantiate(playerBoundaryPrefab, boundaryMidpoint, Quaternion.LookRotation(boundaryDirection));

          // Set up groundplane
          groundPlane = Instantiate(groundPrefab, leftBoundary.transform.position, Quaternion.identity);

          // disable AR planes the manager creates
          arPlaneManager.enabled = false;
          foreach (var plane in arPlaneManager.trackables)
            plane.gameObject.SetActive(false);

          // Move to game start state
          gameState = GameState.Running;
          // Activate appropriate UI
          placementIndicator.SetActive(false);
          crosshairPrefab.SetActive(true);
          scoreText.gameObject.SetActive(true);
          livesText.gameObject.SetActive(true);
        }
        return;

      // Running state: Core game loop, this will be called every frame your game is running
      case GameState.Running:

        // LIVES CHECK
        if (numOfLives <= 0) {
          gameState = GameState.Over;

          // Update UI
          livesText.text = "Game Over!";
          scoreText.text = "Final Score: " + numOfPoints;
        }

        if (CheckForInput())
        {
          // Launch projectile
          GameObject projectile = Instantiate(projectilePrefab,
            Camera.current.transform.position,
            Camera.current.transform.rotation);

          projectile.GetComponentInChildren<Rigidbody>().AddForce(Camera.current.transform.forward);
        }

        // Handle your game logic here per frame
        if (RNG.NextDouble() < enemySpawnRate) {
          spawnEnemy();
        }

        return;

      // Game over state: Don't accept input - You can add a reset if you would like
      case GameState.Over:
        return;
    }
  }

  // Spawn an enemy
  void spawnEnemy() {
    float spawnNoise = (float) (RNG.NextDouble());
    Vector3 pointOnBoundary = leftBoundary.transform.position + (spawnNoise * boundaryDistance * boundaryDirection);
    Vector3 spawnPoint = pointOnBoundary + (boundaryOrtho * enemyDefaultDistance);

    // randomize destination
    spawnNoise = (float) (RNG.NextDouble());
    pointOnBoundary = leftBoundary.transform.position + (spawnNoise * boundaryDistance * boundaryDirection);

    // add height randomization for spawn point...
    Vector3 randHeight = new Vector3(0f, (float) RNG.NextDouble(), 0f) * 0.4f;
    spawnPoint += randHeight;
    // ...and destination
    randHeight = new Vector3(0f, (float) RNG.NextDouble(), 0f) * 0.2f;
    pointOnBoundary += randHeight;
    pointOnBoundary -= boundaryOrtho * 0.5f; // give boundary/dest a bit of buffer to ensure trigger

    // Create enemy and give calculated destination
    GameObject enemy = Instantiate(enemyPrefab, spawnPoint, Quaternion.identity);
    Enemy enemyObj = enemy.GetComponent<Enemy>();
    enemyObj.destination = pointOnBoundary;
  }

  // Use the center of the screen to shoot a ray to look for real world positions, update variables if found
  void UpdatePlacementPose()
  {
    var screenCenter = Camera.current.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
    var hits = new List<ARRaycastHit>();

    arRaycastManager.Raycast(screenCenter, hits, TrackableType.Planes);

    // Check for a hit
    if (hits.Count > 0) {
    // Update placementValid, placementPos, and placementRot variables
      placementValid = true;
      placementPos = hits[0].pose.position;
      placementRot = hits[0].pose.rotation;

      // Show the placement visual if position is valid
      UpdatePlacementIndicator();
    } else
      placementValid = false;
  }

  // Handles the display of the placement indicator
  void UpdatePlacementIndicator() {
    if (placementValid) {
      placementIndicator.SetActive(true);
      placementIndicator.transform.position = placementPos;
      placementIndicator.transform.rotation = placementRot;
    } else {
      placementIndicator.SetActive(false);
    }
  }

  // Check if the user has tapped the screen that frame
  bool CheckForInput()
  {
    if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
      return true;
    return false;
  }
}

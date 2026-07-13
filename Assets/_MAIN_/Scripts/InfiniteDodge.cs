using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class InfiniteDodge : MonoBehaviour
{
    private enum SpawnedObjectType
    {
        Obstacle,
        Coin,
        Building
    }

    private class SpawnedObject
    {
        public GameObject gameObject;
        public Transform objectTransform;
        public SpawnedObjectType objectType;

        public int laneIndex;
        public float halfLength;

        // Used by the coin hover animation.
        public float baseY;
        public float hoverPhase;
    }

    [Header("Player")]
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private Camera playerCamera;

    [Tooltip("Automatically configures the player's Rigidbody.")]
    [SerializeField] private bool configurePlayerRigidbody = true;

    [Header("Start UI")]
    [Tooltip("Assign the panel containing the Start button.")]
    [SerializeField] private GameObject startPanel;

    [Header("Gameplay UI")]
    [SerializeField] private TMP_Text coinCountText;
    [SerializeField] private TMP_Text highScoreText;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalCoinCountText;
    [SerializeField] private TMP_Text finalHighScoreText;

    [Header("Rear View Darkness")]
    [Tooltip(
        "Assign a CanvasGroup containing a full-screen black Image. " +
        "This should be attached to the VR camera."
    )]
    [SerializeField] private CanvasGroup rearViewDarkOverlay;

    [Tooltip(
        "Optional transform defining the forward direction of the road. " +
        "Leave empty when the road faces world positive Z."
    )]
    [SerializeField] private Transform roadDirectionReference;

    [Tooltip(
        "Darkness starts when the camera-road dot product falls below this."
    )]
    [Range(-1f, 1f)]
    [SerializeField] private float rearDarknessStartDot = 0.1f;

    [Tooltip(
        "The view becomes fully dark when the dot product reaches this."
    )]
    [Range(-1f, 1f)]
    [SerializeField] private float rearFullDarknessDot = -0.45f;

    [SerializeField] private float rearDarknessFadeSpeed = 4f;

    [Header("Road")]
    [Tooltip(
        "The road must face forward along the world Z axis and contain Colliders."
    )]
    [SerializeField] private GameObject roadPrefab;

    [Min(3)]
    [SerializeField] private int roadSegmentCount = 6;

    [Min(0)]
    [SerializeField] private int roadSegmentsBehindPlayer = 1;

    [SerializeField] private float roadWorldX = 0f;
    [SerializeField] private float roadWorldY = 0f;
    [SerializeField] private float roadSurfaceOffset = 0f;
    [SerializeField] private float roadRecycleBehindDistance = 5f;

    [Header("Three-Lane Movement")]
    [SerializeField] private string horizontalAxisName = "Horizontal";

    [SerializeField] private bool calculateLaneSpacingFromRoad = true;

    [Tooltip(
        "Used when Calculate Lane Spacing From Road is disabled."
    )]
    [SerializeField] private float manualLaneSpacing = 3f;

    [SerializeField] private float laneChangeSpeed = 10f;
    [SerializeField] private float roadEdgePadding = 0.25f;
    [SerializeField] private float additionalCarEdgePadding = 0.1f;

    [Range(0.1f, 1f)]
    [SerializeField] private float joystickInputThreshold = 0.55f;

    [Range(0f, 0.9f)]
    [SerializeField] private float joystickReleaseThreshold = 0.2f;

    [SerializeField] private bool invertHorizontalInput = false;

    [Header("World Speed")]
    [SerializeField] private float startingWorldSpeed = 12f;
    [SerializeField] private float maximumWorldSpeed = 28f;

    [Tooltip("Speed added every second.")]
    [SerializeField] private float speedIncreasePerSecond = 0.12f;

    [Header("Obstacle Spawning")]
    [SerializeField] private GameObject[] obstaclePrefabs;

    [SerializeField] private float obstacleSpawnDistance = 65f;
    [SerializeField] private float firstObstacleDelay = 1.5f;
    [SerializeField] private float startingObstacleInterval = 1.8f;
    [SerializeField] private float minimumObstacleInterval = 0.65f;

    [Tooltip("Obstacle interval reduction per second.")]
    [SerializeField] private float obstacleIntervalDecreasePerSecond = 0.01f;

    [Range(0f, 1f)]
    [SerializeField] private float startingDoubleObstacleChance = 0f;

    [Range(0f, 1f)]
    [SerializeField] private float maximumDoubleObstacleChance = 0.65f;

    [SerializeField]
    private float doubleObstacleChanceIncreasePerSecond = 0.005f;

    [Header("Coin Spawning")]
    [SerializeField] private GameObject[] coinPrefabs;

    [SerializeField] private float coinSpawnDistance = 72f;
    [SerializeField] private float firstCoinDelay = 0.8f;
    [SerializeField] private float coinSpawnInterval = 2.5f;

    [Min(1)]
    [SerializeField] private int minimumCoinsPerLine = 3;

    [Min(1)]
    [SerializeField] private int maximumCoinsPerLine = 6;

    [SerializeField] private float coinSpacing = 2.25f;
    [SerializeField] private float coinHeightAboveRoad = 1.1f;

    [Header("Coin Animation")]
    [SerializeField] private float coinRotationSpeed = 140f;
    [SerializeField] private float coinHoverHeight = 0.2f;
    [SerializeField] private float coinHoverSpeed = 2.5f;

    [Header("Coin and Obstacle Separation")]
    [Tooltip(
        "Additional forward distance kept between coins and obstacles."
    )]
    [SerializeField] private float coinObstacleSafetyGap = 1.5f;

    [Header("Building Props")]
    [SerializeField] private GameObject[] buildingPrefabs;

    [SerializeField] private float buildingSpawnDistance = 75f;
    [SerializeField] private float firstBuildingDelay = 0f;
    [SerializeField] private float buildingSpawnInterval = 1.25f;
    [SerializeField] private float buildingSideGap = 1.5f;
    [SerializeField] private float buildingZJitter = 2f;

    [Tooltip(
        "Buildings spawned before the player presses the Start button."
    )]
    [Min(0)]
    [SerializeField] private int initialBuildingRows = 10;

    [SerializeField] private float initialBuildingStartDistance = 5f;
    [SerializeField] private float initialBuildingSpacing = 10f;

    [SerializeField] private bool spawnBuildingsOnBothSides = true;
    [SerializeField] private bool randomizeBuildingYRotation = true;

    [Header("Cleanup")]
    [SerializeField] private float cleanupDistanceBehindPlayer = 15f;

    private const string HighScoreKey =
        "InfiniteCarDodgeCoinHighScore";

    private readonly List<Transform> roadSegments =
        new List<Transform>();

    private readonly List<SpawnedObject> spawnedObjects =
        new List<SpawnedObject>();

    private Transform roadContainer;
    private Transform spawnedObjectsContainer;

    private float roadLength;
    private float roadMinimumX;
    private float roadMaximumX;
    private float roadCentreX;
    private float roadSurfaceY;

    private float playerHalfWidth;
    private float minimumPlayerX;
    private float maximumPlayerX;
    private float laneSpacing;

    // 0 = left, 1 = centre, 2 = right.
    private int currentLane = 1;
    private float targetLaneX;

    private bool joystickReadyForLaneChange = true;

    private float fixedPlayerY;
    private float fixedPlayerZ;

    private float elapsedGameTime;
    private float currentWorldSpeed;

    private float obstacleTimer;
    private float coinTimer;
    private float buildingTimer;

    private int currentCoins;
    private int highScore;

    private bool gameStarted;
    private bool gameOver;

    private void Awake()
    {
        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        currentCoins = 0;

        highScore = PlayerPrefs.GetInt(
            HighScoreKey,
            0
        );

        if (startPanel != null)
        {
            startPanel.SetActive(true);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (rearViewDarkOverlay != null)
        {
            rearViewDarkOverlay.alpha = 0f;
            rearViewDarkOverlay.interactable = false;
            rearViewDarkOverlay.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        fixedPlayerY = transform.position.y;
        fixedPlayerZ = transform.position.z;

        ConfigurePlayerPhysics();
        CreateRuntimeContainers();

        if (!CreateInfiniteRoad())
        {
            enabled = false;
            return;
        }

        CalculatePlayerAndLaneBounds();

        currentWorldSpeed = startingWorldSpeed;

        ResetSpawnTimers();

        /*
         * Render scenery before the game starts.
         * Nothing moves until StartGame() is called.
         */
        CreateInitialBuildings();

        UpdateUI();
    }

    private void Update()
    {
        UpdateRearViewDarkness();

        if (!gameStarted || gameOver)
        {
            return;
        }

        HandleLaneInput();

        float deltaTime = Time.deltaTime;

        elapsedGameTime += deltaTime;

        currentWorldSpeed = Mathf.Min(
            maximumWorldSpeed,
            startingWorldSpeed +
            elapsedGameTime * speedIncreasePerSecond
        );

        MoveRoadSegments(deltaTime);
        MoveSpawnedObjects(deltaTime);
        UpdateSpawnTimers(deltaTime);
    }

    private void FixedUpdate()
    {
        if (!gameStarted ||
            gameOver ||
            playerRigidbody == null)
        {
            return;
        }

        float newX = Mathf.MoveTowards(
            playerRigidbody.position.x,
            targetLaneX,
            laneChangeSpeed * Time.fixedDeltaTime
        );

        newX = Mathf.Clamp(
            newX,
            minimumPlayerX,
            maximumPlayerX
        );

        playerRigidbody.MovePosition(
            new Vector3(
                newX,
                fixedPlayerY,
                fixedPlayerZ
            )
        );
    }

    // Attach this function to the Start button.
    public void StartGame()
    {
        if (gameStarted || gameOver)
        {
            return;
        }

        gameStarted = true;
        elapsedGameTime = 0f;
        currentWorldSpeed = startingWorldSpeed;

        ResetSpawnTimers();

        if (startPanel != null)
        {
            startPanel.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    // Attach this function to the Restart button.
    public void RestartGame()
    {
        Time.timeScale = 1f;

        Scene activeScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            activeScene.buildIndex
        );
    }

    private void ResetSpawnTimers()
    {
        obstacleTimer = firstObstacleDelay;
        coinTimer = firstCoinDelay;
        buildingTimer = firstBuildingDelay;
    }

    private bool ValidateReferences()
    {
        if (playerRigidbody == null)
        {
            Debug.LogError(
                "Infinite Car Dodge: Player Rigidbody is missing."
            );

            return false;
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                "Infinite Car Dodge: Player Camera is missing."
            );

            return false;
        }

        if (roadPrefab == null)
        {
            Debug.LogError(
                "Infinite Car Dodge: Road Prefab is not assigned."
            );

            return false;
        }

        if (!HasCollider(roadPrefab))
        {
            Debug.LogError(
                "Infinite Car Dodge: Road Prefab requires a Collider."
            );

            return false;
        }

        if (obstaclePrefabs == null ||
            obstaclePrefabs.Length == 0)
        {
            Debug.LogError(
                "Infinite Car Dodge: Assign obstacle prefabs."
            );

            return false;
        }

        if (coinPrefabs == null ||
            coinPrefabs.Length == 0)
        {
            Debug.LogError(
                "Infinite Car Dodge: Assign coin prefabs."
            );

            return false;
        }

        return true;
    }

    private void ConfigurePlayerPhysics()
    {
        if (!configurePlayerRigidbody)
        {
            return;
        }

        playerRigidbody.useGravity = false;
        playerRigidbody.isKinematic = true;

        playerRigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;

        playerRigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        playerRigidbody.constraints =
            RigidbodyConstraints.FreezePositionY |
            RigidbodyConstraints.FreezePositionZ |
            RigidbodyConstraints.FreezeRotation;
    }

    private void CreateRuntimeContainers()
    {
        GameObject roadContainerObject =
            new GameObject("Generated Road Segments");

        roadContainer = roadContainerObject.transform;

        GameObject objectContainer =
            new GameObject(
                "Generated Obstacles Coins Buildings"
            );

        spawnedObjectsContainer =
            objectContainer.transform;
    }

    private bool CreateInfiniteRoad()
    {
        roadSegments.Clear();

        Quaternion roadRotation =
            roadPrefab.transform.rotation;

        GameObject firstRoad = Instantiate(
            roadPrefab,
            new Vector3(
                roadWorldX,
                roadWorldY,
                fixedPlayerZ
            ),
            roadRotation,
            roadContainer
        );

        if (!TryGetColliderBounds(
                firstRoad,
                out Bounds firstRoadBounds))
        {
            Debug.LogError(
                "Could not calculate Road Collider bounds."
            );

            Destroy(firstRoad);
            return false;
        }

        roadLength = firstRoadBounds.size.z;

        if (roadLength <= 0.01f)
        {
            Debug.LogError(
                "The Road Collider has no usable Z length."
            );

            Destroy(firstRoad);
            return false;
        }

        float firstRoadZ =
            fixedPlayerZ -
            roadSegmentsBehindPlayer * roadLength;

        firstRoad.transform.position = new Vector3(
            roadWorldX,
            roadWorldY,
            firstRoadZ
        );

        roadSegments.Add(firstRoad.transform);

        for (int i = 1; i < roadSegmentCount; i++)
        {
            float roadZ =
                firstRoadZ +
                i * roadLength;

            GameObject roadSegment = Instantiate(
                roadPrefab,
                new Vector3(
                    roadWorldX,
                    roadWorldY,
                    roadZ
                ),
                roadRotation,
                roadContainer
            );

            roadSegments.Add(
                roadSegment.transform
            );
        }

        if (!TryGetColliderBounds(
                firstRoad,
                out firstRoadBounds))
        {
            return false;
        }

        roadMinimumX = firstRoadBounds.min.x;
        roadMaximumX = firstRoadBounds.max.x;
        roadCentreX = firstRoadBounds.center.x;

        roadSurfaceY =
            firstRoadBounds.max.y +
            roadSurfaceOffset;

        return true;
    }

    private void CalculatePlayerAndLaneBounds()
    {
        if (!TryGetColliderBounds(
                gameObject,
                out Bounds playerBounds))
        {
            playerHalfWidth = 0.5f;
        }
        else
        {
            playerHalfWidth =
                playerBounds.extents.x;
        }

        minimumPlayerX =
            roadMinimumX +
            playerHalfWidth +
            roadEdgePadding +
            additionalCarEdgePadding;

        maximumPlayerX =
            roadMaximumX -
            playerHalfWidth -
            roadEdgePadding -
            additionalCarEdgePadding;

        float maximumAllowedSpacing =
            Mathf.Max(
                0.1f,
                Mathf.Min(
                    roadCentreX - minimumPlayerX,
                    maximumPlayerX - roadCentreX
                )
            );

        if (calculateLaneSpacingFromRoad)
        {
            float calculatedSpacing =
                (roadMaximumX - roadMinimumX) / 3f;

            laneSpacing = Mathf.Min(
                calculatedSpacing,
                maximumAllowedSpacing
            );
        }
        else
        {
            laneSpacing = Mathf.Min(
                Mathf.Abs(manualLaneSpacing),
                maximumAllowedSpacing
            );
        }

        laneSpacing = Mathf.Max(
            0.1f,
            laneSpacing
        );

        float relativeCarX =
            transform.position.x -
            roadCentreX;

        currentLane = Mathf.Clamp(
            Mathf.RoundToInt(
                relativeCarX / laneSpacing
            ) + 1,
            0,
            2
        );

        targetLaneX =
            GetLaneX(currentLane);

        Vector3 position =
            playerRigidbody.position;

        position.x = targetLaneX;
        position.y = fixedPlayerY;
        position.z = fixedPlayerZ;

        playerRigidbody.position =
            position;
    }

    private void HandleLaneInput()
    {
        float horizontalInput =
            Input.GetAxisRaw(
                horizontalAxisName
            );

        if (invertHorizontalInput)
        {
            horizontalInput *= -1f;
        }

        if (Mathf.Abs(horizontalInput) <=
            joystickReleaseThreshold)
        {
            joystickReadyForLaneChange = true;
        }

        if (!joystickReadyForLaneChange)
        {
            return;
        }

        if (horizontalInput >=
            joystickInputThreshold)
        {
            ChangeLane(1);
            joystickReadyForLaneChange = false;
        }
        else if (horizontalInput <=
                 -joystickInputThreshold)
        {
            ChangeLane(-1);
            joystickReadyForLaneChange = false;
        }
    }

    private void ChangeLane(int direction)
    {
        currentLane = Mathf.Clamp(
            currentLane + direction,
            0,
            2
        );

        targetLaneX = Mathf.Clamp(
            GetLaneX(currentLane),
            minimumPlayerX,
            maximumPlayerX
        );
    }

    private float GetLaneX(int laneIndex)
    {
        laneIndex = Mathf.Clamp(
            laneIndex,
            0,
            2
        );

        float offset =
            (laneIndex - 1) *
            laneSpacing;

        return Mathf.Clamp(
            roadCentreX + offset,
            minimumPlayerX,
            maximumPlayerX
        );
    }

    private void MoveRoadSegments(float deltaTime)
    {
        float movement =
            currentWorldSpeed * deltaTime;

        float farthestRoadZ =
            float.MinValue;

        for (int i = 0;
             i < roadSegments.Count;
             i++)
        {
            Transform segment =
                roadSegments[i];

            if (segment == null)
            {
                continue;
            }

            segment.position +=
                Vector3.back * movement;

            if (segment.position.z >
                farthestRoadZ)
            {
                farthestRoadZ =
                    segment.position.z;
            }
        }

        for (int i = 0;
             i < roadSegments.Count;
             i++)
        {
            Transform segment =
                roadSegments[i];

            if (segment == null)
            {
                continue;
            }

            if (!TryGetColliderBounds(
                    segment.gameObject,
                    out Bounds bounds))
            {
                continue;
            }

            bool behindPlayer =
                bounds.max.z <
                fixedPlayerZ -
                roadRecycleBehindDistance;

            if (!behindPlayer)
            {
                continue;
            }

            Vector3 position =
                segment.position;

            position.z =
                farthestRoadZ +
                roadLength;

            segment.position = position;
            farthestRoadZ = position.z;
        }
    }

    private void UpdateSpawnTimers(float deltaTime)
    {
        obstacleTimer -= deltaTime;
        coinTimer -= deltaTime;
        buildingTimer -= deltaTime;

        if (obstacleTimer <= 0f)
        {
            SpawnObstacleRow();

            obstacleTimer =
                GetCurrentObstacleInterval();
        }

        if (coinTimer <= 0f)
        {
            SpawnCoinLine();

            coinTimer = Mathf.Max(
                0.2f,
                coinSpawnInterval
            );
        }

        if (buildingPrefabs != null &&
            buildingPrefabs.Length > 0 &&
            buildingTimer <= 0f)
        {
            SpawnBuildingProps();

            buildingTimer = Mathf.Max(
                0.2f,
                buildingSpawnInterval
            );
        }
    }

    private float GetCurrentObstacleInterval()
    {
        return Mathf.Max(
            minimumObstacleInterval,
            startingObstacleInterval -
            elapsedGameTime *
            obstacleIntervalDecreasePerSecond
        );
    }

    private float GetDoubleObstacleChance()
    {
        return Mathf.Clamp(
            startingDoubleObstacleChance +
            elapsedGameTime *
            doubleObstacleChanceIncreasePerSecond,
            startingDoubleObstacleChance,
            maximumDoubleObstacleChance
        );
    }

    private void SpawnObstacleRow()
    {
        int[] lanes = { 0, 1, 2 };

        ShuffleLanes(lanes);

        int desiredObstacleCount =
            Random.value <
            GetDoubleObstacleChance()
                ? 2
                : 1;

        int spawnedCount = 0;

        float spawnZ =
            fixedPlayerZ +
            obstacleSpawnDistance;

        for (int i = 0;
             i < lanes.Length;
             i++)
        {
            if (spawnedCount >=
                desiredObstacleCount)
            {
                break;
            }

            if (TrySpawnObstacle(
                    lanes[i],
                    spawnZ))
            {
                spawnedCount++;
            }
        }
    }

    private bool TrySpawnObstacle(
        int lane,
        float spawnZ)
    {
        GameObject selectedPrefab =
            obstaclePrefabs[
                Random.Range(
                    0,
                    obstaclePrefabs.Length
                )
            ];

        if (selectedPrefab == null)
        {
            return false;
        }

        GameObject obstacle = Instantiate(
            selectedPrefab,
            new Vector3(
                GetLaneX(lane),
                roadSurfaceY,
                spawnZ
            ),
            selectedPrefab.transform.rotation,
            spawnedObjectsContainer
        );

        if (!TryGetColliderBounds(
                obstacle,
                out Bounds obstacleBounds))
        {
            Destroy(obstacle);
            return false;
        }

        Vector3 obstaclePosition =
            obstacle.transform.position;

        obstaclePosition.y +=
            roadSurfaceY -
            obstacleBounds.min.y;

        obstacle.transform.position =
            obstaclePosition;

        TryGetColliderBounds(
            obstacle,
            out obstacleBounds
        );

        float obstacleHalfLength =
            obstacleBounds.extents.z;

        /*
         * Do not place an obstacle over an existing coin.
         */
        if (HasCoinConflict(
                lane,
                spawnZ,
                obstacleHalfLength))
        {
            Destroy(obstacle);
            return false;
        }

        MakeCollidersTriggers(obstacle);
        ConfigureSpawnedRigidbodies(obstacle);

        spawnedObjects.Add(
            new SpawnedObject
            {
                gameObject = obstacle,
                objectTransform =
                    obstacle.transform,

                objectType =
                    SpawnedObjectType.Obstacle,

                laneIndex = lane,
                halfLength =
                    obstacleHalfLength,

                baseY =
                    obstacle.transform.position.y
            }
        );

        return true;
    }

    private void SpawnCoinLine()
    {
        int minimumAmount =
            Mathf.Max(
                1,
                minimumCoinsPerLine
            );

        int maximumAmount =
            Mathf.Max(
                minimumAmount,
                maximumCoinsPerLine
            );

        int coinAmount = Random.Range(
            minimumAmount,
            maximumAmount + 1
        );

        int[] lanes = { 0, 1, 2 };
        ShuffleLanes(lanes);

        float startZ =
            fixedPlayerZ +
            coinSpawnDistance;

        int selectedLane = -1;

        /*
         * Find a lane where none of the planned coins
         * will overlap an existing obstacle.
         */
        for (int i = 0;
             i < lanes.Length;
             i++)
        {
            if (IsCoinLineClear(
                    lanes[i],
                    startZ,
                    coinAmount))
            {
                selectedLane = lanes[i];
                break;
            }
        }

        if (selectedLane < 0)
        {
            // All three lanes currently conflict.
            return;
        }

        for (int i = 0;
             i < coinAmount;
             i++)
        {
            float coinZ =
                startZ +
                i * coinSpacing;

            SpawnCoin(
                selectedLane,
                coinZ,
                i
            );
        }
    }

    private bool IsCoinLineClear(
        int lane,
        float startZ,
        int coinAmount)
    {
        for (int i = 0;
             i < coinAmount;
             i++)
        {
            float coinZ =
                startZ +
                i * coinSpacing;

            if (HasObstacleConflict(
                    lane,
                    coinZ))
            {
                return false;
            }
        }

        return true;
    }

    private void SpawnCoin(
        int lane,
        float spawnZ,
        int coinIndex)
    {
        GameObject selectedPrefab =
            coinPrefabs[
                Random.Range(
                    0,
                    coinPrefabs.Length
                )
            ];

        if (selectedPrefab == null)
        {
            return;
        }

        GameObject coin = Instantiate(
            selectedPrefab,
            new Vector3(
                GetLaneX(lane),
                roadSurfaceY,
                spawnZ
            ),
            selectedPrefab.transform.rotation,
            spawnedObjectsContainer
        );

        if (!TryGetColliderBounds(
                coin,
                out Bounds coinBounds))
        {
            Destroy(coin);
            return;
        }

        float desiredCentreY =
            roadSurfaceY +
            coinHeightAboveRoad;

        Vector3 coinPosition =
            coin.transform.position;

        coinPosition.y +=
            desiredCentreY -
            coinBounds.center.y;

        coin.transform.position =
            coinPosition;

        TryGetColliderBounds(
            coin,
            out coinBounds
        );

        MakeCollidersTriggers(coin);
        ConfigureSpawnedRigidbodies(coin);

        spawnedObjects.Add(
            new SpawnedObject
            {
                gameObject = coin,
                objectTransform =
                    coin.transform,

                objectType =
                    SpawnedObjectType.Coin,

                laneIndex = lane,
                halfLength =
                    coinBounds.extents.z,

                baseY =
                    coin.transform.position.y,

                hoverPhase =
                    coinIndex * 0.45f
            }
        );
    }

    private bool HasObstacleConflict(
        int lane,
        float coinZ)
    {
        for (int i = 0;
             i < spawnedObjects.Count;
             i++)
        {
            SpawnedObject spawned =
                spawnedObjects[i];

            if (spawned == null ||
                spawned.objectTransform == null ||
                spawned.objectType !=
                SpawnedObjectType.Obstacle ||
                spawned.laneIndex != lane)
            {
                continue;
            }

            float distance =
                Mathf.Abs(
                    spawned.objectTransform.position.z -
                    coinZ
                );

            float requiredDistance =
                spawned.halfLength +
                coinObstacleSafetyGap;

            if (distance <
                requiredDistance)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasCoinConflict(
        int lane,
        float obstacleZ,
        float obstacleHalfLength)
    {
        for (int i = 0;
             i < spawnedObjects.Count;
             i++)
        {
            SpawnedObject spawned =
                spawnedObjects[i];

            if (spawned == null ||
                spawned.objectTransform == null ||
                spawned.objectType !=
                SpawnedObjectType.Coin ||
                spawned.laneIndex != lane)
            {
                continue;
            }

            float distance =
                Mathf.Abs(
                    spawned.objectTransform.position.z -
                    obstacleZ
                );

            float requiredDistance =
                obstacleHalfLength +
                spawned.halfLength +
                coinObstacleSafetyGap;

            if (distance <
                requiredDistance)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateInitialBuildings()
    {
        if (buildingPrefabs == null ||
            buildingPrefabs.Length == 0)
        {
            return;
        }

        for (int i = 0;
             i < initialBuildingRows;
             i++)
        {
            float spawnZ =
                fixedPlayerZ +
                initialBuildingStartDistance +
                i * initialBuildingSpacing;

            if (spawnBuildingsOnBothSides)
            {
                SpawnBuilding(-1, spawnZ);
                SpawnBuilding(1, spawnZ);
            }
            else
            {
                int side =
                    Random.value < 0.5f
                        ? -1
                        : 1;

                SpawnBuilding(
                    side,
                    spawnZ
                );
            }
        }
    }

    private void SpawnBuildingProps()
    {
        float spawnZ =
            fixedPlayerZ +
            buildingSpawnDistance +
            Random.Range(
                -buildingZJitter,
                buildingZJitter
            );

        if (spawnBuildingsOnBothSides)
        {
            SpawnBuilding(-1, spawnZ);
            SpawnBuilding(1, spawnZ);
        }
        else
        {
            int side =
                Random.value < 0.5f
                    ? -1
                    : 1;

            SpawnBuilding(
                side,
                spawnZ
            );
        }
    }

    private void SpawnBuilding(
        int side,
        float spawnZ)
    {
        if (buildingPrefabs == null ||
            buildingPrefabs.Length == 0)
        {
            return;
        }

        GameObject selectedPrefab =
            buildingPrefabs[
                Random.Range(
                    0,
                    buildingPrefabs.Length
                )
            ];

        if (selectedPrefab == null)
        {
            return;
        }

        Quaternion rotation =
            selectedPrefab.transform.rotation;

        if (randomizeBuildingYRotation)
        {
            rotation *= Quaternion.Euler(
                0f,
                Random.Range(0f, 360f),
                0f
            );
        }

        GameObject building = Instantiate(
            selectedPrefab,
            new Vector3(
                roadCentreX,
                roadSurfaceY,
                spawnZ
            ),
            rotation,
            spawnedObjectsContainer
        );

        if (!TryGetVisualBounds(
                building,
                out Bounds buildingBounds))
        {
            Destroy(building);
            return;
        }

        float buildingX;

        if (side < 0)
        {
            buildingX =
                roadMinimumX -
                buildingSideGap -
                buildingBounds.extents.x;
        }
        else
        {
            buildingX =
                roadMaximumX +
                buildingSideGap +
                buildingBounds.extents.x;
        }

        Vector3 buildingPosition =
            building.transform.position;

        buildingPosition.x = buildingX;

        buildingPosition.y +=
            roadSurfaceY -
            buildingBounds.min.y;

        building.transform.position =
            buildingPosition;

        ConfigureSpawnedRigidbodies(building);

        spawnedObjects.Add(
            new SpawnedObject
            {
                gameObject = building,
                objectTransform =
                    building.transform,

                objectType =
                    SpawnedObjectType.Building,

                laneIndex = -1,
                halfLength =
                    buildingBounds.extents.z,

                baseY =
                    building.transform.position.y
            }
        );
    }

    private void MoveSpawnedObjects(float deltaTime)
    {
        float movement =
            currentWorldSpeed * deltaTime;

        for (int i = spawnedObjects.Count - 1;
             i >= 0;
             i--)
        {
            SpawnedObject spawned =
                spawnedObjects[i];

            if (spawned == null ||
                spawned.gameObject == null ||
                spawned.objectTransform == null)
            {
                spawnedObjects.RemoveAt(i);
                continue;
            }

            Vector3 position =
                spawned.objectTransform.position;

            position.z -= movement;

            if (spawned.objectType ==
                SpawnedObjectType.Coin)
            {
                position.y =
                    spawned.baseY +
                    Mathf.Sin(
                        Time.time *
                        coinHoverSpeed +
                        spawned.hoverPhase
                    ) *
                    coinHoverHeight;

                spawned.objectTransform.Rotate(
                    0f,
                    coinRotationSpeed * deltaTime,
                    0f,
                    Space.World
                );
            }

            spawned.objectTransform.position =
                position;

            if (position.z <
                fixedPlayerZ -
                cleanupDistanceBehindPlayer)
            {
                Destroy(spawned.gameObject);
                spawnedObjects.RemoveAt(i);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleContact(other.transform);
    }

    private void OnCollisionEnter(
        Collision collision)
    {
        if (collision == null ||
            collision.collider == null)
        {
            return;
        }

        HandleContact(
            collision.collider.transform
        );
    }

    private void HandleContact(
        Transform contactedTransform)
    {
        if (gameOver ||
            !gameStarted ||
            contactedTransform == null)
        {
            return;
        }

        SpawnedObject contactedObject =
            FindSpawnedObject(
                contactedTransform
            );

        if (contactedObject == null)
        {
            return;
        }

        if (contactedObject.objectType ==
            SpawnedObjectType.Coin)
        {
            CollectCoin(contactedObject);
        }
        else if (
            contactedObject.objectType ==
            SpawnedObjectType.Obstacle)
        {
            EndGame();
        }
    }

    private SpawnedObject FindSpawnedObject(
        Transform contactedTransform)
    {
        for (int i = 0;
             i < spawnedObjects.Count;
             i++)
        {
            SpawnedObject spawned =
                spawnedObjects[i];

            if (spawned == null ||
                spawned.objectTransform == null)
            {
                continue;
            }

            bool hitRoot =
                contactedTransform ==
                spawned.objectTransform;

            bool hitChild =
                contactedTransform.IsChildOf(
                    spawned.objectTransform
                );

            if (hitRoot || hitChild)
            {
                return spawned;
            }
        }

        return null;
    }

    private void CollectCoin(
        SpawnedObject collectedCoin)
    {
        if (collectedCoin == null)
        {
            return;
        }

        currentCoins++;

        spawnedObjects.Remove(
            collectedCoin
        );

        if (collectedCoin.gameObject != null)
        {
            Destroy(
                collectedCoin.gameObject
            );
        }

        UpdateUI();
    }

    private void EndGame()
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;
        gameStarted = false;
        currentWorldSpeed = 0f;

        if (currentCoins > highScore)
        {
            highScore = currentCoins;

            PlayerPrefs.SetInt(
                HighScoreKey,
                highScore
            );

            PlayerPrefs.Save();
        }

        if (finalCoinCountText != null)
        {
            finalCoinCountText.text =
                $"Coins: {currentCoins}";
        }

        if (finalHighScoreText != null)
        {
            finalHighScoreText.text =
                $"High Score: {highScore}";
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (coinCountText != null)
        {
            coinCountText.text =
                $"Coins: {currentCoins}";
        }

        if (highScoreText != null)
        {
            int visibleHighScore =
                Mathf.Max(
                    highScore,
                    currentCoins
                );

            highScoreText.text =
                $"High Score: {visibleHighScore}";
        }
    }

    private void UpdateRearViewDarkness()
    {
        if (rearViewDarkOverlay == null)
        {
            return;
        }

        float targetDarkness = 0f;

        /*
         * Only hide the rear view while gameplay is active.
         * This prevents the overlay from hiding menu panels.
         */
        if (gameStarted &&
            !gameOver &&
            playerCamera != null)
        {
            Vector3 cameraForward =
                playerCamera.transform.forward;

            Vector3 roadForward =
                roadDirectionReference != null
                    ? roadDirectionReference.forward
                    : Vector3.forward;

            cameraForward.y = 0f;
            roadForward.y = 0f;

            if (cameraForward.sqrMagnitude >
                    0.001f &&
                roadForward.sqrMagnitude >
                    0.001f)
            {
                cameraForward.Normalize();
                roadForward.Normalize();

                float lookDot = Vector3.Dot(
                    cameraForward,
                    roadForward
                );

                float denominator =
                    rearDarknessStartDot -
                    rearFullDarknessDot;

                if (Mathf.Abs(denominator) >
                    0.001f)
                {
                    targetDarkness =
                        Mathf.Clamp01(
                            (
                                rearDarknessStartDot -
                                lookDot
                            ) /
                            denominator
                        );
                }
            }
        }

        rearViewDarkOverlay.alpha =
            Mathf.MoveTowards(
                rearViewDarkOverlay.alpha,
                targetDarkness,
                rearDarknessFadeSpeed *
                Time.deltaTime
            );
    }

    public void ResetSavedHighScore()
    {
        highScore = 0;

        PlayerPrefs.DeleteKey(
            HighScoreKey
        );

        PlayerPrefs.Save();

        UpdateUI();
    }

    private void ShuffleLanes(int[] lanes)
    {
        for (int i = lanes.Length - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            int temporaryValue =
                lanes[i];

            lanes[i] =
                lanes[randomIndex];

            lanes[randomIndex] =
                temporaryValue;
        }
    }

    private void ConfigureSpawnedRigidbodies(
        GameObject targetObject)
    {
        Rigidbody[] rigidbodies =
            targetObject.GetComponentsInChildren<Rigidbody>(
                true
            );

        for (int i = 0;
             i < rigidbodies.Length;
             i++)
        {
            Rigidbody body =
                rigidbodies[i];

            body.useGravity = false;
            body.isKinematic = true;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private void MakeCollidersTriggers(
        GameObject targetObject)
    {
        Collider[] colliders =
            targetObject.GetComponentsInChildren<Collider>(
                true
            );

        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            Collider currentCollider =
                colliders[i];

            if (currentCollider is MeshCollider meshCollider &&
                !meshCollider.convex)
            {
                meshCollider.convex = true;
            }

            currentCollider.isTrigger = true;
        }
    }

    private bool HasCollider(
        GameObject targetObject)
    {
        if (targetObject == null)
        {
            return false;
        }

        Collider[] colliders =
            targetObject.GetComponentsInChildren<Collider>(
                true
            );

        return colliders.Length > 0;
    }

    private bool TryGetColliderBounds(
        GameObject targetObject,
        out Bounds combinedBounds)
    {
        combinedBounds = new Bounds();

        if (targetObject == null)
        {
            return false;
        }

        Collider[] colliders =
            targetObject.GetComponentsInChildren<Collider>(
                true
            );

        bool foundBounds = false;

        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            Collider currentCollider =
                colliders[i];

            if (currentCollider == null ||
                !currentCollider.enabled ||
                !currentCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!foundBounds)
            {
                combinedBounds =
                    currentCollider.bounds;

                foundBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(
                    currentCollider.bounds
                );
            }
        }

        return foundBounds;
    }

    private bool TryGetVisualBounds(
        GameObject targetObject,
        out Bounds combinedBounds)
    {
        if (TryGetColliderBounds(
                targetObject,
                out combinedBounds))
        {
            return true;
        }

        combinedBounds = new Bounds();

        Renderer[] renderers =
            targetObject.GetComponentsInChildren<Renderer>(
                true
            );

        bool foundBounds = false;

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            Renderer currentRenderer =
                renderers[i];

            if (currentRenderer == null ||
                !currentRenderer.enabled ||
                !currentRenderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!foundBounds)
            {
                combinedBounds =
                    currentRenderer.bounds;

                foundBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(
                    currentRenderer.bounds
                );
            }
        }

        return foundBounds;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        for (int lane = 0;
             lane < 3;
             lane++)
        {
            float laneX = GetLaneX(lane);

            Gizmos.DrawLine(
                new Vector3(
                    laneX,
                    roadSurfaceY,
                    fixedPlayerZ - 10f
                ),
                new Vector3(
                    laneX,
                    roadSurfaceY,
                    fixedPlayerZ + 60f
                )
            );
        }
    }
}
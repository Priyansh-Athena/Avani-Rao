using System.Collections;
using DG.Tweening;
using Google.XR.Cardboard;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TowerStack : MonoBehaviour
{
    [Header("Main References")]
    [Tooltip("Assign the Cardboard player/XR rig, not only the camera.")]
    [SerializeField] private Transform playerRoot;

    [Tooltip("Assign the bottom block already placed in the scene.")]
    [SerializeField] private GameObject baseBlock;

    [Tooltip("Assign one or more block prefabs.")]
    [SerializeField] private GameObject[] blockPrefabs;

    [Header("Start UI")]
    [SerializeField] private GameObject startPanel;

    [Header("Gameplay UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;

    [Tooltip(
        "Optional text used to display +10 or Perfect! +15."
    )]
    [SerializeField] private TMP_Text placementFeedbackText;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text finalHighScoreText;

    [Header("Block Movement")]
    [Tooltip("Starting horizontal distance on either side of the tower.")]
    [SerializeField] private float horizontalMoveDistance = 3.5f;

    [Tooltip("Maximum movement distance at higher difficulties.")]
    [SerializeField] private float maximumHorizontalMoveDistance = 5.5f;

    [SerializeField] private float startingMoveSpeed = 2.5f;
    [SerializeField] private float maximumMoveSpeed = 10f;

    [Tooltip("Continuous movement speed increase every second.")]
    [SerializeField] private float speedIncreasePerSecond = 0.04f;

    [Tooltip("Height between the tower and the moving block.")]
    [SerializeField] private float spawnHeightAboveTower = 2.5f;

    [Tooltip("Starting delay before spawning the next block.")]
    [SerializeField] private float nextBlockDelay = 0.35f;

    [Header("Time-Based Difficulty")]
    [Tooltip("A new difficulty level is reached after this many seconds.")]
    [Min(1f)]
    [SerializeField] private float difficultyIncreaseEvery = 15f;

    [Tooltip("Additional block speed added at each difficulty level.")]
    [SerializeField] private float speedIncreasePerLevel = 0.65f;

    [Tooltip("Additional movement distance added at each difficulty level.")]
    [SerializeField] private float distanceIncreasePerLevel = 0.25f;

    [Tooltip("Next-block delay reduction at each difficulty level.")]
    [SerializeField] private float delayDecreasePerLevel = 0.03f;

    [Tooltip("The next-block delay will never become lower than this.")]
    [SerializeField] private float minimumNextBlockDelay = 0.12f;

    [Header("Placement Detection")]
    [Tooltip(
        "Minimum percentage of the block that must overlap " +
        "the previous block."
    )]
    [Range(0.01f, 1f)]
    [SerializeField] private float minimumOverlapPercent = 0.15f;

    [Tooltip(
        "Overlap required to receive the perfect-placement bonus."
    )]
    [Range(0.5f, 1f)]
    [SerializeField] private float perfectOverlapThreshold = 0.95f;

    [Tooltip(
        "Small tolerance used when detecting that the block " +
        "has reached the tower."
    )]
    [SerializeField] private float landingTolerance = 0.12f;

    [Tooltip(
        "Game Over is triggered if the block falls this far " +
        "below the tower."
    )]
    [SerializeField] private float fallFailDistance = 3f;

    [Header("Player Movement")]
    [Tooltip(
        "Vertical offset between the player and the centre " +
        "of the latest block."
    )]
    [SerializeField] private float playerHeightOffset = 1.5f;

    [SerializeField] private float playerMoveDuration = 0.4f;
    [SerializeField] private Ease playerMoveEase = Ease.InOutSine;

    [Header("Scoring")]
    [SerializeField] private int scorePerBlock = 10;

    [Tooltip(
        "A 1.5 multiplier gives 15 points when the normal score is 10."
    )]
    [Min(1f)]
    [SerializeField] private float perfectScoreMultiplier = 1.5f;

    [Header("Feedback Animation")]
    [SerializeField] private float feedbackVisibleDuration = 0.6f;
    [SerializeField] private float feedbackScaleDuration = 0.2f;
    [SerializeField] private float feedbackFadeDuration = 0.25f;

    [Header("Drop Input")]
    [SerializeField] private bool useCardboardTrigger = true;

    [SerializeField]
    private KeyCode primaryControllerButton =
        KeyCode.Joystick1Button0;

    [SerializeField]
    private bool acceptAlternateControllerButton = true;

    [SerializeField]
    private KeyCode alternateControllerButton =
        KeyCode.Joystick1Button14;

    [Tooltip("Allows testing using the mouse in the Unity Editor.")]
    [SerializeField] private bool allowMouseClick = true;

    [Tooltip(
        "Prevents the Start button trigger from immediately " +
        "dropping the first block."
    )]
    [SerializeField] private float inputDelayAfterSpawn = 0.3f;

    private const string HighScoreKey = "TowerStackHighScore";

    private GameObject currentBlock;
    private Rigidbody currentBlockRigidbody;
    private Collider currentBlockCollider;

    private GameObject lastPlacedBlock;
    private Collider lastPlacedCollider;

    private int currentScore;
    private int highScore;

    private float elapsedGameTime;
    private float movementDirection;
    private float movementCentreX;
    private float minimumMovementX;
    private float maximumMovementX;
    private float nextAllowedDropTime;

    private bool gameStarted;
    private bool gameOver;
    private bool blockIsDropping;
    private bool waitingForNextBlock;

    private Coroutine nextBlockCoroutine;

    private Vector3 feedbackOriginalScale;

    private void Awake()
    {
        highScore = PlayerPrefs.GetInt(
            HighScoreKey,
            0
        );

        currentScore = 0;
        gameStarted = false;
        gameOver = false;

        if (startPanel != null)
        {
            startPanel.SetActive(true);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (placementFeedbackText != null)
        {
            feedbackOriginalScale =
                placementFeedbackText.transform.localScale;

            placementFeedbackText.alpha = 0f;
        }
    }

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        lastPlacedBlock = baseBlock;

        lastPlacedCollider =
            baseBlock.GetComponentInChildren<Collider>();

        UpdateUI();
    }

    private void Update()
    {
        if (!gameStarted || gameOver)
        {
            return;
        }

        elapsedGameTime += Time.deltaTime;

        if (currentBlock == null)
        {
            return;
        }

        if (!blockIsDropping)
        {
            MoveCurrentBlock();

            if (WasDropPressed())
            {
                DropCurrentBlock();
            }
        }
        else
        {
            CheckDroppedBlock();
        }
    }

    public void StartGame()
    {
        if (gameStarted || gameOver)
        {
            return;
        }

        gameStarted = true;
        elapsedGameTime = 0f;

        if (startPanel != null)
        {
            startPanel.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        SpawnNextBlock();
    }

    public void ResetGame()
    {
        Time.timeScale = 1f;

        DOTween.KillAll();

        Scene activeScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            activeScene.buildIndex
        );
    }

    public void RestartGame()
    {
        ResetGame();
    }

    private bool ValidateReferences()
    {
        if (playerRoot == null)
        {
            Debug.LogError(
                "Tower Stack: Player Root is not assigned."
            );

            return false;
        }

        if (baseBlock == null)
        {
            Debug.LogError(
                "Tower Stack: Base Block is not assigned."
            );

            return false;
        }

        if (baseBlock.GetComponentInChildren<Collider>() == null)
        {
            Debug.LogError(
                "Tower Stack: Base Block requires a Collider."
            );

            return false;
        }

        if (blockPrefabs == null ||
            blockPrefabs.Length == 0)
        {
            Debug.LogError(
                "Tower Stack: Assign at least one block prefab."
            );

            return false;
        }

        for (int i = 0; i < blockPrefabs.Length; i++)
        {
            GameObject prefab = blockPrefabs[i];

            if (prefab == null)
            {
                Debug.LogError(
                    $"Tower Stack: Block Prefab {i} is missing."
                );

                return false;
            }

            if (prefab.GetComponentInChildren<Rigidbody>() == null)
            {
                Debug.LogError(
                    $"{prefab.name} requires a Rigidbody."
                );

                return false;
            }

            if (prefab.GetComponentInChildren<Collider>() == null)
            {
                Debug.LogError(
                    $"{prefab.name} requires a Collider."
                );

                return false;
            }
        }

        return true;
    }

    private void SpawnNextBlock()
    {
        if (!gameStarted || gameOver)
        {
            return;
        }

        waitingForNextBlock = false;
        blockIsDropping = false;

        GameObject selectedPrefab =
            blockPrefabs[
                Random.Range(0, blockPrefabs.Length)
            ];

        Bounds lastBounds =
            lastPlacedCollider.bounds;

        float selectedSide =
            Random.value < 0.5f ? -1f : 1f;

        float currentMoveDistance =
            GetCurrentMoveDistance();

        Vector3 initialPosition = new Vector3(
            lastBounds.center.x +
            selectedSide * currentMoveDistance,

            lastBounds.max.y +
            spawnHeightAboveTower,

            lastBounds.center.z
        );

        currentBlock = Instantiate(
            selectedPrefab,
            initialPosition,
            selectedPrefab.transform.rotation
        );

        currentBlockRigidbody =
            currentBlock.GetComponent<Rigidbody>();

        if (currentBlockRigidbody == null)
        {
            currentBlockRigidbody =
                currentBlock.GetComponentInChildren<Rigidbody>();
        }

        currentBlockCollider =
            currentBlock.GetComponent<Collider>();

        if (currentBlockCollider == null)
        {
            currentBlockCollider =
                currentBlock.GetComponentInChildren<Collider>();
        }

        if (currentBlockRigidbody == null ||
            currentBlockCollider == null)
        {
            Debug.LogError(
                "Spawned block requires a Rigidbody and Collider."
            );

            Destroy(currentBlock);
            EndGame();
            return;
        }

        currentBlockRigidbody.isKinematic = true;
        currentBlockRigidbody.useGravity = false;
        currentBlockRigidbody.linearVelocity = Vector3.zero;
        currentBlockRigidbody.angularVelocity = Vector3.zero;

        currentBlockRigidbody.constraints =
            RigidbodyConstraints.FreezeRotation;

        Bounds currentBounds =
            currentBlockCollider.bounds;

        Vector3 correctedPosition =
            currentBlock.transform.position;

        correctedPosition.x +=
            initialPosition.x -
            currentBounds.center.x;

        correctedPosition.z +=
            lastBounds.center.z -
            currentBounds.center.z;

        correctedPosition.y +=
            initialPosition.y -
            currentBounds.min.y;

        currentBlock.transform.position =
            correctedPosition;

        movementCentreX =
            lastBounds.center.x;

        minimumMovementX =
            movementCentreX -
            currentMoveDistance;

        maximumMovementX =
            movementCentreX +
            currentMoveDistance;

        movementDirection =
            selectedSide < 0f ? 1f : -1f;

        nextAllowedDropTime =
            Time.unscaledTime +
            inputDelayAfterSpawn;
    }

    private void MoveCurrentBlock()
    {
        float currentMoveSpeed =
            GetCurrentMoveSpeed();

        Vector3 position =
            currentBlock.transform.position;

        position.x +=
            movementDirection *
            currentMoveSpeed *
            Time.deltaTime;

        if (position.x >= maximumMovementX)
        {
            position.x = maximumMovementX;
            movementDirection = -1f;
        }
        else if (position.x <= minimumMovementX)
        {
            position.x = minimumMovementX;
            movementDirection = 1f;
        }

        currentBlock.transform.position =
            position;
    }

    private int GetDifficultyLevel()
    {
        if (difficultyIncreaseEvery <= 0f)
        {
            return 0;
        }

        return Mathf.FloorToInt(
            elapsedGameTime /
            difficultyIncreaseEvery
        );
    }

    private float GetCurrentMoveSpeed()
    {
        int difficultyLevel =
            GetDifficultyLevel();

        float continuousIncrease =
            elapsedGameTime *
            speedIncreasePerSecond;

        float levelIncrease =
            difficultyLevel *
            speedIncreasePerLevel;

        return Mathf.Min(
            maximumMoveSpeed,
            startingMoveSpeed +
            continuousIncrease +
            levelIncrease
        );
    }

    private float GetCurrentMoveDistance()
    {
        int difficultyLevel =
            GetDifficultyLevel();

        return Mathf.Min(
            maximumHorizontalMoveDistance,
            horizontalMoveDistance +
            difficultyLevel *
            distanceIncreasePerLevel
        );
    }

    private float GetCurrentNextBlockDelay()
    {
        int difficultyLevel =
            GetDifficultyLevel();

        return Mathf.Max(
            minimumNextBlockDelay,
            nextBlockDelay -
            difficultyLevel *
            delayDecreasePerLevel
        );
    }

    private bool WasDropPressed()
    {
        if (Time.unscaledTime <
            nextAllowedDropTime)
        {
            return false;
        }

        bool pressed =
            Input.GetKeyDown(
                primaryControllerButton
            );

        if (acceptAlternateControllerButton)
        {
            pressed |= Input.GetKeyDown(
                alternateControllerButton
            );
        }

        if (useCardboardTrigger)
        {
            pressed |= Api.IsTriggerPressed;
        }

#if UNITY_EDITOR
        if (allowMouseClick)
        {
            pressed |= Input.GetMouseButtonDown(0);
        }
#endif

        return pressed;
    }

    private void DropCurrentBlock()
    {
        if (currentBlock == null ||
            currentBlockRigidbody == null ||
            blockIsDropping)
        {
            return;
        }

        blockIsDropping = true;

        currentBlockRigidbody.isKinematic = false;
        currentBlockRigidbody.useGravity = true;
        currentBlockRigidbody.linearVelocity = Vector3.zero;
    }

    private void CheckDroppedBlock()
    {
        if (currentBlock == null ||
            currentBlockCollider == null ||
            lastPlacedCollider == null)
        {
            EndGame();
            return;
        }

        Bounds fallingBounds =
            currentBlockCollider.bounds;

        Bounds lastBounds =
            lastPlacedCollider.bounds;

        bool reachedTowerHeight =
            fallingBounds.min.y <=
            lastBounds.max.y +
            landingTolerance;

        bool movingDownward =
            currentBlockRigidbody == null ||
            currentBlockRigidbody.linearVelocity.y <= 0.1f;

        if (reachedTowerHeight && movingDownward)
        {
            float overlapX = CalculateOverlap(
                fallingBounds.min.x,
                fallingBounds.max.x,
                lastBounds.min.x,
                lastBounds.max.x
            );

            float overlapZ = CalculateOverlap(
                fallingBounds.min.z,
                fallingBounds.max.z,
                lastBounds.min.z,
                lastBounds.max.z
            );

            float smallestWidthX =
                Mathf.Min(
                    fallingBounds.size.x,
                    lastBounds.size.x
                );

            float smallestWidthZ =
                Mathf.Min(
                    fallingBounds.size.z,
                    lastBounds.size.z
                );

            float overlapPercentX =
                smallestWidthX > 0f
                    ? overlapX / smallestWidthX
                    : 0f;

            float overlapPercentZ =
                smallestWidthZ > 0f
                    ? overlapZ / smallestWidthZ
                    : 0f;

            /*
             * Use the lower percentage so that a block must
             * overlap correctly on both the X and Z axes.
             */
            float overallOverlapPercent =
                Mathf.Min(
                    overlapPercentX,
                    overlapPercentZ
                );

            bool correctlyPlaced =
                overallOverlapPercent >=
                minimumOverlapPercent;

            if (correctlyPlaced)
            {
                CompleteBlockPlacement(
                    overallOverlapPercent
                );
            }
            else
            {
                EndGame();
            }

            return;
        }

        bool fellTooFar =
            fallingBounds.max.y <
            lastBounds.min.y -
            fallFailDistance;

        if (fellTooFar)
        {
            EndGame();
        }
    }

    private float CalculateOverlap(
        float firstMinimum,
        float firstMaximum,
        float secondMinimum,
        float secondMaximum)
    {
        return Mathf.Max(
            0f,
            Mathf.Min(
                firstMaximum,
                secondMaximum
            ) -
            Mathf.Max(
                firstMinimum,
                secondMinimum
            )
        );
    }

    private void CompleteBlockPlacement(
        float overlapPercent)
    {
        if (currentBlock == null ||
            currentBlockCollider == null)
        {
            EndGame();
            return;
        }

        Bounds lastBounds =
            lastPlacedCollider.bounds;

        Bounds currentBounds =
            currentBlockCollider.bounds;

        Vector3 snappedPosition =
            currentBlock.transform.position;

        snappedPosition.y +=
            lastBounds.max.y -
            currentBounds.min.y;

        currentBlock.transform.position =
            snappedPosition;

        Physics.SyncTransforms();

        currentBlockRigidbody.linearVelocity =
            Vector3.zero;

        currentBlockRigidbody.angularVelocity =
            Vector3.zero;

        currentBlockRigidbody.useGravity = false;
        currentBlockRigidbody.isKinematic = true;

        currentBlockRigidbody.constraints =
            RigidbodyConstraints.FreezeAll;

        lastPlacedBlock = currentBlock;
        lastPlacedCollider = currentBlockCollider;

        bool perfectPlacement =
            overlapPercent >=
            perfectOverlapThreshold;

        int earnedScore = perfectPlacement
            ? Mathf.RoundToInt(
                scorePerBlock *
                perfectScoreMultiplier
            )
            : scorePerBlock;

        currentScore += earnedScore;

        if (currentScore > highScore)
        {
            highScore = currentScore;
        }

        UpdateUI();

        ShowPlacementFeedback(
            perfectPlacement,
            earnedScore
        );

        MovePlayerUp();

        currentBlock = null;
        currentBlockCollider = null;
        currentBlockRigidbody = null;

        blockIsDropping = false;

        if (!waitingForNextBlock)
        {
            nextBlockCoroutine =
                StartCoroutine(
                    SpawnNextBlockAfterDelay()
                );
        }
    }

    private IEnumerator SpawnNextBlockAfterDelay()
    {
        waitingForNextBlock = true;

        yield return new WaitForSeconds(
            GetCurrentNextBlockDelay()
        );

        waitingForNextBlock = false;
        nextBlockCoroutine = null;

        if (gameStarted && !gameOver)
        {
            SpawnNextBlock();
        }
    }

    private void ShowPlacementFeedback(
        bool perfectPlacement,
        int earnedScore)
    {
        if (placementFeedbackText == null)
        {
            return;
        }

        placementFeedbackText.DOKill();
        placementFeedbackText.transform.DOKill();

        placementFeedbackText.text =
            perfectPlacement
                ? $"Perfect! +{earnedScore}"
                : $"+{earnedScore}";

        placementFeedbackText.alpha = 1f;

        placementFeedbackText.transform.localScale =
            feedbackOriginalScale * 0.8f;

        Sequence feedbackSequence =
            DOTween.Sequence();

        feedbackSequence.Append(
            placementFeedbackText.transform
                .DOScale(
                    feedbackOriginalScale * 1.1f,
                    feedbackScaleDuration
                )
                .SetEase(Ease.OutBack)
        );

        feedbackSequence.AppendInterval(
            feedbackVisibleDuration
        );

        feedbackSequence.Append(
            placementFeedbackText
                .DOFade(
                    0f,
                    feedbackFadeDuration
                )
        );

        feedbackSequence.Join(
            placementFeedbackText.transform
                .DOScale(
                    feedbackOriginalScale,
                    feedbackFadeDuration
                )
        );
    }

    private void MovePlayerUp()
    {
        if (playerRoot == null ||
            lastPlacedCollider == null)
        {
            return;
        }

        float targetPlayerY =
            lastPlacedCollider.bounds.center.y +
            playerHeightOffset;

        playerRoot.DOKill();

        playerRoot
            .DOMoveY(
                targetPlayerY,
                playerMoveDuration
            )
            .SetEase(playerMoveEase);
    }

    private void EndGame()
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;
        gameStarted = false;

        if (nextBlockCoroutine != null)
        {
            StopCoroutine(nextBlockCoroutine);
            nextBlockCoroutine = null;
        }

        if (currentScore > highScore)
        {
            highScore = currentScore;
        }

        PlayerPrefs.SetInt(
            HighScoreKey,
            highScore
        );

        PlayerPrefs.Save();

        if (finalScoreText != null)
        {
            finalScoreText.text =
                $"Score: {currentScore}";
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
        if (scoreText != null)
        {
            scoreText.text =
                $"Score: {currentScore}";
        }

        if (highScoreText != null)
        {
            highScoreText.text =
                $"High Score: {highScore}";
        }
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
}
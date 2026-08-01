using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TargetPop : MonoBehaviour
{
    [Header("Main References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject ballPrefab;

    [Tooltip(
        "Assign the 5 Spawn Objects here. " +
        "Their first child must be the Spawn Point."
    )]
    [SerializeField] private Transform[] spawnObjects = new Transform[5];

    [Header("Start UI")]
    [Tooltip("Assign the panel containing the instructions and Start button.")]
    [SerializeField] private GameObject startPanel;

    [Header("Gameplay UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text livesText;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text finalHighScoreText;

    [Header("Scoring")]
    [SerializeField] private int scorePerBall = 10;
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int missesPerLife = 3;

    [Header("Ball Throw Settings")]
    [SerializeField] private float startingUpwardImpulse = 6.5f;
    [SerializeField] private float startingSideImpulse = 0.35f;

    [Tooltip("A ball is counted as missed after this many seconds.")]
    [SerializeField] private float maximumBallLifetime = 4.5f;

    [Tooltip(
        "Also count the ball as missed if it falls this far " +
        "below its spawn point."
    )]
    [SerializeField] private float missDistanceBelowSpawn = 0.75f;

    [Header("Spawn Settings")]
    [SerializeField] private float startingSpawnInterval = 1.6f;
    [SerializeField] private float minimumSpawnInterval = 0.45f;
    [SerializeField] private float firstBallDelay = 1f;

    [Header("Difficulty")]
    [SerializeField] private float difficultyIncreaseEvery = 15f;
    [SerializeField] private float spawnIntervalDecrease = 0.15f;
    [SerializeField] private float upwardImpulseIncrease = 0.15f;
    [SerializeField] private float sideImpulseIncrease = 0.08f;
    [SerializeField] private float maximumSideImpulse = 1.5f;

    [Header("Aiming")]
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private LayerMask targetLayerMask = ~0;

    [Header("Controller Input")]
    [SerializeField]
    private KeyCode shortTriggerButton = KeyCode.JoystickButton0;

    [SerializeField] private bool acceptLongTriggerAsPop = true;

    [SerializeField]
    private KeyCode longTriggerButton = KeyCode.JoystickButton14;

    [Tooltip("Allows testing with the mouse inside the Unity Editor.")]
    [SerializeField] private bool allowMouseClick = true;

    [Header("Spawn Animation")]
    [Tooltip("The ball starts at this percentage of its original scale.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float spawnStartScaleMultiplier = 0.1f;

    [Tooltip("Time taken to animate from small scale to normal scale.")]
    [Min(0.01f)]
    [SerializeField] private float spawnScaleDuration = 0.5f;

    [SerializeField] private Ease spawnScaleEase = Ease.OutBack;

    [Header("Pop Animation")]
    [SerializeField] private float popGrowDuration = 0.08f;
    [SerializeField] private float popShrinkDuration = 0.14f;
    [SerializeField] private float popScaleMultiplier = 1.4f;

    [Header("Random Bright Colours")]
    [Range(0f, 1f)]
    [SerializeField] private float minimumSaturation = 0.75f;

    [Range(0f, 1f)]
    [SerializeField] private float maximumSaturation = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float minimumBrightness = 0.9f;

    [Range(0f, 1f)]
    [SerializeField] private float maximumBrightness = 1f;

    private const string HighScoreKey = "TargetPopHighScore";

    private int currentScore;
    private int highScore;
    private int currentLives;
    private int currentMisses;

    private float gameStartTime;

    private bool gameStarted;
    private bool gameOver;

    private Coroutine spawnCoroutine;
    private bool[] occupiedSpawnPoints;

    private readonly List<ActiveBall> activeBalls =
        new List<ActiveBall>();

    private class ActiveBall
    {
        public GameObject gameObject;
        public Transform transform;
        public Rigidbody rigidbody;

        public int spawnIndex;
        public float spawnHeight;
        public float spawnTime;

        public Vector3 originalScale;
        public bool resolved;
    }

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);

        currentScore = 0;
        currentLives = startingLives;
        currentMisses = 0;

        gameStarted = false;
        gameOver = false;

        int spawnCount =
            spawnObjects != null ? spawnObjects.Length : 0;

        occupiedSpawnPoints = new bool[spawnCount];

        if (startPanel != null)
        {
            startPanel.SetActive(true);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        /*
         * Only prepare the game here.
         * Ball spawning starts when StartGame() is called.
         */
        UpdateUI();
    }

    private void Update()
    {
        if (!gameStarted || gameOver)
        {
            return;
        }

        if (WasPopButtonPressed())
        {
            TryPopLookedAtBall();
        }

        CheckForMissedBalls();
    }

    /// <summary>
    /// Attach this function to the Start button's UnityEvent.
    /// </summary>
    public void StartGame()
    {
        if (gameStarted || gameOver)
        {
            return;
        }

        gameStarted = true;
        gameStartTime = Time.time;

        if (startPanel != null)
        {
            startPanel.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        spawnCoroutine = StartCoroutine(SpawnBallRoutine());
    }

    /// <summary>
    /// Attach this function to the Restart button's UnityEvent.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;

        DOTween.KillAll();

        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(currentScene.buildIndex);
    }

    private bool ValidateReferences()
    {
        if (playerCamera == null)
        {
            Debug.LogError(
                "Target Pop: Player Camera is not assigned."
            );

            return false;
        }

        if (ballPrefab == null)
        {
            Debug.LogError(
                "Target Pop: Ball Prefab is not assigned."
            );

            return false;
        }

        if (spawnObjects == null || spawnObjects.Length == 0)
        {
            Debug.LogError(
                "Target Pop: No Spawn Objects are assigned."
            );

            return false;
        }

        for (int i = 0; i < spawnObjects.Length; i++)
        {
            if (spawnObjects[i] == null)
            {
                Debug.LogError(
                    $"Target Pop: Spawn Object {i} is missing."
                );

                return false;
            }

            if (spawnObjects[i].childCount == 0)
            {
                Debug.LogError(
                    $"Target Pop: {spawnObjects[i].name} " +
                    "needs a first child Spawn Point."
                );

                return false;
            }
        }

        Rigidbody prefabRigidbody =
            ballPrefab.GetComponent<Rigidbody>();

        if (prefabRigidbody == null)
        {
            prefabRigidbody =
                ballPrefab.GetComponentInChildren<Rigidbody>();
        }

        if (prefabRigidbody == null)
        {
            Debug.LogError(
                "Target Pop: The Ball Prefab requires a Rigidbody."
            );

            return false;
        }

        Collider prefabCollider =
            ballPrefab.GetComponent<Collider>();

        if (prefabCollider == null)
        {
            prefabCollider =
                ballPrefab.GetComponentInChildren<Collider>();
        }

        if (prefabCollider == null)
        {
            Debug.LogError(
                "Target Pop: The Ball Prefab requires a Collider."
            );

            return false;
        }

        return true;
    }

    private IEnumerator SpawnBallRoutine()
    {
        yield return new WaitForSeconds(firstBallDelay);

        while (gameStarted && !gameOver)
        {
            int availableSpawnIndex = GetAvailableSpawnIndex();

            if (availableSpawnIndex >= 0)
            {
                SpawnBall(availableSpawnIndex);
            }

            yield return new WaitForSeconds(
                GetCurrentSpawnInterval()
            );
        }

        spawnCoroutine = null;
    }

    private int GetAvailableSpawnIndex()
    {
        if (occupiedSpawnPoints == null ||
            occupiedSpawnPoints.Length == 0)
        {
            return -1;
        }

        int randomStartIndex =
            Random.Range(0, occupiedSpawnPoints.Length);

        for (int i = 0; i < occupiedSpawnPoints.Length; i++)
        {
            int index =
                (randomStartIndex + i) %
                occupiedSpawnPoints.Length;

            if (!occupiedSpawnPoints[index])
            {
                return index;
            }
        }

        return -1;
    }

    private void SpawnBall(int spawnIndex)
    {
        Transform spawnPoint =
            spawnObjects[spawnIndex].GetChild(0);

        GameObject newBall = Instantiate(
            ballPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        Transform ballTransform = newBall.transform;

        Rigidbody ballRigidbody =
            newBall.GetComponent<Rigidbody>();

        if (ballRigidbody == null)
        {
            ballRigidbody =
                newBall.GetComponentInChildren<Rigidbody>();
        }

        if (ballRigidbody == null)
        {
            Debug.LogError(
                "Target Pop: Spawned Ball needs a Rigidbody."
            );

            Destroy(newBall);
            return;
        }

        occupiedSpawnPoints[spawnIndex] = true;

        Vector3 originalScale =
            ballTransform.localScale;

        Vector3 startingScale =
            originalScale * spawnStartScaleMultiplier;

        ballTransform.DOKill();
        ballTransform.localScale = startingScale;

        ApplyRandomBrightColor(newBall);

        ballTransform
            .DOScale(originalScale, spawnScaleDuration)
            .SetEase(spawnScaleEase)
            .SetLink(
                newBall,
                LinkBehaviour.KillOnDestroy
            );

        ballRigidbody.isKinematic = false;
        ballRigidbody.useGravity = true;
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;

        float sideImpulse =
            GetCurrentSideImpulse();

        float upwardImpulse =
            GetCurrentUpwardImpulse();

        Vector3 throwDirection = new Vector3(
            Random.Range(-sideImpulse, sideImpulse),
            upwardImpulse,
            Random.Range(
                -sideImpulse * 0.25f,
                sideImpulse * 0.25f
            )
        );

        ballRigidbody.AddForce(
            throwDirection,
            ForceMode.Impulse
        );

        ballRigidbody.AddTorque(
            Random.insideUnitSphere * 1.5f,
            ForceMode.Impulse
        );

        ActiveBall activeBall = new ActiveBall
        {
            gameObject = newBall,
            transform = ballTransform,
            rigidbody = ballRigidbody,

            spawnIndex = spawnIndex,
            spawnHeight = spawnPoint.position.y,
            spawnTime = Time.time,

            originalScale = originalScale,
            resolved = false
        };

        activeBalls.Add(activeBall);
    }

    private void ApplyRandomBrightColor(GameObject ball)
    {
        float saturation = Random.Range(
            minimumSaturation,
            maximumSaturation
        );

        float brightness = Random.Range(
            minimumBrightness,
            maximumBrightness
        );

        Color brightColor = Color.HSVToRGB(
            Random.Range(0f, 1f),
            saturation,
            brightness
        );

        Renderer[] renderers =
            ball.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer ballRenderer in renderers)
        {
            MaterialPropertyBlock propertyBlock =
                new MaterialPropertyBlock();

            ballRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetColor(
                "_BaseColor",
                brightColor
            );

            propertyBlock.SetColor(
                "_Color",
                brightColor
            );

            ballRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private bool WasPopButtonPressed()
    {
        bool controllerPressed =
            Input.GetKeyDown(shortTriggerButton);

        if (acceptLongTriggerAsPop)
        {
            controllerPressed |=
                Input.GetKeyDown(longTriggerButton);
        }

        bool mousePressed =
            allowMouseClick &&
            Input.GetMouseButtonDown(0);

        return controllerPressed || mousePressed;
    }

    private void TryPopLookedAtBall()
    {
        Ray aimRay = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        bool hitSomething = Physics.Raycast(
            aimRay,
            out RaycastHit hit,
            raycastDistance,
            targetLayerMask,
            QueryTriggerInteraction.Collide
        );

        if (!hitSomething)
        {
            return;
        }

        ActiveBall selectedBall =
            FindBallFromHitTransform(hit.transform);

        if (selectedBall != null)
        {
            PopBall(selectedBall);
        }
    }

    private ActiveBall FindBallFromHitTransform(
        Transform hitTransform)
    {
        for (int i = 0; i < activeBalls.Count; i++)
        {
            ActiveBall ball = activeBalls[i];

            if (ball == null ||
                ball.resolved ||
                ball.transform == null)
            {
                continue;
            }

            bool hitBallRoot =
                hitTransform == ball.transform;

            bool hitBallChild =
                hitTransform.IsChildOf(ball.transform);

            if (hitBallRoot || hitBallChild)
            {
                return ball;
            }
        }

        return null;
    }

    private void PopBall(ActiveBall ball)
    {
        if (ball == null ||
            ball.resolved ||
            ball.gameObject == null)
        {
            return;
        }

        ball.resolved = true;

        ReleaseSpawnPoint(ball.spawnIndex);
        activeBalls.Remove(ball);

        currentScore += scorePerBall;

        if (currentScore > highScore)
        {
            highScore = currentScore;

            PlayerPrefs.SetInt(
                HighScoreKey,
                highScore
            );

            PlayerPrefs.Save();
        }

        UpdateUI();

        Collider[] colliders =
            ball.gameObject.GetComponentsInChildren<Collider>(
                true
            );

        foreach (Collider ballCollider in colliders)
        {
            ballCollider.enabled = false;
        }

        if (ball.rigidbody != null)
        {
            ball.rigidbody.linearVelocity = Vector3.zero;
            ball.rigidbody.angularVelocity = Vector3.zero;
            ball.rigidbody.isKinematic = true;
            ball.rigidbody.useGravity = false;
        }

        ball.transform.DOKill();

        Vector3 growScale =
            ball.originalScale * popScaleMultiplier;

        Sequence popSequence = DOTween.Sequence();

        popSequence.Append(
            ball.transform
                .DOScale(
                    growScale,
                    popGrowDuration
                )
                .SetEase(Ease.OutQuad)
        );

        popSequence.Join(
            ball.transform
                .DORotate(
                    ball.transform.eulerAngles +
                    new Vector3(0f, 180f, 0f),
                    popGrowDuration,
                    RotateMode.FastBeyond360
                )
                .SetEase(Ease.OutQuad)
        );

        popSequence.Append(
            ball.transform
                .DOScale(
                    Vector3.zero,
                    popShrinkDuration
                )
                .SetEase(Ease.InBack)
        );

        popSequence.SetLink(
            ball.gameObject,
            LinkBehaviour.KillOnDestroy
        );

        popSequence.OnComplete(() =>
        {
            if (ball.gameObject != null)
            {
                Destroy(ball.gameObject);
            }
        });
    }

    private void CheckForMissedBalls()
    {
        for (int i = activeBalls.Count - 1; i >= 0; i--)
        {
            ActiveBall ball = activeBalls[i];

            if (ball == null)
            {
                activeBalls.RemoveAt(i);
                continue;
            }

            if (ball.gameObject == null)
            {
                ReleaseSpawnPoint(ball.spawnIndex);
                activeBalls.RemoveAt(i);
                continue;
            }

            if (ball.resolved)
            {
                continue;
            }

            bool fellBelowSpawn =
                ball.transform.position.y <
                ball.spawnHeight - missDistanceBelowSpawn;

            bool exceededLifetime =
                Time.time - ball.spawnTime >
                maximumBallLifetime;

            if (fellBelowSpawn || exceededLifetime)
            {
                RegisterMiss(ball, i);
            }
        }
    }

    private void RegisterMiss(
        ActiveBall missedBall,
        int listIndex)
    {
        if (missedBall == null ||
            missedBall.resolved)
        {
            return;
        }

        missedBall.resolved = true;

        ReleaseSpawnPoint(missedBall.spawnIndex);

        if (missedBall.transform != null)
        {
            missedBall.transform.DOKill();
        }

        if (missedBall.gameObject != null)
        {
            Destroy(missedBall.gameObject);
        }

        if (listIndex >= 0 &&
            listIndex < activeBalls.Count)
        {
            activeBalls.RemoveAt(listIndex);
        }

        currentMisses++;

        if (currentMisses >= missesPerLife)
        {
            currentMisses = 0;
            currentLives--;

            if (currentLives <= 0)
            {
                currentLives = 0;
                EndGame();
            }
        }

        UpdateUI();
    }

    private void ReleaseSpawnPoint(int index)
    {
        if (occupiedSpawnPoints == null)
        {
            return;
        }

        if (index >= 0 &&
            index < occupiedSpawnPoints.Length)
        {
            occupiedSpawnPoints[index] = false;
        }
    }

    private int GetDifficultyLevel()
    {
        if (!gameStarted ||
            difficultyIncreaseEvery <= 0f)
        {
            return 0;
        }

        float elapsedTime =
            Time.time - gameStartTime;

        return Mathf.FloorToInt(
            elapsedTime / difficultyIncreaseEvery
        );
    }

    private float GetCurrentSpawnInterval()
    {
        int difficultyLevel =
            GetDifficultyLevel();

        return Mathf.Max(
            minimumSpawnInterval,
            startingSpawnInterval -
            difficultyLevel * spawnIntervalDecrease
        );
    }

    private float GetCurrentUpwardImpulse()
    {
        int difficultyLevel =
            GetDifficultyLevel();

        return startingUpwardImpulse +
               difficultyLevel * upwardImpulseIncrease;
    }

    private float GetCurrentSideImpulse()
    {
        int difficultyLevel =
            GetDifficultyLevel();

        return Mathf.Min(
            maximumSideImpulse,
            startingSideImpulse +
            difficultyLevel * sideImpulseIncrease
        );
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

        if (livesText != null)
        {
            livesText.text =
                $"Lives: {currentLives}";
        }
    }

    private void EndGame()
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;
        gameStarted = false;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        for (int i = activeBalls.Count - 1; i >= 0; i--)
        {
            ActiveBall ball = activeBalls[i];

            if (ball == null)
            {
                continue;
            }

            if (ball.transform != null)
            {
                ball.transform.DOKill();
            }

            if (ball.gameObject != null)
            {
                Destroy(ball.gameObject);
            }
        }

        activeBalls.Clear();

        if (occupiedSpawnPoints != null)
        {
            for (int i = 0;
                 i < occupiedSpawnPoints.Length;
                 i++)
            {
                occupiedSpawnPoints[i] = false;
            }
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

    public void ResetSavedHighScore()
    {
        PlayerPrefs.DeleteKey(HighScoreKey);
        PlayerPrefs.Save();

        highScore = 0;

        UpdateUI();
    }

    private void OnDrawGizmosSelected()
    {
        Camera cameraToUse =
            playerCamera != null
                ? playerCamera
                : Camera.main;

        if (cameraToUse == null)
        {
            return;
        }

        Gizmos.DrawRay(
            cameraToUse.transform.position,
            cameraToUse.transform.forward *
            raycastDistance
        );
    }
}
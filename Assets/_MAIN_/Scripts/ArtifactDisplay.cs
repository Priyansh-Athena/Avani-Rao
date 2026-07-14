using DG.Tweening;
using TMPro;
using UnityEngine;

public class ArtifactDisplay : MonoBehaviour
{
    private enum ArtifactDisplayState
    {
        Locked,
        CanUnlock,
        Unlocked
    }

    [Header("Artifact Data")]
    [SerializeField] private ArtifactData artifactData;

    [Header("Artifact Scene Object")]
    [Tooltip(
        "Assign the artifact model already placed on the table."
    )]
    [SerializeField] private GameObject artifactGameObject;

    [Header("Unlocked HUD")]
    [SerializeField] private GameObject unlockedHUD;
    [SerializeField] private TMP_Text unlockedNameText;
    [SerializeField] private TMP_Text unlockedDescriptionText;

    [Header("Can Unlock HUD")]
    [SerializeField] private GameObject canUnlockHUD;
    [SerializeField] private TMP_Text canUnlockNameText;

    [Header("Locked HUD")]
    [SerializeField] private GameObject lockedHUD;
    [SerializeField] private TMP_Text lockedNameText;
    [SerializeField] private TMP_Text lockedMessageText;

    [Header("HUD Animation")]
    [SerializeField] private float hudFadeDuration = 0.3f;
    [SerializeField] private float hudScaleDuration = 0.3f;
    [SerializeField] private float hudStartingScale = 0.9f;
    [SerializeField] private Ease hudAppearEase = Ease.OutBack;

    [Header("Artifact Appearance")]
    [SerializeField] private float artifactAppearDuration = 0.5f;
    [SerializeField] private Ease artifactAppearEase = Ease.OutBack;

    [Header("Artifact Idle Animation")]
    [Tooltip(
        "Disable this to stop both artifact rotation and hovering."
    )]
    [SerializeField] private bool animateArtifact = true;

    [SerializeField] private float rotationDuration = 10f;
    [SerializeField] private float hoverHeight = 0.08f;
    [SerializeField] private float hoverDuration = 1.5f;

    private const string CoinHighScoreKey =
        "InfiniteCarDodgeCoinHighScore";

    private const string WeaponHighScoreKey =
        "TargetPopHighScore";

    private const string FigurineHighScoreKey =
        "TowerStackHighScore";

    private int currentGameHighScore;

    private Vector3 artifactOriginalLocalPosition;
    private Quaternion artifactOriginalLocalRotation;
    private Vector3 artifactOriginalLocalScale;

    private Vector3 unlockedHUDOriginalScale;
    private Vector3 canUnlockHUDOriginalScale;
    private Vector3 lockedHUDOriginalScale;

    private CanvasGroup unlockedHUDCanvasGroup;
    private CanvasGroup canUnlockHUDCanvasGroup;
    private CanvasGroup lockedHUDCanvasGroup;

    private bool initialized;

    private void Awake()
    {
        PrepareArtifact();
        PrepareHUDs();
    }

    private void Start()
    {
        InitializeArtifact();
    }

    private void PrepareArtifact()
    {
        if (artifactGameObject == null)
        {
            return;
        }

        Transform artifactTransform =
            artifactGameObject.transform;

        artifactOriginalLocalPosition =
            artifactTransform.localPosition;

        artifactOriginalLocalRotation =
            artifactTransform.localRotation;

        artifactOriginalLocalScale =
            artifactTransform.localScale;

        artifactGameObject.SetActive(false);
    }

    private void PrepareHUDs()
    {
        unlockedHUDCanvasGroup = PrepareHUD(
            unlockedHUD,
            out unlockedHUDOriginalScale
        );

        canUnlockHUDCanvasGroup = PrepareHUD(
            canUnlockHUD,
            out canUnlockHUDOriginalScale
        );

        lockedHUDCanvasGroup = PrepareHUD(
            lockedHUD,
            out lockedHUDOriginalScale
        );

        SetHUDImmediately(unlockedHUD, false);
        SetHUDImmediately(canUnlockHUD, false);
        SetHUDImmediately(lockedHUD, false);
    }

    private CanvasGroup PrepareHUD(
        GameObject hud,
        out Vector3 originalScale)
    {
        originalScale = Vector3.one;

        if (hud == null)
        {
            return null;
        }

        originalScale = hud.transform.localScale;

        CanvasGroup canvasGroup =
            hud.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                hud.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        return canvasGroup;
    }

    public void InitializeArtifact()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        currentGameHighScore =
            GetCategoryHighScore(
                artifactData.category
            );

        /*
         * PlayerPrefs is the saved source of truth.
         */
        artifactData.isUnlocked =
            PlayerPrefs.GetInt(
                GetArtifactUnlockKey(),
                0
            ) == 1;

        artifactData.canUnlock =
            artifactData.isUnlocked ||
            currentGameHighScore >=
            artifactData.scoreNeededToUnlock;

        UpdateAllText();

        ArtifactDisplayState state =
            GetCurrentDisplayState();

        ApplyState(state, false);

        initialized = true;
    }

    /// <summary>
    /// Call this after returning from a minigame if the
    /// museum scene remains loaded.
    /// </summary>
    public void RefreshArtifact()
    {
        currentGameHighScore =
            GetCategoryHighScore(
                artifactData.category
            );

        artifactData.isUnlocked =
            PlayerPrefs.GetInt(
                GetArtifactUnlockKey(),
                0
            ) == 1;

        artifactData.canUnlock =
            artifactData.isUnlocked ||
            currentGameHighScore >=
            artifactData.scoreNeededToUnlock;

        UpdateAllText();

        ApplyState(
            GetCurrentDisplayState(),
            true
        );
    }

    private ArtifactDisplayState GetCurrentDisplayState()
    {
        if (artifactData.isUnlocked)
        {
            return ArtifactDisplayState.Unlocked;
        }

        if (artifactData.canUnlock)
        {
            return ArtifactDisplayState.CanUnlock;
        }

        return ArtifactDisplayState.Locked;
    }

    private void ApplyState(
        ArtifactDisplayState state,
        bool animate)
    {
        KillAllAnimations();

        switch (state)
        {
            case ArtifactDisplayState.Unlocked:
                ShowUnlockedState(animate);
                break;

            case ArtifactDisplayState.CanUnlock:
                ShowCanUnlockState(animate);
                break;

            case ArtifactDisplayState.Locked:
                ShowLockedState(animate);
                break;
        }
    }

    private void ShowUnlockedState(bool animate)
    {
        HideHUDImmediately(canUnlockHUD);
        HideHUDImmediately(lockedHUD);

        ShowHUD(
            unlockedHUD,
            unlockedHUDCanvasGroup,
            unlockedHUDOriginalScale,
            animate
        );

        ShowArtifact(animate);
    }

    private void ShowCanUnlockState(bool animate)
    {
        HideHUDImmediately(unlockedHUD);
        HideHUDImmediately(lockedHUD);

        HideArtifact();

        ShowHUD(
            canUnlockHUD,
            canUnlockHUDCanvasGroup,
            canUnlockHUDOriginalScale,
            animate
        );
    }

    private void ShowLockedState(bool animate)
    {
        HideHUDImmediately(unlockedHUD);
        HideHUDImmediately(canUnlockHUD);

        HideArtifact();

        ShowHUD(
            lockedHUD,
            lockedHUDCanvasGroup,
            lockedHUDOriginalScale,
            animate
        );
    }

    /// <summary>
    /// Attach this function to the Unlock button.
    /// </summary>
    public void UnlockArtifact()
    {
        if (artifactData == null ||
            artifactData.isUnlocked)
        {
            return;
        }

        currentGameHighScore =
            GetCategoryHighScore(
                artifactData.category
            );

        bool requirementReached =
            currentGameHighScore >=
            artifactData.scoreNeededToUnlock;

        if (!requirementReached)
        {
            artifactData.canUnlock = false;

            UpdateAllText();

            ApplyState(
                ArtifactDisplayState.Locked,
                true
            );

            return;
        }

        artifactData.canUnlock = true;
        artifactData.isUnlocked = true;

        PlayerPrefs.SetInt(
            GetArtifactUnlockKey(),
            1
        );

        PlayerPrefs.Save();

        UpdateAllText();

        ApplyState(
            ArtifactDisplayState.Unlocked,
            true
        );
    }

    private void UpdateAllText()
    {
        string artifactName =
            string.IsNullOrWhiteSpace(
                artifactData.artifactName
            )
                ? artifactData.name
                : artifactData.artifactName;

        if (unlockedNameText != null)
        {
            unlockedNameText.text =
                artifactName;
        }

        if (unlockedDescriptionText != null)
        {
            unlockedDescriptionText.text =
                artifactData.description;
        }

        if (canUnlockNameText != null)
        {
            canUnlockNameText.text =
                artifactName;
        }

        if (lockedNameText != null)
        {
            lockedNameText.text =
                artifactName;
        }

        if (lockedMessageText != null)
        {
            lockedMessageText.text =
                $"ARTIFACT IS LOCKED\n" +
                $"You need " +
                $"{artifactData.scoreNeededToUnlock} points " +
                $"to unlock this artifact.\n" +
                $"Current Score: {currentGameHighScore}";
        }
    }

    private void ShowHUD(
        GameObject hud,
        CanvasGroup canvasGroup,
        Vector3 originalScale,
        bool animate)
    {
        if (hud == null)
        {
            return;
        }

        hud.SetActive(true);

        hud.transform.DOKill();

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (!animate)
        {
            hud.transform.localScale =
                originalScale;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            return;
        }

        hud.transform.localScale =
            originalScale * hudStartingScale;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;

            canvasGroup
                .DOFade(
                    1f,
                    hudFadeDuration
                )
                .SetUpdate(true);
        }

        hud.transform
            .DOScale(
                originalScale,
                hudScaleDuration
            )
            .SetEase(hudAppearEase)
            .SetUpdate(true);
    }

    private void HideHUDImmediately(GameObject hud)
    {
        SetHUDImmediately(hud, false);
    }

    private void SetHUDImmediately(
        GameObject hud,
        bool visible)
    {
        if (hud == null)
        {
            return;
        }

        hud.transform.DOKill();

        CanvasGroup canvasGroup =
            hud.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        hud.SetActive(visible);
    }

    private void ShowArtifact(bool animate)
    {
        if (artifactGameObject == null)
        {
            return;
        }

        Transform artifactTransform =
            artifactGameObject.transform;

        artifactTransform.DOKill();

        artifactGameObject.SetActive(true);

        artifactTransform.localPosition =
            artifactOriginalLocalPosition;

        artifactTransform.localRotation =
            artifactOriginalLocalRotation;

        if (!animate)
        {
            artifactTransform.localScale =
                artifactOriginalLocalScale;

            StartArtifactIdleAnimation();
            return;
        }

        artifactTransform.localScale =
            Vector3.zero;

        artifactTransform
            .DOScale(
                artifactOriginalLocalScale,
                artifactAppearDuration
            )
            .SetEase(artifactAppearEase)
            .SetUpdate(true)
            .OnComplete(
                StartArtifactIdleAnimation
            );
    }

    private void HideArtifact()
    {
        if (artifactGameObject == null)
        {
            return;
        }

        Transform artifactTransform =
            artifactGameObject.transform;

        artifactTransform.DOKill();

        artifactTransform.localPosition =
            artifactOriginalLocalPosition;

        artifactTransform.localRotation =
            artifactOriginalLocalRotation;

        artifactTransform.localScale =
            artifactOriginalLocalScale;

        artifactGameObject.SetActive(false);
    }

    private void StartArtifactIdleAnimation()
    {
        if (!animateArtifact ||
            artifactGameObject == null ||
            !artifactGameObject.activeInHierarchy)
        {
            return;
        }

        Transform artifactTransform =
            artifactGameObject.transform;

        artifactTransform.DOKill();

        artifactTransform.localPosition =
            artifactOriginalLocalPosition;

        artifactTransform.localRotation =
            artifactOriginalLocalRotation;

        artifactTransform
            .DOLocalRotate(
                new Vector3(0f, 360f, 0f),
                rotationDuration,
                RotateMode.FastBeyond360
            )
            .SetRelative()
            .SetEase(Ease.Linear)
            .SetLoops(-1)
            .SetUpdate(true);

        artifactTransform
            .DOLocalMoveY(
                artifactOriginalLocalPosition.y +
                hoverHeight,
                hoverDuration
            )
            .SetEase(Ease.InOutSine)
            .SetLoops(
                -1,
                LoopType.Yoyo
            )
            .SetUpdate(true);
    }

    /// <summary>
    /// Can be called from another UnityEvent or script.
    /// </summary>
    public void SetArtifactAnimation(bool shouldAnimate)
    {
        animateArtifact = shouldAnimate;

        if (artifactGameObject == null)
        {
            return;
        }

        Transform artifactTransform =
            artifactGameObject.transform;

        artifactTransform.DOKill();

        artifactTransform.localPosition =
            artifactOriginalLocalPosition;

        artifactTransform.localRotation =
            artifactOriginalLocalRotation;

        if (animateArtifact &&
            artifactData != null &&
            artifactData.isUnlocked)
        {
            StartArtifactIdleAnimation();
        }
    }

    private int GetCategoryHighScore(
        ArtifactCategory category)
    {
        switch (category)
        {
            case ArtifactCategory.Coin:
                return PlayerPrefs.GetInt(
                    CoinHighScoreKey,
                    0
                );

            case ArtifactCategory.Weapons:
                return PlayerPrefs.GetInt(
                    WeaponHighScoreKey,
                    0
                );

            case ArtifactCategory.Figurines:
                return PlayerPrefs.GetInt(
                    FigurineHighScoreKey,
                    0
                );

            default:
                return 0;
        }
    }

    private string GetArtifactUnlockKey()
    {
        return
            $"ArtifactUnlocked_" +
            $"{artifactData.category}_" +
            $"{artifactData.name}";
    }

    private bool ValidateReferences()
    {
        if (artifactData == null)
        {
            Debug.LogError(
                $"{name}: Artifact Data is not assigned."
            );

            return false;
        }

        if (artifactGameObject == null)
        {
            Debug.LogError(
                $"{name}: Artifact GameObject is not assigned."
            );

            return false;
        }

        if (unlockedHUD == null ||
            canUnlockHUD == null ||
            lockedHUD == null)
        {
            Debug.LogError(
                $"{name}: Assign all three HUD GameObjects."
            );

            return false;
        }

        return true;
    }

    private void KillAllAnimations()
    {
        if (artifactGameObject != null)
        {
            artifactGameObject.transform.DOKill();
        }

        KillHUDAnimation(
            unlockedHUD,
            unlockedHUDCanvasGroup
        );

        KillHUDAnimation(
            canUnlockHUD,
            canUnlockHUDCanvasGroup
        );

        KillHUDAnimation(
            lockedHUD,
            lockedHUDCanvasGroup
        );
    }

    private void KillHUDAnimation(
        GameObject hud,
        CanvasGroup canvasGroup)
    {
        if (hud != null)
        {
            hud.transform.DOKill();
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }
    }

    private void OnDisable()
    {
        KillAllAnimations();
    }

    public void ResetArtifactUnlock()
    {
        if (artifactData == null)
        {
            return;
        }

        PlayerPrefs.DeleteKey(
            GetArtifactUnlockKey()
        );

        PlayerPrefs.Save();

        artifactData.isUnlocked = false;

        currentGameHighScore =
            GetCategoryHighScore(
                artifactData.category
            );

        artifactData.canUnlock =
            currentGameHighScore >=
            artifactData.scoreNeededToUnlock;

        UpdateAllText();

        ApplyState(
            GetCurrentDisplayState(),
            true
        );
    }
}
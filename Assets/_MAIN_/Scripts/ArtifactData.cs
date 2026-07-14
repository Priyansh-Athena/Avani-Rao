using UnityEngine;

public enum ArtifactCategory
{
    Coin,
    Figurines,
    Weapons
}

[CreateAssetMenu(
    fileName = "New Artifact",
    menuName = "VR Museum/Artifact Data"
)]
public class ArtifactData : ScriptableObject
{
    [Header("Artifact Information")]
    public string artifactName;

    [TextArea(3, 8)]
    public string description;

    public ArtifactCategory category;

    [Header("Unlock Requirement")]
    [Min(0)]
    public int scoreNeededToUnlock = 100;

    [Header("Runtime Status")]
    public bool canUnlock = false;
    public bool isUnlocked = false;
}
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class SpriteTextureOnCube : MonoBehaviour
{
    [Header("Texture")]
    [Tooltip("Assign one of the sliced sprites from the texture sheet.")]
    [SerializeField] private Sprite blockSprite;

    private MeshRenderer meshRenderer;
    private Material materialInstance;

    private void Awake()
    {
        ApplySprite();
    }

    public void ApplySprite()
    {
        if (blockSprite == null)
        {
            Debug.LogWarning(
                $"{name}: No block sprite has been assigned."
            );

            return;
        }

        meshRenderer = GetComponent<MeshRenderer>();

        /*
         * renderer.material creates a separate material instance.
         * This allows different cubes to use different sliced sprites.
         */
        materialInstance = meshRenderer.material;

        Texture2D textureSheet = blockSprite.texture;
        Rect spriteRect = blockSprite.rect;

        Vector2 textureScale = new Vector2(
            spriteRect.width / textureSheet.width,
            spriteRect.height / textureSheet.height
        );

        Vector2 textureOffset = new Vector2(
            spriteRect.x / textureSheet.width,
            spriteRect.y / textureSheet.height
        );

        // Works with materials that define a Main Texture.
        materialInstance.mainTexture = textureSheet;
        materialInstance.mainTextureScale = textureScale;
        materialInstance.mainTextureOffset = textureOffset;

        // Explicit support for URP shaders.
        if (materialInstance.HasProperty("_BaseMap"))
        {
            materialInstance.SetTexture(
                "_BaseMap",
                textureSheet
            );

            materialInstance.SetTextureScale(
                "_BaseMap",
                textureScale
            );

            materialInstance.SetTextureOffset(
                "_BaseMap",
                textureOffset
            );
        }

        // Support for Built-in Standard and some custom shaders.
        if (materialInstance.HasProperty("_MainTex"))
        {
            materialInstance.SetTexture(
                "_MainTex",
                textureSheet
            );

            materialInstance.SetTextureScale(
                "_MainTex",
                textureScale
            );

            materialInstance.SetTextureOffset(
                "_MainTex",
                textureOffset
            );
        }
    }

    public void SetSprite(Sprite newSprite)
    {
        blockSprite = newSprite;
        ApplySprite();
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the creation and positioning of icons on a turn-based progress bar.
/// Assumes a horizontal bar where icons move from left (0.0) to right (1.0).
/// </summary>
public class TurnProgressHandler : MonoBehaviour
{
    [Tooltip("The RectTransform of the progress bar UI element.")]
    public RectTransform _barTransform;

    [Tooltip("The prefab for the entity icon to be displayed on the bar. Must have a RectTransform component.")]
    public GameObject entityIconPrefab;

    private List<Entity> _entities = new List<Entity>();
#region  TestCode
    // [Header("Testing")]
    // [Tooltip("Test sprites to populate the bar on start. Assign at least 2.")]
    // public Sprite[] testSprites;
    // [Tooltip("The speed at which the test icons move across the bar (units per second).")]
    // public float testMoveSpeed = 0.1f;

    // // Dictionary to track the normalized (0-1) position of each entity for animation.
    // private Dictionary<Entity, float> _entityPositions = new Dictionary<Entity, float>();

    // void Start()
    // {
    //     // 바 양쪽에 프리팹을 생성하고 좌에서 우로 움직여라
    //     if (testSprites == null || testSprites.Length < 2)
    //     {
    //         Debug.LogWarning("Testing setup skipped: Please assign at least 2 test sprites in the inspector.", this);
    //         return;
    //     }

    //     ClearAllEntities();
    //     _entityPositions.Clear();

    //     // Create two entities for testing, one at the start and one in the middle.
    //     Entity entity1 = CreatePosition(0f, testSprites[0]);
    //     _entityPositions.Add(entity1, 0f);

    //     Entity entity2 = CreatePosition(0.5f, testSprites[1]);
    //     _entityPositions.Add(entity2, 0.5f);
    // }

    // void Update()
    // {
    //     if (_entityPositions.Count == 0) return;

    //     // Create a temporary list of keys to avoid modifying the dictionary while iterating.
    //     var entitiesToUpdate = new List<Entity>(_entityPositions.Keys);

    //     foreach (var entity in entitiesToUpdate)
    //     {
    //         // Move entity from left to right
    //         float currentPos = _entityPositions[entity];
    //         currentPos += testMoveSpeed * Time.deltaTime;

    //         // When it reaches the end, loop back to the beginning.
    //         if (currentPos > 1f)
    //         {
    //             currentPos %= 1f;
    //         }

    //         _entityPositions[entity] = currentPos;
    //         SetTranspos(entity, currentPos);
    //     }
    // }
#endregion
    /// <summary>
    /// Represents an entity (icon) on the progress bar.
    /// </summary>
    public struct Entity
    {
        public RectTransform Transform;
        public Image Image;
        // Consider adding a reference to your character/combatant data here
        // for easier identification, e.g., public ICombatant combatant;
    }

    /// <summary>
    /// Creates a new entity icon on the progress bar at a specified normalized position.
    /// </summary>
    /// <param name="v">The normalized position (0.0 to 1.0) along the bar.</param>
    /// <returns>The newly created Entity.</returns>
    public Entity CreatePosition(float v,Sprite sprite)
    {
        if (entityIconPrefab == null)
        {
            Debug.LogError("Entity Icon Prefab is not set in the TurnProgressHandler.", this);
            return default; // Return an empty Entity
        }

        // Instantiate the icon prefab and parent it to the bar transform.
        GameObject iconInstance = Instantiate(entityIconPrefab, _barTransform);
        RectTransform iconTransform = iconInstance.GetComponentInChildren<RectTransform>();
        Image image = iconInstance.GetComponentInChildren<Image>();
        image.sprite = sprite;

        if (iconTransform == null)
        {
            Debug.LogError("The entityIconPrefab does not have a RectTransform component.", this);
            Destroy(iconInstance);
            return default;
        }
        
        // Set anchor and pivot for consistent positioning.
        // Anchor to the middle-left of the bar.
        iconTransform.anchorMin = new Vector2(0, 0.5f);
        iconTransform.anchorMax = new Vector2(0, 0.5f);
        // Set the icon's own pivot to its center.
        iconTransform.pivot = new Vector2(0.5f, 0.5f);
        // Reset scale in case prefab has weird scaling from parent.
        iconTransform.localScale = Vector3.one;


        Entity newEntity = new Entity { Transform = iconTransform ,Image=image};
        _entities.Add(newEntity);

        // Set its initial position along the bar.
        SetTranspos(newEntity, v);

        return newEntity;
    }

    /// <summary>
    /// Sets the position of an entity's icon on the progress bar.
    /// </summary>
    /// <param name="entity">The entity to position.</param>
    /// <param name="v">The normalized position (0.0 to 1.0) along the bar.</param>
    public void SetTranspos(Entity entity, float v)
    {
        if (entity.Transform == null || _barTransform == null)
        {
            Debug.LogWarning("Cannot set position. Entity transform or bar transform is null.");
            return;
        }

        // Clamp the value to ensure it's within the 0-1 range.
        v = Mathf.Clamp01(v);

        // Since the anchor is at the left edge (x=0), the anchored position's x
        // is simply the normalized value multiplied by the bar's width.
        float barWidth = _barTransform.rect.width;
        float newX = v * barWidth;

        // The icon is vertically centered relative to the anchor (which is at y=0.5).
        entity.Transform.anchoredPosition = new Vector2(newX, 0);
    }

    /// <summary>
    /// Removes an entity's icon from the progress bar and destroys it.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public void RemoveEntity(Entity entity)
    {
        if (entity.Transform != null)
        {
            // It's good practice to check if the entity is actually in the list before removing.
            if (_entities.Remove(entity))
            {
                Destroy(entity.Transform.gameObject);
            }
        }
    }
    
    /// <summary>
    /// Removes all entity icons from the bar.
    /// </summary>
    public void ClearAllEntities()
    {
        // Iterate backwards when removing from a list you are iterating over.
        for (int i = _entities.Count - 1; i >= 0; i--)
        {
            RemoveEntity(_entities[i]);
        }
        _entities.Clear();
    }
}
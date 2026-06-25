using UnityEngine;

/// <summary>
/// Auto-attached by ItemDistanceTracker at runtime — do NOT add this manually.
/// Polls distance to the player each frame and calls MarkFound when close enough.
/// Works with CharacterController; no Rigidbody or trigger needed.
/// </summary>
public class ProximityCollector : MonoBehaviour
{
    private ItemDistanceTracker _tracker;
    private Transform           _player;
    private string              _countryName;
    private string              _label;
    private float               _radius;
    private bool                _collected = false;

    /// <summary>Called by ItemDistanceTracker immediately after AddComponent.</summary>
    public void Setup(ItemDistanceTracker tracker, string countryName, string label,
                      Transform player, float radius)
    {
        _tracker     = tracker;
        _countryName = countryName;
        _label       = label;
        _player      = player;
        _radius      = radius;
    }

    private void Update()
    {
        if (_collected || _player == null || _tracker == null) return;

        if (Vector3.Distance(transform.position, _player.position) <= _radius)
        {
            _collected = true;                          // guard against double-fire
            _tracker.MarkFound(_countryName, _label);  // tracker destroys this GameObject
        }
    }

    // Cyan wire sphere visible in Scene view so you can see the collect radius
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}

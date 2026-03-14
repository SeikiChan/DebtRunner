using UnityEngine;

public class XPPickup : MonoBehaviour
{
    [SerializeField] private int amount = 1;
    [Header("SFX / 音效")]
    [SerializeField] private AudioClip sfxCollect;
    [Header("Magnet")]
    [SerializeField, Min(0f)] private float magnetRadius = 2.6f;
    [SerializeField, Min(0f)] private float magnetSpeed = 8f;
    [SerializeField, Min(0f)] private float autoCollectDistance = 0.18f;

    private static Transform cachedPlayer;
    private bool collected;
    private float autoCollectDistanceSqr;
    private float funnelEndTime = -1f;
    private bool denseFunnelCompleted;

    public int Amount => Mathf.Max(1, amount);

    public void SetAmount(int value) => amount = Mathf.Max(1, value);

    private void Awake()
    {
        RebuildCachedValues();
    }

    private void OnValidate()
    {
        RebuildCachedValues();
    }

    private void Update()
    {
        if (collected)
            return;

        Transform player = ResolvePlayer();
        if (player == null)
            return;

        float effectiveRadius = magnetRadius;
        if (GameFlowController.Instance != null)
            effectiveRadius += GameFlowController.Instance.BonusXPMagnetRadius;
        if (PickupMagnetUtility.UpdateMagnetMotion(
            transform,
            player,
            effectiveRadius,
            magnetSpeed,
            autoCollectDistanceSqr,
            ref funnelEndTime,
            ref denseFunnelCompleted))
        {
            Collect();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Collect();
    }

    public bool ForceCollect()
    {
        if (collected)
            return false;

        Collect();
        return collected;
    }

    private void Collect()
    {
        if (collected)
            return;

        collected = true;
        RunLogger.Event($"XP pickup collected: +{amount}");
        if (GameFlowController.Instance != null)
        {
            GameFlowController.Instance.AddXP(amount);
            GameFlowController.Instance.NotifyGameplayTutorialPickupCollected(transform.position);
        }
        if (sfxCollect != null && SFXManager.Instance != null)
            SFXManager.Instance.PlayPickupCollect(sfxCollect, 0.35f);
        Destroy(gameObject);
    }

    private static Transform ResolvePlayer()
    {
        if (cachedPlayer != null)
            return cachedPlayer;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            cachedPlayer = playerObject.transform;

        return cachedPlayer;
    }

    private void RebuildCachedValues()
    {
        amount = Mathf.Max(1, amount);
        magnetRadius = Mathf.Max(0f, magnetRadius);
        magnetSpeed = Mathf.Max(0f, magnetSpeed);
        autoCollectDistance = Mathf.Max(0f, autoCollectDistance);

        autoCollectDistanceSqr = autoCollectDistance * autoCollectDistance;
    }
}

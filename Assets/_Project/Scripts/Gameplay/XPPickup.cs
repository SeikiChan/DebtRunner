using UnityEngine;

public class XPPickup : MonoBehaviour
{
    [SerializeField] private int amount = 1;

    public void SetAmount(int value) => amount = Mathf.Max(1, value);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        GameFlowController.Instance.AddXP(amount);
        Destroy(gameObject);
    }
}

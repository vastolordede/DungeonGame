using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float regenRate = 20f;

    [Header("Costs")]
    public float dashCost = 25f;
    public float attackCost = 15f;

    [Header("Debug")]
    public bool enableRegenLog = true;
    public float regenLogInterval = 5f;

    private float regenLogTimer = 0f;

    private void Start()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        Regenerate();
    }

    private void Regenerate()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina += regenRate * Time.deltaTime;

            if (currentStamina > maxStamina)
                currentStamina = maxStamina;

            if (enableRegenLog)
            {
                regenLogTimer += Time.deltaTime;

                if (regenLogTimer >= regenLogInterval)
                {
                    Debug.Log("🔋 Regen stamina: " + currentStamina);
                    regenLogTimer = 0f;
                }
            }
        }
        else
        {
            regenLogTimer = 0f;
        }
    }

    public bool HasEnoughStamina(float cost)
    {
        return currentStamina >= cost;
    }

    public bool TryUseStamina(float cost)
    {
        if (currentStamina < cost)
        {
            Debug.Log("❌ Not enough stamina");
            return false;
        }

        currentStamina -= cost;

        Debug.Log("⚡ Stamina used: -" + cost +
                  " | Current: " + currentStamina);

        return true;
    }
}
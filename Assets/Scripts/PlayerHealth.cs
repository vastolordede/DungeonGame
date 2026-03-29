using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 100;
    public int currentHP = 100;

    private int lastHP;

    private void Start()
    {
        currentHP = maxHP;
        lastHP = currentHP;
    }

    public void TakeDamage(int damage)
    {
        if (currentHP <= 0)
            return;

        currentHP -= damage;

        if (currentHP < 0)
            currentHP = 0;

        LogIfChanged();

        if (currentHP <= 0)
        {
            Debug.Log("☠️ Player chết");
        }
    }

    private void LogIfChanged()
    {
        if (currentHP != lastHP)
        {
            Debug.Log("🩸 Player HP: " + currentHP + "/" + maxHP);
            lastHP = currentHP;
        }
    }
}
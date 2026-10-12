using System;
using System.Collections;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField, Min(0.1f)] private float invulnerabilityDuration = 0.9f;
    [SerializeField, Min(0f)] private float knockbackDistance = 0.45f;
    [SerializeField, Min(0.01f)] private float knockbackDuration = 0.15f;
    [SerializeField, Min(1)] private int blinkCount = 3;
    [SerializeField, Min(0.01f)] private float blinkHalfPeriod = 0.12f;

    private TopDownCharacterController movement;
    private PlayerBoxInteraction boxInteraction;
    private MagicSphereInteraction magicSphereInteraction;
    private SpriteRenderer[] spriteRenderers;
    private bool[] originalSpriteStates;
    private int currentHealth;
    private float invulnerableUntil;
    private bool isInvincible;
    private Coroutine blinkRoutine;

    public event Action<int, int> HealthChanged;

    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
        movement = GetComponent<TopDownCharacterController>();
        boxInteraction = GetComponent<PlayerBoxInteraction>();
        magicSphereInteraction = GetComponent<MagicSphereInteraction>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalSpriteStates = new bool[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalSpriteStates[i] = spriteRenderers[i].enabled;
        }
    }

    public bool TakeDamage()
    {
        if (currentHealth <= 0 || isInvincible || Time.time < invulnerableUntil)
        {
            return false;
        }

        currentHealth = Mathf.Max(0, currentHealth - 1);
        invulnerableUntil = Time.time + invulnerabilityDuration;

        if (boxInteraction != null)
        {
            boxInteraction.CancelForDamage();
        }

        if (magicSphereInteraction != null)
        {
            magicSphereInteraction.CancelForDamage();
        }

        if (movement != null)
        {
            movement.ApplyKnockback(-movement.FacingDirection, knockbackDistance, knockbackDuration);
        }

        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
        }
        blinkRoutine = StartCoroutine(BlinkAfterDamage());

        if (HealthChanged != null)
        {
            HealthChanged(currentHealth, maxHealth);
        }

        return true;
    }

    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
        if (invincible && blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
            RestoreOriginalSpriteStates();
        }
    }

    private IEnumerator BlinkAfterDamage()
    {
        for (int blink = 0; blink < blinkCount; blink++)
        {
            SetSpritesVisible(false);
            yield return new WaitForSeconds(blinkHalfPeriod);
            SetSpritesVisible(true);
            yield return new WaitForSeconds(blinkHalfPeriod);
        }

        RestoreOriginalSpriteStates();
        blinkRoutine = null;
    }

    private void OnDisable()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
        RestoreOriginalSpriteStates();
    }

    private void SetSpritesVisible(bool visible)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].enabled = visible && originalSpriteStates[i];
            }
        }
    }

    private void RestoreOriginalSpriteStates()
    {
        if (spriteRenderers == null || originalSpriteStates == null)
        {
            return;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].enabled = originalSpriteStates[i];
            }
        }
    }
}

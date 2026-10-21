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
    private Renderer[] playerRenderers;
    private bool[] originalRendererStates;
    private int currentHealth;
    private float invulnerableUntil;
    private bool isInvincible;
    private Coroutine blinkRoutine;

    public event Action<int, int> HealthChanged;
    public event Action DamageTaken;

    public int CurrentHealth { get { return currentHealth; } }
    public int MaxHealth { get { return maxHealth; } }

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
        movement = GetComponent<TopDownCharacterController>();
        boxInteraction = GetComponent<PlayerBoxInteraction>();
        magicSphereInteraction = GetComponent<MagicSphereInteraction>();
        playerRenderers = GetComponentsInChildren<Renderer>(true);
        originalRendererStates = new bool[playerRenderers.Length];
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            originalRendererStates[i] = playerRenderers[i].enabled;
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

        if (boxInteraction != null)
        {
            boxInteraction.DropHeldObjectForDamage();
        }

        if (magicSphereInteraction != null)
        {
            magicSphereInteraction.DropHeldObjectForDamage();
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
        if (DamageTaken != null)
        {
            DamageTaken();
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
            RestoreOriginalRendererStates();
        }
    }

    private IEnumerator BlinkAfterDamage()
    {
        for (int blink = 0; blink < blinkCount; blink++)
        {
            SetPlayerRenderersVisible(false);
            yield return new WaitForSeconds(blinkHalfPeriod);
            SetPlayerRenderersVisible(true);
            yield return new WaitForSeconds(blinkHalfPeriod);
        }

        RestoreOriginalRendererStates();
        blinkRoutine = null;
    }

    private void OnDisable()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
        RestoreOriginalRendererStates();
    }

    private void SetPlayerRenderersVisible(bool visible)
    {
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null)
            {
                playerRenderers[i].enabled = visible && originalRendererStates[i];
            }
        }
    }

    private void RestoreOriginalRendererStates()
    {
        if (playerRenderers == null || originalRendererStates == null)
        {
            return;
        }

        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null)
            {
                playerRenderers[i].enabled = originalRendererStates[i];
            }
        }
    }
}

using Spine.Unity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpinePlayerAnimation : MonoBehaviour
{
    [SerializeField] private SkeletonAnimation skeletonAnimation;
    [SerializeField] private TopDownCharacterController movement;
    [SerializeField] private PlayerBoxInteraction boxInteraction;
    [SerializeField] private MagicSphereInteraction sphereInteraction;
    [SerializeField] private string idleAnimation = "Idle";
    [SerializeField] private string walkingAnimation = "Walking";
    [SerializeField] private string holdingIdleAnimation = "Idle_B";
    [SerializeField] private string holdingWalkingAnimation = "Walking_B";
    [SerializeField] private string hurtAnimation = "Hurt";

    private bool hasAppliedAnimation;
    private bool isWalking;
    private bool isHoldingObject;
    private bool isHurtPlaying;
    private bool pendingHurt;
    private bool isFacingLeft;
    private PlayerHealth playerHealth;
    private Spine.TrackEntry hurtTrackEntry;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (boxInteraction == null)
        {
            boxInteraction = GetComponent<PlayerBoxInteraction>();
        }

        if (sphereInteraction == null)
        {
            sphereInteraction = GetComponent<MagicSphereInteraction>();
        }
    }

    private void OnEnable()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.DamageTaken += PlayHurtAnimation;
        }
    }

    private void Start()
    {
        if (movement == null)
        {
            movement = GetComponent<TopDownCharacterController>();
        }

        if (skeletonAnimation == null)
        {
            skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);
        }

        if (skeletonAnimation == null)
        {
            Debug.LogError("Spine Player Animation needs a SkeletonAnimation child.", this);
            enabled = false;
            return;
        }

        UpdateFacingDirection(true);
        UpdateAnimation(true);
        if (pendingHurt)
        {
            pendingHurt = false;
            PlayHurtAnimation();
        }
    }

    private void Update()
    {
        UpdateFacingDirection(false);

        if (isHurtPlaying)
        {
            return;
        }

        UpdateAnimation(false);
    }

    private void UpdateFacingDirection(bool force)
    {
        if (movement == null || skeletonAnimation == null)
        {
            return;
        }

        float horizontalDirection = movement.FacingDirection.x;
        if (Mathf.Abs(horizontalDirection) <= 0.01f)
        {
            return;
        }

        bool shouldFaceLeft = horizontalDirection < 0f;
        if (!force && shouldFaceLeft == isFacingLeft)
        {
            return;
        }

        isFacingLeft = shouldFaceLeft;
        float scaleX = Mathf.Abs(skeletonAnimation.Skeleton.ScaleX);
        skeletonAnimation.Skeleton.ScaleX = isFacingLeft ? -scaleX : scaleX;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.DamageTaken -= PlayHurtAnimation;
        }

        ClearHurtTrackEntry();
        isHurtPlaying = false;
        pendingHurt = false;
    }

    private void PlayHurtAnimation()
    {
        if (skeletonAnimation == null)
        {
            pendingHurt = true;
            return;
        }

        Spine.Animation hurt = skeletonAnimation.Skeleton.Data.FindAnimation(hurtAnimation);
        if (hurt == null)
        {
            Debug.LogWarning("Spine animation not found: " + hurtAnimation, this);
            return;
        }

        ClearHurtTrackEntry();
        isHurtPlaying = true;
        hasAppliedAnimation = false;
        hurtTrackEntry = skeletonAnimation.AnimationState.SetAnimation(0, hurt, false);
        hurtTrackEntry.Complete += OnHurtAnimationFinished;
        hurtTrackEntry.Interrupt += OnHurtAnimationFinished;
    }

    private void OnHurtAnimationFinished(Spine.TrackEntry trackEntry)
    {
        if (trackEntry != hurtTrackEntry)
        {
            return;
        }

        ClearHurtTrackEntry();
        isHurtPlaying = false;
        hasAppliedAnimation = false;
        UpdateAnimation(true);
    }

    private void ClearHurtTrackEntry()
    {
        if (hurtTrackEntry == null)
        {
            return;
        }

        hurtTrackEntry.Complete -= OnHurtAnimationFinished;
        hurtTrackEntry.Interrupt -= OnHurtAnimationFinished;
        hurtTrackEntry = null;
    }

    private void UpdateAnimation(bool force)
    {
        bool shouldWalk = movement != null && movement.IsMoving;
        bool shouldUseHoldingAnimation = (boxInteraction != null && boxInteraction.IsHoldingObject)
            || (sphereInteraction != null && sphereInteraction.IsHoldingSphere);
        if (!force && hasAppliedAnimation && shouldWalk == isWalking
            && shouldUseHoldingAnimation == isHoldingObject)
        {
            return;
        }

        isWalking = shouldWalk;
        isHoldingObject = shouldUseHoldingAnimation;
        hasAppliedAnimation = true;
        string animationName = isHoldingObject
            ? (isWalking ? holdingWalkingAnimation : holdingIdleAnimation)
            : (isWalking ? walkingAnimation : idleAnimation);
        skeletonAnimation.AnimationState.SetAnimation(0, animationName, true);
    }
}

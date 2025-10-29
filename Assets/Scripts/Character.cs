using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMotor2D : MonoBehaviour
{
    public float currentSpeed;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 40f;
    public float airControlMultiplier = 0.6f;

    [Header("Jump")]
    public bool canJump = true;
    public float jumpForce = 12f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundMask;

    [Header("Per-Character Flavor")]
    public float mass = 1f;
    public float frictionOnGround = 0f;

    [Header("Collision Masks")]
    public LayerMask wallMask; // Assign layers that count as walls in Inspector

    Rigidbody2D rb;
    float targetXVel;
    float inputX;
    bool wantJump;
    float lastGroundedTime;
    float lastJumpPressedTime;
    bool isGrounded;

    // Group control
    static readonly List<CharacterMotor2D> allMotors = new List<CharacterMotor2D>();
    static bool globallyFrozen = false;

    // Per-motor block state
    bool blockedHorizontally = false;
    float lastBlockNormalX = 0f;

    void OnEnable()  { if (!allMotors.Contains(this)) allMotors.Add(this); }
    void OnDisable() { allMotors.Remove(this); }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.mass = Mathf.Max(0.01f, mass);
    }

    void Update()
    {
        currentSpeed = rb.linearVelocity.magnitude;

        // Ground check
        isGrounded = (groundCheck != null) &&
                     Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
        if (isGrounded) lastGroundedTime = Time.time;

        if (!globallyFrozen && Mathf.Abs(targetXVel) > 0.05f)
        {
            var s = transform.localScale;
            s.x = Mathf.Sign(targetXVel) * Mathf.Abs(s.x);
            transform.localScale = s;
        }

        if (!globallyFrozen && AnyBlocked())
            FreezeAll();

        if (globallyFrozen && AnyBlockedHasUnfreezeIntent())
            UnfreezeAll();

        if (globallyFrozen)
        {
            targetXVel = 0f;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    void FixedUpdate()
    {
        if (globallyFrozen)
        {
            // Only kill horizontal motion; let gravity/jumps proceed.
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float accel = isGrounded ? acceleration : acceleration * airControlMultiplier;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetXVel, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

        bool canCoyote = (Time.time - lastGroundedTime) <= coyoteTime;
        bool buffered  = (Time.time - lastJumpPressedTime) <= jumpBufferTime;

        if (wantJump && canJump && (isGrounded || canCoyote || buffered))
        {
            wantJump = false;
            lastJumpPressedTime = -999f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void OnCollisionEnter2D(Collision2D c)  { CheckHorizontalBlock(c); }
    void OnCollisionStay2D (Collision2D c)  { CheckHorizontalBlock(c); }
    void OnCollisionExit2D (Collision2D c)
    {
        if (((1 << c.gameObject.layer) & wallMask) != 0)
        {
            blockedHorizontally = false;
            lastBlockNormalX = 0f;
        }
    }

    void CheckHorizontalBlock(Collision2D c)
    {
        if (((1 << c.gameObject.layer) & wallMask) == 0) return;

        foreach (var cp in c.contacts)
        {
            if (Mathf.Abs(cp.normal.x) > 0.25f) 
            {
                blockedHorizontally = true;
                lastBlockNormalX = Mathf.Sign(cp.normal.x); 
                return;
            }
        }
    }

    static bool AnyBlocked()
    {
        for (int i = 0; i < allMotors.Count; i++)
        {
            var m = allMotors[i];
            if (m != null && m.blockedHorizontally) return true;
        }
        return false;
    }

    static bool AnyBlockedHasUnfreezeIntent()
    {
        for (int i = 0; i < allMotors.Count; i++)
        {
            var m = allMotors[i];
            if (m == null || !m.blockedHorizontally) continue;

            float ix = m.inputX;

            if (Mathf.Abs(m.lastBlockNormalX) > 0.01f)
            {
                if (Mathf.Abs(ix) > 0.05f && Mathf.Sign(ix) == Mathf.Sign(m.lastBlockNormalX))
                    return true;
            }
            else
            {
                if (Mathf.Abs(ix) > 0.05f)
                    return true;
            }
        }
        return false;
    }

    static void FreezeAll()
    {
        if (globallyFrozen) return;
        globallyFrozen = true;
        for (int i = 0; i < allMotors.Count; i++)
        {
            var m = allMotors[i];
            if (m == null || m.rb == null) continue;

            m.wantJump = false;
            m.targetXVel = 0f;
            m.rb.linearVelocity = new Vector2(0f, m.rb.linearVelocity.y);
        }
    }

    public static void UnfreezeAll()
    {
        globallyFrozen = false;

        var any = (allMotors.Count > 0) ? allMotors[0] : null;
        if (any != null) any.StartCoroutine(any.EscapeFromWall());
    }
    IEnumerator EscapeFromWall()
    {
        for (int i = 0; i < allMotors.Count; i++)
        {
            var m = allMotors[i];
            if (m == null || !m.blockedHorizontally) continue;

            float nx = m.lastBlockNormalX;
            if (Mathf.Abs(nx) < 0.01f) continue;

            // Small positional away from the wall
            Vector2 delta = new Vector2(nx * 0.02f, 0f);
            m.rb.position += delta;

            // Clear the block  next frame doesn't instantly freeze
            m.blockedHorizontally = false;
            m.lastBlockNormalX = 0f;
        }

        yield return new WaitForFixedUpdate();
    }

    // Input
    public void SetMoveInput(float x)
    {
        inputX = x;                 
        targetXVel = x * moveSpeed; // compute desired speed (ignored when frozen)
    }

    public void PressJump()
    {
        if (globallyFrozen) return;
        lastJumpPressedTime = Time.time;
        wantJump = true;
    }

    public void UseAbility()
    {
        if (globallyFrozen || !isGrounded) return;
        Vector2 dir = new Vector2(Mathf.Sign(transform.localScale.x), 0f);
        rb.AddForce(dir * 8f, ForceMode2D.Impulse);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    public float GetSpeed()
    {
        return rb == null ? 0f : rb.linearVelocity.magnitude;
    }
}


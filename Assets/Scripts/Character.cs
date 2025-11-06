using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMotor2D : MonoBehaviour
{
    public float currentSpeed;

    public string characterColor;

    public float minX = -30f;
    public float maxX = 50f;
    public float minY = -20f;
    public float maxY = 20f;

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

        // Face direction if not frozen
        if (!globallyFrozen && Mathf.Abs(targetXVel) > 0.05f)
        {
            var s = transform.localScale;
            s.x = Mathf.Sign(targetXVel) * Mathf.Abs(s.x);
            transform.localScale = s;
        }

        // Freeze if any player is blocked
        if (!globallyFrozen && AnyBlocked())
            FreezeAll();

        // Unfreeze if a blocked player shows "away from wall" intent (existing rule)
        if (globallyFrozen && AnyBlockedHasUnfreezeIntent())
            UnfreezeAll();

        // NEW: Unfreeze when no one is blocked AND any grounded player is pressing move
        if (globallyFrozen && !AnyBlocked() && AnyGroundedHasMoveInput())
            UnfreezeAll();

        // While frozen: stop horizontal only, keep vertical natural
        if (globallyFrozen)
        {
            targetXVel = 0f;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // World clamp
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }

    void FixedUpdate()
    {
        if (globallyFrozen)
        {
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

    // ---- Group helpers ----
    static bool AnyBlocked()
    {
        for (int i = 0; i < allMotors.Count; i++)
        {
            var m = allMotors[i];
            if (m != null && m.blockedHorizontally) return true;
        }
        return false;
    }
    static void NormalizeAllPhysics()
    {
        if (allMotors.Count < 2) return;
        var refMotor = allMotors[0];
        foreach (var m in allMotors)
        {
            if (m == null || m.rb == null) continue;

            // movement constants
            m.moveSpeed = refMotor.moveSpeed;
            m.acceleration = refMotor.acceleration;
            m.airControlMultiplier = refMotor.airControlMultiplier;

            // Rigidbody constants (updated API)
            m.rb.mass = refMotor.rb.mass;
            m.rb.linearDamping = refMotor.rb.linearDamping;     // ✅ new name
            m.rb.angularDamping = refMotor.rb.angularDamping;   // ✅ new name
            m.rb.gravityScale = refMotor.rb.gravityScale;
            m.rb.interpolation = refMotor.rb.interpolation;
            m.rb.collisionDetectionMode = refMotor.rb.collisionDetectionMode;

            // optional: sync collider material too
            var myCol = m.GetComponent<Collider2D>();
            var refCol = refMotor.GetComponent<Collider2D>();
            if (myCol && refCol) myCol.sharedMaterial = refCol.sharedMaterial;
        }
    }


    // NEW helper: any grounded player currently pressing horizontal input?
    static bool AnyGroundedHasMoveInput()
    {
        for (int i = 0; i < allMotors.Count; i++)
        {
            var m = allMotors[i];
            if (m == null) continue;
            if (m.isGrounded && Mathf.Abs(m.inputX) > 0.05f)
                return true;
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

    static void SyncAllStats()
    {
        if (allMotors.Count < 2) return;

        // Use the first character as reference
        var refMotor = allMotors[0];
        foreach (var m in allMotors)
        {
            if (m == null) continue;
            m.moveSpeed = refMotor.moveSpeed;
            m.acceleration = refMotor.acceleration;
            m.mass = refMotor.mass;
            if (m.rb != null) m.rb.mass = refMotor.mass; // make sure Rigidbody mass also updates
        }
    }


    public static void UnfreezeAll()
    {
        globallyFrozen = false;

        // Optional tiny separation nudge to avoid instant re-block
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

            Vector2 delta = new Vector2(nx * 0.02f, 0f);
            m.rb.position += delta;

            m.blockedHorizontally = false;
            m.lastBlockNormalX = 0f;
        }

        yield return new WaitForFixedUpdate();
    }

    // Input
    public void SetMoveInput(float x)
    {
        inputX = x;
        targetXVel = x * moveSpeed;
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

    void Start()
    {
        if (characterColor == "Red") gameObject.tag = "RedCharacter";
        else if (characterColor == "Green") gameObject.tag = "GreenCharacter";
        SyncAllStats();
        NormalizeAllPhysics();
    }
}

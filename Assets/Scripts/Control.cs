using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DualPlayerInput : MonoBehaviour
{
    public CharacterMotor2D playerA;
    public CharacterMotor2D playerB;

    [Tooltip("Small vertical lift during swap to avoid immediate re-collision")]
    public float liftOnSwap = 0.02f;

#if ENABLE_INPUT_SYSTEM
    InputAction moveAction;
    InputAction jumpAction;
    InputAction swapAction;
#endif

    void Awake()
    {
#if ENABLE_INPUT_SYSTEM
        var map = new InputActionMap("Gameplay");

        moveAction = map.AddAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

        jumpAction = map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");

        //Tab to swap
        swapAction = map.AddAction("Swap", InputActionType.Button, "<Keyboard>/tab");

        map.Enable();
#endif
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 move = moveAction.ReadValue<Vector2>();
        float x = Mathf.Clamp(move.x, -1f, 1f);

        if (playerA) playerA.SetMoveInput(x);
        if (playerB) playerB.SetMoveInput(x);

        if (jumpAction.WasPressedThisFrame())
        {
            if (playerA) playerA.PressJump();
            if (playerB) playerB.PressJump();
        }

        if (swapAction.WasPerformedThisFrame())
        {
            DoSwap();
        }
#endif
    }

    //Swap positions
    void DoSwap()
    {
        if (!playerA || !playerB) return;

        var rbA = playerA.GetComponent<Rigidbody2D>();
        var rbB = playerB.GetComponent<Rigidbody2D>();
        if (!rbA || !rbB) return;

        // If using the global freeze, unfreeze
        CharacterMotor2D.UnfreezeAll();

        Vector2 posA = rbA.position;
        Vector2 posB = rbB.position;
        Vector2 velA = rbA.linearVelocity; 
        Vector2 velB = rbB.linearVelocity;


        rbA.linearVelocity = Vector2.zero;
        rbB.linearVelocity = Vector2.zero;


        float y = liftOnSwap;
        rbA.position = new Vector2(posB.x, posB.y + y);
        rbB.position = new Vector2(posA.x, posA.y + y);

        // Swap momentum
        rbA.linearVelocity = velB;
        rbB.linearVelocity = velA;

        Physics2D.SyncTransforms();
    }
}

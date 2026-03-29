using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer sr;

    // 0 = Down, 1 = Side, 2 = Up
    private int currentDirection = 0;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    public void UpdateAnimation(Vector2 moveInput)
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        animator.SetBool("IsMoving", isMoving);

        if (!isMoving)
        {
            animator.SetInteger("Direction", currentDirection);
            return;
        }

        float x = moveInput.x;
        float y = moveInput.y;

        if (Mathf.Abs(x) > Mathf.Abs(y))
        {
            currentDirection = 1;

            if (x < 0)
                sr.flipX = true;
            else if (x > 0)
                sr.flipX = false;
        }
        else
        {
            if (y > 0)
                currentDirection = 2;
            else if (y < 0)
                currentDirection = 0;
        }

        animator.SetInteger("Direction", currentDirection);
    }

    public int GetCurrentDirection()
    {
        return currentDirection;
    }
}
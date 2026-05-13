using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private PlayerController controller;

    public System.Action onCastSpellEffect; // 注册外部技能释放动作

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = transform.root.GetComponent<PlayerController>();
    }

    void Update()
    {
        bool isRunning = controller.MoveInput != 0 && controller.IsGrounded;
        float verticalVelocity = controller.Velocity.y;
        bool isGrounded = controller.IsGrounded;

        animator.SetBool("isRunning", isRunning);
        animator.SetBool("isJumpUp", !isGrounded && verticalVelocity > 0.1f);
        animator.SetBool("isFalling", !isGrounded && verticalVelocity < -0.1f);
    }

    public void PlayMeleeAttack()
    {
        controller.canMove = false;
        animator.SetTrigger("meleeAttack");
    }

    public void PlayCastSpell()
    {
        controller.canMove = false;
        animator.SetTrigger("castSpell");
    }

    // 动画中某帧设置调用这个事件
    public void TriggerCastEffect()
    {
        onCastSpellEffect?.Invoke();
    }

    public void EnableMovement()
    {
        controller.canMove = true;
    }
}

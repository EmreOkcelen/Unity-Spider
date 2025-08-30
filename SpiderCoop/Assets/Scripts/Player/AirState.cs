using UnityEngine;


public class AirState : State
{
    private float coyoteTime = 0.12f;
    private float coyoteTimer = 0f;


    public AirState(PlayerController player, StateMachine sm) : base(player, sm) { }


    public override void Enter()
    {
        coyoteTimer = coyoteTime;
    }


    public override void LogicUpdate()
    {
        if (player.inputFire)
        {
            stateMachine.ChangeState(player.webState);
            return;
        }


        if (player.isGrounded && player.rb.linearVelocity.y <= 0.1f)
        {
            stateMachine.ChangeState(player.groundedState);
            return;
        }
    }


    public override void PhysicsUpdate()
    {
        coyoteTimer -= Time.fixedDeltaTime;
        Vector3 move = new Vector3(player.inputMove.x, 0, player.inputMove.y).normalized;
        player.Move(move, useAirControl: true);
    }
}
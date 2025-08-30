using UnityEngine;

public class GroundedState : State
{
    public GroundedState(PlayerController player, StateMachine sm) : base(player, sm) { }

    public override void Enter()
    {
        // Debug.Log("Entered GroundedState");
    }

    public override void LogicUpdate()
    {
        // Eðer ateþ edildiyse web state'e geç
        if (player.inputFire)
        {
            stateMachine.ChangeState(player.webState);
            return;
        }

        // Zemin altýnda ise havaya geç
        if (!player.isGrounded)
        {
            stateMachine.ChangeState(player.airState);
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        Vector3 move = new Vector3(player.inputMove.x, 0, player.inputMove.y).normalized;
        player.Move(move, useAirControl: false);
    }
}

using UnityEngine;

public class WebState : State
{
    private SimpleWebShooter webShooter;
    private bool isPulling = false;
    private bool isClimbing = false;

    private float climbSpeed = 20f;

    public WebState(PlayerController player, StateMachine sm, SimpleWebShooter webShooter) : base(player, sm)
    {
        this.webShooter = webShooter;
    }

    public override void Enter()
    {
        Vector3? hitPoint = webShooter.TryShoot();
        if (hitPoint.HasValue)
        {
            isPulling = true;
            webShooter.StartPulling(player.rb);
        }
        else
        {
            stateMachine.ChangeState(player.isGrounded ? player.groundedState : player.airState);
        }
    }

    public override void LogicUpdate()
    {
        // ip iptal (sol týk tekrar)
        if (Input.GetMouseButtonDown(0) && isPulling)
        {
            StopClimbIfActive(); // týrmanmayý býrak
            webShooter.StopPulling();
            isPulling = false;
            stateMachine.ChangeState(player.isGrounded ? player.groundedState : player.airState);
            return;
        }

        // space basýlý tutuluyorsa webe týrman
        if (isPulling && player.inputJumpHeld)
        {
            if (!isClimbing) // ilk kez basýldý
            {
                isClimbing = true;
                player.rb.isKinematic = true; // physics devre dýþý
            }
            webShooter.Climb(player.rb, climbSpeed);
        }
        else if (isClimbing) // Space býrakýldýysa
        {
            StopClimbIfActive();
        }

        // isPulling ve actual joint uyumlu deðilse çýk
        if (!isPulling || (webShooter != null && !webShooter.IsAttached()))
        {
            StopClimbIfActive();
            isPulling = false;
            stateMachine.ChangeState(player.isGrounded ? player.groundedState : player.airState);
        }
    }

    public override void Exit()
    {
        StopClimbIfActive();
        if (isPulling)
        {
            webShooter.StopPulling();
            isPulling = false;
        }
    }

    private void StopClimbIfActive()
    {
        if (isClimbing)
        {
            isClimbing = false;
            player.rb.isKinematic = false; // physics tekrar aktif
        }
    }
}

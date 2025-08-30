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
        // ip iptal (sol t�k tekrar)
        if (Input.GetMouseButtonDown(0) && isPulling)
        {
            StopClimbIfActive(); // t�rmanmay� b�rak
            webShooter.StopPulling();
            isPulling = false;
            stateMachine.ChangeState(player.isGrounded ? player.groundedState : player.airState);
            return;
        }

        // space bas�l� tutuluyorsa webe t�rman
        if (isPulling && player.inputJumpHeld)
        {
            if (!isClimbing) // ilk kez bas�ld�
            {
                isClimbing = true;
                player.rb.isKinematic = true; // physics devre d���
            }
            webShooter.Climb(player.rb, climbSpeed);
        }
        else if (isClimbing) // Space b�rak�ld�ysa
        {
            StopClimbIfActive();
        }

        // isPulling ve actual joint uyumlu de�ilse ��k
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

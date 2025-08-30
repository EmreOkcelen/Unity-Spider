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
        // ip iptal (sol tık tekrar)
        if (Input.GetMouseButtonDown(0) && isPulling)
        {
            StopClimbIfActive();
            webShooter.StopPulling();
            isPulling = false;
            stateMachine.ChangeState(player.isGrounded ? player.groundedState : player.airState);
            return;
        }

        // space basılı tutuluyorsa webe tırman
        if (isPulling && player.inputJumpHeld)
        {
            if (!isClimbing) // ilk kez basıldı
            {
                isClimbing = true;
                player.rb.isKinematic = true; // physics devre dışı
            }
            webShooter.Climb(player.rb, climbSpeed);
        }
        else if (isClimbing) // Space bırakıldıysa
        {
            // Artık fizik açmıyoruz, player ağda sabit kalacak
            isClimbing = false;
            // rb.isKinematic true kalıyor → düşmeyecek
        }

        // isPulling ve actual joint uyumlu değilse çık
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
        if (!isClimbing)
        {
            isClimbing = false;

            // Fizik tekrar aç
            player.rb.isKinematic = false;

            // Gerçekçi momentum: tırmanma hızını kuvvet olarak uygula
            player.rb.AddForce(Vector3.down * climbSpeed * 0.001f, ForceMode.VelocityChange);
        }
    }
}

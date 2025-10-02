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
        // Kesin: yalnızca owner TryShoot yapabilir
        if (!player.IsOwner)
        {
            Debug.Log($"WebState.Enter cancelled: not owner for {player.name}");
            stateMachine.ChangeState(player.isGrounded ? player.groundedState : player.airState);
            return;
        }

        if (webShooter == null)
        {
            Debug.LogError($"WebState.Enter: webShooter is null for {player.name}");
            stateMachine.ChangeState(player.isGrounded ? player.groundedState : player.airState);
            return;
        }

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

        // kesin temizlik: eğer herhangi bir durumda rb kinematik kaldıysa aç
        if (player.rb != null && player.rb.isKinematic)
        {
            player.rb.isKinematic = false;
        }
    }


    private void StopClimbIfActive()
    {
        // Eğer tırmanmıyorsak bir şey yapma
        if (!isClimbing)
            return;

        // Tırmanma aktifse, iptal et
        isClimbing = false;

        // Fizik tekrar aç
        player.rb.isKinematic = false;

        // Gerçekçi momentum: kinematikten fiziksel hale dönerken küçük bir ivme uygula
        // (Çok küçük bir aşağı vuruş, oyuncunun sabit kalmasını engeller)
        try
        {
            player.rb.AddForce(Vector3.down * (climbSpeed * 0.1f + 1f), ForceMode.VelocityChange);
        }
        catch
        {
            // Eğer yine bir sebeple kinematikse AddForce hata fırlatır; güvenlik için swallow et.
        }
    }

}

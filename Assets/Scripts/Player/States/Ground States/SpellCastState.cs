using UnityEngine;

public class SpellCastState : GroundedStates
{
    private float elapsed = 0f;
    private float castMoment = 0f;
    private float castDuration = 0f;
    private float castDirection = 1f;
    private bool casted = false;
    private bool isSatanCaster = false;
    private bool jumpRequested = false;
    private SpellData activeSpell;
    private string activeCastBool;

    public SpellCastState(Player player, StateMovMachine stateMachine)
        : base(player, stateMachine) { }

    public override void Enter()
    {
        base.Enter();
        elapsed = 0f;
        casted = false;
        jumpRequested = false;
        activeSpell = null;
        activeCastBool = string.Empty;

        isSatanCaster = player.GetCurrentCharState() == player.SatanState;
        castDirection = player.ActiveSR != null && player.ActiveSR.flipX ? -1f : 1f;

        if (player.SpellController == null)
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        if (!player.SpellController.TryGetReadySpell(isSatanCaster, out activeSpell))
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        castDuration = Mathf.Max(0.05f, activeSpell.castDuration);
        castMoment = castDuration * Mathf.Clamp01(activeSpell.castMomentNormalized);

        if (!string.IsNullOrWhiteSpace(activeSpell.castTrigger))
        {
            Animator.SetTrigger(activeSpell.castTrigger);
        }

        activeCastBool = activeSpell.castBool;
        if (!string.IsNullOrWhiteSpace(activeCastBool))
        {
            Animator.SetBool(activeCastBool, true);
        }
    }

    public override void HandleInput()
    {
        base.HandleInput();
        jumpRequested = player.PlayerInput.actions["Jump"].WasPressedThisFrame();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (jumpRequested)
        {
            stateMachine.ChangeState(player.JumpingState);
            return;
        }

        elapsed += Time.deltaTime;
        if (!casted && elapsed >= castMoment)
        {
            CastSpell();
            casted = true;
        }

        if (elapsed >= castDuration)
        {
            if (player.Rb.linearVelocity.y < 0f)
            {
                stateMachine.ChangeState(player.AirState);
                return;
            }

            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        if (!string.IsNullOrWhiteSpace(activeCastBool))
        {
            Animator.SetBool(activeCastBool, false);
        }
    }

    private void CastSpell()
    {
        if (activeSpell == null || activeSpell.SpellPrefab == null)
        {
            return;
        }

        BoxCollider2D box = player.GetComponent<BoxCollider2D>();
        Vector2 origin = box != null ? box.bounds.center : (Vector2)player.transform.position;
        Vector2 direction = Vector2.right * castDirection;
        Vector2 spawnPos = origin + direction * 1f;

        GameObject spellObj = Object.Instantiate(activeSpell.SpellPrefab, spawnPos, Quaternion.identity);

        SpellBase spellBase = spellObj.GetComponent<SpellBase>();
        if (spellBase != null)
        {
            spellBase.Initialize(player, activeSpell, castDirection);
            spellBase.Cast(spawnPos);
        }
        else
        {
            Bullet bullet = spellObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.ConfigureFromSpell(activeSpell);
                bullet.SetDirection(castDirection);
            }
        }

        player.SpellController.StartCooldown(isSatanCaster, activeSpell);
    }
}

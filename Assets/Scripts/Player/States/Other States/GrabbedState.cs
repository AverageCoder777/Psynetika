using UnityEngine;

/*
Игрока схватили (нейрончик вцепился в лицо). Любые действия заблокированы, идёт QTE:
одна случайная кнопка, каждое нажатие сокращает захват (QteSequence).
  захват кончился раньше таймера — захватчик сброшен, игрок снова управляем;
  не кончился — персонаж совершает самоубийство (смерть текущего героя, дальше обычный DyingState);
               при QteSettings.killOnFail = false (паутина Мамы паука) — просто урон failDamage.

Войти можно из любого состояния, кроме смерти и уже идущего захвата: от захвата не уклониться.
Захватчик узнаёт исход через IPlayerGrabber.OnGrabEnded ровно один раз — в том числе когда
захват оборвался со стороны (игрок погиб от другого урона).
*/
public class GrabbedState : State
{
    private static readonly int GrabbedHash = Animator.StringToHash("Grabbed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");

    private readonly QteSequence qte = new();
    private IPlayerGrabber grabber;
    private IPlayerGrabber pendingGrabber;
    private QteSettings pendingSettings;

    public GrabbedState(PlayerController player, StateMachine stateMachine, PlayerStaticSettings settings)
        : base(player, stateMachine, settings) { }

    public IPlayerGrabber Grabber => grabber;

    public bool TryGrab(IPlayerGrabber by, QteSettings qteSettings)
    {
        if (by == null || stateMachine?.CurrentPlayerState == null) return false;

        State current = stateMachine.CurrentPlayerState;
        if (current == this || current == player.DyingState) return false;
        if (!player.IsVisibleToEnemies) return false;

        pendingGrabber = by;
        pendingSettings = qteSettings;
        stateMachine.ChangeState(this);
        return true;
    }

    // Захватчик отпускает сам (погиб, оторвали). Игрок выходит из захвата без последствий.
    public void Release(IPlayerGrabber by)
    {
        if (by == null || by != grabber || stateMachine.CurrentPlayerState != this) return;

        grabber = null;
        ExitToControl();
    }

    public override void Enter()
    {
        base.Enter();
        grabber = pendingGrabber;
        pendingGrabber = null;
        qte.Begin(pendingSettings);
        pendingSettings = null;

        movement.Rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, movement.Rb.linearVelocity.y));
        SetGrabbed(true);
        QtePromptUI.Instance.Show(qte);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        switch (qte.Tick(Time.deltaTime))
        {
            case QteSequence.Status.Succeeded:
                Finish(GrabOutcome.Escaped);
                ExitToControl();
                break;

            case QteSequence.Status.Failed:
                Finish(GrabOutcome.Failed);
                QteSettings failed = qte.Settings;
                if (failed == null || failed.killOnFail)
                {
                    // Смерть придёт событием PlayerHealth.Died → PlayerController переведёт в DyingState.
                    player.GetComponent<PlayerHealth>().KillCurrentCharacter();
                    if (stateMachine.CurrentPlayerState == this)
                    {
                        ExitToControl();
                    }
                    break;
                }

                // Мягкий провал (паутина): захват просто кончился, остаётся урон.
                ExitToControl();
                if (failed.failDamage > 0)
                {
                    DamageHelper.TryDamage(player.GetComponent<PlayerHealth>(), failed.failDamage, DamageType.Physical);
                }
                break;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        // Падать можно (схватили в прыжке), но ни шагу в сторону.
        movement.Rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, movement.Rb.linearVelocity.y));
    }

    public override void Exit()
    {
        base.Exit();
        // Выход не через QTE — например, смерть от чужого урона: захватчик должен отцепиться.
        Finish(GrabOutcome.Interrupted);
        SetGrabbed(false);
        QtePromptUI.Instance.Hide();
    }

    private void Finish(GrabOutcome outcome)
    {
        IPlayerGrabber by = grabber;
        grabber = null;
        by?.OnGrabEnded(outcome);
    }

    private void ExitToControl()
    {
        bool onGround = DetectFloor() != "None";
        charManager.ActiveAnimator.SetBool(GroundedHash, onGround);
        stateMachine.ChangeState(onGround ? player.IdleState : player.FlyingState);
    }

    // Анимации захвата у героев может ещё не быть — не спамим предупреждениями аниматора.
    private void SetGrabbed(bool value)
    {
        Animator animator = charManager.ActiveAnimator;
        if (animator == null) return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == GrabbedHash)
            {
                animator.SetBool(GrabbedHash, value);
                return;
            }
        }
    }
}

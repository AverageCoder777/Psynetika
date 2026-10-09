using System.Globalization;
using System.IO;
using UnityEngine;


public class EnemyLogger : MonoBehaviour
{
    public EnemyController[] spiders;
    public string squadId = "Spiders";
    private string logPath;

    void Start()
    {
        logPath = Path.Combine(Application.persistentDataPath, "enemy_log_after.txt");
        File.WriteAllText(logPath, "");
        Debug.Log("enemy log: " + logPath);
    }

    void FixedUpdate()
    {
        if (spiders == null) return;

        EnemySquad squad = EnemySquad.Get(squadId);
        int holder = squad != null ? IndexOf(squad.Holder) : -1;

        for (int i = 0; i < spiders.Length; i++)
        {
            EnemyController s = spiders[i];
            if (s == null) continue; // паук уничтожен

            Vector2 p = s.transform.position;
            Vector2 v = s.Movement != null && s.Movement.Body != null ? s.Movement.Body.linearVelocity : Vector2.zero;
            int hp = s.Health != null ? s.Health.CurrentHp : -1;

            // Диагностика: работает ли контроллер, упирается ли паук, видит ли игрока.
            bool run = s.enabled && s.StateMachine != null && s.StateMachine.CurrentEnemyState != null;
            bool blocked = s.Movement != null && s.Movement.LastMoveBlocked;
            float dist = s.Sensor != null ? s.Sensor.HorizontalDistanceToPlayer : -1f;

            string line = string.Format(CultureInfo.InvariantCulture,
                "[{0:F3}] id={1} st={2} pos=({3:F2},{4:F2}) vel=({5:F2},{6:F2}) hp={7} turn={8} holder={9} run={10} blocked={11} dist={12:F2}",
                Time.time, i, s.CurrentStateId, p.x, p.y, v.x, v.y, hp, holder == i, holder, run, blocked, dist);
            File.AppendAllText(logPath, line + "\n");
        }
    }

    // Номер паука в массиве по его EnemyAttack; -1, если очередь свободна
    private int IndexOf(EnemyAttack attack)
    {
        if (attack == null) return -1;
        for (int i = 0; i < spiders.Length; i++)
        {
            if (spiders[i] != null && spiders[i].Attack == attack) return i;
        }
        return -1;
    }
}

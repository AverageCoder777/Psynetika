// Чем закончился захват игрока.
public enum GrabOutcome
{
    Escaped = 0,     // игрок прошёл QTE и сбросил захватчика
    Failed = 1,      // время QTE вышло — персонаж погибает
    Interrupted = 2  // захват оборван со стороны: смерть игрока от другого урона и т.п.
}

/*
Тот, кто держит игрока (нейрончик на лице). Игрок в GrabbedState сообщает ему исход ровно один раз;
сам захватчик, погибнув или передумав, отпускает игрока через PlayerController.ReleaseGrab.
*/
public interface IPlayerGrabber
{
    void OnGrabEnded(GrabOutcome outcome);
}

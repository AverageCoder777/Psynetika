using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField]
    private SatanPlayer _satanPlayer;

    [SerializeField]
    private SobakaPlayer _sobakaPlayer;

    [SerializeField] private GameObject _effectCHangeCharacter;
    [SerializeField] private UILineInfo _ui;
    private string _activeCharacterName = "satan";
    

    public void ChangePlayer()
    { 
        if(_activeCharacterName == "satan")
        {
            
        }
    }
}

using UnityEngine;

public class InGameUI : MonoBehaviour
{
    public InGameTopUI TopUI => _topUI;
    public InGameBottomUI BottomUI => _bottomUI;
    
    [SerializeField] private InGameTopUI _topUI;
    [SerializeField] private InGameBottomUI _bottomUI;
    [SerializeField] private Animator _animator;

    public void PlayAnimation(string trigger)
    {
        _animator.SetTrigger(trigger);
    }
}

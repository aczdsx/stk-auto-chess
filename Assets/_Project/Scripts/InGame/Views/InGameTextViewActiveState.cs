using CookApps.AutoBattler;
using UnityEngine;

public class InGameTextViewActiveState : StateMachineBehaviour
{
    private InGameTextView _textView;
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(_textView == null)
        {
            _textView = animator.GetComponent<InGameTextView>();
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(_textView != null)
        {
            _textView.ReturnTextView();
        }
    }
}

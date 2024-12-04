using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SG
{
    public class CharacterAnimatorManager : MonoBehaviour
    {
        CharacterManager character;

        float horizontal;
        float vertical;

        protected virtual void Awake()
        {
            character = GetComponent<CharacterManager>();
        }

        public void UpdateAnimatorMovementParameters(float horizontalMovement, float verticalMovement)
        {
            if (character.animator != null)
            {
                character.animator.SetFloat("Horizontal", horizontalMovement, 0.1f, Time.deltaTime);
                character.animator.SetFloat("Vertical", verticalMovement, 0.1f, Time.deltaTime);
            }
        }

        public virtual void PlayTargetActionAnimation(string targetAnimation, bool isPerformingAction, bool applyRootMotion = true)
        {
            character.animator.applyRootMotion = applyRootMotion;
            character.animator.CrossFade(targetAnimation, 0.2f);
            character.isPerformingAction = isPerformingAction;
        }
    }
}

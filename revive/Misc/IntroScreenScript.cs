using UnityEngine;
using UnityEngine.InputSystem;

namespace lethalCompanyRevive.Misc
{
    internal class IntroScreenScript : MonoBehaviour
    {
        void Update()
        {
            if(Keyboard.current[Key.Escape].wasPressedThisFrame)
            {
                Destroy(gameObject);
            }
        }
    }
}

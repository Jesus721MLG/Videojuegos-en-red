using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Battleship
{
    public class ImageFader : MonoBehaviour
    {
        Animator _anim;

        private void Start()
        {
            _anim = GetComponent<Animator>();
        }

        public void Enable()
        {
            _anim.SetTrigger("Enable");
        }

        public void FadeOut()
        {
            _anim.SetTrigger("FadeOut");
        }
    }
}

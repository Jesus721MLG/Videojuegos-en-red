using UnityEngine;

namespace Battleship
{
    public class AnimationAudio : MonoBehaviour
    {
        public void PlayClip(AudioClip clip)
        {
            GetComponent<AudioSource>().PlayOneShot(clip);
        }
    }
}

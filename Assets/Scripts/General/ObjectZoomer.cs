using System.Collections;
using UnityEngine;

namespace Battleship
{
    public class ObjectZoomer : MonoBehaviour
    {
        [SerializeField] float _zoomDifference;
        [SerializeField] float _duration = 1f;

        Vector3 _startPosition;
        Vector3 _endPosition;
        Vector3 _defaultPosition;
        Vector3 _defaultPositionWhileZoomed;
        float _elapsedTime;
        float _percentageComplete;

        private void Start()
        {
            _defaultPosition = transform.position;
            _defaultPositionWhileZoomed = new Vector3(transform.position.x, transform.position.y + _zoomDifference, transform.position.z);
        }

        IEnumerator CO_ZoomOverTime(Transform t, bool zoomIn) 
        {
            _elapsedTime = 0;
            _percentageComplete = 0;
            _startPosition = new Vector3 (t.position.x, t.position.y, t.position.z);

            if (zoomIn)
            {
                t.position = _defaultPosition;
                _endPosition = new Vector3(t.position.x, t.position.y + _zoomDifference, t.position.z);
            }
            else
            {
                t.position = _defaultPositionWhileZoomed;
                _endPosition = new Vector3(t.position.x, t.position.y - _zoomDifference, t.position.z);
            }

            while (_elapsedTime < _duration)
            {
                yield return new WaitForEndOfFrame();

                _elapsedTime += Time.deltaTime;
                _percentageComplete = _elapsedTime / _duration;

                transform.position = Vector3.Lerp(_startPosition, _endPosition, Mathf.SmoothStep(0, 1, _percentageComplete));
            }
        }

        public void ZoomOverTime(Transform t, bool zoomIn)
        {
            StartCoroutine(CO_ZoomOverTime(t, zoomIn));
        }
    }
}

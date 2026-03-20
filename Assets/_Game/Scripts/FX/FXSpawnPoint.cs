using UnityEngine;

namespace BulletRoute.FX
{
    public class FXSpawnPoint : MonoBehaviour
    {
        [Header("FX Settings")]
        [SerializeField] private string _fxName;
        [SerializeField] private bool _playOnEnable;
        [SerializeField] private bool _loop;
        [SerializeField] private float _delay;
        [SerializeField] private Vector3 _offset;

        public string FXName => _fxName;
        public Vector3 SpawnPosition => transform.position + _offset;

        private void OnEnable()
        {
            if (_playOnEnable)
            {
                if (_delay > 0)
                    Invoke(nameof(PlayFX), _delay);
                else
                    PlayFX();
            }
        }

        public void PlayFX()
        {
            if (BulletRoute.Core.ServiceLocator.TryGet<FXManager>(out var fxManager))
            {
                fxManager.SpawnFX(_fxName, SpawnPosition, transform.rotation);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(SpawnPosition, 0.1f);
            Gizmos.DrawLine(transform.position, SpawnPosition);
        }
    }
}

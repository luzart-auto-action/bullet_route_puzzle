using UnityEngine;

namespace BulletRoute.FX
{
    public class FXAutoDestroy : MonoBehaviour
    {
        [SerializeField] private float _lifetime = 3f;

        private void OnEnable()
        {
            Invoke(nameof(Deactivate), _lifetime);
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            CancelInvoke();
        }
    }
}

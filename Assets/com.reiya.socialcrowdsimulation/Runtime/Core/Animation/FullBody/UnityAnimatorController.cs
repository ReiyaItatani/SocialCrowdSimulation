using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Unity.Mathematics;
using UnityEngine.Events;

namespace CollisionAvoidance
{
    public class UnityAnimatorController : PlayableSampleBase
    {
        #region PUBLIC ATTRIBUTES
        public UnityAction UnityAnimatorControllerUpdated;

        [Header("Transform")]
        [SerializeField]
        private float maxTurnAnglePerSecond = 90f;
        [Header("Animation")]
        public Animator _targetAnimator = null;
        public RuntimeAnimatorController _animatorController = null;
        public AgentPathController _agentPathController = null;

        [Range(0.0f, 1.0f)]
        public float _weight = 1.0f;

        #endregion

        #region PROTECTED ATTRIBUTES
        [SerializeField, ReadOnly]
        protected float _debugSpeed = 0;

        protected bool _initialized = false;
        protected Vector3 _lastDirection = Vector3.forward;
        #endregion

        #region CREATION AND DESTRUCTION

        protected override void Awake()
        {
            _animator = _targetAnimator;
            _animator.applyRootMotion = false;

            InitTransform();
        }

        protected override void InitPlayableGraph()
        {
            _initialized = true;
            var animatorPlayable = AnimatorControllerPlayable.Create(_playableGraph, _animatorController);
            _playableOutput.SetSourcePlayable(animatorPlayable);
        }

        public void UnsubscribePlayableGraph()
        {
            StopPlayableGraph();
            _initialized = false;
        }

        private void InitTransform()
        {
            if (_agentPathController == null) return;

            transform.position = _agentPathController.GetWorldInitPosition();
            Vector3 initDir = _agentPathController.GetWorldInitDirection();
            transform.rotation = quaternion.LookRotation(initDir, Vector3.up);
            _lastDirection = initDir.normalized;
        }

        #endregion

        #region UPDATE

        private void LateUpdate(){
            if (UnityAnimatorControllerUpdated != null)
            {
                UnityAnimatorControllerUpdated.Invoke();
            }
        }

        public void UpdateAnimation()
        {
            if (!_initialized)
            {
                StartPlayableGraph();
            }

            if (_agentPathController != null)
            {
                UpdateTransform();
                PlayAnimation();
            }
        }

        protected virtual void PlayAnimation()
        {
            Vector3 velocity = _agentPathController.GetCurrentSpeed() *
                               _targetAnimator.transform.InverseTransformVector(_agentPathController.GetCurrentDirection());
            float speed = _agentPathController.GetCurrentSpeed();

            _debugSpeed = speed;

            _playableOutput.SetWeight(_weight);

            _animator.SetFloat("VelX", velocity.x);
            _animator.SetFloat("VelZ", velocity.z);
        }

        private void UpdateTransform()
        {
            _targetAnimator.transform.position = _agentPathController.GetCurrentPosition();

            Vector3 targetDirection = _agentPathController.GetCurrentDirection().normalized;

            float angleDiff = Vector3.Angle(_lastDirection, targetDirection);
            float maxTurnAngleThisFrame = maxTurnAnglePerSecond * Time.deltaTime;

            if (angleDiff > maxTurnAngleThisFrame)
            {
                float t = maxTurnAngleThisFrame / angleDiff;
                targetDirection = Vector3.Slerp(_lastDirection, targetDirection, t);
            }
            _targetAnimator.transform.rotation = quaternion.LookRotation(targetDirection, Vector3.up);
            _lastDirection = targetDirection;
        }

        #endregion
    }
}

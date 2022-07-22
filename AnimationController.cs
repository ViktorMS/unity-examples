using Unity.Netcode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{

    public class AnimationController : NetworkBehaviour
    {
        public float SpeedChangeRate = 10.0f;
        public float MoveSpeed = 2.0f;
        public float SprintSpeed = 5.335f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;


        // player
        private float _speed;
        private float _lastSpeed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private Animator _animator;

        private bool _hasAnimator;

        private Vector3 lastTransformPosition = Vector3.zero;



        private void Start()
        {
            _hasAnimator = TryGetComponent(out _animator);
            AssignAnimationIDs();

        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private float getSpeed()
        {
            var now = transform.position;
            var then = lastTransformPosition;
            var speed = (now - then).magnitude / Time.deltaTime;
            lastTransformPosition = now;
            return speed;
        }

        private void Update()
        {
            GroundedCheck();

            if (IsOwner)
            {
                //
            }
            else
            {
                _speed = getSpeed();


                _hasAnimator = TryGetComponent(out _animator);

                if (Grounded) Move();
                else JumpAndGravity();

            }
        }


        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void Move()
        {
            // float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;
            float inputMagnitude = 1f;
            // Todo Get if sprint
            bool inputsprint = false;
            if (_speed > 2) inputsprint = true;

            float targetSpeed = inputsprint ? SprintSpeed : MoveSpeed;

            if (_speed < 0.1) targetSpeed = 0;

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (true)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    //_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                //if (_jumpTimeoutDelta >= 0.0f)
                //{
                //    _jumpTimeoutDelta -= Time.deltaTime;
                //}
            }
            else
            {
                // reset the jump timeout timer
                //_jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                //if (_fallTimeoutDelta >= 0.0f)
                {
                //    _fallTimeoutDelta -= Time.deltaTime;
                }
                //else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                //_input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                //_verticalVelocity += Gravity * Time.deltaTime;
            }
        }

    }


}

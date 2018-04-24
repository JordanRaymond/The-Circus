using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CC
{
    public class StatesManager : MonoBehaviour
    {
        public ControllerStats stats;
        public ControllerStates states;
        public InputVariables inp;

        [System.Serializable]
        public class InputVariables
        {
            public float horizontal;
            public float vertical;
            public float moveAmount;
            public Vector3 moveDirection;
            public Vector3 aimPosition;
            public Vector3 rotateDirection;
        }

        [System.Serializable]
        public class ControllerStates
        {
            public bool onGround;
            public bool isAiming;
            public bool isCrouching;
            public bool isRunning;
            public bool isInteracting;
        }

        #region References
        public Animator anim;
        public GameObject activeModel;
        [HideInInspector] public AnimatorHook animHook;
        [HideInInspector] public Rigidbody rigid;
        [HideInInspector] public Collider controllerCollider;


        //[HideInInspector]
        //public Transform referencesParent;
        [HideInInspector] public Transform mTransform;
        public CharState currentState;

        public LayerMask ignoreLayers;
        public LayerMask ignoreForGround;

        public float delta;

        List<Collider> ragdollColliders = new List<Collider>();
        List<Rigidbody> ragdollRigids = new List<Rigidbody>();

   
        #endregion

        #region Init
        void Start() {

        }

        public void Init() {
            mTransform = transform;

            SetupAnimator();
            rigid = GetComponent<Rigidbody>();
            rigid.isKinematic = false;
            rigid.drag = 4;
            rigid.angularDrag = 999;
            rigid.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            controllerCollider = GetComponent<Collider>();

            SetupRagdoll();

            ignoreLayers = ~(1 << 9);
            ignoreForGround = ~(1 << 9 | 1 << 10);

            animHook = activeModel.AddComponent<AnimatorHook>();
            animHook.Init(this); 
        }

        void SetupAnimator() {
            if (activeModel == null) {
                anim = GetComponentInChildren<Animator>();
                activeModel = anim.gameObject;
            }

            if (anim == null) {
                anim = activeModel.GetComponent<Animator>();
            }

            anim.applyRootMotion = false;
        }

        void SetupRagdoll() {
            Rigidbody[] rigids = activeModel.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody r in rigids) {
                if (r == rigid) continue;

                Collider c = r.gameObject.GetComponent<Collider>();
                c.isTrigger = true;
                r.isKinematic = true;
                r.gameObject.layer = 10;
                ragdollRigids.Add(r);
                ragdollColliders.Add(c);
            }
        }
        #endregion

        #region Fixed Update
        public void FixedTick(float p_delta) {
            delta = p_delta;

            switch (currentState) {
                case CharState.normal:
                    states.onGround = OnGround();

                    if (states.isAiming) {
                        MovementAiming();
                    }
                    else {
                        MovementNormal();
                    }

                    RotationNormal();
                    break;
                case CharState.onAir:
                    rigid.drag = 0;
                    states.onGround = OnGround();
                    break;
                case CharState.cover:
                    break;
                case CharState.vaulting:
                    break;
                default:
                    break;
            }
        }

        void MovementNormal() {

            rigid.drag = (inp.moveAmount > 0.05f) ? 0 : 4;

            float speed = stats.walkSpeed;
            if (states.isRunning) {
                speed = stats.runSpeed;
            }
            if (states.isCrouching) {
                speed = stats.crouchSpeed;
            }

            Vector3 dir = Vector3.zero;
            dir = mTransform.forward * (speed * inp.moveAmount);
            rigid.velocity = dir;
        }

        void RotationNormal() {
            if (!states.isAiming)
                inp.rotateDirection = inp.moveDirection;

            Vector3 targetDir = inp.rotateDirection;
            targetDir.y = 0;

            if (targetDir == Vector3.zero) {
                targetDir = mTransform.forward;
            }

            Quaternion lookDir = Quaternion.LookRotation(targetDir);
            Quaternion targetRot = Quaternion.Slerp(mTransform.rotation, lookDir, stats.rotateSpeed * delta);

            mTransform.rotation = targetRot;
        }

        void MovementAiming() {
            float speed = stats.aimSpeed;
            Vector3 v = inp.moveDirection * speed;
            rigid.velocity = v;
        }
        #endregion

        void RotationAiming() {

        }

        #region Update
        public void Tick(float p_delta) {
            delta = p_delta;

            switch (currentState) {
                case CharState.normal:
                    states.onGround = OnGround();

                    HandleAnimationAll();

                    //if (states.isAiming) {

                    //}
                    //else {
                    //    RotationNormal();
                    //    MovementNormal();
                    //}

                    break;
                case CharState.onAir:
                    rigid.drag = 0;
                    states.onGround = OnGround();
                    break;
                case CharState.cover:
                    break;
                case CharState.vaulting:
                    break;
                default:
                    break;
            }
        }

        void HandleAnimationAll() {
            anim.SetBool(StaticStrings.sprint, states.isRunning);
            anim.SetBool(StaticStrings.aiming, states.isAiming);
            anim.SetBool(StaticStrings.crouch, states.isCrouching);

            if (states.isAiming) {
                HandleAnimationAiming();
            }
            else {
                HandleAnimationNormal();
            }

        }

        void HandleAnimationNormal() {
            rigid.drag = (inp.moveAmount > 0.05f) ? 0 : 4;

            float anim_v = inp.moveAmount;
            anim.SetFloat(StaticStrings.vertical, anim_v, 0.15f, delta);
        }

        void HandleAnimationAiming() {
            float v = inp.vertical;
            float h = inp.horizontal;

            anim.SetFloat(StaticStrings.horizontal, h, 0.2f, delta);
            anim.SetFloat(StaticStrings.vertical, v, 0.2f, delta);
        }
        #endregion

        bool OnGround() {
            Vector3 origin = mTransform.position;
            origin.y += 0.6f;
            Vector3 dir = Vector3.down;
            float dis = 0.7f;
            RaycastHit hit;

            if (Physics.Raycast(origin, dir, out hit, dis, ignoreForGround)) {
                Vector3 targetPosition = hit.point;
                mTransform.position = targetPosition;

                return true;
            }

            return false;
        }
    }

    public enum CharState
    {
        normal, onAir, cover, vaulting
    }
}
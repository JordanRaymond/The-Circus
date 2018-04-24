using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CC
{
    public class AnimatorHook : MonoBehaviour
    {
        Animator anim;
        StatesManager states;

        float m_h_weight;   // Main Hands
        float o_h_weight;   // Off hand ?
        float l_weight;     // Look at
        float b_weight;     // Body

        Transform rh_target; // Right hand 
        Transform lh_target; // Left hand
        Transform shoulder;
        Transform aimPivot;

        public bool disable_o_h;
        public bool disable_m_h;

        Vector3 lookDir;

        public void Init(StatesManager st) {
            states = st;
            anim = states.anim;

            shoulder = anim.GetBoneTransform(HumanBodyBones.RightShoulder).transform;
            aimPivot = new GameObject().transform;
            aimPivot.name = "aim pivot";
            aimPivot.transform.parent = states.transform;

            rh_target = new GameObject().transform;
            rh_target.name = "right hand target";
            rh_target.parent = aimPivot;
            states.inp.aimPosition = states.transform.position + transform.forward * 15;
            states.inp.aimPosition.y += 1.4f; 
        }

        private void OnAnimatorMove() {
            lookDir = states.inp.aimPosition - aimPivot.position;
            Debug.DrawRay(transform.position + new Vector3(0, 1.5f, 0), lookDir, Color.red);

            HandleShoulder();
        }

        void HandleShoulder() {
            HandleShoulderPosition();
            HandleShoulderRotation();
        }

        void HandleShoulderPosition() {
            aimPivot.position = shoulder.position;
        }

        void HandleShoulderRotation() {
            Vector3 targetDir = lookDir;
            if (targetDir == Vector3.zero)
                targetDir = aimPivot.forward;
            Quaternion tr = Quaternion.LookRotation(targetDir);
            aimPivot.rotation = Quaternion.Slerp(aimPivot.rotation, tr, states.delta * 15);
        }

        void HandleWeights() {
            if (states.states.isInteracting) {
                m_h_weight = 0;
                o_h_weight = 0;
                l_weight = 0;

                return;
            }

            // Target look 
            float t_l_weight = 0;
            // Target main hand
            float t_m_weight = 0;

            if (states.states.isAiming) {
                t_m_weight = 1;
                b_weight = 0.4f;
            } else {
                b_weight = 0.3f;
            }

            o_h_weight = (lh_target != null && !disable_o_h) ? 1 : 0;

            // Look direction
            Vector3 ld = states.inp.aimPosition - states.mTransform.position;
            float angle = Vector3.Angle(states.mTransform.forward, ld);
            t_l_weight = angle < 76 ? 1 : 0;

            if (disable_m_h ||  angle > 45)
                t_m_weight = 0;

            // Look at
            l_weight = Mathf.Lerp(l_weight, t_l_weight, states.delta * 3);
            m_h_weight = Mathf.Lerp(m_h_weight, t_m_weight, states.delta * 9);

        }

        void OnAnimatorIK() {
            HandleWeights();

            anim.SetLookAtWeight(l_weight, b_weight, 1, 1, 1);
            anim.SetLookAtPosition(states.inp.aimPosition);

            if (lh_target != null) {
                UpdateIK(AvatarIKGoal.LeftHand, lh_target, o_h_weight);
            }

            UpdateIK(AvatarIKGoal.RightHand, rh_target, m_h_weight);

        }

        void UpdateIK(AvatarIKGoal goal, Transform t, float w) {
            anim.SetIKPositionWeight(goal, w);
            anim.SetIKRotationWeight(goal, w);
            anim.SetIKPosition(goal, t.position);
            anim.SetIKRotation(goal, t.rotation);
        }

        public void Tick() {
            RecoilActual();
        }

        #region Recoil 
        float recoiltT;
        Vector3 offsetPosition;
        Vector3 offsetRotation;
        Vector3 basePosition;
        Vector3 baseRotation;
        bool recoilIsInit;

        public void RecoilAnim() {
            if (!recoilIsInit) {
                recoilIsInit = true;
                recoiltT = 0;
                offsetPosition = Vector3.zero;
            }
        }

        public void RecoilActual() {
            if (recoilIsInit) {
                recoiltT += states.delta * 3;
                if (recoiltT > 1) {
                    recoiltT = 1;
                    recoilIsInit = false;
                }

                offsetPosition = Vector3.forward;
                offsetRotation = Vector3.right * 90;

                rh_target.localPosition = basePosition + offsetRotation;
                rh_target.localEulerAngles = baseRotation + offsetRotation;
            }
        }
        #endregion
    }
}
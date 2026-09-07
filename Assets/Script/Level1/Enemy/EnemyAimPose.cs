using UnityEngine;

namespace DefenderOfIndependence.Level1
{
    [DefaultExecutionOrder(100)]
    public sealed class EnemyAimPose : MonoBehaviour
    {
        [SerializeField] private Transform leftUpperArm;
        [SerializeField] private Transform leftForearm;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightUpperArm;
        [SerializeField] private Transform rightForearm;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftGripTarget;
        [SerializeField] private Transform rightGripTarget;
        [SerializeField, Range(0f, 1f)] private float poseWeight = 0.9f;

        public bool IsConfigured => leftUpperArm != null && leftForearm != null && leftHand != null &&
                                    rightUpperArm != null && rightForearm != null && rightHand != null &&
                                    leftGripTarget != null && rightGripTarget != null;

        private void LateUpdate()
        {
            ApplyPose();
        }

        public void ApplyPose()
        {
            if (!IsConfigured)
            {
                return;
            }

            AimLimb(rightUpperArm, rightForearm, rightHand, rightGripTarget.position);
            AimLimb(leftUpperArm, leftForearm, leftHand, leftGripTarget.position);
        }

        private void AimLimb(Transform upperArm, Transform forearm, Transform hand, Vector3 target)
        {
            // A few CCD passes are enough for this simple authored aiming pose.
            // Both joints aim the hand itself at the grip marker; aiming the
            // upper arm's immediate child would incorrectly pull the elbow there.
            for (int iteration = 0; iteration < 4; iteration++)
            {
                RotateBoneToward(forearm, hand.position, target, poseWeight);
                RotateBoneToward(upperArm, hand.position, target, poseWeight * 0.8f);
            }
        }

        private static void RotateBoneToward(Transform bone, Vector3 childPosition, Vector3 target, float weight)
        {
            Vector3 currentDirection = childPosition - bone.position;
            Vector3 targetDirection = target - bone.position;
            if (currentDirection.sqrMagnitude < 0.0001f || targetDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion desired = Quaternion.FromToRotation(currentDirection, targetDirection) * bone.rotation;
            bone.rotation = Quaternion.Slerp(bone.rotation, desired, weight);
        }
    }
}

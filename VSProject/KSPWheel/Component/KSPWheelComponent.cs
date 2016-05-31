using UnityEngine;

namespace KSPWheel
{
    /// <summary>
    /// This class is a wrapper around the KSPWheelCollider class to allow for easier use while in the Unity Editor.<para/>
    /// It will merely instantiate a KSPWheelCollider object and update its internal variables with the ones entered into the Editor Inspector panel.<para/>
    /// Also includes a few display-only variables for debugging in the editor
    /// </summary>
    [AddComponentMenu("Physics/KSPWheel")]
    public class KSPWheelComponent : MonoBehaviour
    {

        #region REGION - Unity Editor Inspector Assignable Fields
        // These variables are set onto the KSPWheelCollider object when Start is called,
        // and updated during script OnValidate() to update any changed values from the editor inspector panel

        /// <summary>
        /// The rigidbody that this wheel will apply forces to and sample velocity from
        /// </summary>
        public Rigidbody rigidBody;

        public Transform steeringTransform;

        public Transform suspensionTransform;

        public Transform wheelTransform;

        /// <summary>
        /// The radius of the wheel to simulate; this is the -actual- size to simulate, not a pre-scaled value
        /// </summary>
        public float wheelRadius = 0.5f;

        /// <summary>
        /// The mass of the -wheel- in... kg? tons? NFC
        /// </summary>
        public float wheelMass = 1f;//used to simulate wheel rotational inertia for brakes and friction purposes

        /// <summary>
        /// The length of the suspension travel
        /// </summary>
        public float suspensionLength = 0.5f;

        /// <summary>
        /// The 'target' parameter for suspension; 0 = fully uncompressed, 1 = fully compressed
        /// </summary>
        public float target = 0;

        /// <summary>
        /// The maximum force the suspension will exhert, in newtons
        /// </summary>
        public float spring = 1000;

        /// <summary>
        /// The damping ratio for the suspension spring force
        /// </summary>
        public float damper = 1500;

        /// <summary>
        /// The maximum torque the motor can exhert against the wheel
        /// </summary>
        public float maxMotorTorque = 0;

        /// <summary>
        /// The maximum torque the brakes can exhert against the wheel while attempting to bring its angular velocity to zero
        /// </summary>
        public float maxBrakeTorque = 0;

        /// <summary>
        /// The maximum deflection for the steering of this wheel, in degrees
        /// </summary>
        public float maxSteerAngle = 0;

        /// <summary>
        /// Throttle/motor torque lerp speed
        /// </summary>
        public float throttleResponse = 2;

        /// <summary>
        /// Steering angle lerp speed
        /// </summary>
        public float steeringResponse = 2;

        /// <summary>
        /// Brake torque lerp speed
        /// </summary>
        public float brakeResponse = 2;

        /// <summary>
        /// The forward friction constant (rolling friction)
        /// </summary>
        public float forwardFrictionCoefficient = 1f;

        /// <summary>
        /// The sideways friction constant
        /// </summary>
        public float sideFrictionCoefficient = 1f;

        /// <summary>
        /// Global surface friction coefficient applied to both forward and sideways friction
        /// </summary>
        public float surfaceFrictionCoefficient = 1f;

        /// <summary>
        /// If should use differential motor input for steering
        /// </summary>
        public bool tankSteer = false;

        /// <summary>
        /// If this wheel should have its steering inverted; to be used on 'right side' wheels
        /// </summary>
        public bool invertSteer = false;

        /// <summary>
        /// If true, this wheel will rotate opposite for torque inputs (e.g. rpm will go negative for positive torque inputs)
        /// </summary>
        public bool invertMotor = false;

        /// <summary>
        /// If true, will use sphere-casts instead of ray-casts
        /// </summary>
        public bool sphereCast = false;

        #endregion ENDREGION - Unity Editor Inspector Assignable Fields

        // these variables are updated every fixed-tick after the wheel has been updated
        // used merely to display some info while in the editor for debugging purposes
        #region REGION - Unity Editor Display-Only Variables

        public Vector3 worldVelocity;
        public Vector3 localVelocity;
        public Vector3 totalLocalForce;
        public Vector3 hitNormal;
        public float springForce;
        public float dampForce;
        public float wheelRPM;        
        public float sLong;
        public float sLat;
        public float fLong;
        public float fLat;

        #endregion ENDREGION - Unity Editor Display Variables

        private KSPWheelCollider wheelCollider;
        
        private float curMotorTorque;
        private float curSteer;
        private float curBrakeTorque;

        public void Start()
        {
            wheelCollider = new KSPWheelCollider(gameObject, rigidBody);
            OnValidate();//manually call to set all current parameters into wheel collider object
        }

        private void sampleInput()
        {
            float left = Input.GetKey(KeyCode.A) ? -1 : 0;
            float right = Input.GetKey(KeyCode.D) ? 1 : 0;
            float fwd = Input.GetKey(KeyCode.W) ? 1 : 0;
            float rev = Input.GetKey(KeyCode.S) ? -1 : 0;
            float brakeInput = Input.GetKey(KeyCode.Space) ? 1 : 0;
            float forwardInput = fwd + rev;
            float turnInput = left + right;
            if (invertSteer) { turnInput = -turnInput; }
            if (invertMotor) { forwardInput = -forwardInput; }
            if (tankSteer)
            {
                forwardInput = forwardInput + turnInput;
                if (forwardInput > 1) { forwardInput = 1; }
                if (forwardInput < -1) { forwardInput = -1; }
            }
            curMotorTorque = Mathf.Lerp(curMotorTorque, forwardInput * maxMotorTorque, throttleResponse);
            curSteer = Mathf.Lerp(curSteer, turnInput * maxSteerAngle, steeringResponse);
            curBrakeTorque = Mathf.Lerp(curBrakeTorque, brakeInput * maxBrakeTorque, brakeResponse);
        }

        public void FixedUpdate()
        {
            sampleInput();
            wheelCollider.motorTorque = curMotorTorque;
            wheelCollider.steeringAngle = curSteer;
            wheelCollider.brakeTorque = curBrakeTorque;
            wheelCollider.updateWheel();
            if (steeringTransform != null)
            {
                steeringTransform.localRotation = Quaternion.AngleAxis(curSteer, steeringTransform.up);
            }
            if (suspensionTransform != null)
            {
                suspensionTransform.position = gameObject.transform.position - (suspensionLength - wheelCollider.compressionDistance) * gameObject.transform.up;
            }
            if (wheelTransform != null)
            {
                wheelTransform.Rotate(wheelTransform.right, wheelCollider.perFrameRotation, Space.World);
            }
            totalLocalForce = wheelCollider.calculatedForces;
            hitNormal = wheelCollider.hit.normal;
            worldVelocity = wheelCollider.worldVelocityAtHit;
            localVelocity = wheelCollider.wheelLocalVelocity;
            springForce = wheelCollider.springForce;
            dampForce = wheelCollider.dampForce;
            wheelRPM = wheelCollider.rpm;
            sLong = wheelCollider.longitudinalSlip;
            sLat = wheelCollider.lateralSlip;
            fLong = wheelCollider.longitudinalForce;
            fLat = wheelCollider.lateralForce;
        }

        public void OnValidate()
        {
            if (wheelCollider != null)
            {
                wheelCollider.radius = wheelRadius;
                wheelCollider.mass = wheelMass;
                wheelCollider.length = suspensionLength;
                wheelCollider.target = target;
                wheelCollider.spring = spring;
                wheelCollider.damper = damper;
                wheelCollider.motorTorque = maxMotorTorque;
                wheelCollider.brakeTorque = maxBrakeTorque;
                wheelCollider.forwardFrictionCoefficient = forwardFrictionCoefficient;
                wheelCollider.sideFrictionCoefficient = sideFrictionCoefficient;
                wheelCollider.sphereCast = sphereCast;
            }
        }

        /// <summary>
        /// Display a visual representation of the wheel in the editor. Unity has no inbuilt gizmo for 
        /// circles, so a sphere is used. Unlike the original WC, I've represented the wheel at top and bottom 
        /// of suspension travel
        /// </summary>
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(gameObject.transform.position, wheelRadius);
            Vector3 pos2 = gameObject.transform.position + -gameObject.transform.up * suspensionLength;
            if (wheelCollider != null) { pos2 += gameObject.transform.up * wheelCollider.compressionDistance; }
            Gizmos.DrawWireSphere(pos2, wheelRadius);
            Gizmos.DrawRay(gameObject.transform.position - gameObject.transform.up * wheelRadius, -gameObject.transform.up * suspensionLength);
        }

    }
}

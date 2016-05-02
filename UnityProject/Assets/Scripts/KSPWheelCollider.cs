using System;
using UnityEngine;

namespace KSPWheel
{
    
    public class KSPWheelCollider
    {

        #region REGION - Configuration Fields
        //Component configuration fields; not adjusted by component, but could be manipulated by other scripts

        /// <summary>
        /// The game object this script should be attached to / affect
        /// </summary>
        public GameObject wheel;

        /// <summary>
        /// The rigidbody that this wheel will apply forces to and sample velocity from
        /// </summary>
        public Rigidbody rigidBody;

        /// <summary>
        /// The radius of the wheel to simulate; this is the -actual- size to simulate, not a pre-scaled value
        /// </summary>
        public float wheelRadius;

        /// <summary>
        /// The mass of the -wheel- in... kg? tons? NFC
        /// </summary>
        public float wheelMass;//used to simulate wheel rotational inertia for brakes and friction purposes

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
        public float spring = 100;

        /// <summary>
        /// The damping ratio for the suspension spring force
        /// </summary>
        public float damper = 1;

        /// <summary>
        /// The maximum torque the motor can exhert against the wheel
        /// </summary>
        public float motorTorque = 0;

        /// <summary>
        /// The maximum torque the brakes can exhert against the wheel while attempting to bring its angular velocity to zero
        /// </summary>
        public float brakeTorque = 0;

        /// <summary>
        /// The maximum deflection for the steering of this wheel, in degrees
        /// </summary>
        public float maxSteerAngle = 0;

        /// <summary>
        /// The forward friction constant (rolling friction)
        /// </summary>
        public float fwdFrictionConst = 1f;

        /// <summary>
        /// The sideways friction constant
        /// </summary>
        public float sideFrictionConst = 1f;

        #endregion ENDREGION - Configuration Fields

        #region REGION - Public Accessible derived values

        /// <summary>
        /// The pre-calculated position that the wheel mesh should be positioned at; alternatively you can calculate the position manually given the 'compressionLength' value
        /// </summary>
        public Vector3 wheelMeshPosition;

        /// <summary>
        /// The distance that the suspension is compressed
        /// </summary>
        public float compressionDistance;

        /// <summary>
        /// The percentage of compression calculated as (compressLength / suspensionLength)
        /// </summary>
        public float compressionPercent;

        /// <summary>
        /// The final calculated force being exerted by the spring; this can be used as the 'down force' of the wheel <para/>
        /// springForce = rawSpringForce - dampForce
        /// </summary>
        public float springForce;

        /// <summary>
        /// The amount of force of the spring that is negated by the damping value
        /// </summary>
        public float dampForce;

        /// <summary>
        /// The velocity of the wheel as seen at the wheel mounting point in the local reference of the wheel collider object (the object this script is attached to)
        /// </summary>
        public Vector3 wheelMountLocalVelocity;

        /// <summary>
        /// The velocity of the wheel as seen by the surface at the point of contact
        /// </summary>
        public Vector3 wheelLocalVelocity;

        /// <summary>
        /// The velocity of the wheel in world space at the point of contact
        /// </summary>
        public Vector3 worldVelocityAtHit;

        /// <summary>
        /// The summed forces that will be/have been applied to the rigidbody at the point of contact with the surface
        /// </summary>
        public Vector3 forceToApply;

        /// <summary>
        /// At each update set to true or false depending on if the wheel is in contact with the ground
        /// </summary>
        public bool grounded;

        /// <summary>
        /// If grounded == true, this is populated with a reference to the raycast hit information
        /// </summary>
        public RaycastHit hit;

        /// <summary>
        /// The current measured RPM of the wheel; derived from down-force, motor-force, wheel mass, and previous RPM
        /// </summary>
        public float wheelRPM;

        #endregion ENDREGION - Public Accessible derived values

        #region REGION - Public editor-display variables

        public float fwdFrictionForce;
        public float sideFrictionForce;
        public float dotX;
        public float dotZ;
        public float dotY;

        public float prevCompressionDistance;
        public float springVelocity;
        public float compressionPercentInverse;

        #endregion ENDREGION - Public editor-display variables

        #region REGION - Private working variables
        public Vector3 wheelForward;
        public Vector3 wheelRight;
        public Vector3 wheelUp;
        public GameObject hitObject;
        public float currentSteerAngle;
        private float fwdInput = 0;
        private float rotInput = 0;
        private KSPFrictionCurve frictionCurve;
        #endregion ENDREGION - Private working variables

        public KSPWheelCollider(GameObject wheel, Rigidbody rigidBody)
        {
            this.wheel = wheel;
            this.rigidBody = rigidBody;
            hitObject = new GameObject("Wheelhit-" + this.wheel.name);
            hitObject.transform.parent = this.wheel.transform;
            hitObject.transform.position = this.wheel.transform.position;
            hitObject.transform.rotation = this.wheel.transform.rotation;
            frictionCurve = new KSPFrictionCurve();
        }

        public void setInputState(float fwd, float rot)
        {
            fwdInput = fwd;
            rotInput = rot;
        }

        public void setSteeringAngle(float angle)
        {
            currentSteerAngle = angle;
            rotInput = currentSteerAngle / maxSteerAngle;
        }

        /// <summary>
        /// UpdateWheel() should be called by the controlling component/container on every FixedUpdate that this wheel should apply forces for.<para/>
        /// Collider and physics integration can be disabled by simply no longer calling UpdateWheel
        /// </summary>
        public void UpdateWheel()
        {
            calculateSteeringAngle();

            float rayDistance = suspensionLength + wheelRadius;

            wheelForward = Quaternion.AngleAxis(currentSteerAngle, wheel.transform.up) * wheel.transform.forward;
            wheelUp = wheel.transform.up;
            wheelRight = Vector3.Cross(wheelForward, wheelUp);
            grounded = false;
            if (Physics.Raycast(wheel.transform.position, -wheel.transform.up, out hit, rayDistance))
            {
                grounded = true;
                prevCompressionDistance = compressionDistance;

                wheelMeshPosition = hit.point + (wheel.transform.up * wheelRadius);
                hitObject.transform.position = hit.point;
                hitObject.transform.LookAt(hit.point + wheelForward, hit.normal);

                worldVelocityAtHit = rigidBody.GetPointVelocity(hitObject.transform.position);
                wheelLocalVelocity = hitObject.transform.InverseTransformDirection(worldVelocityAtHit);//speed of the wheel over the road in the roads frame of reference            
                wheelMountLocalVelocity = wheel.transform.InverseTransformDirection(worldVelocityAtHit);//used for spring/damper 'velocity' value

                dotX = Vector3.Dot(hitObject.transform.right, rigidBody.velocity.normalized);
                dotY = Vector3.Dot(hitObject.transform.up, rigidBody.velocity.normalized);
                dotZ = Vector3.Dot(hitObject.transform.forward, rigidBody.velocity.normalized);

                compressionDistance = suspensionLength + wheelRadius - (hit.distance);
                compressionPercent = compressionDistance / suspensionLength;
                compressionPercentInverse = 1.0f - compressionPercent;

                springVelocity = compressionDistance - prevCompressionDistance;
                dampForce = damper * springVelocity;

                springForce = (compressionDistance - (suspensionLength * target)) * spring;
                springForce += dampForce;

                forceToApply = hit.normal * springForce;
                forceToApply += calculateForwardFriction(springForce) * hitObject.transform.forward;
                forceToApply += calculateSideFriction(springForce, wheelLocalVelocity.x) * hitObject.transform.right;
                forceToApply += calculateForwardInput(springForce) * hitObject.transform.forward;
                rigidBody.AddForceAtPosition(forceToApply, wheel.transform.position, ForceMode.Force);
                calculateWheelRPM(springForce);
            }
            else
            {
                springForce = dampForce = 0;
                wheelMountLocalVelocity = Vector3.zero;
                wheelLocalVelocity = Vector3.zero;
                wheelMeshPosition = wheel.transform.position + (-wheel.transform.up * suspensionLength * (1f - target));
            }
        }

        private float calculateForwardFriction(float downForce)
        {
            float friction = 0;
            friction = fwdFrictionConst * -wheelLocalVelocity.z;
            friction *= downForce;
            fwdFrictionForce = friction;
            return friction;
        }

        private float calculateSideFriction(float downForce, float slipVelocity)
        {
            float val = -frictionCurve.Evaluate(slipVelocity) * slipVelocity;
            if (val != 0)
            {
                MonoBehaviour.print("slipVal: " + val);
            }            
            return val;
        }
        
        private float calculateForwardInput(float downForce)
        {
            float fwdForce = fwdInput * motorTorque;
            return fwdForce;
        }

        //TODO
        private float calculateBrakeTorque(float downForce)
        {
            float friction = 0;

            return friction;
        }

        //TODO - use wheel mass, downforce, brake and motor input to determine actual current RPM
        private void calculateWheelRPM(float downForce)
        {
            wheelRPM = (wheelLocalVelocity.z / (wheelRadius * 2 * Mathf.PI)) * Mathf.PI;
        }

        private void calculateSteeringAngle()
        {
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, rotInput * maxSteerAngle, Time.fixedDeltaTime);
        }
    }
}
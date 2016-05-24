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
        /// The steering response speed; higher values result in more responsive steering
        /// </summary>
        public float steerLerpSpeed = 1;

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
        /// The amount of force of the spring that was negated by the damping value<para/>
        /// The 'springForce' variable already has this value applied;
        /// raw spring force (before damper) can be calculated by springForce+dampForce
        /// </summary>
        public float dampForce;

        /// <summary>
        /// The velocity of the wheel as seen at the wheel mounting point in the local reference of the wheel collider object (the object this script is attached to)<para/>
        /// This does not take into account the steering angle of the wheel
        /// </summary>
        public Vector3 wheelMountLocalVelocity;

        /// <summary>
        /// The velocity of the wheel as seen by the surface at the point of contact, taking into account steering angle and angle of the collider to the surface.
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
        /// If grounded == true, this is populated with a reference to the raycast hit information
        /// </summary>
        public RaycastHit hit;

        /// <summary>
        /// The current measured RPM of the wheel; derived from down-force, motor-force, wheel mass, and previous RPM
        /// </summary>
        public float wheelRPM;

        /// <summary>
        /// At each update set to true or false depending on if the wheel is in contact with the ground<para/>
        /// Saved persistently in the KSPWheelModule, manually restored upon instantiation of the KSPWheelCollider; this prevents erroneous callbacks on part-load.
        /// </summary>
        public bool grounded;

        #endregion ENDREGION - Public Accessible derived values

        #region REGION - Public editor-display variables
        public float fwdFrictionForce;
        public float sideFrictionForce;
        public float prevCompressionDistance;
        public float springVelocity;
        public float compressionPercentInverse;
        public Vector3 wheelForward;
        public Vector3 wheelRight;
        public Vector3 wheelUp;
        public float currentSteerAngle;
        public float sideSlip;
        #endregion ENDREGION - Public editor-display variables

        #region REGION - Private working variables
        private float fwdInput = 0;
        private float rotInput = 0;        
        private int sideStickyTimer = 0;
        private int fwdStickyTimer = 0;
        private float maxStickyVelocity = 0.25f;//TODO -- expose as a configurable field
        private ConfigurableJoint stickyJoint;
        private int raycastMask = ~(1 << 26);//cast to all layers except 26; 1<<26 sets 26 to the layer; ~inverts all bits in the mask
        private Action<Vector3> onImpactCallback;//simple blind callback for when the wheel changes from !grounded to grounded, the input variable is the wheel-local impact velocity

        private KSPFrictionCurve frictionCurve;//TODO -- currently un-used
        #endregion ENDREGION - Private working variables

        #region REGION - Public accessible methods / API methods

        public KSPWheelCollider(GameObject wheel, Rigidbody rigidBody)
        {
            this.wheel = wheel;
            this.rigidBody = rigidBody;
            frictionCurve = new KSPFrictionCurve();
        }

        public void setImpactCallback(Action<Vector3> callback) { onImpactCallback = callback; }

        /// <summary>
        /// Set the raycast layer mask
        /// </summary>
        /// <param name="mask"></param>
        public void setRaycastMask(int mask)
        {
            raycastMask = mask;
        }

        /// <summary>
        /// Set the current input state
        /// </summary>
        /// <param name="fwd"></param>
        /// <param name="rot"></param>
        public void setInputState(float fwd, float rot)
        {
            fwdInput = fwd;
            rotInput = rot;
        }

        /// <summary>
        /// Set the current steering angle explicitly, and update the internal rotInput state to match the new explicit steering angle
        /// </summary>
        /// <param name="angle"></param>
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
            wheelRight = -Vector3.Cross(wheelForward, wheelUp);
            grounded = false;
            if (Physics.Raycast(wheel.transform.position, -wheel.transform.up, out hit, rayDistance, raycastMask))
            {
                prevCompressionDistance = compressionDistance;
                wheelMeshPosition = hit.point + (wheel.transform.up * wheelRadius);
                worldVelocityAtHit = rigidBody.GetPointVelocity(hit.point);
                wheelLocalVelocity.z = Vector3.Dot(worldVelocityAtHit.normalized, wheelForward) * worldVelocityAtHit.magnitude;
                wheelLocalVelocity.x = Vector3.Dot(worldVelocityAtHit.normalized, wheelRight) * worldVelocityAtHit.magnitude;
                wheelLocalVelocity.y = Vector3.Dot(worldVelocityAtHit.normalized, wheel.transform.up) * worldVelocityAtHit.magnitude;
                wheelMountLocalVelocity = wheel.transform.InverseTransformDirection(worldVelocityAtHit);//used for spring/damper 'velocity' value
                
                if (Math.Abs(wheelLocalVelocity.x) < maxStickyVelocity) { sideStickyTimer++; }
                else { sideStickyTimer = 0; }
                if (Math.Abs(wheelLocalVelocity.z) < maxStickyVelocity) { fwdStickyTimer++; }
                else { fwdStickyTimer = 0; }
                setupStickyJoint(fwdStickyTimer, sideStickyTimer);
                
                compressionDistance = suspensionLength + wheelRadius - (hit.distance);
                compressionPercent = compressionDistance / suspensionLength;
                compressionPercentInverse = 1.0f - compressionPercent;

                springVelocity = compressionDistance - prevCompressionDistance;
                dampForce = damper * springVelocity;

                springForce = (compressionDistance - (suspensionLength * target)) * spring;
                springForce += dampForce;
                if (springForce < 0) { springForce = 0; }

                forceToApply = hit.normal * springForce;// * Vector3.Dot(hit.normal, wheel.transform.up);//spring and damper -- suspension force
                float fDot = Mathf.Abs(Vector3.Dot(wheelUp, hit.normal));
                forceToApply += calculateForwardFriction(springForce) * wheelForward * fDot;
                float wDot = 1.0f - Mathf.Abs(Vector3.Dot(wheelRight, hit.normal));
                forceToApply += calculateSideFriction(springForce) * wheelRight * wDot;
                forceToApply += calculateForwardInput(springForce) * wheelForward;
                rigidBody.AddForceAtPosition(forceToApply, wheel.transform.position, ForceMode.Force);
                calculateWheelRPM(springForce);
                if (!grounded && onImpactCallback!=null)//if was not previously grounded, call-back with grounded state
                {
                    onImpactCallback.Invoke(wheelLocalVelocity);
                }
                grounded = true;
            }
            else
            {
                grounded = false;
                //springForce = dampForce = 0;
                //prevCompressionDistance = 0;
                //compressionDistance = 0;
                //compressionPercent = 0;
                //compressionPercentInverse = 1;
                //worldVelocityAtHit = Vector3.zero;
                //wheelMountLocalVelocity = Vector3.zero;
                //wheelLocalVelocity = Vector3.zero;
                wheelMeshPosition = wheel.transform.position + (-wheel.transform.up * suspensionLength * (1f - target));
                Component.Destroy(stickyJoint);
            }
        }

        #endregion ENDREGION - Public accessible methods / API methods

        #region REGION - Private/internal update methods

        /// <summary>
        /// Per-fixed-update configuration of the rigidbody joints that are used for sticky friction and anti-punchthrough behaviour
        /// //TODO -- anti-punchthrough setup; somehow ensure the part cannot actually punch-through by using joint constraints
        /// //TODO -- how to tell if it was a punch-through or the surface was moved? Perhaps start the raycast slightly above the wheel?
        /// //TODO -- or, start at center of wheel and validate that it cannot compress suspension past (travel-radius), 
        /// //TODO -- so there will be at least (radis*1) between the wheel origin and the surface
        /// </summary>
        /// <param name="fwd"></param>
        /// <param name="side"></param>
        private void setupStickyJoint(int fwd, int side)
        {            
            if (stickyJoint == null)
            {
                stickyJoint = rigidBody.gameObject.AddComponent<ConfigurableJoint>();
                stickyJoint.anchor = wheel.transform.localPosition;
                stickyJoint.axis = Vector3.right;
                stickyJoint.secondaryAxis = Vector3.up;
                //stickyJoint.breakForce = 10f;
                //stickyJoint.breakTorque = 10f;
            }
            stickyJoint.connectedAnchor = hit.point;
            if (fwd > 5 && fwdInput==0)
            {
                stickyJoint.zMotion = ConfigurableJointMotion.Locked;
            }
            else
            {
                stickyJoint.zMotion = ConfigurableJointMotion.Free;
            }
            if (side > 5 && wheelLocalVelocity.magnitude < maxStickyVelocity)
            {
                stickyJoint.xMotion = ConfigurableJointMotion.Locked;
            }
            else
            {
                stickyJoint.xMotion = ConfigurableJointMotion.Free;
            }
        }

        //TODO use proper friction curve, wheel RPM, wheel-mass, and downforce to determine fwd friction
        private float calculateForwardFriction(float downForce)
        {
            float friction = 0;
            friction = fwdFrictionConst * -wheelLocalVelocity.z;
            friction *= downForce;
            fwdFrictionForce = friction;
            return friction;
        }
        
        //TODO use proper friction curve to determine side friction
        private float calculateSideFriction(float downForce)
        {
            Vector3 localVelocity = wheelLocalVelocity;
            sideSlip = -frictionCurve.Evaluate(localVelocity.normalized.x) * localVelocity.x * downForce * 0.00005f;
            float val = sideSlip * sideFrictionConst;

            sideSlip = val = -downForce * localVelocity.normalized.x * sideFrictionConst;            
            float sprungMass = downForce;//approximation of force->mass
            if (sprungMass > rigidBody.mass) { sprungMass = rigidBody.mass; }
            float vel = Mathf.Abs(localVelocity.x);
            if (Mathf.Abs(val) > vel * sprungMass) { val = Mathf.Sign(val) * vel * sprungMass; }
            return val;
        }
        
        //TODO roll this, brake, wheelRPM, and fwd friction all into a single method
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

        /// <summary>
        /// Updates the current steering angle from the current rotation input (-1...0...1), maxSteerAngle (config param), and steerLerpSpeed (response speed)
        /// </summary>
        private void calculateSteeringAngle()
        {
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, rotInput * maxSteerAngle, Time.fixedDeltaTime * steerLerpSpeed);
        }

        # endregion ENDREGION - Private/internal update methods
    }
}
using System;
using UnityEngine;

namespace KSPWheel
{
    
    public class KSPWheelCollider
    {

        #region REGION - Public Accessible values

        /// <summary>
        /// The game object this script should be attached to / affect, set from constructor
        /// </summary>
        public readonly GameObject wheel;

        // TODO really should be read-only; but silly KSP doesn't have RB's on the PART during MODULE initialization; needs to be delayed until first fixed update at least?
        /// <summary>
        /// The rigidbody that this wheel will apply forces to and sample velocity from, set from constructor
        /// </summary>
        public Rigidbody rigidBody;

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
        public Vector3 calculatedForces;

        /// <summary>
        /// If grounded == true, this is populated with a reference to the raycast hit information
        /// </summary>
        public RaycastHit hit;

        #endregion ENDREGION - Public Accessible values

        #region REGION - Private working variables

        private float currentWheelMass;
        private float currentWheelRadius;
        private float currentSuspenionLength;
        private float currentSuspensionTarget;
        private float currentSpring;
        private float currentDamper;
        private float currentFwdFrictionCoef;
        private float currentSideFrictionCoef;
        private float currentSurfaceFrictionCoef;
        private float currentSteerAngle;
        private float currentMotorTorque;
        private float currentBrakeTorque;
        private float currentSuspensionCompression;
        private float currentAngularVelocity;//angular velocity of wheel; rotations in radians per second
        private float currentMomentOfInertia;//moment of inertia of wheel; used for mass in acceleration calculations regarding wheel angular velocity

        private int currentRaycastMask = ~(1 << 26);//default cast to all layers except 26; 1<<26 sets 26 to the layer; ~inverts all bits in the mask (26 = KSP WheelColliderIgnore layer)

        private bool currentlyGrounded;
        private bool useSphereCast = false;

        private int sideStickyTimer = 0;
        private int fwdStickyTimer = 0;
        private float maxStickyVelocity = 0.00f;

        private Vector3 wheelForward;//cached wheel forward axis (longitudinal axis)
        private Vector3 wheelRight;//cached wheel right axis (lateral axis)
        private Vector3 wheelUp;//cached wheel up axis (suspension axis)
        private float prevSuspensionCompression;//cached value of previous suspension compression, used to determine damper value
        private float inertiaInverse;//cached inertia inverse used to eliminate division operations from per-tick update code
        private float radiusInverse;//cached radius inverse used to eliminate division operations from per-tick update code
        private float massInverse;//cached mass inverse used to eliminate division operations from per-tick update code
        private float vSpring;//linear velocity of spring in m/s, derived from prevPos - currentPos along suspension axis
        private float fSpring;//force exerted by spring this physics frame, in newtons
        private float fDamp;//force exerted by the damper this physics frame, in newto
        private float sLong;//fwd slip ratio
        private float sLat;//side slip rations
        private float fLong;//final longitudinal force calculated from friction model
        private float fLat;//final lateral force calculated from friction model

        private ConfigurableJoint stickyJoint;//the joint used for sticky friction
        private Action<Vector3> onImpactCallback;//simple blind callback for when the wheel changes from !grounded to grounded, the input variable is the wheel-local impact velocity

        private KSPWheelFrictionCurve fwdFrictionCurve;
        private KSPWheelFrictionCurve sideFrictionCurve;//current sideways friction curve

        #endregion ENDREGION - Private working variables

        #region REGION - Public accessible methods, API get/set methods

        /// <summary>
        /// Initialize a wheel-collider object for the given GameObject (the wheel collider), and the given rigidbody (the RB that the wheel-collider will apply forces to)
        /// </summary>
        /// <param name="wheel"></param>
        /// <param name="rigidBody"></param>
        public KSPWheelCollider(GameObject wheel, Rigidbody rigidBody)
        {
            this.wheel = wheel;
            if (wheel == null) { throw new NullReferenceException("Wheel game object for WheelCollider may not be null!"); }
            this.rigidBody = rigidBody;
            if (rigidBody == null) { throw new NullReferenceException("Rigidbody for wheel collider may not be null!"); }
            //default friction curves; may be set to custom curves through the get/set methods below
            sideFrictionCurve = new KSPWheelFrictionCurve(0.06f, 1.2f, 0.08f, 1.0f, 0.65f);
            fwdFrictionCurve = new KSPWheelFrictionCurve(0.06f, 1.2f, 0.08f, 1.0f, 0.65f);
        }

        public float spring
        {
            get { return currentSpring; }
            set { currentSpring = value; }
        }

        public float damper
        {
            get { return currentDamper; }
            set { currentDamper = value; }
        }

        public float length
        {
            get { return currentSuspenionLength; }
            set { currentSuspenionLength = value; }
        }

        public float target
        {
            get { return currentSuspensionTarget; }
            set { currentSuspensionTarget = value; }
        }

        public float mass
        {
            get { return currentWheelMass; }
            set
            {
                currentWheelMass = value;
                currentMomentOfInertia = currentWheelMass * currentWheelRadius * currentWheelRadius * 0.5f;
                inertiaInverse = 1.0f / currentMomentOfInertia;
                massInverse = 1.0f / currentWheelMass;
            }
        }

        public float radius
        {
            get { return currentWheelRadius; }
            set
            {
                currentWheelRadius = value;
                currentMomentOfInertia = currentWheelMass * currentWheelRadius * currentWheelRadius * 0.5f;
                inertiaInverse = 1.0f / currentMomentOfInertia;
                radiusInverse = 1.0f / currentWheelRadius;
            }
        }

        public KSPWheelFrictionCurve forwardFrictionCurve
        {
            get { return fwdFrictionCurve; }
            set { if (value != null) { fwdFrictionCurve = value; } }
        }

        public KSPWheelFrictionCurve sidewaysFrictionCurve
        {
            get { return sideFrictionCurve; }
            set { if (value != null) { sideFrictionCurve = value; } }
        }

        public float forwardFrictionCoefficient
        {
            get { return currentFwdFrictionCoef; }
            set { currentFwdFrictionCoef = value; }
        }

        public float sideFrictionCoefficient
        {
            get { return currentSideFrictionCoef; }
            set { currentSideFrictionCoef = value; }
        }

        public float surfaceFrictionCoefficient
        {
            get { return currentSurfaceFrictionCoef; }
            set { currentSurfaceFrictionCoef = value; }
        }

        /// <summary>
        /// Get/set the actual brake torque to be used for wheel velocity update/calculations.  Should always be a positive value; sign of the value will be determined dynamically. <para/>
        /// This should be the actual value used, any brake-response / anti-lock functionality should be handled in the external control module.
        /// </summary>
        public float brakeTorque
        {
            get { return currentBrakeTorque; }
            set { currentBrakeTorque = value; }
        }

        /// <summary>
        /// Get/set the current motor torque value to be applied to the wheels.  Can be negative for reversable motors / reversed wheels.<para/>
        /// Any throttle-response/etc should be calculated in the external module before setting this value.
        /// </summary>
        public float motorTorque
        {
            get { return currentMotorTorque; }
            set { currentMotorTorque = value; }
        }

        /// <summary>
        /// Get/set the current steering angle to be used by wheel friction code.<para/>
        /// Any steering-response speed should be handled externally and the value set here should be the post-lerp value;
        /// e.g. the actual current degrees of the steering
        /// </summary>
        public float steeringAngle
        {
            get { return currentSteerAngle; }
            set { currentSteerAngle = value; }
        }

        /// <summary>
        /// Return true/false if tire was grounded on the last suspension check
        /// </summary>
        public bool isGrounded
        {
            get { return currentlyGrounded; }
        }

        /// <summary>
        /// Wheel rotation in revloutions per minute
        /// </summary>
        public float rpm
        {
            //wWheel / (pi*2) * 60f
            //all values converted to constants
            get { return currentAngularVelocity * 9.549296585f; }
            set { currentAngularVelocity = value * 0.104719755f; }
        }

        /// <summary>
        /// angular velocity in radians per second
        /// </summary>
        public float angularVelocity
        {
            get { return currentAngularVelocity; }
            set { currentAngularVelocity = value; }
        }

        /// <summary>
        /// compression distance of the suspension system; 0 = max droop, max = max suspension length
        /// </summary>
        public float compressionDistance
        {
            get { return currentSuspensionCompression; }
        }

        /// <summary>
        /// Seat the reference to the wheel-impact callback.  This method will be called when the wheel first contacts the surface, passing in the wheel-local impact velocity
        /// </summary>
        /// <param name="callback"></param>
        public void setImpactCallback(Action<Vector3> callback)
        {
            onImpactCallback = callback;
        }

        /// <summary>
        /// Get/Set the current raycast layer mask to be used by the wheel-collider ray/sphere-casting.<para/>
        /// This determines which colliders will be checked against for suspension positioning/spring force calculation.
        /// </summary>
        public int raycastMask
        {
            get { return currentRaycastMask; }
            set { currentRaycastMask = value; }
        }

        /// <summary>
        /// Return the per-render-frame rotation for the wheel mesh<para/>
        /// this value can be used such as wheelMeshObject.transform.Rotate(Vector3.right, getWheelFrameRotation(), Space.Self)
        /// </summary>
        /// <returns></returns>
        public float perFrameRotation
        {
            // returns rpm * 0.16666_ * 360f * secondsPerFrame
            // degrees per frame = (rpm / 60) * 360 * secondsPerFrame
            get { return rpm * 6 * Time.deltaTime; }
        }

        public bool sphereCast
        {
            get { return useSphereCast; }
            set { useSphereCast = value; }
        }

        public float springForce
        {
            get { return fSpring; }
        }

        public float dampForce
        {
            get { return fDamp; }
        }

        public float longitudinalForce
        {
            get { return fLong; }
        }

        public float lateralForce
        {
            get { return fLat; }
        }

        public float longitudinalSlip
        {
            get { return sLong; }
        }

        public float lateralSlip
        {
            get { return sLat; }
        }

        #endregion

        #region REGION - Per-tick update methods; -must- be called every FixedUpdate that you want the value change of effect processed

        /// <summary>
        /// UpdateWheel() should be called by the controlling component/container on every FixedUpdate that this wheel should apply forces for.<para/>
        /// Collider and physics integration can be disabled by simply no longer calling UpdateWheel
        /// </summary>
        public void updateWheel()
        {
            calculatedForces = Vector3.zero;
            wheelForward = Quaternion.AngleAxis(currentSteerAngle, wheel.transform.up) * wheel.transform.forward;
            wheelUp = wheel.transform.up;
            wheelRight = -Vector3.Cross(wheelForward, wheelUp);
            integrateDriveTorque();
            prevSuspensionCompression = currentSuspensionCompression;
            bool prevGrounded = currentlyGrounded;
            float prevVSpring = vSpring;
            float prevSpringForce = fSpring;
            if (checkSuspensionContact())
            {
                currentlyGrounded = true;

                wheelLocalVelocity.z = Vector3.Dot(worldVelocityAtHit.normalized, wheelForward) * worldVelocityAtHit.magnitude;
                wheelLocalVelocity.x = Vector3.Dot(worldVelocityAtHit.normalized, wheelRight) * worldVelocityAtHit.magnitude;
                wheelLocalVelocity.y = Vector3.Dot(worldVelocityAtHit.normalized, wheel.transform.up) * worldVelocityAtHit.magnitude;
                
                vSpring = (currentSuspensionCompression - prevSuspensionCompression)/Time.fixedDeltaTime;//per second velocity
                fDamp = damper * vSpring;

                fSpring = (currentSuspensionCompression - (length * target)) * spring;
                if (fSpring < 0) { fSpring = 0; }//if spring would be negative at this point, zero it to allow the damper to still function; this normally occurs when target > 0, at the lower end of wheel droop
                fSpring += fDamp;
                if (fSpring < 0) { fSpring = 0; }//if final spring value is negative, zero it out; negative springs are not possible without attachment to the ground; gravity is our negative spring :)

                //TODO actual sprung mass can be derived (mostly?) by the delta between current and prev spring velocity, and the previous spring force (e.g. the previous spring (F) force effected (A) change in velocity, thus the mass must by (M))
                //TODO this 'mass' is important to know because it is needed to derive proper maximum bound for sideways friction

                updateStickyJoint(fSpring);

                calculatedForces += hit.normal * fSpring;
                calcFriction(fSpring);
                calculatedForces += fLong * wheelForward;
                calculatedForces += fLat * wheelRight;
                rigidBody.AddForceAtPosition(calculatedForces, wheel.transform.position, ForceMode.Force);
                if (!prevGrounded && onImpactCallback!=null)//if was not previously grounded, call-back with impact data
                {
                    onImpactCallback.Invoke(wheelLocalVelocity);
                }
            }
            else
            {
                currentlyGrounded = false;
                fSpring = fDamp = 0;
                prevSuspensionCompression = 0;
                currentSuspensionCompression = 0;
                worldVelocityAtHit = Vector3.zero;
                wheelLocalVelocity = Vector3.zero;
                Component.Destroy(stickyJoint);
            }
            integrateBrakeTorque();
        }

        #endregion ENDREGION - Public accessible methods / API methods

        #region REGION - Private/internal update methods

        private void integrateDriveTorque()
        {
            //motor torque is easy to integrate into angular velocity; it is simply (torque / momentOfInertia) * deltaTime
            currentAngularVelocity += currentMotorTorque * inertiaInverse * Time.fixedDeltaTime;//acceleration is in radians/second; only operating on 1 * fixedDeltaTime seconds, so only update for that length of time
        }

        private void integrateBrakeTorque()
        {
            //maximum torque exerted by brakes onto wheel this FixedUpdate frame
            float wBrake = currentBrakeTorque * inertiaInverse * Time.fixedDeltaTime;
            //clamp the actual value to the current angular velocity of the wheel
            if (wBrake > Mathf.Abs(currentAngularVelocity))
            {
                wBrake = Mathf.Abs(currentAngularVelocity);
            }
            //sign it opposite of current wheel spin direction
            wBrake *= -Mathf.Sign(currentAngularVelocity);
            //and finally, integrate it into wheel angular velocity
            currentAngularVelocity += wBrake;
        }

        /// <summary>
        /// Per-fixed-update configuration of the rigidbody joints that are used for sticky friction and anti-punchthrough behaviour
        /// //TODO -- anti-punchthrough setup; somehow ensure the part cannot actually punch-through by using joint constraints
        /// //TODO -- how to tell if it was a punch-through or the surface was moved? Perhaps start the raycast slightly above the wheel?
        /// //TODO -- or, start at center of wheel and validate that it cannot compress suspension past (travel-radius), 
        /// //TODO -- so there will be at least (radius*1) between the wheel origin and the surface
        /// </summary>
        /// <param name="fwd"></param>
        /// <param name="side"></param>
        private void updateStickyJoint(float downForce)
        {
            if (stickyJoint == null)
            {
                stickyJoint = rigidBody.gameObject.AddComponent<ConfigurableJoint>();
                stickyJoint.anchor = wheel.transform.localPosition;
                stickyJoint.axis = Vector3.right;
                stickyJoint.secondaryAxis = Vector3.up;
            }
            stickyJoint.connectedAnchor = hit.point;
            //stickyJoint.breakForce = downForce;
            //stickyJoint.breakTorque = downForce;

            if (Math.Abs(wheelLocalVelocity.x) < maxStickyVelocity) { sideStickyTimer++; }
            else { sideStickyTimer = 0; }
            if (Math.Abs(wheelLocalVelocity.z) < maxStickyVelocity) { fwdStickyTimer++; }
            else { fwdStickyTimer = 0; }


            //below were tests of using the sticky joint for suspension as well; sadly I was unable to figure out usable settings for it (it liked to bounce and oscillate)
            //stickyJoint.targetPosition = Vector3.up * (suspensionLength - target);
            //SoftJointLimitSpring sjls = new SoftJointLimitSpring();
            //sjls.spring = spring*0.5f;
            //sjls.damper = damper;
            //stickyJoint.linearLimitSpring = sjls;

            //JointDrive jd = new JointDrive();
            //jd.mode = JointDriveMode.Position;
            //jd.positionSpring = spring*0.5f;
            //jd.positionDamper = damper;
            //jd.maximumForce = float.PositiveInfinity;
            //stickyJoint.yDrive = jd;

            if (fwdStickyTimer > 5 && currentMotorTorque==0)
            {
                stickyJoint.zMotion = ConfigurableJointMotion.Locked;
            }
            else
            {
                stickyJoint.zMotion = ConfigurableJointMotion.Free;
            }
            if (sideStickyTimer > 5)
            {
                stickyJoint.xMotion = ConfigurableJointMotion.Locked;
            }
            else
            {
                stickyJoint.xMotion = ConfigurableJointMotion.Free;
            }
        }

        private bool checkSuspensionContact()
        {
            if (useSphereCast) { return spherecastSuspension(); }
            return raycastSuspension();
        }

        private bool raycastSuspension()
        {
            if (Physics.Raycast(wheel.transform.position, -wheel.transform.up, out hit, length + radius, currentRaycastMask))
            {
                worldVelocityAtHit = rigidBody.GetPointVelocity(hit.point);
                currentSuspensionCompression = length + radius - hit.distance;
                return true;
            }
            return false;            
        }

        private bool spherecastSuspension()
        {
            if (Physics.SphereCast(wheel.transform.position + wheel.transform.up*radius, radius, -wheel.transform.up, out hit, length + radius, currentRaycastMask))
            {
                currentSuspensionCompression = length - hit.distance + radius;
                Vector3 hitPos = wheel.transform.position - (length - currentSuspensionCompression) * wheel.transform.up - wheel.transform.up * radius;
                worldVelocityAtHit = rigidBody.GetPointVelocity(hitPos);
                return true;
            }
            return false;
        }
        
        #region REGION - Friction model calculations methods based on : http://www.asawicki.info/Mirror/Car%20Physics%20for%20Games/Car%20Physics%20for%20Games.html

        /// <summary>
        /// Working, but incomplete; fwd traction input/output needs attention
        /// </summary>
        /// <param name="downForce"></param>
        private void calcFriction(float downForce)
        {
            //long velocity
            float vLong = wheelLocalVelocity.z;
            //lat velocity
            float vLat = wheelLocalVelocity.x;
            //linear velocity of wheel
            float vWheel = currentAngularVelocity * currentWheelRadius;
            //long slip ratio
            sLong = calcLongSlip(vLong, vWheel);
            //lat slip ratio
            sLat = calcLatSlip(vLong, vLat);
            //raw max longitudinal force based purely on the slip ratio
            float fLongMax = fwdFrictionCurve.evaluate(sLong) * downForce * currentFwdFrictionCoef * currentSurfaceFrictionCoef;
            //raw max lateral force based purely on the slip ratio
            float fLatMax = sideFrictionCurve.evaluate(sLat) * downForce * currentSideFrictionCoef * currentSurfaceFrictionCoef;
            
            // 'limited' lateral force
            // TODO - this should actually be limited by the amount of force necessary to arrest the velocity of this wheel in this frame
            // so limit max should be (abs(vLat) * sprungMass) / Time.fixedDeltaTime  (in newtons)
            fLat = fLatMax;
            if (fLat > Mathf.Abs(vLat) * downForce * 2f) { fLat = Mathf.Abs(vLat) * downForce * 2f; }
            fLat *= -Mathf.Sign(vLat);// sign it opposite to the current vLat

            //linear velocity delta between wheel and surface in meters per second
            float vDelta = vWheel - vLong;
            //angular velocity delta between wheel and surface in radians per second; radius inverse used to avoid div operations
            float wDelta = vDelta * radiusInverse;
            //amount of torque needed to bring wheel to surface speed over one second
            float tDelta = wDelta * currentMomentOfInertia;
            //newtons of force needed to bring wheel to surface speed over one second; radius inverse used to avoid div operations
            // float fDelta = tDelta * radiusInverse; // unused
            //absolute value of the torque needed to bring the wheel to road speed instantaneously/this frame
            float tTractMax = Mathf.Abs(tDelta) / Time.fixedDeltaTime;

            //newtons needed to bring wheel to ground velocity this frame; radius inverse used to avoid div operations
            float fTractMax = tTractMax * radiusInverse;
            //final maximum force value is the smallest of the two force values;
            // if fTractMax is used the wheel will be brought to surface velocity,
            // otherwise fLongMax is used and the wheel is still slipping
            fTractMax = Mathf.Min(fTractMax, fLongMax);
            // convert the clamped traction value into a torque value and apply to the wheel
            float tractionTorque = fTractMax * currentWheelRadius * -Mathf.Sign(vDelta);
            // and set the longitudinal force to the force calculated for the wheel/surface torque
            fLong = fTractMax * Mathf.Sign(vDelta);
            //use wheel inertia to determine final wheel acceleration from torques; inertia inverse used to avoid div operations
            float angularAcceleration = tractionTorque * inertiaInverse;
            //apply acceleration to wheel for the current physics time frame, as wAccel is in radians/second (as is current angular velocity)
            currentAngularVelocity += angularAcceleration*Time.fixedDeltaTime;
        }

        /// <summary>
        /// Returns a slip ratio between 0 and 1, 0 being no slip, 1 being lots of slip
        /// </summary>
        /// <param name="vLong"></param>
        /// <param name="vWheel"></param>
        /// <returns></returns>
        private float calcLongSlip(float vLong, float vWheel)
        {
            float sLong = 0;
            if(vLong==0 && vWheel == 0) { return 0f; }//no slip present
            float a = Mathf.Max(vLong, vWheel);
            float b = Mathf.Min(vLong, vWheel);
            sLong = (a - b) / Mathf.Abs(a);
            sLong = Mathf.Clamp(sLong, 0, 1);
            return sLong;
        }

        /// <summary>
        /// Returns a slip ratio between 0 and 1, 0 being no slip, 1 being lots of slip
        /// </summary>
        /// <param name="vLong"></param>
        /// <param name="vLat"></param>
        /// <returns></returns>
        private float calcLatSlip(float vLong, float vLat)
        {
            float sLat = 0;
            if (vLat == 0)//vLat = 0, so there can be no sideways slip
            {
                return 0f;
            }
            else if (vLong == 0)//vLat!=0, but vLong==0, so all slip is sideways
            {
                return 1f;
            }
            sLat = Mathf.Abs(Mathf.Atan(vLat / vLong));//radians
            sLat = sLat * Mathf.Rad2Deg;//degrees
            sLat = sLat / 90f;//percentage (0 - 1)
            return sLat;
        }

        //TODO hmmm... not sure if this will work without knowing the velocity contribution from gravity;
        // in the absence of gravity, the sprung mass = force / acceleration
        private void calcSprungMass(float prevVSpring, float vSpring, float prevFSpring)
        {
            float vDelta = prevVSpring - vSpring;
        }

        #endregion ENDREGION - Friction calculations methods based on alternate source: 
        
        #endregion ENDREGION - Private/internal update methods

    }
}
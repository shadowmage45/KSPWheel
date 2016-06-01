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

        // TODO really should be read-only, being grabbed from wheel game object when collider is initialized
        // but silly KSP doesn't have RB's on the PART during MODULE initialization; needs to be delayed until first fixed update at least?
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
        /// The summed forces that have been applied to the rigidbody at the point of contact with the surface this frame
        /// </summary>
        public Vector3 calculatedForces;

        /// <summary>
        /// If grounded == true, this is populated with a reference to the raycast hit information
        /// </summary>
        public RaycastHit hit;

        #endregion ENDREGION - Public Accessible values

        #region REGION - Private variables

        //most of these vars should be self documenting -,-
        private float currentWheelMass = 1f;
        private float currentWheelRadius = 0.5f;
        private float currentSuspenionLength = 1f;
        private float currentSuspensionTarget = 0f;
        private float currentSpring = 10f;
        private float currentDamper = 2f;
        private float currentFwdFrictionCoef = 1f;
        private float currentSideFrictionCoef = 1f;
        private float currentSurfaceFrictionCoef = 1f;
        private float currentSteerAngle = 0f;
        private float currentMotorTorque = 0f;
        private float currentBrakeTorque = 0f;
        private float currentSuspensionCompression = 0f;
        private float currentAngularVelocity = 0f;//angular velocity of wheel; rotations in radians per second
        private float currentMomentOfInertia = 1.0f*0.5f*0.5f*0.5f;//moment of inertia of wheel; used for mass in acceleration calculations regarding wheel angular velocity.  MOI of a solid cylinder = ((m*r*r)/2)
        private int currentRaycastMask = ~(1 << 26);//default cast to all layers except 26; 1<<26 sets 26 to the layer; ~inverts all bits in the mask (26 = KSP WheelColliderIgnore layer)
        private bool currentlyGrounded = false;
        private bool useSphereCast = false;

        //sticky-friction vars;
        //TODO -- add get/set methods for these to expose them for configuration
        //TODO -- finish implementing sticky friction stuff =\
        private float maxStickyVelocity = 0.00f;
        private float sideStickyTimeMax = 0.25f;
        private float fwdStickyTimeMax = 0.25f;

        //cached per-update variables
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
        private float sideStickyTimer = 0;
        private float fwdStickyTimer = 0;

        //run-time references to various objects
        private ConfigurableJoint stickyJoint;//the joint used for sticky friction
        private Action<Vector3> onImpactCallback;//simple blind callback for when the wheel changes from !grounded to grounded, the input variable is the wheel-local impact velocity

        private KSPWheelFrictionCurve fwdFrictionCurve;
        private KSPWheelFrictionCurve sideFrictionCurve;//current sideways friction curve

        #endregion ENDREGION - Private variables

        #region REGION - Public accessible methods, API get/set methods

        /// <summary>
        /// Initialize a wheel-collider object for the given GameObject (the wheel collider), and the given rigidbody (the RB that the wheel-collider will apply forces to)<para/>
        /// -Both- must be valid references (i.e. cannot be null)
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

        /// <summary>
        /// Get/Set the current spring stiffness value.  This is the configurable value that influences the 'springForce' used in suspension calculations
        /// </summary>
        public float spring
        {
            get { return currentSpring; }
            set { currentSpring = value; }
        }

        /// <summary>
        /// Get/Set the current damper resistance value.  This is the configurable value that influences the 'dampForce' used in suspension calculations
        /// </summary>
        public float damper
        {
            get { return currentDamper; }
            set { currentDamper = value; }
        }

        /// <summary>
        /// Get/Set the current length of the suspension.  This is a ray that extends from the bottom of the wheel as positioned at the wheel collider
        /// </summary>
        public float length
        {
            get { return currentSuspenionLength; }
            set { currentSuspenionLength = value; }
        }

        /// <summary>
        /// Get/Set the current target value.  This is a 0-1 value that determines how far up the suspension the wheel should be kept. Below this point there is no spring force, only damper forces.
        /// </summary>
        public float target
        {
            get { return currentSuspensionTarget; }
            set { currentSuspensionTarget = value; }
        }

        /// <summary>
        /// Get/Set the current wheel mass.  This determines wheel acceleration from torque (not vehicle acceleration; that is determined by down-force).  Lighter wheels will slip easier from brake and motor torque.
        /// </summary>
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

        /// <summary>
        /// Get/Set the wheel radius.  This determines the simulated size of the wheel, and along with mass determines the wheel moment-of-inertia which plays into wheel acceleration
        /// </summary>
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

        /// <summary>
        /// Get/Set the current forward friction curve.  This determines the maximum available traction force for a given slip ratio.  See the KSPWheelFrictionCurve class for more info.
        /// </summary>
        public KSPWheelFrictionCurve forwardFrictionCurve
        {
            get { return fwdFrictionCurve; }
            set { if (value != null) { fwdFrictionCurve = value; } }
        }

        /// <summary>
        /// Get/Set the current sideways friction curve.  This determines the maximum available traction force for a given slip ratio.  See the KSPWheelFrictionCurve class for more info.
        /// </summary>
        public KSPWheelFrictionCurve sidewaysFrictionCurve
        {
            get { return sideFrictionCurve; }
            set { if (value != null) { sideFrictionCurve = value; } }
        }

        /// <summary>
        /// Get/set the current forward friction coefficient; this is a direct multiple to the maximum available traction/force from forward friction<para/>
        /// Higher values denote more friction, greater traction, and less slip
        /// </summary>
        public float forwardFrictionCoefficient
        {
            get { return currentFwdFrictionCoef; }
            set { currentFwdFrictionCoef = value; }
        }

        /// <summary>
        /// Get/set the current sideways friction coefficient; this is a direct multiple to the maximum available traction/force from sideways friction<para/>
        /// Higher values denote more friction, greater traction, and less slip
        /// </summary>
        public float sideFrictionCoefficient
        {
            get { return currentSideFrictionCoef; }
            set { currentSideFrictionCoef = value; }
        }

        /// <summary>
        /// Get/set the current surface friction coefficient; this is a direct multiple to the maximum available traction for both forwards and sideways friction calculations<para/>
        /// Higher values denote more friction, greater traction, and less slip
        /// </summary>
        public float surfaceFrictionCoefficient
        {
            get { return currentSurfaceFrictionCoef; }
            set { currentSurfaceFrictionCoef = value; }
        }

        /// <summary>
        /// Get/set the actual brake torque to be used for wheel velocity update/calculations.  Should always be a positive value; sign of the value will be determined dynamically. <para/>
        /// Any braking-response speed should be calculated in the external module before setting this value.
        /// </summary>
        public float brakeTorque
        {
            get { return currentBrakeTorque; }
            set { currentBrakeTorque = Mathf.Abs(value); }
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
        /// Any steering-response speed should be calculated in the external module before setting this value.
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
        /// Wheel rotation in revloutions per minute, linked to angular velocity (changing one changes the other)
        /// </summary>
        public float rpm
        {
            // wWheel / (pi*2) * 60f
            // all values converted to combined constants
            get { return currentAngularVelocity * 9.549296585f; }
            set { currentAngularVelocity = value * 0.104719755f; }
        }

        /// <summary>
        /// angular velocity in radians per second, linked to rpm (changing one changes the other)
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
        /// Seat the reference to the wheel-impact callback.  This method will be called when the wheel first contacts the surface, passing in the wheel-local impact velocity (impact force is unknown)
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

        /// <summary>
        /// Get/set toggle for if should use sphere-cast or raycast; can be toggled at runtime/inbetween updates
        /// </summary>
        public bool sphereCast
        {
            get { return useSphereCast; }
            set { useSphereCast = value; }
        }

        /// <summary>
        /// Returns the last calculated value for spring force, in newtons; this is the force that is exerted on rigidoby along suspension axis<para/>
        /// This already has dampForce applied to it; for raw spring force = springForce-dampForce
        /// </summary>
        public float springForce
        {
            get { return fSpring; }
        }

        /// <summary>
        /// Returns the last calculated value for damper force, in newtons
        /// </summary>
        public float dampForce
        {
            get { return fDamp; }
        }

        /// <summary>
        /// Returns the last calculated longitudinal (forwards) force exerted by the wheel on the rigidbody
        /// </summary>
        public float longitudinalForce
        {
            get { return fLong; }
        }

        /// <summary>
        /// Returns the last calculated lateral (sideways) force exerted by the wheel on the rigidbody
        /// </summary>
        public float lateralForce
        {
            get { return fLat; }
        }

        /// <summary>
        /// Returns the last caclulated longitudinal slip ratio; this is basically (vWheelDelta-vLong)/vLong with some error checking, clamped to a 0-1 value; does not infer slip direction, merely the ratio
        /// </summary>
        public float longitudinalSlip
        {
            get { return sLong; }
        }

        /// <summary>
        /// Returns the last caclulated lateral slip ratio; this is basically vLat/vLong with some error checking, clamped to a 0-1 value; does not infer slip direction, merely the ratio
        /// </summary>
        public float lateralSlip
        {
            get { return sLat; }
        }

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
            prevSuspensionCompression = currentSuspensionCompression;
            bool prevGrounded = currentlyGrounded;
            if (checkSuspensionContact())//suspension compression is updated in the suspension contact check
            {
                currentlyGrounded = true;
                float mag = worldVelocityAtHit.magnitude;
                wheelLocalVelocity.z = Vector3.Dot(worldVelocityAtHit.normalized, wheelForward) * mag;
                wheelLocalVelocity.x = Vector3.Dot(worldVelocityAtHit.normalized, wheelRight) * mag;
                wheelLocalVelocity.y = Vector3.Dot(worldVelocityAtHit.normalized, wheel.transform.up) * mag;

                //calculate damper force from the current compression velocity of the spring; damp force can be negative
                vSpring = (currentSuspensionCompression - prevSuspensionCompression) / Time.fixedDeltaTime;//per second velocity
                fDamp = damper * vSpring;

                //calculate spring force basically from displacement * spring
                fSpring = (currentSuspensionCompression - (length * target)) * spring;
                //if spring would be negative at this point, zero it to allow the damper to still function; this normally occurs when target > 0, at the lower end of wheel droop below target position
                if (fSpring < 0) { fSpring = 0; }
                //integrate damper value into spring force
                fSpring += fDamp;
                //if final spring value is negative, zero it out; negative springs are not possible without attachment to the ground; gravity is our negative spring :)
                if (fSpring < 0) { fSpring = 0; }

                integrateForces();
                updateStickyJoint(fSpring);
                if (!prevGrounded && onImpactCallback != null)//if was not previously grounded, call-back with impact data
                {
                    onImpactCallback.Invoke(wheelLocalVelocity);
                }
            }
            else
            {
                integrateUngroundedTorques();
                currentlyGrounded = false;
                fSpring = fDamp = 0;
                prevSuspensionCompression = 0;
                currentSuspensionCompression = 0;
                worldVelocityAtHit = Vector3.zero;
                wheelLocalVelocity = Vector3.zero;
                Component.Destroy(stickyJoint);
            }
        }

        #endregion ENDREGION - Public accessible methods, API get/set methods

        #region REGION - Private/internal update methods

        /// <summary>
        /// Integrate the torques and forces for a grounded wheel, using the pre-calculated fSpring downforce value.
        /// </summary>
        private void integrateForces()
        {
            calcFriction(fSpring);
            calculatedForces += hit.normal * fSpring;
            calculatedForces += fLong * wheelForward;
            calculatedForces += fLat * wheelRight;
            rigidBody.AddForceAtPosition(calculatedForces, hit.point, ForceMode.Force);
            if (hit.collider.attachedRigidbody != null && !hit.collider.attachedRigidbody.isKinematic)
            {
                hit.collider.attachedRigidbody.AddForceAtPosition(-calculatedForces, hit.point, ForceMode.Force);
            }
        }

        /// <summary>
        /// Integrate drive and brake torques into wheel velocity for when -not- grounded.
        /// This allows for wheels to change velocity from user input while the vehicle is not in contact with the surface.
        /// Not-yet-implemented are torques on the rigidbody due to wheel accelerations.
        /// </summary>
        private void integrateUngroundedTorques()
        {
            //velocity change due to motor; if brakes are engaged they can cancel this out the same tick
            //acceleration is in radians/second; only operating on fixedDeltaTime seconds, so only update for that length of time
            currentAngularVelocity += currentMotorTorque * inertiaInverse * Time.fixedDeltaTime;
            // maximum torque exerted by brakes onto wheel this frame
            float wBrake = currentBrakeTorque * inertiaInverse * Time.fixedDeltaTime;
            // clamp the max brake angular change to the current angular velocity
            wBrake = Mathf.Min(Mathf.Abs(currentAngularVelocity), wBrake);
            // sign it opposite of current wheel spin direction
            // and finally, integrate it into wheel angular velocity
            currentAngularVelocity += wBrake * -Mathf.Sign(currentAngularVelocity);
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
            stickyJoint.breakForce = downForce;
            stickyJoint.breakTorque = downForce;

            if (Math.Abs(wheelLocalVelocity.z) < maxStickyVelocity && currentMotorTorque == 0) { fwdStickyTimer+=Time.fixedDeltaTime; }
            else { fwdStickyTimer = 0; }

            if (Math.Abs(wheelLocalVelocity.x) < maxStickyVelocity) { sideStickyTimer+=Time.fixedDeltaTime; }
            else { sideStickyTimer = 0; }

            if (fwdStickyTimer > fwdStickyTimeMax)
            {
                stickyJoint.zMotion = ConfigurableJointMotion.Locked;
            }
            else
            {
                stickyJoint.zMotion = ConfigurableJointMotion.Free;
            }
            if (sideStickyTimer > sideStickyTimeMax)
            {
                stickyJoint.xMotion = ConfigurableJointMotion.Locked;
            }
            else
            {
                stickyJoint.xMotion = ConfigurableJointMotion.Free;
            }
        }

        /// <summary>
        /// Uses either ray- or sphere-cast to check for suspension contact with the ground, calculates current suspension compression, and caches the world-velocity at the contact point
        /// </summary>
        /// <returns></returns>
        private bool checkSuspensionContact()
        {
            if (useSphereCast) { return spherecastSuspension(); }
            return raycastSuspension();
        }

        /// <summary>
        /// Check suspension contact using a ray-cast; return true/false for if contact was detected
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Check suspension contact using a sphere-cast; return true/false for if contact was detected.
        /// </summary>
        /// <returns></returns>
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
            //initial motor/brake torque integration, brakes integrated further after friction applied
            currentAngularVelocity += currentMotorTorque * inertiaInverse * Time.fixedDeltaTime;//acceleration is in radians/second; only operating on 1 * fixedDeltaTime seconds, so only update for that length of time
            // maximum torque exerted by brakes onto wheel this frame
            float wBrakeMax = currentBrakeTorque * inertiaInverse * Time.fixedDeltaTime;
            // clamp the max brake angular change to the current angular velocity
            float wBrake = Mathf.Min(Mathf.Abs(currentAngularVelocity), wBrakeMax);
            // sign it opposite of current wheel spin direction
            // and finally, integrate it into wheel angular velocity
            currentAngularVelocity += wBrake * -Mathf.Sign(currentAngularVelocity);
            // this is the remaining brake torque that can be used to counteract acceleration caused by traction friction
            float wBrakeDelta = wBrakeMax - wBrake;

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

            //TODO actual sprung mass can be derived (mostly?) by the delta between current and prev spring velocity
            // and the previous spring force (e.g. the previous spring (F) force effected (A) change in velocity, thus the mass must by (M))
            // this 'mass' is important to know because it is needed to derive proper maximum bound for sideways friction
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
            //use wheel inertia to determine final wheel acceleration from torques; inertia inverse used to avoid div operations; convert to delta-time, as accel is normally radians/s
            float angularAcceleration = tractionTorque * inertiaInverse * Time.fixedDeltaTime;
            //apply acceleration to wheel angular velocity
            currentAngularVelocity += angularAcceleration;
            //second integration pass of brakes, to allow for locked-wheels after friction calculation
            if (Mathf.Abs(currentAngularVelocity) < wBrakeDelta) { currentAngularVelocity = 0; }//brakes have locked up the tire
            else { currentAngularVelocity += -Mathf.Sign(currentAngularVelocity) * wBrakeDelta; }//
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
        // else mass = force / (acceleration - gravity)
        private void calcSprungMass(float prevVSpring, float vSpring, float vGrav, float prevFSpring)
        {
            float vDelta = prevVSpring - vSpring - vGrav;
            float sprungMass = prevFSpring / vDelta;
        }

        #endregion ENDREGION - Friction calculations methods based on alternate source: 
        
        #endregion ENDREGION - Private/internal update methods

    }
}
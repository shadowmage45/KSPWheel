using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    /// <summary>
    /// A replacement for the stock wheel system that uses the KSPWheelCollider class for phsyics handling.
    /// Intended to be a fully-functional (but possibly not fully-equivalent) replacement for the stock wheel modules and U5 WheelCollider component
    /// </summary>
    public class KSPWheelModuleKF : PartModule
    {
        #region REGION - Basic wheel parameters that cannot be pulled from the U5 WheelCollider component

        /// <summary>
        /// Name of the transform that the wheel collider component should be attached to/manipulate.
        /// </summary>
        [KSPField]
        public string wheelColliderName;

        /// <summary>
        /// Name of the transform that should be rotated around its X axis for wheel rotation<para/>
        /// May be null if no transform should be rotated.  Will accept CSV list if multiple wheels should be animated.
        /// </summary>
        [KSPField]
        public string wheelPivotName;

        /// <summary>
        /// Name of the mesh that represents the 'non-busted wheel'<para/>
        /// May be null if no wheel mesh exists or should be manipulated
        /// </summary>
        [KSPField]
        public string wheelName;

        /// <summary>
        /// Name of the transform that should be moved for suspension<para/>
        /// May be null if there is no visual response to suspension forces for the wheel (repulsors)
        /// </summary>
        [KSPField]
        public string suspensionName;

        /// <summary>
        /// Name of the transform used for steering rotation.  This transform will be rotated around its Y axis, with the default orientation of the model representing straight forward<para/>
        /// May be null if there is no steering on the model; may re-use the suspension transform for steering transform if model is setup in such a manner.
        /// </summary>
        [KSPField]
        public string steeringName;


        /// <summary>
        /// Determines if this wheel should use tank-steering.  This will adjust the fwd/reverse input for steering input rather than manipulating the orientation of the wheel.
        /// </summary>
        [KSPField]
        public bool tankSteering;

        /// <summary>
        /// Determines the max impact velocity that this parts suspension can withstand; impacts above this velocity will result in part destruction;
        /// this is checked anytime the wheel transitions from a non-grounded to a grounded state
        /// </summary>
        [KSPField]
        public float impactTolerance;

        [KSPField]
        public int raycastMask = ~(1 << 26 | 1 << 10);//ignore layers 26 and 10 (wheelCollidersIgnore & scaledScenery)

        [KSPField]
        public string boundsColliderName;

        /// <summary>
        /// If true, steering will be inverted for this wheel.  Toggleable in editor and flight.  Persistent.
        /// </summary>
        [KSPField(guiName ="Invert Steering", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true)]
        public bool invertSteering;

        /// <summary>
        /// If true the steering will be locked to zero and will not respond to steering input.
        /// </summary>
        [KSPField(guiName = "Steering Lock", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Locked", disabledText = "Free", suppressEditorShipModified = true)]
        public bool steeringLocked;

        /// <summary>
        /// If true, motor response will be inverted for this wheel.  Toggleable in editor and flight.  Persistent.
        /// </summary>
        [KSPField(guiName = "Invert Motor", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true)]
        public bool invertMotor;

        /// <summary>
        /// If true, motor response will be inverted for this wheel.  Toggleable in editor and flight.  Persistent.
        /// </summary>
        [KSPField(guiName = "Motor Lock", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_Toggle(enabledText = "Locked", disabledText = "Enabled", suppressEditorShipModified = true)]
        public bool motorLocked;

        #endregion

        #region REGION - Optional wheel parameters that may vary on a part to part basis

        [KSPField]
        public float maxSteeringAngle;

        [KSPField(guiActive = true, guiActiveEditor = true),
         UI_FloatRange(suppressEditorShipModified = true, minValue = -2f, maxValue = 2f, stepIncrement = 0.25f)]
        public float suspensionOffset = 0f;
        
        [KSPField]
        public Vector3 suspensionAxis = Vector3.up;
        [KSPField]
        public Vector3 steeringAxis = Vector3.up;

        [KSPField]
        public float minLoadRating = 0.05f;

        [KSPField]
        public float maxLoadRating = 5f;

        [KSPField]
        public bool brakesLocked = false;

        [KSPField]
        public float throttleResponse = 2f;
        [KSPField]
        public float brakeResponse = 2f;
        [KSPField]
        public float steeringResponse = 10f;
        [KSPField]
        public float maxRPM = 600f;
        [KSPField]
        public float resourceAmount = 1f;
        #endregion

        #region REGION - Optional wheel parameters that may be loaded from the WheelCollider if present
        [KSPField(guiName ="Radius", guiActive =true),
         UI_FloatRange(suppressEditorShipModified = true, minValue = 0.025f, maxValue = 1f, stepIncrement = 0.025f)]
        public float wheelRadius = -1;

        [KSPField]
        public float wheelMass = -1;

        [KSPField(guiName = "Length", guiActive = true),
         UI_FloatRange(suppressEditorShipModified = true, minValue = 0.025f, maxValue = 2f, stepIncrement = 0.025f)]
        public float suspensionTravel = -1;

        [KSPField]
        public float suspensionTarget = -1;

        [KSPField]
        public float suspensionSpring = -1;

        [KSPField]
        public float suspensionDamper = -1;

        [KSPField(guiName ="Motor Torque", guiActive = true, guiActiveEditor = true),
         UI_FloatRange(minValue =0, maxValue = 100, stepIncrement = 0.5f)]
        public float maxMotorTorque = -1;

        [KSPField]
        public float maxBrakeTorque = -1;
        #endregion

        #region REGION - Animation handling
        [KSPField]
        public string animationName = String.Empty;
        [KSPField]
        public float animationSpeed=1;
        [KSPField]
        public int animationLayer=1;
        #endregion

        #region REGION - Persistent data
        [KSPField(isPersistant = true)]
        public string persistentState = KSPWheelState.DEPLOYED.ToString();

        [KSPField(isPersistant = true)]
        public bool grounded = false;
        #endregion

        #region REGION - Private working/cached variables
        private Transform wheelColliderTransform;//the transform that the wheel-collider is attached to
        private Transform[] wheelPivotTransforms;//
        private Transform wheelMesh;
        private Transform suspensionMesh;
        private Transform steeringMesh;
        private Vector3 suspensionDefaultLocation;

        private KSPWheelCollider wheel;
        private KSPWheelState wheelState = KSPWheelState.DEPLOYED;
        #endregion

        #region REGION - Debug fields

        [KSPField(guiName = "SpringMult", guiActive = true),
         UI_FloatRange(minValue = 0.05f, maxValue = 10, stepIncrement = 0.05f, suppressEditorShipModified =true)]
        public float springMult = 1f;

        [KSPField(guiName = "DampMult", guiActive = true),
         UI_FloatRange(minValue = 0.05f, maxValue = 10, stepIncrement = 0.05f, suppressEditorShipModified =true)]
        public float dampMult = 1f;

        //TODO -- implement dynamic load-setting; min and max range should be configurable through part config file
        // allow the user to adjust the suspension spring/damper indirectly by specifying the load the wheel should be rated for.
        // should likey only be available in the editor after initial testing/development is done.
        //TODO -- auto-calc spring/damper from suspension length, target, input load, and desired damping ratio (1=critical damping)
        [KSPField(guiName = "LoadRating", guiActive = true, guiActiveEditor = true, isPersistant = true),
         UI_FloatRange(minValue = 0.05f, maxValue = 5, stepIncrement = 0.05f, suppressEditorShipModified = true)]
        public float loadRating = 0.05f;

        [KSPField(guiName ="FwdInput", guiActive =true)]
        public float fwdInput;

        [KSPField(guiName = "RotInput", guiActive = true)]
        public float rotInput;

        [KSPField(guiName = "BrakeInput", guiActive = true)]
        public float brakeInput;

        [KSPField(guiName = "Hit", guiActive = true)]
        public string colliderHit;

        [KSPField(guiName = "RPM", guiActive = true)]
        public float rpm;

        [KSPField(guiName = "Steer", guiActive = true)]
        public float steer;

        [KSPField(guiName = "fLong", guiActive = true)]
        public float fLong;

        [KSPField(guiName = "fLat", guiActive = true)]
        public float fLat;

        [KSPField(guiName = "comp", guiActive = true)]
        public float comp;

        [KSPField(guiName = "spr", guiActive = true)]
        public float spr;

        [KSPField(guiName = "dmp", guiActive = true)]
        public float dmp;

        [KSPField(guiName = "EC/s", guiActive = true)]
        public float guiResourceUse = 0f;

        private GameObject debugHitObject;

        #endregion

        #region REGION - GUI Handling methods

        public void onSpringUpdated(BaseField field, object obj)
        {
            if ((float)obj != springMult)
            {
                wheel.spring = suspensionSpring * springMult;
                spr = wheel.spring;
            }
        }

        public void onDamperUpdated(BaseField field, object obj)
        {
            if ((float)obj != dampMult)
            {
                wheel.damper = suspensionDamper * dampMult;
                dmp = wheel.damper;
            }
        }

        public void onLoadUpdated(BaseField field, object obj)
        {
            if ((float)obj != loadRating)
            {
                calcSuspension(loadRating, suspensionTravel, suspensionTarget, 1, out suspensionSpring, out suspensionDamper);
                wheel.spring = suspensionSpring * springMult;
                wheel.damper = suspensionDamper * dampMult;
                spr = wheel.spring;
                dmp = wheel.damper;
            }
        }
       
        #endregion

        #region REGION - Standard KSP/Unity Overrides

        public override string GetInfo()
        {
            String data = brakesLocked ? "Landing Leg" : "Wheel";
            data = data + "\nMin Load: " + minLoadRating;
            data = data + "\nMax Load: " + maxLoadRating;
            if (maxSteeringAngle > 0) { data = data + "\nMax Steer: " + maxSteeringAngle; }
            if (!brakesLocked && maxBrakeTorque > 0) { data = data + "\nMax Brake: " + maxBrakeTorque; }
            if (maxMotorTorque > 0) { data = data +"\nMax Torque: "+maxMotorTorque; }
            if (maxMotorTorque > 0 && maxRPM > 0) { data = data + "\nMax Motor RPM: " + maxRPM; }
            //TODO resource-use stats at max torque
            return data;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            //NOOP?
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.SetValue("persistentState", persistentState, true);
        }
        
        /// <summary>
        /// Initializes wheel parameters, removes stock wheel collider component, instantiates custom wheel collider component container, sets up animation handling (if needed)
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Utils.printHierarchy(part.gameObject);
            wheelState = (KSPWheelState)Enum.Parse(typeof(KSPWheelState), persistentState);
            locateTransforms();
            WheelCollider collider = wheelColliderTransform.GetComponent<WheelCollider>();
            if (collider != null)
            {
                GameObject.Destroy(collider);
            }
            if (loadRating > 0)
            {
                calcSuspension(loadRating, suspensionTravel, suspensionTarget, 1.0f, out suspensionSpring, out suspensionDamper);
            }
            Fields["springMult"].uiControlFlight.onFieldChanged = onSpringUpdated;
            Fields["dampMult"].uiControlFlight.onFieldChanged = onDamperUpdated;
            BaseField f = Fields["loadRating"];
            f.uiControlEditor.onFieldChanged = f.uiControlFlight.onFieldChanged = onLoadUpdated;
            UI_FloatRange rng = (UI_FloatRange)f.uiControlFlight;
            if (rng != null)
            {
                rng.minValue = minLoadRating;
                rng.maxValue = maxLoadRating;
                rng.stepIncrement = 0.1f;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                Collider[] colliders = part.GetComponentsInChildren<Collider>();
                int len = colliders.Length;
                for (int i = 0; i < len; i++)
                {
                    // remove stock 'collisionEnhancer' collider from wheels, if present;
                    // these things screw with wheel updates/raycasting, and cause improper collisions on wheels
                    if (colliders[i].gameObject.name.ToLower().StartsWith("collisionenhancer"))
                    {
                        GameObject.Destroy(colliders[i].gameObject);
                    }
                }
                debugHitObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Collider c = debugHitObject.GetComponent<Collider>();
                GameObject.Destroy(c);
                debugHitObject.transform.NestToParent(part.transform);
                debugHitObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            }
            part.collider = null;//clear the part collider that causes explosions.... collisions still happen, but things won't break
            
            //remove the bounds collider from the model
            //TODO this should possibly be removed during Start() or after vessel position has been set on reload
            if (!string.IsNullOrEmpty(boundsColliderName))
            {
                Transform tr = part.transform.FindRecursive(boundsColliderName);
                Collider c = tr.GetComponent<Collider>();
                GameObject.Destroy(c);
                MonoBehaviour.print("Destroying bounds collider: " + c);
            }
        }

        public void Start()
        {

        }

        /// <summary>
        /// Updates the wheel collider component physics if it is not broken or retracted
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) { return; }
            if (!FlightGlobals.ready || !FlightDriver.fetch) { return; }
            //workaround for part rigidbody not being present during start/load or initial fixedupdate ticks
            //TODO can set it up in a coroutine, continuing to yield until rigidbody is present?
            //      is that really a simpler solution? still have  to check for null each fixed-update tick until the wheel is present
            if (wheel == null)
            {
                Rigidbody rb = part.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    return;
                }
                else
                {
                    wheel = wheelColliderTransform.gameObject.AddComponent<KSPWheelCollider>();
                    wheel.rigidbody = rb;
                    wheel.radius = wheelRadius;
                    wheel.mass = wheelMass;
                    wheel.length = suspensionTravel;
                    wheel.target = 0f;// suspensionTarget;
                    wheel.spring = suspensionSpring;
                    wheel.damper = suspensionDamper;
                    //wheel.isGrounded = grounded;
                    wheel.setImpactCallback(onWheelImpact);
                    wheel.raycastMask = raycastMask;
                    if (brakesLocked) { wheel.brakeTorque = maxBrakeTorque; }
                }
            }
            wheel.radius = wheelRadius;
            wheel.length = suspensionTravel;
            spr = wheel.spring;
            dmp = wheel.damper;
            if (part.collisionEnhancer != null) { part.collisionEnhancer.OnTerrainPunchThrough = CollisionEnhancerBehaviour.DO_NOTHING; }            
            sampleInput();
            //update the wheels input state from current keyboard input
            //Update the wheel physics state as long as it is not broken or fully retracted
            //yes, this means updates happen during deploy and retract animations (as they should! -- wheels don't just work when they are deployed...).
            if (wheelState != KSPWheelState.BROKEN && wheelState != KSPWheelState.RETRACTED)
            {
                //wheel.gravityForce = FlightIntegrator.ActiveVesselFI.geeForce;
                wheel.gravityVector = vessel.gravityForPos;
                wheel.updateWheel();
                debugHitObject.transform.position = wheelColliderTransform.position - (wheelColliderTransform.up * suspensionTravel) + (wheelColliderTransform.up * wheel.compressionDistance) - (wheelColliderTransform.up * wheelRadius);
            }            
            fLong = wheel.longitudinalForce;
            fLat = wheel.lateralForce;
            rpm = wheel.rpm;
            steer = wheel.steeringAngle;
            grounded = wheel.isGrounded;
            comp = wheel.compressionDistance;
            colliderHit = grounded ? wheel.contactColliderHit.gameObject.name+" : "+wheel.contactColliderHit.gameObject.layer : "None";
            updateLandedState();
        }

        /// <summary>
        /// Updates the mesh animation status from the wheel collider components current state (steer angle, wheel rotation, suspension compression)
        /// </summary>
        public void Update()
        {
            if (!FlightGlobals.ready || !FlightDriver.fetch || wheel==null) { return; }
            //TODO block/reset input state when not deployed, re-orient wheels to default (zero steering rotation) when retracted/ing?
            if (!HighLogic.LoadedSceneIsFlight || wheelState==KSPWheelState.BROKEN || wheelState==KSPWheelState.RETRACTED) { return; }            
            if (suspensionMesh != null)
            {
                float offset = -suspensionTravel + wheel.compressionDistance + suspensionOffset;
                Vector3 pos = suspensionDefaultLocation;
                Vector3 scale = Vector3.one;// wheelColliderTransform.localScale;
                if (suspensionAxis.x != 0)
                {
                    pos.x = offset * suspensionAxis.x / scale.x ;
                }
                else if (suspensionAxis.y != 0)
                {
                    pos.y = offset * suspensionAxis.y / scale.y;
                }
                else if (suspensionAxis.z !=0)
                {
                    pos.z = offset * suspensionAxis.z / scale.z;
                }
                suspensionMesh.localPosition = pos;
            }
            if (steeringMesh != null)
            {
                steeringMesh.localRotation = Quaternion.Euler(steeringAxis * wheel.steeringAngle);
            }
            if (wheelPivotTransforms != null && wheelPivotTransforms.Length>0)
            {
                int len = wheelPivotTransforms.Length;
                for (int i = 0; i < len; i++)
                {
                    wheelPivotTransforms[i].Rotate(wheel.perFrameRotation, 0, 0, Space.Self);
                }
            }
        }

        #endregion

        #region REGION - Custom update methods

        //TODO also need to check the rest of the parts' colliders for contact/grounded state somehow
        private void updateLandedState()
        {
            bool grounded = wheel.isGrounded;
            part.GroundContact = grounded;
            vessel.checkLanded();
        }
        
        private float updateResourceDrain(float input)
        {
            float percent = 1f;
            if (input > 0)
            {
                float drain = maxMotorTorque * input * resourceAmount * TimeWarp.fixedDeltaTime;
                double d = part.RequestResource("ElectricCharge", drain);
                percent = (float)d / drain;
                guiResourceUse = (float)d / TimeWarp.fixedDeltaTime;
            }
            return percent;
        }

        /// <summary>
        /// Temporary very basic input handling code
        /// </summary>
        private void sampleInput()
        {
            float fI = part.vessel.ctrlState.wheelThrottle + part.vessel.ctrlState.wheelThrottleTrim;
            float rI = part.vessel.ctrlState.wheelSteer + part.vessel.ctrlState.wheelSteerTrim;
            float bI = brakesLocked ? 1 : part.vessel.ActionGroups[KSPActionGroup.Brakes] ? 1 : 0;
            if (motorLocked) { fI = 0; }
            if (steeringLocked) { rI = 0; }
            if (invertSteering) { rI = -rI; }
            if (invertMotor) { fI = -fI; }
            if (tankSteering)
            {
                fI = fI + rI;
                if (fI > 1) { fI = 1; }
                if (fI < -1) { fI = -1; }
            }

            if (throttleResponse > 0)
            {
                fI = Mathf.Lerp(fwdInput, fI, throttleResponse * Time.deltaTime);
            }
            if (steeringResponse > 0)
            {
                rI = Mathf.Lerp(rotInput, rI, steeringResponse * Time.deltaTime);
            }
            if (!brakesLocked && brakeResponse > 0)
            {
                bI = Mathf.Lerp(brakeInput, bI, brakeResponse * Time.deltaTime);
            }

            float rpm = wheel.rpm;
            if (fI > 0 && wheel.rpm > maxRPM) { fI = 0; }
            else if (fI < 0 && wheel.rpm < -maxRPM) { fI = 0; }

            fwdInput = fI * updateResourceDrain(Mathf.Abs(fI));
            rotInput = rI;
            brakeInput = bI;
            wheel.motorTorque = maxMotorTorque * fwdInput;
            wheel.steeringAngle = maxSteeringAngle * rotInput;
            wheel.brakeTorque = maxBrakeTorque * brakeInput;
        }

        /// <summary>
        /// Called from the KSPWheelCollider on first ground contact<para/>
        /// The input Vector3 is the wheel-local impact velocity.  Relative impact speed can be derived from localImpactVelocity.magnitude
        /// </summary>
        /// <param name="localImpactVelocity"></param>
        public void onWheelImpact(Vector3 localImpactVelocity)
        {
            //TODO
        }
        
        /// <summary>
        /// Input load in tons, suspension length, target (0-1), and desired damp ratio (1 = critical)
        /// and output spring and damper for that load and ratio
        /// </summary>
        private void calcSuspension(float load, float length, float target, float dampRatio, out float spring, out float damper)
        {

            float targetCompression = target * length;
            if (targetCompression <= 0) { targetCompression = 0.01f; }
            spring = load * 10 / targetCompression;
            //damper = 2 * Mathf.Sqrt(load * spring) * dampRatio;
            float k = spring;
            float o = Mathf.Sqrt(k / load);//natural frequency
            float cd = 2 * load * o;//critical damping coefficient
            //cd = 2 * Mathf.Sqrt(k * load);
            damper = cd * dampRatio;
        }

        /// <summary>
        /// Locate the wheel-pivot transforms from the list of wheel-pivot names (may be singular or CSV list), will find multiple same-named transforms
        /// ALL of them must rotate on the same axis (x-axis by default, currently not configurable)
        /// </summary>
        private void locateTransforms()
        {
            wheelColliderTransform = part.transform.FindRecursive(wheelColliderName);
            String[] pivotNames = wheelPivotName.Split(',');
            List<Transform> transforms = new List<Transform>();
            int len = pivotNames.Length;
            for (int i = 0; i < len; i++)
            {
                part.transform.FindRecursiveMulti(pivotNames[i].Trim(), transforms);
            }
            wheelPivotTransforms = transforms.ToArray();
            if (!String.IsNullOrEmpty(wheelName)) { wheelMesh = part.transform.FindRecursive(wheelName); }
            if (!String.IsNullOrEmpty(suspensionName))
            {
                suspensionMesh = part.transform.FindRecursive(suspensionName);
                if (suspensionMesh != null) { suspensionDefaultLocation = suspensionMesh.transform.localPosition; }
            }
            if (!String.IsNullOrEmpty(steeringName)) { steeringMesh = part.transform.FindRecursive(steeringName); }
            MonoBehaviour.print("WheelMesh: " + wheelMesh);
            MonoBehaviour.print("SuspensionMesh: " + suspensionMesh);
            MonoBehaviour.print("SteeringMesh: " + steeringMesh);
            foreach (Transform tr in wheelPivotTransforms) { MonoBehaviour.print("PivotTransform: " + tr); }
        }

        #endregion

    }

}

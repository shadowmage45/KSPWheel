using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPWheel
{
    /// <summary>
    /// A replacement for the stock wheel system that uses the KSPWheelCollider class for phsyics handling.
    /// Intended to be a fully-functional (but possibly not fully-equivalent) replacement for the stock wheel modules and U5 WheelCollider component
    /// </summary>
    public class KSPWheelModule : PartModule
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
        /// Name of the mesh that represents the 'busted wheel'<para/>
        /// May be null if no busted wheel mesh exists or should be manipulated (if not present, and wheel is breakable, the normal mesh will be used if it is present)
        /// </summary>
        [KSPField]
        public string bustedWheelName;

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

        //bogey functions are used for aligning the 'foot' to the ground
        [KSPField]
        public string bogeyName;

        [KSPField]
        public Vector3 bogeyRotAxis=Vector3.right;

        [KSPField]
        public Vector3 bogeyUpAxis = Vector3.up;

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

        /// <summary>
        /// Determines how far above the initial position in the model that the wheel-collider should be located.
        /// This is needed as the setup for stock models varies widely for wheel-collider positioning;
        /// some have it near the top of suspension travel, others at the bottom.
        /// Needs to be set on a per-part/model basis.
        /// </summary>
        [KSPField]
        public float wheelColliderOffset;
        [KSPField]
        public float maxSteeringAngle;
        [KSPField]
        public float suspensionOffset = 0f;
        [KSPField]
        public float suspensionExtPos = 0f;
        [KSPField]
        public Vector3 wheelColliderRotation = Vector3.zero;
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

        //public float 

        #endregion

        #region REGION - Optional wheel parameters that may be loaded from the WheelCollider if present
        [KSPField]
        public float wheelRadius = -1;
        [KSPField]
        public float wheelMass = -1;
        [KSPField]
        public float suspensionTravel = -1;
        [KSPField]
        public float suspensionTarget = -1;
        [KSPField]
        public float suspensionSpring = -1;
        [KSPField]
        public float suspensionDamper = -1;
        [KSPField]
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
        private Transform bustedWheelMesh;
        private Transform suspensionMesh;
        private Transform steeringMesh;
        private Transform bogeyMesh;

        private KSPWheelCollider wheel;
        private KSPWheelState wheelState = KSPWheelState.DEPLOYED;
        private WheelAnimationHandler animationControl;
        private ModuleLight lightModule;
        private ModuleStatusLight statusLightModule;
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

        #endregion

        #region REGION - GUI Handling methods

        public void onSpringUpdated(BaseField field, object obj)
        {
            wheel.spring = suspensionSpring * springMult;
            MonoBehaviour.print("Set spring to: " + wheel.spring);
        }

        public void onDamperUpdated(BaseField field, object obj)
        {
            wheel.damper = suspensionDamper * dampMult;
            MonoBehaviour.print("Set damper to: " + wheel.damper);
        }

        public void onLoadUpdated(BaseField field, object obj)
        {
            calcSuspension(loadRating, suspensionTravel, suspensionTarget, 1, out suspensionSpring, out suspensionDamper);
            wheel.spring = suspensionSpring * springMult;
            wheel.damper = suspensionDamper * dampMult;
        }
        
        [KSPAction("Toggle Gear", KSPActionGroup.Gear)]
        public void toggleGearAction(KSPActionParam param)
        {
            if (param.type == KSPActionType.Activate) { deploy(); }
            else if (param.type == KSPActionType.Deactivate) { retract(); }
        }
        
        [KSPEvent(guiName = "Toggle Gear", guiActive = true, guiActiveEditor = true)]
        public void toggleGearEvent()
        {
            toggleDeploy();
        }

        //TODO -- enable/disable for broken status
        [KSPEvent(guiName = "Repair Gear", guiActive = false, guiActiveEditor = false)]
        public void repairWheel()
        {

        }
        
        private void deploy()
        {
            if (wheelState == KSPWheelState.RETRACTED || wheelState == KSPWheelState.RETRACTING) { toggleDeploy(); }
        }
        
        private void retract()
        {
            if (wheelState == KSPWheelState.DEPLOYED || wheelState == KSPWheelState.DEPLOYING) { toggleDeploy(); }
        }

        private void toggleDeploy()
        {
            if (animationControl == null)
            {
                MonoBehaviour.print("Animation control is null!");
                return;
            }
            if (wheelState == KSPWheelState.DEPLOYED || wheelState == KSPWheelState.DEPLOYING)
            {
                wheelState = KSPWheelState.RETRACTING;
                animationControl.setToAnimationState(wheelState, false);
            }
            else if (wheelState == KSPWheelState.RETRACTED || wheelState == KSPWheelState.RETRACTING)
            {
                wheelState = KSPWheelState.DEPLOYING;
                animationControl.setToAnimationState(wheelState, false);
            }
        }

        #endregion

        #region REGION - Standard KSP/Unity Overrides

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
            wheelState = (KSPWheelState)Enum.Parse(typeof(KSPWheelState), persistentState);
            wheelColliderTransform = part.transform.FindRecursive(wheelColliderName);
            locateWheelPivotTransforms();
            if (!String.IsNullOrEmpty(wheelName)){ wheelMesh = part.transform.FindRecursive(wheelName); }
            if (!String.IsNullOrEmpty(bustedWheelName)) { bustedWheelMesh = part.transform.FindRecursive(bustedWheelName); }
            if (!String.IsNullOrEmpty(suspensionName)) { suspensionMesh = part.transform.FindRecursive(suspensionName); }
            if (!String.IsNullOrEmpty(steeringName)) { steeringMesh = part.transform.FindRecursive(steeringName); }
            if (!String.IsNullOrEmpty(animationName)) { animationControl = new WheelAnimationHandler(this, animationName, animationSpeed, animationLayer, wheelState); }
            if (!String.IsNullOrEmpty(bogeyName)) { bogeyMesh = part.transform.FindRecursive(bogeyName); }
            WheelCollider collider = wheelColliderTransform.GetComponent<WheelCollider>();
            if (collider != null)
            {
                wheelRadius = wheelRadius == -1 ? collider.radius : wheelRadius;
                wheelMass = wheelMass == -1 ? collider.mass : wheelMass;
                suspensionTravel = suspensionTravel == -1? collider.suspensionDistance : suspensionTravel;
                suspensionTarget = suspensionTarget == -1? collider.suspensionSpring.targetPosition : suspensionTarget;
                suspensionSpring = suspensionSpring == -1 ? collider.suspensionSpring.spring : suspensionSpring ;
                suspensionDamper = suspensionDamper == -1 ? collider.suspensionSpring.damper : suspensionDamper;
                maxBrakeTorque = maxBrakeTorque == -1 ? collider.brakeTorque : maxBrakeTorque;
                maxMotorTorque = maxMotorTorque == -1 ? collider.motorTorque : maxMotorTorque;
            }
            Component.Destroy(collider);//remove that stock crap, replace it with some new hotness below in the Start() method
            if (animationControl != null) { animationControl.setToAnimationState(wheelState, false); }
            Events["toggleGearEvent"].active = animationControl != null;
            Actions["toggleGearAction"].active = animationControl != null;
            Events["repairWheel"].active = wheelState == KSPWheelState.BROKEN;
            Fields["springMult"].uiControlFlight.onFieldChanged = onSpringUpdated;
            Fields["dampMult"].uiControlFlight.onFieldChanged = onDamperUpdated;
            BaseField f = Fields["loadRating"];
            f.uiControlEditor.onFieldChanged = f.uiControlFlight.onFieldChanged = onLoadUpdated;
            //TODO -- there has got to be an easier way to handle these; perhaps check if the collider is part of the 
            // model hierarchy for the part/vessel?
            if (HighLogic.LoadedSceneIsFlight)
            {
                Collider[] colliders = part.GetComponentsInChildren<Collider>();
                int len = colliders.Length;
                for (int i = 0; i < len; i++)
                {
                    //set all colliders in the part to wheel-collider-ignore layer; no stock models that I've investigated have colliders on the same object as meshes, they all use separate colliders
                    colliders[i].gameObject.layer = 26;//wheelcollidersignore
                    //remove stock 'collisionEnhancer' collider from wheels, if present; these things screw with wheel updates/raycasting, and cause improper collisions on wheels
                    if (colliders[i].gameObject.name.ToLower() == "collisionenhancer")
                    {
                        GameObject.Destroy(colliders[i].gameObject);
                    }
                }
            }
            part.collider = null;//clear the part collider that causes explosions.... collisions still happen, but things won't break
            
            wheelColliderTransform.Rotate(wheelColliderRotation, Space.Self);
            wheelColliderTransform.localPosition += Vector3.up * wheelColliderOffset;
            
            if (wheelState == KSPWheelState.BROKEN)
            {
                if (wheelMesh != null) { wheelMesh.gameObject.SetActive(false); }
                if (bustedWheelMesh != null) { bustedWheelMesh.gameObject.SetActive(true); }
            }
            else
            {
                if (wheelMesh != null) { wheelMesh.gameObject.SetActive(true); }
                if (bustedWheelMesh != null) { bustedWheelMesh.gameObject.SetActive(false); }
            }
        }

        public void Start()
        {
            lightModule = part.GetComponent<ModuleLight>();
            if (lightModule != null && wheelState == KSPWheelState.DEPLOYED)
            {
                lightModule.LightsOn();
            }
            statusLightModule = part.GetComponent<ModuleStatusLight>();
        }

        /// <summary>
        /// Updates the wheel collider component physics if it is not broken or retracted
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) { return; }
            if (!FlightGlobals.ready || !FlightDriver.fetch) { return; }
            if (wheel == null)
            {
                Rigidbody rb = part.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    return;
                }
                else
                {
                    //delaying until Start as the part.rigidbody is not initialized until ?? (need to find out when...)
                    wheel = new KSPWheelCollider(wheelColliderTransform.gameObject, part.gameObject.GetComponent<Rigidbody>());
                    wheel.radius = wheelRadius;
                    wheel.mass = wheelMass;
                    wheel.length = suspensionTravel;
                    wheel.target = suspensionTarget;
                    wheel.spring = suspensionSpring;
                    wheel.damper = suspensionDamper;
                    //wheel.isGrounded = grounded;
                    wheel.setImpactCallback(onWheelImpact);
                    if (brakesLocked) { wheel.brakeTorque = maxBrakeTorque; }
                }
            }
            if (part.collisionEnhancer != null) { part.collisionEnhancer.OnTerrainPunchThrough = CollisionEnhancerBehaviour.DO_NOTHING; }            
            sampleInput();
            //update the wheels input state from current keyboard input
            //Update the wheel physics state as long as it is not broken or fully retracted
            //yes, this means updates happen during deploy and retract animations (as they should! -- wheels don't just work when they are deployed...).
            if (wheelState != KSPWheelState.BROKEN && wheelState != KSPWheelState.RETRACTED)
            {
                //wheel.gravityForce = FlightIntegrator.ActiveVesselFI.geeForce;
                wheel.updateWheel();
            }
            fLong = wheel.longitudinalForce;
            fLat = wheel.lateralForce;
            rpm = wheel.rpm;
            steer = wheel.steeringAngle;
            grounded = wheel.isGrounded;
            comp = wheel.compressionDistance;
            colliderHit = grounded ? wheel.contactData.colliderHit.name : "None";
            if (maxMotorTorque > 0 && fwdInput != 0)
            {
                updateResourceDrain();
            }
            updateLandedState();
        }

        /// <summary>
        /// Updates the mesh animation status from the wheel collider components current state (steer angle, wheel rotation, suspension compression)
        /// </summary>
        public void Update()
        {
            if (animationControl != null) { animationControl.updateAnimationState(); }
            if (!FlightGlobals.ready || !FlightDriver.fetch || wheel==null) { return; }
            //TODO block/reset input state when not deployed, re-orient wheels to default (zero steering rotation) when retracted/ing?
            if (!HighLogic.LoadedSceneIsFlight || wheelState==KSPWheelState.BROKEN || wheelState==KSPWheelState.RETRACTED) { return; }            
            if (suspensionMesh != null)
            {
                float offset = wheel.compressionDistance + suspensionOffset;
                Vector3 pos = suspensionMesh.localPosition;
                if (suspensionAxis.x != 0)
                {
                    pos.x = suspensionExtPos + offset * suspensionAxis.x;
                }
                else if (suspensionAxis.y != 0)
                {
                    pos.y = suspensionExtPos + offset * suspensionAxis.y;
                }
                else if (suspensionAxis.z !=0)
                {
                    pos.z = suspensionExtPos + offset * suspensionAxis.z;
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
            if (statusLightModule != null)
            {
                statusLightModule.SetStatus(brakeInput != 0);
            }
            if (bogeyMesh != null)
            {
                if (wheel.isGrounded)//orient foot to ground
                {
                    Vector3 normal = wheel.contactData.hitNormal;
                    float dot = Vector3.Dot(bogeyUpAxis, normal);
                    //Quaternion.ro
                }
                else
                {
                    //default orientation? which would be?
                }                
            }
        }

        #endregion

        #region REGION - Custom update methods

        //TODO...
        private void updateLandedState()
        {
            bool grounded = wheel.isGrounded;
            part.GroundContact = grounded;
            vessel.checkLanded();
        }

        //TODO -- handle resource drain for motor-enabled parts
        private void updateResourceDrain()
        {
            float drain = maxMotorTorque * fwdInput;
        }

        /// <summary>
        /// Temporary very basic input handling code
        /// </summary>
        private void sampleInput()
        {
            fwdInput = part.vessel.ctrlState.wheelThrottle + part.vessel.ctrlState.wheelThrottleTrim;
            rotInput = part.vessel.ctrlState.wheelSteer + part.vessel.ctrlState.wheelSteerTrim;
            brakeInput = brakesLocked ? 1 : part.vessel.ActionGroups[KSPActionGroup.Brakes] ? 1 : 0;
            if (motorLocked) { fwdInput = 0; }
            if (steeringLocked) { rotInput = 0; }
            if (invertSteering) { rotInput = -rotInput; }
            if (invertMotor) { fwdInput = -fwdInput; }
            if (tankSteering)
            {
                fwdInput = fwdInput + rotInput;
                if (fwdInput > 1) { fwdInput = 1; }
                if (fwdInput < -1) { fwdInput = -1; }
            }
            wheel.motorTorque = maxMotorTorque * fwdInput;
            wheel.steeringAngle = maxSteeringAngle * rotInput;
            wheel.brakeTorque = maxBrakeTorque * brakeInput;
        }

        /// <summary>
        /// Callback from animationControl for when an animation transitions from one state to another
        /// </summary>
        /// <param name="state"></param>
        public void onAnimationStateChanged(KSPWheelState state)
        {
            MonoBehaviour.print("Recv callback from anim state change, new state: " + state);
            wheelState = state;
            if (state == KSPWheelState.RETRACTED)
            {
                //TODO reset suspension and steering transforms to neutral?
                if (lightModule != null) { lightModule.LightsOff(); }
            }
            else if (state == KSPWheelState.DEPLOYED)
            {
                if (lightModule != null) { lightModule.LightsOn(); }
            }
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
        /// WIP - may or may not be correct...
        /// </summary>
        /// <param name="load"></param>
        private void calcSuspension(float load, float length, float target, float dampRatio, out float spring, out float damper)
        {
            float targetDistance = length - (target * length);
            spring = (load * 10) / targetDistance;            
            damper = 2 * Mathf.Sqrt(load * spring) * dampRatio;
        }

        /// <summary>
        /// Locate the wheel-pivot transforms from the list of wheel-pivot names (may be singular or CSV list), will find multiple same-named transforms
        /// ALL of them must rotate on the same axis (x-axis by default, currently not configurable)
        /// </summary>
        private void locateWheelPivotTransforms()
        {
            String[] pivotNames = wheelPivotName.Split(',');
            List<Transform> transforms = new List<Transform>();
            int len = pivotNames.Length;
            for (int i = 0; i < len; i++)
            {
                part.transform.FindRecursiveMulti(pivotNames[i].Trim(), transforms);
            }
            wheelPivotTransforms = transforms.ToArray();
        }

        //debug code...
        //public void OnCollisionEnter(Collision c)
        //{
        //    MonoBehaviour.print("OCE: " + c.collider);
        //    int len = c.contacts.Length;
        //    for (int i = 0; i < len; i++)
        //    {
        //        MonoBehaviour.print("C: " + c.contacts[i].thisCollider + " :: " + c.contacts[i].otherCollider);
        //    }
        //}

        #endregion

    }

}
